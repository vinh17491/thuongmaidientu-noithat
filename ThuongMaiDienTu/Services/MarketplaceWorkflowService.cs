using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Models;

namespace ThuongMaiDienTu.Services;

public interface IMarketplaceWorkflowService
{
    Task<bool> ChangeShipmentStatusAsync(
        long shipmentId, string nextStatus, string? note, CancellationToken cancellationToken = default);
    Task<bool> ChangeStoreOrderStatusAsync(
        long storeOrderId, string nextStatus, string? note, CancellationToken cancellationToken = default);
}

public sealed class MarketplaceWorkflowService(
    ThuongMaiDienTuDbContext context,
    ICurrentUserService currentUser,
    ICarrierOwnershipService carrierOwnership,
    IHttpContextAccessor httpContextAccessor) : IMarketplaceWorkflowService
{
    public async Task<bool> ChangeShipmentStatusAsync(
        long shipmentId, string nextStatus, string? note, CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            return false;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var shipment = await carrierOwnership.ScopeShipments(context.Shipments)
            .Include(item => item.StoreOrder).ThenInclude(item => item.Order)
            .SingleOrDefaultAsync(item => item.ShipmentId == shipmentId, cancellationToken);
        if (shipment is null || !ShipmentWorkflow.CanTransition(shipment.Status, nextStatus))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var oldStatus = shipment.Status;
        var now = DateTime.UtcNow;
        shipment.Status = nextStatus;
        shipment.UpdatedAt = now;
        if (nextStatus == ShipmentWorkflow.PickedUp) shipment.PickedUpAt = now;
        if (nextStatus == ShipmentWorkflow.Delivered) shipment.DeliveredAt = now;

        context.ShipmentStatusHistories.Add(new ShipmentStatusHistory
        {
            ShipmentId = shipment.ShipmentId,
            OldStatus = oldStatus,
            NewStatus = nextStatus,
            ChangedByUserId = currentUser.UserId.Value,
            Note = NormalizeNote(note),
            CreatedAt = now
        });
        AddAudit("SHIPMENT_STATUS_CHANGED", "Shipment", shipment.ShipmentId,
            new { Status = oldStatus }, new { Status = nextStatus });

        string? nextStoreOrderStatus = nextStatus switch
        {
            ShipmentWorkflow.ReadyForPickup
                when shipment.StoreOrder.Status == StoreOrderWorkflow.Confirmed
                => StoreOrderWorkflow.Processing,
            ShipmentWorkflow.PickedUp or ShipmentWorkflow.InTransit
                => StoreOrderWorkflow.Shipping,
            ShipmentWorkflow.Delivered
                when await context.Shipments
                    .Where(item => item.StoreOrderId == shipment.StoreOrderId &&
                                   item.ShipmentId != shipment.ShipmentId)
                    .AllAsync(item => item.Status == ShipmentWorkflow.Delivered, cancellationToken)
                => StoreOrderWorkflow.Delivered,
            _ => null
        };

        if (nextStoreOrderStatus is not null &&
            shipment.StoreOrder.Status != nextStoreOrderStatus)
        {
            var oldStoreOrderStatus = shipment.StoreOrder.Status;
            shipment.StoreOrder.Status = nextStoreOrderStatus;
            shipment.StoreOrder.UpdatedAt = now;
            context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                StoreOrderId = shipment.StoreOrderId,
                OldStatus = oldStoreOrderStatus,
                NewStatus = nextStoreOrderStatus,
                ChangedByUserId = currentUser.UserId.Value,
                Note = NormalizeNote(note),
                CreatedAt = now
            });
            await context.SaveChangesAsync(cancellationToken);
            await AggregateParentOrderAsync(shipment.StoreOrder.OrderId, now, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ChangeStoreOrderStatusAsync(
        long storeOrderId, string nextStatus, string? note, CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            return false;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var query = context.StoreOrders
            .Include(item => item.Store)
            .Include(item => item.Order)
            .Include(item => item.OrderItems).ThenInclude(item => item.Sku)
            .Include(item => item.Shipments);
        var storeOrder = await query.SingleOrDefaultAsync(
            item => item.StoreOrderId == storeOrderId &&
                    (currentUser.IsAdmin ||
                     (currentUser.UserId.HasValue &&
                      item.Store.OwnerUserId == currentUser.UserId.Value)),
            cancellationToken);
        if (storeOrder is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var pickedUp = storeOrder.Shipments.Any(shipment =>
            shipment.Status is ShipmentWorkflow.PickedUp or
                ShipmentWorkflow.InTransit or ShipmentWorkflow.Delivered);
        if (!StoreOrderWorkflow.CanTransition(storeOrder.Status, nextStatus, pickedUp))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var oldStatus = storeOrder.Status;
        var now = DateTime.UtcNow;
        storeOrder.Status = nextStatus;
        storeOrder.UpdatedAt = now;
        context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            StoreOrderId = storeOrder.StoreOrderId,
            OldStatus = oldStatus,
            NewStatus = nextStatus,
            ChangedByUserId = currentUser.UserId.Value,
            Note = NormalizeNote(note),
            CreatedAt = now
        });
        AddAudit("STORE_ORDER_STATUS_CHANGED", "StoreOrder", storeOrder.StoreOrderId,
            new { Status = oldStatus }, new { Status = nextStatus });

        if (nextStatus == StoreOrderWorkflow.Cancelled)
        {
            foreach (var item in storeOrder.OrderItems)
            {
                item.Sku.StockQuantity = checked(item.Sku.StockQuantity + item.Quantity);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await AggregateParentOrderAsync(storeOrder.OrderId, now, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task AggregateParentOrderAsync(
        long orderId, DateTime now, CancellationToken cancellationToken)
    {
        var order = await context.Orders.SingleAsync(item => item.OrderId == orderId, cancellationToken);
        var storeOrders = await context.StoreOrders
            .Where(item => item.OrderId == orderId)
            .Select(item => new { item.Status, item.TotalAmount })
            .ToListAsync(cancellationToken);
        order.OrderStatus = StoreOrderWorkflow.AggregateParentStatus(
            storeOrders.Select(item => item.Status));
        var oldTotal = order.TotalAmount;
        order.TotalAmount = StoreOrderWorkflow.CalculatePayableParentTotal(
            storeOrders.Select(item => (item.Status, item.TotalAmount)));
        order.ShippingFee = await context.StoreOrders
            .Where(item => item.OrderId == orderId &&
                           item.Status != StoreOrderWorkflow.Cancelled)
            .SumAsync(item => item.ShippingFee, cancellationToken);
        order.UpdatedAt = now;
        if (order.OrderStatus == StoreOrderWorkflow.Delivered &&
            order.PaymentMethod == OrderWorkflowHelper.Cod)
        {
            order.PaymentStatus = OrderWorkflowHelper.Paid;
        }
        else if (order.PaymentMethod == OrderWorkflowHelper.Cod)
        {
            order.PaymentStatus = OrderWorkflowHelper.Unpaid;
        }

        if (oldTotal != order.TotalAmount)
        {
            AddAudit("ORDER_TOTAL_RECALCULATED", "Order", order.OrderId,
                new { TotalAmount = oldTotal },
                new
                {
                    order.TotalAmount,
                    HasCancelledPart = storeOrders.Any(item =>
                        item.Status == StoreOrderWorkflow.Cancelled)
                });
        }
    }

    private void AddAudit(string action, string entity, long id, object before, object after) =>
        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Action = action,
            EntityName = entity,
            EntityId = id.ToString(),
            BeforeJson = JsonSerializer.Serialize(before),
            AfterJson = JsonSerializer.Serialize(after),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTime.UtcNow
        });

    private static string? NormalizeNote(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(note.Trim().Length, 500)];
}
