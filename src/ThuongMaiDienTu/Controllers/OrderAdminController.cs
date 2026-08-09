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
    private readonly ICurrentUserService _currentUser;
    private readonly IStoreOwnershipService _ownership;

    public OrderAdminController(
        ThuongMaiDienTuDbContext context,
        ICurrentUserService currentUser,
        IStoreOwnershipService ownership)
    {
        _context = context;
        _currentUser = currentUser;
        _ownership = ownership;
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

        var query = _ownership.ScopeOrders(_context.Orders.AsNoTracking());
        if (status is not null)
        {
            query = query.Where(item => item.OrderStatus == status);
        }

        var sellerUserId = _currentUser.UserId;
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
                TotalAmount = _currentUser.IsAdmin
                    ? item.TotalAmount
                    : item.OrderItems
                        .Where(orderItem =>
                            sellerUserId.HasValue &&
                            orderItem.Sku.Product.Store.OwnerUserId == sellerUserId.Value)
                        .Sum(orderItem => orderItem.LineTotal ??
                            orderItem.UnitPrice * orderItem.Quantity),
                TotalQuantity = item.OrderItems
                    .Where(orderItem =>
                        _currentUser.IsAdmin ||
                        (sellerUserId.HasValue &&
                         orderItem.Sku.Product.Store.OwnerUserId == sellerUserId.Value))
                    .Sum(orderItem => orderItem.Quantity),
                CustomerName = item.User.FullName
            })
            .ToListAsync();

        ViewBag.Status = status;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var sellerUserId = _currentUser.UserId;
        var model = await _ownership.ScopeOrders(_context.Orders)
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
                Subtotal = _currentUser.IsAdmin
                    ? item.Subtotal
                    : item.OrderItems
                        .Where(orderItem =>
                            sellerUserId.HasValue &&
                            orderItem.Sku.Product.Store.OwnerUserId == sellerUserId.Value)
                        .Sum(orderItem => orderItem.LineTotal ??
                            orderItem.UnitPrice * orderItem.Quantity),
                DiscountAmount = _currentUser.IsAdmin ? item.DiscountAmount : 0,
                ShippingFee = _currentUser.IsAdmin
                    ? item.ShippingFee
                    : item.OrderItems
                        .Where(orderItem =>
                            sellerUserId.HasValue &&
                            orderItem.Sku.Product.Store.OwnerUserId == sellerUserId.Value &&
                            orderItem.StoreOrder != null)
                        .Select(orderItem => orderItem.StoreOrder!.ShippingFee)
                        .Distinct()
                        .Sum(),
                TotalAmount = _currentUser.IsAdmin
                    ? item.TotalAmount
                    : item.OrderItems
                        .Where(orderItem =>
                            sellerUserId.HasValue &&
                            orderItem.Sku.Product.Store.OwnerUserId == sellerUserId.Value)
                        .Sum(orderItem => orderItem.LineTotal ??
                            orderItem.UnitPrice * orderItem.Quantity),
                AllowedNextStatuses = _currentUser.IsAdmin
                    ? OrderWorkflowHelper.GetNextStatuses(item.OrderStatus)
                    : Array.Empty<string>(),
                Items = item.OrderItems
                    .Where(orderItem =>
                        _currentUser.IsAdmin ||
                        (sellerUserId.HasValue &&
                         orderItem.Sku.Product.Store.OwnerUserId == sellerUserId.Value))
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
        if (!_currentUser.IsAdmin)
        {
            return Forbid();
        }

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
            var order = await _ownership.ScopeOrders(_context.Orders
                .Include(item => item.OrderItems)
                    .ThenInclude(item => item.Sku))
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

            var now = DateTime.Now;
            var storeOrders = await _context.StoreOrders
                .Include(item => item.Shipments)
                .Where(item => item.OrderId == order.OrderId)
                .ToListAsync();
            foreach (var storeOrder in storeOrders)
            {
                if (storeOrder.Status != model.NewStatus)
                {
                    _context.OrderStatusHistories.Add(new Models.OrderStatusHistory
                    {
                        StoreOrderId = storeOrder.StoreOrderId,
                        OldStatus = storeOrder.Status,
                        NewStatus = model.NewStatus,
                        ChangedByUserId = _currentUser.UserId,
                        Note = "Quản trị viên cập nhật trạng thái đơn",
                        CreatedAt = now
                    });
                    storeOrder.Status = model.NewStatus;
                    storeOrder.UpdatedAt = now;
                }

                if (model.NewStatus == OrderWorkflowHelper.Cancelled)
                {
                    foreach (var shipment in storeOrder.Shipments.Where(item => item.Status != "CANCELLED"))
                    {
                        _context.ShipmentStatusHistories.Add(new Models.ShipmentStatusHistory
                        {
                            ShipmentId = shipment.ShipmentId,
                            OldStatus = shipment.Status,
                            NewStatus = "CANCELLED",
                            ChangedByUserId = _currentUser.UserId!.Value,
                            Note = "Quản trị viên hủy đơn",
                            CreatedAt = now
                        });
                        shipment.Status = "CANCELLED";
                        shipment.UpdatedAt = now;
                    }
                }
            }

            order.OrderStatus = model.NewStatus;
            order.UpdatedAt = now;

            if (model.NewStatus == OrderWorkflowHelper.Cancelled)
            {
                order.ShippingFee = 0;
                order.TotalAmount = 0;
            }

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
