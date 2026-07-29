using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Route("cua-hang")]
public sealed class StoresController(ThuongMaiDienTuDbContext context) : Controller
{
    private const int PageSize = 12;

    [HttpGet("{storeSlug}")]
    public async Task<IActionResult> Details(
        string storeSlug,
        string? search = null,
        long? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? minimumRating = null,
        bool inStock = false,
        bool onSale = false,
        string? sort = null,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = StoreSlugService.Normalize(storeSlug);
        if (string.IsNullOrWhiteSpace(normalizedSlug) ||
            !string.Equals(normalizedSlug, storeSlug, StringComparison.Ordinal)) return NotFound();

        page = Math.Max(1, page);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (search?.Length > 100) search = search[..100];
        if (minPrice < 0) minPrice = null;
        if (maxPrice < 0 || (minPrice.HasValue && maxPrice.HasValue && maxPrice < minPrice)) maxPrice = null;
        if (minimumRating is < 1 or > 5) minimumRating = null;
        sort = sort?.ToLowerInvariant() switch { "price-asc" or "price-desc" or "rating" or "name" => sort.ToLowerInvariant(), _ => "newest" };

        var store = await context.Stores.AsNoTracking()
            .Where(item => item.Slug == normalizedSlug && item.Status == "ACTIVE")
            .Select(item => new
            {
                item.StoreId, item.StoreName, item.Description, item.CreatedAt,
                ProductCount = item.Products.Count(product => product.Status == "ACTIVE" && product.Category.Status == "ACTIVE" && product.ProductSkus.Any(sku => sku.Status == "ACTIVE"))
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (store is null) return NotFound();

        var reviewSummary = await context.Reviews.AsNoTracking()
            .Where(review => review.Status == "VISIBLE" && review.Product.StoreId == store.StoreId && review.Product.Status == "ACTIVE" && review.Product.Category.Status == "ACTIVE")
            .GroupBy(_ => 1)
            .Select(group => new { Average = group.Average(review => (double)review.Rating), Count = group.Count() })
            .SingleOrDefaultAsync(cancellationToken);

        var now = DateTime.Now;
        var products = context.Products.AsNoTracking()
            .Where(product => product.StoreId == store.StoreId && product.Status == "ACTIVE" && product.Category.Status == "ACTIVE" && product.ProductSkus.Any(sku => sku.Status == "ACTIVE"))
            .Select(product => new
            {
                product.ProductId, product.ProductName, product.CreatedAt,
                CategoryId = product.CategoryId, CategoryName = product.Category.CategoryName,
                BestSku = product.ProductSkus.Where(sku => sku.Status == "ACTIVE")
                    .OrderBy(sku => sku.SalePrice.HasValue && sku.SalePrice > 0 && sku.SalePrice < sku.Price && (!sku.SaleStart.HasValue || sku.SaleStart <= now) && (!sku.SaleEnd.HasValue || sku.SaleEnd >= now) ? sku.SalePrice : sku.Price)
                    .ThenBy(sku => sku.SkuId)
                    .Select(sku => new { sku.Price, sku.SalePrice, sku.SaleStart, sku.SaleEnd, sku.StockQuantity }).First(),
                Image = product.ProductImages.Where(image => image.IsPrimary && (image.ImageUrl.StartsWith("/") || image.ImageUrl.StartsWith("http://") || image.ImageUrl.StartsWith("https://"))).OrderBy(image => image.SortOrder).Select(image => new { image.ImageUrl, image.AltText }).FirstOrDefault(),
                AverageRating = product.Reviews.Where(review => review.Status == "VISIBLE").Select(review => (double?)review.Rating).Average() ?? 0,
                ReviewCount = product.Reviews.Count(review => review.Status == "VISIBLE")
            });

        if (search is not null) products = products.Where(product => product.ProductName.Contains(search));
        if (categoryId.HasValue) products = products.Where(product => product.CategoryId == categoryId.Value);
        if (minPrice.HasValue) products = products.Where(product => (product.BestSku.SalePrice.HasValue && product.BestSku.SalePrice < product.BestSku.Price && (!product.BestSku.SaleStart.HasValue || product.BestSku.SaleStart <= now) && (!product.BestSku.SaleEnd.HasValue || product.BestSku.SaleEnd >= now) ? product.BestSku.SalePrice : product.BestSku.Price) >= minPrice.Value);
        if (maxPrice.HasValue) products = products.Where(product => (product.BestSku.SalePrice.HasValue && product.BestSku.SalePrice < product.BestSku.Price && (!product.BestSku.SaleStart.HasValue || product.BestSku.SaleStart <= now) && (!product.BestSku.SaleEnd.HasValue || product.BestSku.SaleEnd >= now) ? product.BestSku.SalePrice : product.BestSku.Price) <= maxPrice.Value);
        if (minimumRating.HasValue) products = products.Where(product => product.AverageRating >= minimumRating.Value);
        if (inStock) products = products.Where(product => product.BestSku.StockQuantity > 0);
        if (onSale) products = products.Where(product => product.BestSku.SalePrice.HasValue && product.BestSku.SalePrice < product.BestSku.Price && (!product.BestSku.SaleStart.HasValue || product.BestSku.SaleStart <= now) && (!product.BestSku.SaleEnd.HasValue || product.BestSku.SaleEnd >= now));

        products = sort switch
        {
            "price-asc" => products.OrderBy(product => product.BestSku.SalePrice.HasValue && product.BestSku.SalePrice < product.BestSku.Price && (!product.BestSku.SaleStart.HasValue || product.BestSku.SaleStart <= now) && (!product.BestSku.SaleEnd.HasValue || product.BestSku.SaleEnd >= now) ? product.BestSku.SalePrice : product.BestSku.Price),
            "price-desc" => products.OrderByDescending(product => product.BestSku.SalePrice.HasValue && product.BestSku.SalePrice < product.BestSku.Price && (!product.BestSku.SaleStart.HasValue || product.BestSku.SaleStart <= now) && (!product.BestSku.SaleEnd.HasValue || product.BestSku.SaleEnd >= now) ? product.BestSku.SalePrice : product.BestSku.Price),
            "rating" => products.OrderByDescending(product => product.AverageRating).ThenBy(product => product.ProductName),
            "name" => products.OrderBy(product => product.ProductName),
            _ => products.OrderByDescending(product => product.CreatedAt)
        };
        var total = await products.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Min(page, totalPages);
        var rows = await products.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(cancellationToken);
        var cards = rows.Select(row =>
        {
            var price = ProductPricingHelper.Calculate(row.BestSku.Price, row.BestSku.SalePrice, row.BestSku.SaleStart, row.BestSku.SaleEnd, now);
            return new PublicStoreProductCardViewModel { ProductId = row.ProductId, ProductName = row.ProductName, CategoryName = row.CategoryName, ImageUrl = row.Image?.ImageUrl, AltText = row.Image?.AltText ?? row.ProductName, OriginalPrice = row.BestSku.Price, CurrentPrice = price.CurrentPrice, IsOnSale = price.IsOnSale, DiscountPercent = price.DiscountPercent, AverageRating = row.AverageRating, ReviewCount = row.ReviewCount, StockQuantity = row.BestSku.StockQuantity };
        }).ToList();
        var categories = await context.Products.AsNoTracking().Where(product => product.StoreId == store.StoreId && product.Status == "ACTIVE" && product.Category.Status == "ACTIVE" && product.ProductSkus.Any(sku => sku.Status == "ACTIVE")).Select(product => new PublicStoreCategoryViewModel { CategoryId = product.CategoryId, CategoryName = product.Category.CategoryName }).Distinct().OrderBy(item => item.CategoryName).ToListAsync(cancellationToken);
        return View(new PublicStorePageViewModel { StoreSlug = normalizedSlug, Store = new PublicStoreHeaderViewModel { StoreName = store.StoreName, Description = store.Description, CreatedAt = store.CreatedAt, ProductCount = store.ProductCount, AverageRating = reviewSummary?.Average ?? 0, ReviewCount = reviewSummary?.Count ?? 0 }, Search = search, CategoryId = categoryId, MinPrice = minPrice, MaxPrice = maxPrice, MinimumRating = minimumRating, InStock = inStock, OnSale = onSale, Sort = sort, Page = page, PageSize = PageSize, TotalProducts = total, TotalPages = totalPages, Categories = categories, Products = cards });
    }
}
