using System.ComponentModel.DataAnnotations;

namespace ThuongMaiDienTu.ViewModels;

public class ProductAdminFormViewModel : IValidatableObject
{
    public long? ProductId { get; set; }

    public long? SkuId { get; set; }

    public long? ImageId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự.")]
    [Display(Name = "Tên sản phẩm")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Range(1, long.MaxValue, ErrorMessage = "Danh mục không hợp lệ.")]
    [Display(Name = "Danh mục")]
    public long CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập slug.")]
    [StringLength(220, ErrorMessage = "Slug không được vượt quá 220 ký tự.")]
    [RegularExpression(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ gồm chữ thường không dấu, số và dấu gạch ngang.")]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Thương hiệu không được vượt quá 100 ký tự.")]
    [Display(Name = "Thương hiệu")]
    public string? Brand { get; set; }

    [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự.")]
    [Display(Name = "Mô tả ngắn")]
    public string? ShortDescription { get; set; }

    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }

    [StringLength(180, ErrorMessage = "Tiêu đề SEO không được vượt quá 180 ký tự.")]
    [Display(Name = "Tiêu đề SEO")]
    public string? SeoTitle { get; set; }

    [StringLength(320, ErrorMessage = "Mô tả SEO không được vượt quá 320 ký tự.")]
    [Display(Name = "Mô tả SEO")]
    public string? SeoDescription { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái sản phẩm.")]
    [Display(Name = "Trạng thái sản phẩm")]
    public string ProductStatus { get; set; } = "ACTIVE";

    [Required(ErrorMessage = "Vui lòng nhập mã SKU.")]
    [StringLength(80, ErrorMessage = "Mã SKU không được vượt quá 80 ký tự.")]
    [RegularExpression(
        @"^[A-Za-z0-9._-]+$",
        ErrorMessage = "Mã SKU chỉ gồm chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.")]
    [Display(Name = "Mã SKU")]
    public string SkuCode { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Giá thường phải lớn hơn 0.")]
    [Display(Name = "Giá thường")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Giá khuyến mãi phải lớn hơn 0.")]
    [Display(Name = "Giá khuyến mãi")]
    public decimal? SalePrice { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Bắt đầu khuyến mãi")]
    public DateTime? SaleStart { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Kết thúc khuyến mãi")]
    public DateTime? SaleEnd { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không được âm.")]
    [Display(Name = "Tồn kho")]
    public int StockQuantity { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái SKU.")]
    [Display(Name = "Trạng thái SKU")]
    public string SkuStatus { get; set; } = "ACTIVE";

    [StringLength(500, ErrorMessage = "URL ảnh không được vượt quá 500 ký tự.")]
    [Display(Name = "URL ảnh chính")]
    public string? ImageUrl { get; set; }

    [Display(Name = "File ảnh chính")]
    public IFormFile? ImageFile { get; set; }

    public string ImageSource { get; set; } = "url";

    [StringLength(180, ErrorMessage = "Mô tả ảnh không được vượt quá 180 ký tự.")]
    [Display(Name = "Mô tả ảnh")]
    public string? AltText { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SalePrice.HasValue && SalePrice.Value >= Price)
        {
            yield return new ValidationResult(
                "Giá khuyến mãi phải nhỏ hơn giá thường.",
                new[] { nameof(SalePrice) });
        }

        if (SaleStart.HasValue && SaleEnd.HasValue && SaleEnd.Value <= SaleStart.Value)
        {
            yield return new ValidationResult(
                "Thời gian kết thúc phải sau thời gian bắt đầu.",
                new[] { nameof(SaleEnd) });
        }

        if (!string.IsNullOrWhiteSpace(ImageUrl) && !IsValidImageUrl(ImageUrl))
        {
            yield return new ValidationResult(
                "URL ảnh phải là địa chỉ HTTP/HTTPS hoặc đường dẫn bắt đầu bằng '/'.",
                new[] { nameof(ImageUrl) });
        }
    }

    private static bool IsValidImageUrl(string value)
    {
        if (value.StartsWith("/", StringComparison.Ordinal) &&
            !value.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
