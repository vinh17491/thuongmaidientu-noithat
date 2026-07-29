namespace ThuongMaiDienTu.ViewModels;

public sealed class HomeViewModel
{
    public IReadOnlyList<HomeCategoryViewModel> Categories { get; init; } = [];
    public IReadOnlyList<HomeProductViewModel> NewProducts { get; init; } = [];
    public IReadOnlyList<HomeStoreViewModel> FeaturedStores { get; init; } = [];
    public IReadOnlyList<string> Carriers { get; init; } = [];
}

public sealed class HomeCategoryViewModel
{
    public long CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
}

public sealed class HomeProductViewModel
{
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string StoreName { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal Price { get; init; }
}

public sealed class HomeStoreViewModel
{
    public string StoreName { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int ProductCount { get; init; }
}
