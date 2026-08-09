using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Authorize(Roles = "CUSTOMER")]
public class OrdersController : Controller
{
    private readonly ThuongMaiDienTuDbContext _context;

    public OrdersController(ThuongMaiDienTuDbContext context)
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

        var model = await _context.Orders
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.OrderId)
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
            .ToListAsync();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        var model = await LoadDetailsAsync(id, userId);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var order = await _context.Orders
                .Include(item => item.OrderItems)
                    .ThenInclude(item => item.Sku)
                .SingleOrDefaultAsync(item =>
                    item.OrderId == id &&
                    item.UserId == userId);

            if (order is null)
            {
                await transaction.RollbackAsync();
                return NotFound();
            }

            if (order.OrderStatus != OrderWorkflowHelper.Pending)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] =
                    "Chỉ đơn đang chờ xác nhận mới có thể hủy.";
                return RedirectToAction(nameof(Details), new { id });
            }

            foreach (var item in order.OrderItems)
            {
                item.Sku.StockQuantity = checked(item.Sku.StockQuantity + item.Quantity);
            }

            var now = DateTime.Now;
            var storeOrders = await _context.StoreOrders
                .Include(item => item.Shipments)
                .Where(item => item.OrderId == order.OrderId)
                .ToListAsync();
            foreach (var storeOrder in storeOrders)
            {
                if (storeOrder.Status != OrderWorkflowHelper.Cancelled)
                {
                    _context.OrderStatusHistories.Add(new Models.OrderStatusHistory
                    {
                        StoreOrderId = storeOrder.StoreOrderId,
                        OldStatus = storeOrder.Status,
                        NewStatus = OrderWorkflowHelper.Cancelled,
                        ChangedByUserId = userId,
                        Note = "Khách hàng hủy đơn",
                        CreatedAt = now
                    });
                    storeOrder.Status = OrderWorkflowHelper.Cancelled;
                    storeOrder.UpdatedAt = now;
                }

                foreach (var shipment in storeOrder.Shipments.Where(item => item.Status != "CANCELLED"))
                {
                    _context.ShipmentStatusHistories.Add(new Models.ShipmentStatusHistory
                    {
                        ShipmentId = shipment.ShipmentId,
                        OldStatus = shipment.Status,
                        NewStatus = "CANCELLED",
                        ChangedByUserId = userId,
                        Note = "Khách hàng hủy đơn",
                        CreatedAt = now
                    });
                    shipment.Status = "CANCELLED";
                    shipment.UpdatedAt = now;
                }
            }

            order.OrderStatus = OrderWorkflowHelper.Cancelled;
            order.ShippingFee = 0;
            order.TotalAmount = 0;
            order.UpdatedAt = now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = "Đã hủy đơn hàng và hoàn lại tồn kho.";
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] =
                "Không thể hủy đơn hàng lúc này. Vui lòng thử lại.";
        }
        catch (OverflowException)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] =
                "Không thể hoàn tồn kho do dữ liệu số lượng không hợp lệ.";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] =
                "Đã xảy ra lỗi khi hủy đơn. Không có thay đổi nào được ghi nhận.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<OrderDetailsViewModel?> LoadDetailsAsync(long orderId, long userId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(item => item.OrderId == orderId && item.UserId == userId)
            .Select(item => new OrderDetailsViewModel
            {
                OrderId = item.OrderId,
                OrderCode = item.OrderCode,
                CreatedAt = item.CreatedAt,
                ReceiverName = item.ReceiverName,
                ReceiverPhone = item.ReceiverPhone,
                ShippingAddress = item.ShippingAddress,
                Note = item.Note,
                Status = item.OrderStatus,
                PaymentMethod = item.PaymentMethod,
                PaymentStatus = item.PaymentStatus,
                Subtotal = item.Subtotal,
                DiscountAmount = item.DiscountAmount,
                ShippingFee = item.ShippingFee,
                TotalAmount = item.TotalAmount,
                CanCancel = item.OrderStatus == OrderWorkflowHelper.Pending,
                Items = item.OrderItems
                    .OrderBy(orderItem => orderItem.OrderItemId)
                    .Select(orderItem => new OrderDetailsItemViewModel
                    {
                        OrderItemId = orderItem.OrderItemId,
                        SkuId = orderItem.SkuId,
                        ProductId = orderItem.Sku.ProductId,
                        ProductName = orderItem.ProductName,
                        SkuCode = orderItem.SkuCode,
                        UnitPrice = orderItem.UnitPrice,
                        Quantity = orderItem.Quantity,
                        LineTotal = orderItem.LineTotal ?? orderItem.UnitPrice * orderItem.Quantity,
                        IsReviewed = orderItem.Reviews.Any(review => review.UserId == userId),
                        CanReview =
                            item.OrderStatus == OrderWorkflowHelper.Delivered &&
                            !orderItem.Reviews.Any(review => review.UserId == userId),
                        ReviewRating = orderItem.Reviews
                            .Where(review => review.UserId == userId)
                            .Select(review => (int?)review.Rating)
                            .FirstOrDefault()
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync();
    }

    private bool TryGetCurrentUserId(out long userId)
    {
        return long.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}
