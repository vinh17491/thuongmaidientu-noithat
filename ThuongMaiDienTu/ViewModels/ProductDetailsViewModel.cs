namespace ThuongMaiDienTu.ViewModels;

public class ProductDetailsViewModel
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

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

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public IReadOnlyList<ProductReviewViewModel> Reviews { get; set; } = [];
    public IReadOnlyList<ProductSkuOptionViewModel> Skus { get; set; } = [];
    public IReadOnlyList<ProductImageViewModel> Images { get; set; } = [];

    public int ReviewCount => Reviews.Count;

    public decimal AverageRating => Reviews.Count == 0
        ? 0
        : Reviews.Average(item => (decimal)item.Rating);
}

public sealed class ProductSkuOptionViewModel
{
    public long SkuId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CurrentPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsOnSale { get; set; }
}

public sealed class ProductImageViewModel
{
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class ProductReviewViewModel
{
    public string CustomerName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
