namespace ThuongMaiDienTu.ViewModels;

public class ProductAdminDetailsViewModel
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string StoreName { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public string ProductStatus { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? SkuId { get; set; }

    public string? SkuCode { get; set; }

    public decimal? Price { get; set; }

    public decimal? SalePrice { get; set; }

    public decimal? CurrentPrice { get; set; }

    public string PromotionStatus { get; set; } = "Không khuyến mãi";

    public DateTime? SaleStart { get; set; }

    public DateTime? SaleEnd { get; set; }

    public int? StockQuantity { get; set; }

    public string? SkuStatus { get; set; }

    public string? ImageUrl { get; set; }

    public string? AltText { get; set; }

    public bool HasOrderItems { get; set; }
}
