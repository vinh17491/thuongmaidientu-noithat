using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Authorize(Roles = "ADMIN,CARRIER")]
public sealed class CarrierShipmentsController(
    ThuongMaiDienTuDbContext context,
    ICarrierOwnershipService ownership,
    IMarketplaceWorkflowService workflow,
    ICurrentUserService currentUser) : Controller
{
    private const int PageSize = 20;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        page = Math.Max(1, page);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();
        var query = ownership.ScopeShipments(context.Shipments.AsNoTracking());
        if (search is not null) query = query.Where(item => item.TrackingCode.Contains(search));
        if (status is not null) query = query.Where(item => item.Status == status);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(item => new CarrierShipmentItemViewModel
            {
                ShipmentId = item.ShipmentId, TrackingCode = item.TrackingCode,
                ProviderName = item.Provider.ProviderName,
                ServiceName = item.Service.ServiceName,
                StoreOrderCode = item.StoreOrder.StoreOrderCode,
                Status = item.Status, ShippingFee = item.ShippingFee,
                CreatedAt = item.CreatedAt
            }).ToListAsync();
        return View(new CarrierShipmentListViewModel
        {
            Items = items, Search = search, Status = status, Page = page,
            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize))
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var item = await ownership.ScopeShipments(context.Shipments.AsNoTracking())
            .Where(shipment => shipment.ShipmentId == id)
            .Select(shipment => new CarrierShipmentDetailsViewModel
            {
                ShipmentId = shipment.ShipmentId, TrackingCode = shipment.TrackingCode,
                ProviderName = shipment.Provider.ProviderName,
                ServiceName = shipment.Service.ServiceName,
                StoreOrderCode = shipment.StoreOrder.StoreOrderCode,
                OrderCode = shipment.StoreOrder.Order.OrderCode,
                StoreName = shipment.StoreOrder.Store.StoreName,
                CustomerName = shipment.StoreOrder.Order.User.FullName,
                Status = shipment.Status, ShippingFee = shipment.ShippingFee,
                PickupAddress = shipment.PickupAddress,
                DeliveryAddress = shipment.DeliveryAddress,
                EstimatedDeliveryAt = shipment.EstimatedDeliveryAt,
                PickedUpAt = shipment.PickedUpAt, DeliveredAt = shipment.DeliveredAt,
                CreatedAt = shipment.CreatedAt,
                History = context.ShipmentStatusHistories
                    .Where(history => history.ShipmentId == shipment.ShipmentId)
                    .OrderByDescending(history => history.CreatedAt)
                    .Select(history => new ShipmentHistoryViewModel
                    {
                        OldStatus = history.OldStatus, NewStatus = history.NewStatus,
                        Note = history.Note, CreatedAt = history.CreatedAt
                    }).ToList()
            }).SingleOrDefaultAsync();
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(long id, string newStatus, string? note)
    {
        newStatus = newStatus?.Trim().ToUpperInvariant() ?? string.Empty;
        var changed = await workflow.ChangeShipmentStatusAsync(id, newStatus, note);
        if (!changed) return NotFound();
        TempData["SuccessMessage"] = "Đã cập nhật trạng thái và ghi lịch sử.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTracking(long id, string trackingCode)
    {
        trackingCode = trackingCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (trackingCode.Length is < 3 or > 100) return BadRequest();
        var shipment = await ownership.ScopeShipments(context.Shipments)
            .SingleOrDefaultAsync(item => item.ShipmentId == id);
        if (shipment is null) return NotFound();
        if (await context.Shipments.AnyAsync(item =>
            item.TrackingCode == trackingCode && item.ShipmentId != id))
        {
            TempData["ErrorMessage"] = "Tracking code đã tồn tại.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var oldTrackingCode = shipment.TrackingCode;
        shipment.TrackingCode = trackingCode;
        shipment.UpdatedAt = DateTime.UtcNow;
        context.AuditLogs.Add(new ThuongMaiDienTu.Models.AuditLog
        {
            ActorUserId = currentUser.UserId,
            Action = "SHIPMENT_TRACKING_CHANGED",
            EntityName = "Shipment",
            EntityId = shipment.ShipmentId.ToString(),
            BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { TrackingCode = oldTrackingCode }),
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new { TrackingCode = trackingCode }),
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }
}
