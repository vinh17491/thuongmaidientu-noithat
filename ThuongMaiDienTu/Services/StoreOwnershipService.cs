using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Models;

namespace ThuongMaiDienTu.Services;

public interface IStoreOwnershipService
{
    IQueryable<Product> ScopeProducts(IQueryable<Product> query);
    IQueryable<Order> ScopeOrders(IQueryable<Order> query);
    IQueryable<Review> ScopeReviews(IQueryable<Review> query);
    Task<Store?> GetActiveOwnedStoreAsync(CancellationToken cancellationToken = default);
}

public sealed class StoreOwnershipService(
    ThuongMaiDienTuDbContext context,
    ICurrentUserService currentUser) : IStoreOwnershipService
{
    public IQueryable<Product> ScopeProducts(IQueryable<Product> query) =>
        currentUser.IsAdmin
            ? query
            : query.Where(product =>
                currentUser.UserId.HasValue &&
                product.Store.OwnerUserId == currentUser.UserId.Value);

    public IQueryable<Order> ScopeOrders(IQueryable<Order> query) =>
        currentUser.IsAdmin
            ? query
            : query.Where(order => order.OrderItems.Any(item =>
                currentUser.UserId.HasValue &&
                item.Sku.Product.Store.OwnerUserId == currentUser.UserId.Value));

    public IQueryable<Review> ScopeReviews(IQueryable<Review> query) =>
        currentUser.IsAdmin
            ? query
            : query.Where(review =>
                currentUser.UserId.HasValue &&
                review.Product.Store.OwnerUserId == currentUser.UserId.Value);

    public Task<Store?> GetActiveOwnedStoreAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Task.FromResult<Store?>(null);
        }

        return context.Stores
            .AsNoTracking()
            .Where(store =>
                store.OwnerUserId == currentUser.UserId.Value &&
                store.Status == "ACTIVE")
            .OrderBy(store => store.StoreId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
