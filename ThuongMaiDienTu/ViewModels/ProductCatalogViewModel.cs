using Microsoft.AspNetCore.Mvc.Rendering;

namespace ThuongMaiDienTu.ViewModels;

public class ProductCatalogViewModel
{
    public string? SearchTerm { get; set; }

    public long? CategoryId { get; set; }

    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];

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
}
