namespace ThuongMaiDienTu.Services;

public static class OrderWorkflowHelper
{
    public const string Pending = "PENDING";
    public const string Confirmed = "CONFIRMED";
    public const string Processing = "PROCESSING";
    public const string Shipping = "SHIPPING";
    public const string Delivered = "DELIVERED";
    public const string Cancelled = "CANCELLED";

    public const string Cod = "COD";
    public const string BankTransfer = "BANK_TRANSFER";

    public const string Unpaid = "UNPAID";
    public const string Paid = "PAID";
    public const string Failed = "FAILED";

    public static readonly IReadOnlyList<string> OrderStatuses =
    [
        Pending, Confirmed, Processing, Shipping, Delivered, Cancelled
    ];

    public static readonly IReadOnlyList<string> PaymentMethods =
    [
        Cod, BankTransfer
    ];

    public static bool IsValidTransition(string currentStatus, string newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            (Pending, Confirmed) => true,
            (Pending, Cancelled) => true,
            (Confirmed, Processing) => true,
            (Processing, Shipping) => true,
            (Shipping, Delivered) => true,
            _ => false
        };
    }

    public static IReadOnlyList<string> GetNextStatuses(string currentStatus)
    {
        return currentStatus switch
        {
            Pending => [Confirmed, Cancelled],
            Confirmed => [Processing],
            Processing => [Shipping],
            Shipping => [Delivered],
            _ => []
        };
    }

    public static string OrderStatusLabel(string status)
    {
        return status switch
        {
            Pending => "Chờ xác nhận",
            Confirmed => "Đã xác nhận",
            Processing => "Đang xử lý",
            Shipping => "Đang giao",
            Delivered => "Đã giao",
            Cancelled => "Đã hủy",
            _ => status
        };
    }

    public static string PaymentMethodLabel(string method)
    {
        return method switch
        {
            Cod => "Thanh toán khi nhận hàng (COD)",
            BankTransfer => "Chuyển khoản (mô phỏng)",
            _ => method
        };
    }

    public static string PaymentStatusLabel(string status)
    {
        return status switch
        {
            Unpaid => "Chưa thanh toán",
            Paid => "Đã thanh toán",
            Failed => "Thanh toán lỗi",
            _ => status
        };
    }

    public static string StatusBadgeClass(string status)
    {
        return status switch
        {
            Pending => "bg-warning text-dark",
            Confirmed => "bg-info text-dark",
            Processing => "bg-primary",
            Shipping => "bg-primary",
            Delivered => "bg-success",
            Cancelled => "bg-secondary",
            _ => "bg-secondary"
        };
    }
}
