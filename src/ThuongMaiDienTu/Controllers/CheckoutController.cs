using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Models;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Authorize(Roles = "CUSTOMER")]
public class CheckoutController : Controller
{
    private readonly ThuongMaiDienTuDbContext _context;
    private readonly IShippingQuoteService _shippingQuotes;

    public CheckoutController(
        ThuongMaiDienTuDbContext context,
        IShippingQuoteService shippingQuotes)
    {
        _context = context;
        _shippingQuotes = shippingQuotes;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        var cart = await LoadCartAsync(userId, asNoTracking: true);
        if (cart is null || cart.CartItems.Count == 0)
        {
            TempData["ErrorMessage"] = "Giỏ hàng đang trống. Vui lòng chọn sản phẩm trước khi đặt hàng.";
            return RedirectToAction("Index", "Cart");
        }

        var user = await _context.Users
            .AsNoTracking()
            .SingleAsync(item => item.UserId == userId);

        var model = await BuildCheckoutModelAsync(cart, new CheckoutViewModel
        {
            ReceiverName = user.FullName,
            ReceiverPhone = user.Phone ?? string.Empty,
            PaymentMethod = OrderWorkflowHelper.Cod
        });

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutInputViewModel input)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        input.ReceiverName = input.ReceiverName?.Trim() ?? string.Empty;
        input.ReceiverPhone = input.ReceiverPhone?.Trim() ?? string.Empty;
        input.ShippingAddress = input.ShippingAddress?.Trim() ?? string.Empty;
        input.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();

        if (!OrderWorkflowHelper.PaymentMethods.Contains(input.PaymentMethod))
        {
            ModelState.AddModelError(
                nameof(input.PaymentMethod),
                "Phương thức thanh toán không hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            return await ReturnCheckoutWithCurrentDataAsync(userId, input);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var cart = await LoadCartAsync(userId, asNoTracking: false);
            if (cart is null || cart.CartItems.Count == 0)
            {
                throw new CheckoutValidationException(
                    "Giỏ hàng đang trống hoặc đã được xử lý.");
            }

            var prepared = await PrepareItemsAsync(cart);
            if (prepared.Errors.Count > 0)
            {
                throw new CheckoutValidationException(
                    string.Join(" ", prepared.Errors));
            }

            var now = DateTime.Now;
            const decimal discountAmount = 0m;
            var subtotal = prepared.Items.Sum(item => item.UnitPrice * item.CartItem.Quantity);
            var storeGroups = prepared.Items
                .GroupBy(item => item.CartItem.Sku.Product.StoreId)
                .ToList();
            var quotes = new Dictionary<long, ShippingSelection>();
            foreach (var group in storeGroups)
            {
                var quote = await _shippingQuotes.GetBestInternalQuoteAsync(
                    group.Key, input.ShippingAddress);
                if (quote is null)
                {
                    throw new CheckoutValidationException(
                        "Hiện chưa có dịch vụ vận chuyển phù hợp cho một cửa hàng trong giỏ.");
                }

                quotes[group.Key] = quote;
            }

            var shippingFee = quotes.Values.Sum(quote => quote.Fee);

            var order = new Order
            {
                OrderCode = await GenerateOrderCodeAsync(),
                UserId = userId,
                ReceiverName = input.ReceiverName,
                ReceiverPhone = input.ReceiverPhone,
                ShippingAddress = input.ShippingAddress,
                Note = input.Note,
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                ShippingFee = shippingFee,
                TotalAmount = subtotal - discountAmount + shippingFee,
                PaymentMethod = input.PaymentMethod,
                PaymentStatus = OrderWorkflowHelper.Unpaid,
                OrderStatus = OrderWorkflowHelper.Pending,
                CreatedAt = now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var group in storeGroups)
            {
                var quote = quotes[group.Key];
                var store = group.First().CartItem.Sku.Product.Store;
                var storeSubtotal = group.Sum(item =>
                    item.UnitPrice * item.CartItem.Quantity);
                var storeOrder = new StoreOrder
                {
                    OrderId = order.OrderId,
                    StoreId = group.Key,
                    StoreOrderCode = $"{order.OrderCode}-{group.Key}",
                    Subtotal = storeSubtotal,
                    DiscountAmount = 0,
                    ShippingFee = quote.Fee,
                    TotalAmount = storeSubtotal + quote.Fee,
                    Status = OrderWorkflowHelper.Pending,
                    CreatedAt = now
                };
                _context.StoreOrders.Add(storeOrder);
                await _context.SaveChangesAsync();

                foreach (var item in group)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        StoreOrderId = storeOrder.StoreOrderId,
                        StoreNameSnapshot = store.StoreName,
                        SkuId = item.CartItem.SkuId,
                        ProductName = item.CartItem.Sku.Product.ProductName,
                        SkuCode = item.CartItem.Sku.SkuCode,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.CartItem.Quantity
                    });

                    item.CartItem.Sku.StockQuantity -= item.CartItem.Quantity;
                }

                _context.Shipments.Add(new Shipment
                {
                    StoreOrderId = storeOrder.StoreOrderId,
                    ProviderId = quote.ProviderId,
                    ServiceId = quote.ServiceId,
                    ShippingFee = quote.Fee,
                    TrackingCode = $"VC-{Guid.NewGuid():N}".ToUpperInvariant(),
                    Status = "CREATED",
                    PickupAddress = store.StoreName,
                    DeliveryAddress = input.ShippingAddress,
                    EstimatedDeliveryAt = now.AddDays(quote.EstimatedMaxDays),
                    CreatedAt = now
                });
            }

            _context.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Đặt hàng thành công.";
            return RedirectToAction(nameof(Success), new { id = order.OrderId });
        }
        catch (CheckoutValidationException exception)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, exception.Message);
            return await ReturnCheckoutWithCurrentDataAsync(userId, input);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(
                string.Empty,
                "Không thể tạo đơn hàng lúc này. Dữ liệu giỏ hoặc tồn kho có thể vừa thay đổi.");
            return await ReturnCheckoutWithCurrentDataAsync(userId, input);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(
                string.Empty,
                "Đã xảy ra lỗi khi đặt hàng. Không có thay đổi nào được ghi nhận.");
            return await ReturnCheckoutWithCurrentDataAsync(userId, input);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Success(long id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        var model = await _context.Orders
            .AsNoTracking()
            .Where(item => item.OrderId == id && item.UserId == userId)
            .Select(item => new OrderListItemViewModel
            {
                OrderId = item.OrderId,
                OrderCode = item.OrderCode,
                CreatedAt = item.CreatedAt,
                Status = item.OrderStatus,
                PaymentMethod = item.PaymentMethod,
                PaymentStatus = item.PaymentStatus,
                TotalAmount = item.TotalAmount,
                TotalQuantity = item.OrderItems.Sum(orderItem => orderItem.Quantity)
            })
            .SingleOrDefaultAsync();

        return model is null ? NotFound() : View(model);
    }

    private async Task<IActionResult> ReturnCheckoutWithCurrentDataAsync(
        long userId,
        CheckoutInputViewModel input)
    {
        var cart = await LoadCartAsync(userId, asNoTracking: true);
        if (cart is null || cart.CartItems.Count == 0)
        {
            TempData["ErrorMessage"] = "Giỏ hàng đang trống.";
            return RedirectToAction("Index", "Cart");
        }

        var model = new CheckoutViewModel
        {
            ReceiverName = input.ReceiverName,
            ReceiverPhone = input.ReceiverPhone,
            ShippingAddress = input.ShippingAddress,
            Note = input.Note,
            PaymentMethod = input.PaymentMethod
        };

        return View("Index", await BuildCheckoutModelAsync(cart, model));
    }

    private async Task<CheckoutViewModel> BuildCheckoutModelAsync(
        Cart cart,
        CheckoutViewModel input)
    {
        var prepared = await PrepareItemsAsync(cart);

        input.Items = prepared.Items
            .Select(item => new CheckoutItemViewModel
            {
                SkuId = item.CartItem.SkuId,
                ProductName = item.CartItem.Sku.Product.ProductName,
                SkuCode = item.CartItem.Sku.SkuCode,
                Quantity = item.CartItem.Quantity,
                UnitPrice = item.UnitPrice
            })
            .ToList();
        input.Subtotal = input.Items.Sum(item => item.LineTotal);
        var storeIds = prepared.Items
            .Select(item => item.CartItem.Sku.Product.StoreId)
            .Distinct()
            .ToList();
        decimal shippingFee = 0;
        foreach (var storeId in storeIds)
        {
            var quote = await _shippingQuotes.GetBestInternalQuoteAsync(
                storeId, input.ShippingAddress ?? "Chưa nhập địa chỉ");
            shippingFee += quote?.Fee ?? 0;
        }
        input.ShippingFee = shippingFee;
        input.TotalAmount = input.Subtotal + input.ShippingFee;
        input.AvailabilityErrors = prepared.Errors;
        input.CanPlaceOrder = prepared.Errors.Count == 0 && input.Items.Count > 0;
        return input;
    }

    private async Task<PreparedCheckout> PrepareItemsAsync(Cart cart)
    {
        var now = DateTime.Now;
        var items = new List<PreparedCheckoutItem>();
        var errors = new List<string>();

        foreach (var cartItem in cart.CartItems)
        {
            var sku = cartItem.Sku;
            var product = sku.Product;
            var active =
                sku.Status == "ACTIVE" &&
                product.Status == "ACTIVE" &&
                product.Category.Status == "ACTIVE" &&
                product.Store.Status == "ACTIVE";

            if (!active)
            {
                errors.Add($"{product.ProductName}: sản phẩm hiện không còn được bán.");
                continue;
            }

            if (cartItem.Quantity <= 0)
            {
                errors.Add($"{product.ProductName}: số lượng không hợp lệ.");
                continue;
            }

            if (sku.StockQuantity < cartItem.Quantity)
            {
                errors.Add(
                    $"{product.ProductName}: chỉ còn {sku.StockQuantity} sản phẩm trong kho.");
                continue;
            }

            var price = ProductPricingHelper.Calculate(
                sku.Price,
                sku.SalePrice,
                sku.SaleStart,
                sku.SaleEnd,
                now).CurrentPrice;

            items.Add(new PreparedCheckoutItem(cartItem, price));
        }

        return new PreparedCheckout(items, errors);
    }

    private async Task<Cart?> LoadCartAsync(long userId, bool asNoTracking)
    {
        IQueryable<Cart> query = _context.Carts;
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query
            .Include(item => item.CartItems)
                .ThenInclude(item => item.Sku)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(item => item.Category)
            .Include(item => item.CartItems)
                .ThenInclude(item => item.Sku)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(item => item.Store)
            .SingleOrDefaultAsync(item => item.UserId == userId);
    }

    private async Task<string> GenerateOrderCodeAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var code = $"DH-{DateTime.Now:yyyyMMddHHmmssfff}-{suffix}";
            if (!await _context.Orders.AnyAsync(item => item.OrderCode == code))
            {
                return code;
            }
        }

        throw new CheckoutValidationException(
            "Không thể sinh mã đơn hàng. Vui lòng thử lại.");
    }

    private bool TryGetCurrentUserId(out long userId)
    {
        return long.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }

    private sealed record PreparedCheckout(
        IReadOnlyList<PreparedCheckoutItem> Items,
        IReadOnlyList<string> Errors);

    private sealed record PreparedCheckoutItem(CartItem CartItem, decimal UnitPrice);

    private sealed class CheckoutValidationException(string message) : Exception(message);
}
