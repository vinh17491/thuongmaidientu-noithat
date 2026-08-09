using Microsoft.AspNetCore.Mvc.Rendering;

namespace ThuongMaiDienTu.ViewModels;

public class ProductAdminIndexViewModel
{
    public string? SearchTerm { get; set; }

    public long? CategoryId { get; set; }

    public string? Status { get; set; }

    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];

    public IReadOnlyList<ProductAdminListItemViewModel> Items { get; set; } = [];
}

public class ProductAdminListItemViewModel
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string StoreName { get; set; } = string.Empty;

    public string? SkuCode { get; set; }

    public decimal? Price { get; set; }

    public decimal? SalePrice { get; set; }

    public decimal? CurrentPrice { get; set; }

    public string PromotionStatus { get; set; } = "Không khuyến mãi";

    public DateTime? SaleStart { get; set; }

    public DateTime? SaleEnd { get; set; }

    public int? StockQuantity { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
