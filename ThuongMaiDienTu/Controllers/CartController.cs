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
public class CartController : Controller
{
    private readonly ThuongMaiDienTuDbContext _context;

    public CartController(ThuongMaiDienTuDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        var cart = await _context.Carts
            .AsNoTracking()
            .Include(item => item.CartItems)
                .ThenInclude(item => item.Sku)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(item => item.Category)
            .Include(item => item.CartItems)
                .ThenInclude(item => item.Sku)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(item => item.Store)
            .Include(item => item.CartItems)
                .ThenInclude(item => item.Sku)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(item => item.ProductImage)
            .SingleOrDefaultAsync(item => item.UserId == userId);

        if (cart is null)
        {
            return View(new CartViewModel());
        }

        var productIds = cart.CartItems
            .Select(item => item.Sku.ProductId)
            .Distinct()
            .ToList();

        var managedSkuIds = productIds.Count == 0
            ? new Dictionary<long, long>()
            : await _context.ProductSkus
                .AsNoTracking()
                .Where(sku => productIds.Contains(sku.ProductId))
                .GroupBy(sku => sku.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    SkuId = group.Min(sku => sku.SkuId)
                })
                .ToDictionaryAsync(item => item.ProductId, item => item.SkuId);

        var now = DateTime.Now;
        var items = cart.CartItems
            .OrderByDescending(item => item.AddedAt)
            .Select(item => MapCartItem(
                item,
                managedSkuIds.TryGetValue(item.Sku.ProductId, out var managedSkuId) &&
                managedSkuId == item.SkuId,
                now))
            .ToList();

        return View(new CartViewModel { Items = items });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddToCartViewModel model)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = FirstModelError("Số lượng hoặc sản phẩm không hợp lệ.");
            return RedirectToAction(nameof(Index));
        }

        var sku = await LoadSkuForValidationAsync(model.SkuId);
        var restriction = await GetPurchaseRestrictionAsync(sku, model.Quantity);

        if (restriction is not null)
        {
            TempData["ErrorMessage"] = restriction;
            return RedirectToAction(nameof(Index));
        }

        var cart = await _context.Carts
            .Include(item => item.CartItems)
            .SingleOrDefaultAsync(item => item.UserId == userId);

        if (cart is null)
        {
            cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.Now
            };
            _context.Carts.Add(cart);
        }

        var existingItem = cart.CartItems
            .SingleOrDefault(item => item.SkuId == model.SkuId);

        if (existingItem is not null)
        {
            var newQuantity = (long)existingItem.Quantity + model.Quantity;
            if (newQuantity > sku!.StockQuantity)
            {
                TempData["ErrorMessage"] =
                    $"Tổng số lượng trong giỏ không được vượt quá tồn kho ({sku.StockQuantity}).";
                return RedirectToAction(nameof(Index));
            }

            existingItem.Quantity = (int)newQuantity;
        }
        else
        {
            cart.CartItems.Add(new CartItem
            {
                SkuId = model.SkuId,
                Quantity = model.Quantity,
                AddedAt = DateTime.Now
            });
        }

        cart.UpdatedAt = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Không thể cập nhật giỏ hàng. Vui lòng thử lại.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateCartItemViewModel model)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = FirstModelError("Số lượng không hợp lệ.");
            return RedirectToAction(nameof(Index));
        }

        var cartItem = await _context.CartItems
            .Include(item => item.Cart)
            .Include(item => item.Sku)
                .ThenInclude(item => item.Product)
                    .ThenInclude(item => item.Category)
            .Include(item => item.Sku)
                .ThenInclude(item => item.Product)
                    .ThenInclude(item => item.Store)
            .SingleOrDefaultAsync(item =>
                item.CartItemId == model.CartItemId &&
                item.Cart.UserId == userId);

        if (cartItem is null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm trong giỏ hàng của bạn.";
            return RedirectToAction(nameof(Index));
        }

        var restriction = await GetPurchaseRestrictionAsync(cartItem.Sku, model.Quantity);
        if (restriction is not null)
        {
            TempData["ErrorMessage"] = restriction;
            return RedirectToAction(nameof(Index));
        }

        cartItem.Quantity = model.Quantity;
        cartItem.Cart.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật số lượng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(long cartItemId)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        var cartItem = await _context.CartItems
            .Include(item => item.Cart)
            .SingleOrDefaultAsync(item =>
                item.CartItemId == cartItemId &&
                item.Cart.UserId == userId);

        if (cartItem is null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm trong giỏ hàng của bạn.";
            return RedirectToAction(nameof(Index));
        }

        cartItem.Cart.UpdatedAt = DateTime.Now;
        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProductSku?> LoadSkuForValidationAsync(long skuId)
    {
        return await _context.ProductSkus
            .Include(item => item.Product)
                .ThenInclude(item => item.Category)
            .Include(item => item.Product)
                .ThenInclude(item => item.Store)
            .SingleOrDefaultAsync(item => item.SkuId == skuId);
    }

    private async Task<string?> GetPurchaseRestrictionAsync(
        ProductSku? sku,
        int requestedQuantity)
    {
        if (sku is null)
        {
            return "Sản phẩm không tồn tại.";
        }

        var managedSkuId = await _context.ProductSkus
            .Where(item => item.ProductId == sku.ProductId)
            .MinAsync(item => item.SkuId);

        if (sku.SkuId != managedSkuId ||
            sku.Status != "ACTIVE" ||
            sku.Product.Status != "ACTIVE" ||
            sku.Product.Category.Status != "ACTIVE" ||
            sku.Product.Store.Status != "ACTIVE")
        {
            return "Sản phẩm hiện không còn được bán.";
        }

        if (sku.StockQuantity <= 0)
        {
            return "Sản phẩm hiện đã hết hàng.";
        }

        if (requestedQuantity > sku.StockQuantity)
        {
            return $"Số lượng không được vượt quá tồn kho ({sku.StockQuantity}).";
        }

        return null;
    }

    private static CartItemViewModel MapCartItem(
        CartItem item,
        bool isManagedSku,
        DateTime now)
    {
        var sku = item.Sku;
        var product = sku.Product;
        var priceInfo = ProductPricingHelper.Calculate(
            sku.Price,
            sku.SalePrice,
            sku.SaleStart,
            sku.SaleEnd,
            now);

        var isMarketActive =
            isManagedSku &&
            sku.Status == "ACTIVE" &&
            product.Status == "ACTIVE" &&
            product.Category.Status == "ACTIVE" &&
            product.Store.Status == "ACTIVE";

        var isAvailable =
            isMarketActive &&
            sku.StockQuantity > 0 &&
            item.Quantity <= sku.StockQuantity;

        var availabilityMessage = isMarketActive switch
        {
            false => "Sản phẩm hiện không còn được bán.",
            true when sku.StockQuantity <= 0 => "Sản phẩm đã hết hàng.",
            true when item.Quantity > sku.StockQuantity =>
                $"Số lượng trong giỏ vượt tồn kho hiện tại ({sku.StockQuantity}).",
            _ => "Sản phẩm sẵn sàng mua."
        };

        return new CartItemViewModel
        {
            CartItemId = item.CartItemId,
            SkuId = sku.SkuId,
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            SkuCode = sku.SkuCode,
            ImageUrl = product.ProductImage?.ImageUrl,
            AltText = product.ProductImage?.AltText,
            Quantity = item.Quantity,
            StockQuantity = sku.StockQuantity,
            Price = sku.Price,
            SalePrice = sku.SalePrice,
            CurrentPrice = priceInfo.CurrentPrice,
            IsOnSale = priceInfo.IsOnSale,
            DiscountPercent = priceInfo.DiscountPercent,
            IsAvailable = isAvailable,
            CanUpdateQuantity = isMarketActive && sku.StockQuantity > 0,
            AvailabilityMessage = availabilityMessage
        };
    }

    private bool TryGetCurrentUserId(out long userId)
    {
        return long.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }

    private string FirstModelError(string fallback)
    {
        return ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
            ?? fallback;
    }
}
