using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[AllowAnonymous]
public class ProductsController : Controller
{
    private readonly ThuongMaiDienTuDbContext _context;

    public ProductsController(ThuongMaiDienTuDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, long? categoryId)
    {
        searchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        var query = GetPublicSkuQuery();

        if (searchTerm is not null)
        {
            query = query.Where(sku =>
                sku.Product.ProductName.Contains(searchTerm) ||
                (sku.Product.Brand != null && sku.Product.Brand.Contains(searchTerm)) ||
                sku.SkuCode.Contains(searchTerm));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(sku => sku.Product.CategoryId == categoryId.Value);
        }

        var skus = await query
            .OrderByDescending(sku => sku.Product.CreatedAt)
            .ThenBy(sku => sku.Product.ProductName)
            .ToListAsync();

        var now = DateTime.Now;
        var model = new ProductCatalogViewModel
        {
            SearchTerm = searchTerm,
            CategoryId = categoryId,
            Categories = await GetCategoryOptionsAsync(),
            Items = skus.Select(sku => MapCatalogItem(sku, now)).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var sku = await GetPublicSkuQuery()
            .FirstOrDefaultAsync(item => item.ProductId == id.Value);

        if (sku is null)
        {
            return NotFound();
        }

        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(review =>
                review.ProductId == id.Value &&
                review.Status == "VISIBLE")
            .OrderByDescending(review => review.CreatedAt)
            .Select(review => new ProductReviewViewModel
            {
                CustomerName = review.User.FullName,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            })
            .ToListAsync();

        var priceInfo = ProductPricingHelper.Calculate(
            sku.Price,
            sku.SalePrice,
            sku.SaleStart,
            sku.SaleEnd,
            DateTime.Now);

        var product = sku.Product;
        var model = new ProductDetailsViewModel
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Slug = product.Slug,
            Brand = product.Brand,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            CategoryName = product.Category.CategoryName,
            StoreName = product.Store.StoreName,
            SkuId = sku.SkuId,
            SkuCode = sku.SkuCode,
            Price = sku.Price,
            SalePrice = sku.SalePrice,
            CurrentPrice = priceInfo.CurrentPrice,
            IsOnSale = priceInfo.IsOnSale,
            DiscountPercent = priceInfo.DiscountPercent,
            SaleStart = sku.SaleStart,
            SaleEnd = sku.SaleEnd,
            StockQuantity = sku.StockQuantity,
            ImageUrl = product.ProductImage?.ImageUrl,
            AltText = product.ProductImage?.AltText,
            SeoTitle = product.SeoTitle,
            SeoDescription = product.SeoDescription,
            Reviews = reviews
        };

        return View(model);
    }

    private IQueryable<Models.ProductSku> GetPublicSkuQuery()
    {
        return _context.ProductSkus
            .AsNoTracking()
            .Include(sku => sku.Product)
                .ThenInclude(product => product.Category)
            .Include(sku => sku.Product)
                .ThenInclude(product => product.Store)
            .Include(sku => sku.Product)
                .ThenInclude(product => product.ProductImage)
            .Where(sku =>
                sku.SkuId == _context.ProductSkus
                    .Where(other => other.ProductId == sku.ProductId)
                    .Min(other => other.SkuId) &&
                sku.Status == "ACTIVE" &&
                sku.Product.Status == "ACTIVE" &&
                sku.Product.Category.Status == "ACTIVE" &&
                sku.Product.Store.Status == "ACTIVE");
    }

    private async Task<IReadOnlyList<SelectListItem>> GetCategoryOptionsAsync()
    {
        return await _context.ProductCategories
            .AsNoTracking()
            .Where(category => category.Status == "ACTIVE")
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.CategoryName)
            .Select(category => new SelectListItem(
                category.CategoryName,
                category.CategoryId.ToString()))
            .ToListAsync();
    }

    private static ProductCatalogItemViewModel MapCatalogItem(
        Models.ProductSku sku,
        DateTime now)
    {
        var priceInfo = ProductPricingHelper.Calculate(
            sku.Price,
            sku.SalePrice,
            sku.SaleStart,
            sku.SaleEnd,
            now);

        return new ProductCatalogItemViewModel
        {
            ProductId = sku.Product.ProductId,
            ProductName = sku.Product.ProductName,
            Slug = sku.Product.Slug,
            Brand = sku.Product.Brand,
            ShortDescription = sku.Product.ShortDescription,
            CategoryName = sku.Product.Category.CategoryName,
            StoreName = sku.Product.Store.StoreName,
            SkuId = sku.SkuId,
            SkuCode = sku.SkuCode,
            Price = sku.Price,
            SalePrice = sku.SalePrice,
            CurrentPrice = priceInfo.CurrentPrice,
            IsOnSale = priceInfo.IsOnSale,
            DiscountPercent = priceInfo.DiscountPercent,
            SaleStart = sku.SaleStart,
            SaleEnd = sku.SaleEnd,
            StockQuantity = sku.StockQuantity,
            ImageUrl = sku.Product.ProductImage?.ImageUrl,
            AltText = sku.Product.ProductImage?.AltText
        };
    }
}
