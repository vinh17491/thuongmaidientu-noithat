using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Models;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ThuongMaiDienTuDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            ThuongMaiDienTuDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel
            {
                Categories = await _context.ProductCategories.AsNoTracking()
                    .Where(category => category.Status == "ACTIVE")
                    .OrderBy(category => category.SortOrder)
                    .Take(8)
                    .Select(category => new HomeCategoryViewModel
                    {
                        CategoryId = category.CategoryId,
                        CategoryName = category.CategoryName
                    })
                    .ToListAsync(),
                NewProducts = await _context.Products.AsNoTracking()
                    .Where(product =>
                        product.Status == "ACTIVE" &&
                        product.Store.Status == "ACTIVE" &&
                        product.Category.Status == "ACTIVE")
                    .OrderByDescending(product => product.CreatedAt)
                    .Take(8)
                    .Select(product => new HomeProductViewModel
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        StoreName = product.Store.StoreName,
                        ImageUrl = product.ProductImages
                            .OrderByDescending(image => image.IsPrimary)
                            .ThenBy(image => image.SortOrder)
                            .Select(image => image.ImageUrl)
                            .FirstOrDefault(),
                        Price = product.ProductSkus
                            .Where(sku => sku.Status == "ACTIVE")
                            .Min(sku => (decimal?)sku.Price) ?? 0
                    })
                    .ToListAsync(),
                FeaturedStores = await _context.Stores.AsNoTracking()
                    .Where(store => store.Status == "ACTIVE")
                    .OrderByDescending(store => store.Products.Count(product =>
                        product.Status == "ACTIVE"))
                    .Take(6)
                    .Select(store => new HomeStoreViewModel
                    {
                        StoreName = store.StoreName,
                        Slug = store.Slug,
                        ProductCount = store.Products.Count(product =>
                            product.Status == "ACTIVE")
                    })
                    .ToListAsync(),
                Carriers = await _context.ShippingProviders.AsNoTracking()
                    .Where(provider => provider.Status == "ACTIVE")
                    .OrderBy(provider => provider.ProviderName)
                    .Take(6)
                    .Select(provider => provider.ProviderName)
                    .ToListAsync()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
