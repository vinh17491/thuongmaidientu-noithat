using System.ComponentModel.DataAnnotations;

namespace ThuongMaiDienTu.ViewModels;

public class ReviewCreateViewModel
{
    [Range(1, long.MaxValue, ErrorMessage = "Sản phẩm trong đơn hàng không hợp lệ.")]
    public long OrderItemId { get; set; }

    public long OrderId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
    [Display(Name = "Số sao")]
    public int Rating { get; set; } = 5;

    [StringLength(1000, ErrorMessage = "Nội dung đánh giá không được quá 1000 ký tự.")]
    [Display(Name = "Nội dung đánh giá")]
    public string? Comment { get; set; }
}

public class ReviewAdminListItemViewModel
{
    public long ReviewId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public long? OrderItemId { get; set; }

    public string? OrderCode { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
