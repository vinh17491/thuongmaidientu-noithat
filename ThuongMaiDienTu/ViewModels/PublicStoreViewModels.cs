namespace ThuongMaiDienTu.ViewModels;

public sealed class PublicStorePageViewModel
{
    public PublicStoreHeaderViewModel Store { get; init; } = new();
    public string StoreSlug { get; init; } = string.Empty;
    public string? Search { get; init; }
    public long? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MinimumRating { get; init; }
    public bool InStock { get; init; }
    public bool OnSale { get; init; }
    public string Sort { get; init; } = "newest";
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalProducts { get; init; }
    public int TotalPages { get; init; }
    public IReadOnlyList<PublicStoreCategoryViewModel> Categories { get; init; } = [];
    public IReadOnlyList<PublicStoreProductCardViewModel> Products { get; init; } = [];
}

public sealed class PublicStoreHeaderViewModel
{
    public string StoreName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public int ProductCount { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
}

public sealed class PublicStoreCategoryViewModel
{
    public long CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
}

public sealed class PublicStoreProductCardViewModel
{
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string? AltText { get; init; }
    public decimal OriginalPrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public bool IsOnSale { get; init; }
    public int DiscountPercent { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int StockQuantity { get; init; }
}
