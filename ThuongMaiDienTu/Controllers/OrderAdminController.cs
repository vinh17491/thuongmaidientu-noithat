using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Authorize(Roles = "ADMIN,SELLER")]
public class OrderAdminController : Controller
{
    private readonly ThuongMaiDienTuDbContext _context;

    public OrderAdminController(ThuongMaiDienTuDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status)
    {
        status = string.IsNullOrWhiteSpace(status)
            ? null
            : status.Trim().ToUpperInvariant();

        if (status is not null && !OrderWorkflowHelper.OrderStatuses.Contains(status))
        {
            status = null;
        }

        var query = _context.Orders.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(item => item.OrderStatus == status);
        }

        var model = await query
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
                TotalQuantity = item.OrderItems.Sum(orderItem => orderItem.Quantity),
                CustomerName = item.User.FullName
            })
            .ToListAsync();

        ViewBag.Status = status;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var model = await _context.Orders
            .AsNoTracking()
            .Where(item => item.OrderId == id)
            .Select(item => new OrderDetailsViewModel
            {
                OrderId = item.OrderId,
                OrderCode = item.OrderCode,
                CreatedAt = item.CreatedAt,
                CustomerName = item.User.FullName,
                CustomerEmail = item.User.Email,
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
                AllowedNextStatuses = OrderWorkflowHelper
                    .GetNextStatuses(item.OrderStatus),
                Items = item.OrderItems
                    .OrderBy(orderItem => orderItem.OrderItemId)
                    .Select(orderItem => new OrderDetailsItemViewModel
                    {
                        SkuId = orderItem.SkuId,
                        ProductName = orderItem.ProductName,
                        SkuCode = orderItem.SkuCode,
                        UnitPrice = orderItem.UnitPrice,
                        Quantity = orderItem.Quantity,
                        LineTotal = orderItem.LineTotal ?? orderItem.UnitPrice * orderItem.Quantity
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync();

        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(long id, UpdateOrderStatusViewModel model)
    {
        if (id != model.OrderId)
        {
            return BadRequest();
        }

        model.NewStatus = model.NewStatus?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!ModelState.IsValid ||
            !OrderWorkflowHelper.OrderStatuses.Contains(model.NewStatus))
        {
            TempData["ErrorMessage"] = "Trạng thái đơn hàng không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var order = await _context.Orders
                .Include(item => item.OrderItems)
                    .ThenInclude(item => item.Sku)
                .SingleOrDefaultAsync(item => item.OrderId == id);

            if (order is null)
            {
                await transaction.RollbackAsync();
                return NotFound();
            }

            if (!OrderWorkflowHelper.IsValidTransition(
                    order.OrderStatus,
                    model.NewStatus))
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] =
                    $"Không thể chuyển từ “{OrderWorkflowHelper.OrderStatusLabel(order.OrderStatus)}” " +
                    $"sang “{OrderWorkflowHelper.OrderStatusLabel(model.NewStatus)}”.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (model.NewStatus == OrderWorkflowHelper.Cancelled)
            {
                foreach (var item in order.OrderItems)
                {
                    item.Sku.StockQuantity =
                        checked(item.Sku.StockQuantity + item.Quantity);
                }
            }

            order.OrderStatus = model.NewStatus;
            order.UpdatedAt = DateTime.Now;

            if (model.NewStatus == OrderWorkflowHelper.Delivered)
            {
                order.PaymentStatus = OrderWorkflowHelper.Paid;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái đơn hàng.";
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] =
                "Không thể cập nhật trạng thái đơn hàng lúc này.";
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
                "Đã xảy ra lỗi khi cập nhật đơn. Không có thay đổi nào được ghi nhận.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
