using System.ComponentModel.DataAnnotations;
using ThuongMaiDienTu.Services;

namespace ThuongMaiDienTu.ViewModels;

public class CheckoutInputViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
    [StringLength(120, ErrorMessage = "Tên người nhận không được quá 120 ký tự.")]
    [Display(Name = "Tên người nhận")]
    public string ReceiverName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự.")]
    [RegularExpression(@"^[0-9+\s().-]{6,20}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string ReceiverPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
    [StringLength(300, ErrorMessage = "Địa chỉ không được quá 300 ký tự.")]
    [Display(Name = "Địa chỉ nhận hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Ghi chú không được quá 500 ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; } = OrderWorkflowHelper.Cod;
}

public class CheckoutViewModel : CheckoutInputViewModel
{

    public IReadOnlyList<CheckoutItemViewModel> Items { get; set; } = [];

    public decimal Subtotal { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal TotalAmount { get; set; }

    public bool CanPlaceOrder { get; set; }

    public IReadOnlyList<string> AvailabilityErrors { get; set; } = [];
}

public class CheckoutItemViewModel
{
    public long SkuId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SkuCode { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}

public class OrderListItemViewModel
{
    public long OrderId { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public int TotalQuantity { get; set; }

    public string? CustomerName { get; set; }
}

public class OrderDetailsViewModel
{
    public long OrderId { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerEmail { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string ShippingAddress { get; set; } = string.Empty;

    public string? Note { get; set; }

    public string Status { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal TotalAmount { get; set; }

    public bool CanCancel { get; set; }

    public IReadOnlyList<string> AllowedNextStatuses { get; set; } = [];

    public IReadOnlyList<OrderDetailsItemViewModel> Items { get; set; } = [];
}

public class OrderDetailsItemViewModel
{
    public long OrderItemId { get; set; }

    public long SkuId { get; set; }

    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SkuCode { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }

    public bool CanReview { get; set; }

    public bool IsReviewed { get; set; }

    public int? ReviewRating { get; set; }
}

public class UpdateOrderStatusViewModel
{
    [Range(1, long.MaxValue, ErrorMessage = "Đơn hàng không hợp lệ.")]
    public long OrderId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái mới.")]
    public string NewStatus { get; set; } = string.Empty;
}
