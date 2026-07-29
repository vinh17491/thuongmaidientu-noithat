using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Authorize(Roles = "ADMIN,SELLER")]
public class DashboardController : Controller
{
    private const int LowStockThreshold = 5;
    private readonly ThuongMaiDienTuDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStoreOwnershipService _ownership;

    public DashboardController(
        ThuongMaiDienTuDbContext context,
        ICurrentUserService currentUser,
        IStoreOwnershipService ownership)
    {
        _context = context;
        _currentUser = currentUser;
        _ownership = ownership;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var orders = _ownership.ScopeOrders(_context.Orders.AsNoTracking());
        var products = _ownership.ScopeProducts(_context.Products.AsNoTracking());
        var sellerUserId = _currentUser.UserId;
        var skus = _context.ProductSkus.AsNoTracking().Where(item =>
            _currentUser.IsAdmin ||
            (sellerUserId.HasValue &&
             item.Product.Store.OwnerUserId == sellerUserId.Value));

        var statusCounts = await orders
            .AsNoTracking()
            .GroupBy(item => item.OrderStatus)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.Status, item => item.Count);

        var model = new DashboardViewModel
        {
            TotalProducts = await products.CountAsync(),
            TotalCustomers = _currentUser.IsAdmin
                ? await _context.Users.AsNoTracking()
                    .CountAsync(item => item.Role == "CUSTOMER")
                : await orders.Select(item => item.UserId).Distinct().CountAsync(),
            TotalOrders = await orders.CountAsync(),
            TotalRevenue = await orders
                .Where(item => item.OrderStatus == OrderWorkflowHelper.Delivered)
                .SelectMany(item => item.OrderItems)
                .Where(item =>
                    _currentUser.IsAdmin ||
                    (sellerUserId.HasValue &&
                     item.Sku.Product.Store.OwnerUserId == sellerUserId.Value))
                .SumAsync(item => (decimal?)(item.LineTotal ??
                    item.UnitPrice * item.Quantity)) ?? 0m,
            OrdersByStatus = OrderWorkflowHelper.OrderStatuses
                .Select(status => new DashboardOrderStatusViewModel
                {
                    Status = status,
                    Count = statusCounts.GetValueOrDefault(status)
                })
                .ToList(),
            TopSellingProducts = await _context.OrderItems
                .AsNoTracking()
                .Where(item =>
                    item.Order.OrderStatus == OrderWorkflowHelper.Delivered &&
                    (_currentUser.IsAdmin ||
                     (sellerUserId.HasValue &&
                      item.Sku.Product.Store.OwnerUserId == sellerUserId.Value)))
                .GroupBy(item => new
                {
                    item.SkuId,
                    item.ProductName
                })
                .Select(group => new DashboardTopProductViewModel
                {
                    SkuId = group.Key.SkuId,
                    ProductName = group.Key.ProductName,
                    TotalQuantitySold = group.Sum(item => item.Quantity),
                    TotalRevenue = group.Sum(item =>
                        item.LineTotal ?? item.UnitPrice * item.Quantity)
                })
                .OrderByDescending(item => item.TotalQuantitySold)
                .ThenByDescending(item => item.TotalRevenue)
                .Take(5)
                .ToListAsync(),
            RevenueByMonth = await orders
                .AsNoTracking()
                .Where(item => item.OrderStatus == OrderWorkflowHelper.Delivered)
                .SelectMany(item => item.OrderItems)
                .Where(item =>
                    _currentUser.IsAdmin ||
                    (sellerUserId.HasValue &&
                     item.Sku.Product.Store.OwnerUserId == sellerUserId.Value))
                .GroupBy(item => new
                {
                    item.Order.CreatedAt.Year,
                    item.Order.CreatedAt.Month
                })
                .Select(group => new DashboardRevenueMonthViewModel
                {
                    Year = group.Key.Year,
                    Month = group.Key.Month,
                    TotalOrders = group.Select(item => item.OrderId).Distinct().Count(),
                    TotalRevenue = group.Sum(item =>
                        item.LineTotal ?? item.UnitPrice * item.Quantity)
                })
                .OrderByDescending(item => item.Year)
                .ThenByDescending(item => item.Month)
                .Take(12)
                .ToListAsync(),
            LowStockProducts = await skus
                .Where(item =>
                    item.Status == "ACTIVE" &&
                    item.Product.Status == "ACTIVE" &&
                    item.StockQuantity <= LowStockThreshold)
                .OrderBy(item => item.StockQuantity)
                .ThenBy(item => item.Product.ProductName)
                .Select(item => new DashboardLowStockViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.ProductName,
                    SkuCode = item.SkuCode,
                    StockQuantity = item.StockQuantity
                })
                .ToListAsync()
        };

        return View(model);
    }
}
