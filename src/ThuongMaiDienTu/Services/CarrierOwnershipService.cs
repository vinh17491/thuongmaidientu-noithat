using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Models;

namespace ThuongMaiDienTu.Services;

public interface ICarrierOwnershipService
{
    long? CurrentCarrierUserId { get; }
    IQueryable<ShippingProvider> ScopeProviders(IQueryable<ShippingProvider> query);
    IQueryable<Shipment> ScopeShipments(IQueryable<Shipment> query);
}

public sealed class CarrierOwnershipService(ICurrentUserService currentUser)
    : ICarrierOwnershipService
{
    public long? CurrentCarrierUserId =>
        string.Equals(currentUser.Role, "CARRIER", StringComparison.Ordinal)
            ? currentUser.UserId
            : null;

    public IQueryable<ShippingProvider> ScopeProviders(IQueryable<ShippingProvider> query) =>
        currentUser.IsAdmin
            ? query
            : query.Where(provider =>
                CurrentCarrierUserId.HasValue &&
                provider.OwnerUserId == CurrentCarrierUserId.Value);

    public IQueryable<Shipment> ScopeShipments(IQueryable<Shipment> query) =>
        currentUser.IsAdmin
            ? query
            : query.Where(shipment =>
                CurrentCarrierUserId.HasValue &&
                shipment.Provider.OwnerUserId == CurrentCarrierUserId.Value);
}
