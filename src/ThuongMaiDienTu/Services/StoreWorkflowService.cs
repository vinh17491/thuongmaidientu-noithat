using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Models;

namespace ThuongMaiDienTu.Services;

public static class StoreStatuses
{
    public const string Pending = "PENDING";
    public const string Active = "ACTIVE";
    public const string Rejected = "REJECTED";
    public const string Suspended = "SUSPENDED";
}

public static class StoreWorkflow
{
    public static bool CanTransition(string current, string next) =>
        (current, next) switch
        {
            (StoreStatuses.Pending, StoreStatuses.Active) => true,
            (StoreStatuses.Pending, StoreStatuses.Rejected) => true,
            (StoreStatuses.Active, StoreStatuses.Suspended) => true,
            (StoreStatuses.Suspended, StoreStatuses.Active) => true,
            (StoreStatuses.Rejected, StoreStatuses.Pending) => true,
            _ => false
        };
}

public interface IStoreWorkflowService
{
    Task<bool> ChangeStatusAsync(long storeId, string nextStatus, string reason, CancellationToken cancellationToken = default);
}

public sealed class StoreWorkflowService(
    ThuongMaiDienTuDbContext context,
    ICurrentUserService currentUser) : IStoreWorkflowService
{
    public async Task<bool> ChangeStatusAsync(long storeId, string nextStatus, string reason, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAdmin || !currentUser.UserId.HasValue || string.IsNullOrWhiteSpace(reason)) return false;
        nextStatus = nextStatus.Trim().ToUpperInvariant();
        var store = await context.Stores.SingleOrDefaultAsync(item => item.StoreId == storeId, cancellationToken);
        if (store is null || !StoreWorkflow.CanTransition(store.Status, nextStatus)) return false;
        if (reason.Length > 500) return false;

        var oldStatus = store.Status;
        store.Status = nextStatus;
        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId.Value,
            Action = "STORE_STATUS_CHANGED",
            EntityName = "Store",
            EntityId = store.StoreId.ToString(),
            BeforeJson = JsonSerializer.Serialize(new { Status = oldStatus }),
            AfterJson = JsonSerializer.Serialize(new { Status = nextStatus, Reason = reason.Trim() }),
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
