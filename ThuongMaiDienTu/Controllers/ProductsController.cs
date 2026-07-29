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

    [HttpGet("/san-pham")]
    public async Task<IActionResult> Index(ProductCatalogQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();
        const int pageSize = 12;
        var now = DateTime.Now;
        var products = _context.Products.AsNoTracking()
            .Where(product => product.Status == "ACTIVE" && product.Store.Status == "ACTIVE" && product.Category.Status == "ACTIVE" && product.ProductSkus.Any(sku => sku.Status == "ACTIVE"))
            .Select(product => new
            {
                product.ProductId, product.ProductName, product.Brand, product.ShortDescription, product.CreatedAt,
                product.CategoryId, CategoryName = product.Category.CategoryName, StoreId = product.StoreId, StoreName = product.Store.StoreName, product.Slug,
                BestSku = product.ProductSkus.Where(sku => sku.Status == "ACTIVE")
                    .OrderBy(sku => sku.SalePrice.HasValue && sku.SalePrice > 0 && sku.SalePrice < sku.Price && (!sku.SaleStart.HasValue || sku.SaleStart <= now) && (!sku.SaleEnd.HasValue || sku.SaleEnd >= now) ? sku.SalePrice : sku.Price)
                    .ThenBy(sku => sku.SkuId)
                    .Select(sku => new { sku.SkuId, sku.Price, sku.SalePrice, sku.SaleStart, sku.SaleEnd, sku.StockQuantity }).First(),
                Image = product.ProductImages.Where(image => image.IsPrimary && (image.ImageUrl.StartsWith("/") || image.ImageUrl.StartsWith("http://") || image.ImageUrl.StartsWith("https://"))).OrderBy(image => image.SortOrder).Select(image => new { image.ImageUrl, image.AltText }).FirstOrDefault(),
                AverageRating = product.Reviews.Where(review => review.Status == "VISIBLE").Select(review => (double?)review.Rating).Average() ?? 0,
                ReviewCount = product.Reviews.Count(review => review.Status == "VISIBLE")
            });
        if (query.SearchTerm is not null) products = products.Where(product => product.ProductName.Contains(query.SearchTerm) || (product.ShortDescription != null && product.ShortDescription.Contains(query.SearchTerm)) || (product.Brand != null && product.Brand.Contains(query.SearchTerm)) || product.StoreName.Contains(query.SearchTerm));
        if (query.CategoryId.HasValue) products = products.Where(product => product.CategoryId == query.CategoryId.Value);
        if (query.StoreId.HasValue) products = products.Where(product => product.StoreId == query.StoreId.Value);
        if (query.Brand is not null) products = products.Where(product => product.Brand == query.Brand);
        if (query.MinPrice.HasValue) products = products.Where(product => (product.BestSku.SalePrice.HasValue && product.BestSku.SalePrice < product.BestSku.Price && (!product.BestSku.SaleStart.HasValue || product.BestSku.SaleStart <= now) && (!product.BestSku.SaleEnd.HasValue || product.BestSku.SaleEnd >= now) ? product.BestSku.SalePrice : product.BestSku.Price) >= query.MinPrice.Value);
        if (query.MaxPrice.HasValue) products = products.Where(product => (product.BestSku.SalePrice.HasValue && product.BestSku.SalePrice < product.BestSku.Price && (!product.BestSku.SaleStart.HasValue || product.BestSku.SaleStart <= now) && (!product.BestSku.SaleEnd.HasValue || product.BestSku.SaleEnd >= now) ? product.BestSku.SalePrice : product.BestSku.Price) <= query.MaxPrice.Value);
        if (query.MinimumRating.HasValue) products = products.Where(product => product.AverageRating >= query.MinimumRating.Value);
        if (query.InStockOnly) products = products.Where(product => product.BestSku.StockQuantity > 0);
        if (query.OnSaleOnly) products = products.Where(product => product.BestSku.SalePrice.HasValue && product.BestSku.SalePrice < product.BestSku.Price && (!product.BestSku.SaleStart.HasValue || product.BestSku.SaleStart <= now) && (!product.BestSku.SaleEnd.HasValue || product.BestSku.SaleEnd >= now));
        products = query.Sort switch
        {
            "price_asc" => products.OrderBy(product => product.BestSku.SalePrice.HasValue && product.BestSku.SalePrice < product.BestSku.Price && (!product.BestSku.SaleStart.HasValue || product.BestSku.SaleStart <= now) && (!product.BestSku.SaleEnd.HasValue || product.BestSku.SaleEnd >= now) ? product.BestSku.SalePrice : product.BestSku.Price).ThenBy(product => product.ProductId),
            "price_desc" => products.OrderByDescending(product => product.BestSku.SalePrice.HasValue && product.BestSku.SalePrice < product.BestSku.Price && (!product.BestSku.SaleStart.HasValue || product.BestSku.SaleStart <= now) && (!product.BestSku.SaleEnd.HasValue || product.BestSku.SaleEnd >= now) ? product.BestSku.SalePrice : product.BestSku.Price).ThenBy(product => product.ProductId),
            "rating_desc" => products.OrderByDescending(product => product.AverageRating).ThenBy(product => product.ProductId),
            "name_asc" => products.OrderBy(product => product.ProductName).ThenBy(product => product.ProductId),
            _ => products.OrderByDescending(product => product.CreatedAt).ThenBy(product => product.ProductId)
        };
        var totalItems = await products.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        query.Page = Math.Min(query.Page, totalPages);
        var rows = await products.Skip((query.Page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var model = new ProductCatalogViewModel
        {
            SearchTerm = query.SearchTerm, CategoryId = query.CategoryId, StoreId = query.StoreId, Brand = query.Brand,
            MinPrice = query.MinPrice, MaxPrice = query.MaxPrice, MinimumRating = query.MinimumRating,
            InStockOnly = query.InStockOnly, OnSaleOnly = query.OnSaleOnly, Sort = query.Sort, Page = query.Page,
            PageSize = pageSize, TotalItems = totalItems, TotalPages = totalPages,
            Categories = await GetCategoryOptionsAsync(cancellationToken), Stores = await GetStoreOptionsAsync(cancellationToken), Brands = await GetBrandOptionsAsync(cancellationToken),
            Items = rows.Select(row => { var price = ProductPricingHelper.Calculate(row.BestSku.Price, row.BestSku.SalePrice, row.BestSku.SaleStart, row.BestSku.SaleEnd, now); return new ProductCatalogItemViewModel { ProductId = row.ProductId, ProductName = row.ProductName, Brand = row.Brand, ShortDescription = row.ShortDescription, CategoryName = row.CategoryName, StoreName = row.StoreName, SkuId = row.BestSku.SkuId, Price = row.BestSku.Price, SalePrice = row.BestSku.SalePrice, CurrentPrice = price.CurrentPrice, IsOnSale = price.IsOnSale, DiscountPercent = price.DiscountPercent, SaleStart = row.BestSku.SaleStart, SaleEnd = row.BestSku.SaleEnd, StockQuantity = row.BestSku.StockQuantity, ImageUrl = row.Image?.ImageUrl, AltText = row.Image?.AltText, AverageRating = row.AverageRating, ReviewCount = row.ReviewCount }; }).ToList()
        };

        return View(model);
    }

    [HttpGet("/san-pham/{productSlug}")]
    public async Task<IActionResult> CanonicalDetails(string productSlug, CancellationToken cancellationToken = default)
    {
        var normalized = StoreSlugService.Normalize(productSlug);
        if (normalized != productSlug) return NotFound();
        var productId = await GetPublicSkuQuery()
            .Where(item => item.Product.Slug == normalized)
            .Select(item => (long?)item.ProductId)
            .FirstOrDefaultAsync(cancellationToken);
        return productId.HasValue ? await Details(productId.Value, true) : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> Details(long? id, bool canonical = false)
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

        if (!canonical)
        {
            return RedirectToActionPermanent(nameof(CanonicalDetails), new { productSlug = sku.Product.Slug });
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
        var activeSkus = product.ProductSkus
            .Where(item => item.Status == "ACTIVE")
            .OrderBy(item => item.SkuId)
            .Select(item =>
            {
                var pricing = ProductPricingHelper.Calculate(
                    item.Price, item.SalePrice, item.SaleStart, item.SaleEnd, DateTime.Now);
                return new ProductSkuOptionViewModel
                {
                    SkuId = item.SkuId,
                    SkuCode = item.SkuCode,
                    Price = item.Price,
                    SalePrice = item.SalePrice,
                    CurrentPrice = pricing.CurrentPrice,
                    StockQuantity = item.StockQuantity,
                    IsOnSale = pricing.IsOnSale,
                    DiscountPercent = pricing.DiscountPercent
                };
            })
            .ToList();
        var model = new ProductDetailsViewModel
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Slug = product.Slug,
            Brand = product.Brand,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            CategoryName = product.Category.CategoryName,
            CategorySlug = product.Category.Slug,
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
            Reviews = reviews,
            Skus = activeSkus,
            Images = product.ProductImages
                .Where(image => PublicAssetUrlHelper.IsSafeImageUrl(image.ImageUrl))
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.SortOrder)
                .ThenBy(image => image.ImageId)
                .Select(image => new ProductImageViewModel
                {
                    ImageUrl = image.ImageUrl,
                    AltText = image.AltText,
                    IsPrimary = image.IsPrimary,
                    SortOrder = image.SortOrder
                })
                .ToList()
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
                .ThenInclude(product => product.ProductImages)
            .Include(sku => sku.Product)
                .ThenInclude(product => product.ProductSkus)
            .Where(sku =>
                sku.Status == "ACTIVE" &&
                sku.Product.Status == "ACTIVE" &&
                sku.Product.Category.Status == "ACTIVE" &&
                sku.Product.Store.Status == "ACTIVE");
    }

    private async Task<IReadOnlyList<SelectListItem>> GetCategoryOptionsAsync(CancellationToken cancellationToken)
    {
        return await _context.ProductCategories
            .AsNoTracking()
            .Where(category => category.Status == "ACTIVE")
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.CategoryName)
            .Select(category => new SelectListItem(
                category.CategoryName,
                category.CategoryId.ToString()))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<SelectListItem>> GetStoreOptionsAsync(CancellationToken cancellationToken) => await _context.Stores.AsNoTracking().Where(store => store.Status == "ACTIVE").OrderBy(store => store.StoreName).Select(store => new SelectListItem(store.StoreName, store.StoreId.ToString())).ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<SelectListItem>> GetBrandOptionsAsync(CancellationToken cancellationToken) => await _context.Products.AsNoTracking().Where(product => product.Status == "ACTIVE" && product.Store.Status == "ACTIVE" && product.Brand != null && product.Brand != "").Select(product => product.Brand!).Distinct().OrderBy(brand => brand).Select(brand => new SelectListItem(brand, brand)).ToListAsync(cancellationToken);

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
