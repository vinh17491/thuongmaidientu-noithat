using System.ComponentModel.DataAnnotations;

namespace ThuongMaiDienTu.ViewModels;

public sealed class StoreProfileInputModel
{
    public long? StoreId { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Tên gian hàng")]
    public string StoreName { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [StringLength(180)]
    [Display(Name = "Tiêu đề SEO")]
    public string? SeoTitle { get; set; }

    [StringLength(320)]
    [Display(Name = "Mô tả SEO")]
    public string? SeoDescription { get; set; }
}

public sealed class StoreProfileViewModel
{
    public long StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class AdminStoreListItemViewModel
{
    public long StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public string SellerName { get; init; } = string.Empty;
    public string SellerEmail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int ProductCount { get; init; }
}

public sealed class AdminStoreIndexViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<AdminStoreListItemViewModel> Items { get; init; } = [];
    public int Page { get; init; }
    public bool HasNextPage { get; init; }
}

public sealed class AdminStoreDetailsViewModel
{
    public AdminStoreListItemViewModel Store { get; init; } = new();
    public string? Description { get; init; }
    public string Slug { get; init; } = string.Empty;
    public IReadOnlyList<string> AuditEntries { get; set; } = [];
}

public sealed class StoreStatusInputModel
{
    public long StoreId { get; set; }
    public string NextStatus { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string? Reason { get; set; }
}
