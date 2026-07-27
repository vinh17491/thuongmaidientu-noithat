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

    public DashboardController(ThuongMaiDienTuDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var statusCounts = await _context.Orders
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
            TotalProducts = await _context.Products.AsNoTracking().CountAsync(),
            TotalCustomers = await _context.Users
                .AsNoTracking()
                .CountAsync(item => item.Role == "CUSTOMER"),
            TotalOrders = await _context.Orders.AsNoTracking().CountAsync(),
            TotalRevenue = await _context.Orders
                .AsNoTracking()
                .Where(item => item.OrderStatus == OrderWorkflowHelper.Delivered)
                .SumAsync(item => (decimal?)item.TotalAmount) ?? 0m,
            OrdersByStatus = OrderWorkflowHelper.OrderStatuses
                .Select(status => new DashboardOrderStatusViewModel
                {
                    Status = status,
                    Count = statusCounts.GetValueOrDefault(status)
                })
                .ToList(),
            TopSellingProducts = await _context.VwBestSellingProducts
                .AsNoTracking()
                .OrderByDescending(item => item.TotalQuantitySold)
                .ThenByDescending(item => item.TotalRevenue)
                .Take(5)
                .Select(item => new DashboardTopProductViewModel
                {
                    SkuId = item.SkuId,
                    ProductName = item.ProductName,
                    TotalQuantitySold = item.TotalQuantitySold ?? 0,
                    TotalRevenue = item.TotalRevenue ?? 0m
                })
                .ToListAsync(),
            RevenueByMonth = await _context.VwRevenueByMonths
                .AsNoTracking()
                .OrderByDescending(item => item.RevenueYear)
                .ThenByDescending(item => item.RevenueMonth)
                .Take(12)
                .Select(item => new DashboardRevenueMonthViewModel
                {
                    Year = item.RevenueYear ?? 0,
                    Month = item.RevenueMonth ?? 0,
                    TotalOrders = item.TotalOrders ?? 0,
                    TotalRevenue = item.TotalRevenue ?? 0m
                })
                .ToListAsync(),
            LowStockProducts = await _context.ProductSkus
                .AsNoTracking()
                .Where(item =>
                    item.SkuId == _context.ProductSkus
                        .Where(other => other.ProductId == item.ProductId)
                        .Min(other => other.SkuId) &&
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
