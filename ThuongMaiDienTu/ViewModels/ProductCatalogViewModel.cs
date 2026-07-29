using Microsoft.AspNetCore.Mvc.Rendering;

namespace ThuongMaiDienTu.ViewModels;

public class ProductCatalogViewModel
{
    public string? SearchTerm { get; set; }
    public long? CategoryId { get; set; }
    public long? StoreId { get; set; }
    public string? Brand { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinimumRating { get; set; }
    public bool InStockOnly { get; set; }
    public bool OnSaleOnly { get; set; }
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; } = 1;
    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
    public IReadOnlyList<SelectListItem> Stores { get; set; } = [];
    public IReadOnlyList<SelectListItem> Brands { get; set; } = [];
    public IReadOnlyList<ProductCatalogItemViewModel> Items { get; set; } = [];
}

public class ProductCatalogItemViewModel
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string? ShortDescription { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string StoreName { get; set; } = string.Empty;

    public long SkuId { get; set; }

    public string SkuCode { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal? SalePrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public bool IsOnSale { get; set; }

    public int DiscountPercent { get; set; }

    public DateTime? SaleStart { get; set; }

    public DateTime? SaleEnd { get; set; }

    public int StockQuantity { get; set; }

    public bool IsInStock => StockQuantity > 0;

    public string? ImageUrl { get; set; }

    public string? AltText { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public sealed class ProductCatalogQuery
{
    public string? SearchTerm { get; set; }
    public long? CategoryId { get; set; }
    public long? StoreId { get; set; }
    public string? Brand { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinimumRating { get; set; }
    public bool InStockOnly { get; set; }
    public bool OnSaleOnly { get; set; }
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;

    public void Normalize()
    {
        SearchTerm = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm.Trim();
        if (SearchTerm?.Length > 100) SearchTerm = SearchTerm[..100];
        Brand = string.IsNullOrWhiteSpace(Brand) ? null : Brand.Trim();
        if (MinPrice < 0) MinPrice = null;
        if (MaxPrice < 0 || (MinPrice.HasValue && MaxPrice.HasValue && MaxPrice < MinPrice)) MaxPrice = null;
        if (MinimumRating is < 1 or > 5) MinimumRating = null;
        Page = Math.Max(1, Page);
        Sort = Sort?.ToLowerInvariant() switch
        {
            "price_asc" or "price-asc" or "price-desc" or "price_desc" or "rating_desc" or "name_asc" => Sort.ToLowerInvariant().Replace("-", "_"),
            _ => "newest"
        };
    }
}
