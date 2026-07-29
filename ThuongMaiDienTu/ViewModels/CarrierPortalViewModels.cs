using System.ComponentModel.DataAnnotations;

namespace ThuongMaiDienTu.ViewModels;

public sealed class CarrierDashboardViewModel
{
    public int TotalShipments { get; init; }
    public decimal TotalShippingFee { get; init; }
    public decimal SuccessRate { get; init; }
    public IReadOnlyDictionary<string, int> Counts { get; init; } =
        new Dictionary<string, int>();
    public IReadOnlyList<CarrierProviderViewModel> Providers { get; init; } = [];
    public IReadOnlyList<CarrierServiceViewModel> Services { get; init; } = [];
}

public sealed class CarrierProviderViewModel
{
    public long ProviderId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Description { get; init; }
}

public sealed class ShippingProviderInputViewModel
{
    [Range(1, long.MaxValue)] public long ProviderId { get; set; }
    [Required, StringLength(200)] public string ProviderName { get; set; } = string.Empty;
    [StringLength(30)] public string? Phone { get; set; }
    [EmailAddress, StringLength(255)] public string? Email { get; set; }
    public string? Description { get; set; }
}

public sealed class CarrierServiceViewModel
{
    public long ServiceId { get; init; }
    public long ProviderId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string ServiceCode { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public decimal BaseFee { get; init; }
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<CarrierRateRuleViewModel> Rules { get; init; } = [];
}

public sealed class CarrierRateRuleViewModel
{
    public long RuleId { get; init; }
    public string OriginArea { get; init; } = string.Empty;
    public string DestinationArea { get; init; } = string.Empty;
    public decimal MinWeight { get; init; }
    public decimal MaxWeight { get; init; }
    public decimal Fee { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class ShippingServiceInputViewModel
{
    [Range(1, long.MaxValue)] public long ProviderId { get; set; }
    [Required, StringLength(80)] public string ServiceCode { get; set; } = string.Empty;
    [Required, StringLength(200)] public string ServiceName { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "999999999")] public decimal BaseFee { get; set; }
    [Range(1, 365)] public int EstimatedMinDays { get; set; }
    [Range(1, 365)] public int EstimatedMaxDays { get; set; }
    [Range(typeof(decimal), "0.001", "999999")] public decimal? MaxWeight { get; set; }
}

public sealed class ShippingRateInputViewModel
{
    [Range(1, long.MaxValue)] public long ServiceId { get; set; }
    [Required, StringLength(120)] public string OriginArea { get; set; } = string.Empty;
    [Required, StringLength(120)] public string DestinationArea { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "999999")] public decimal MinWeight { get; set; }
    [Range(typeof(decimal), "0.001", "999999")] public decimal MaxWeight { get; set; }
    [Range(typeof(decimal), "0", "999999999")] public decimal Fee { get; set; }
    [Range(typeof(decimal), "0", "999999999")] public decimal ExtraFeePerKg { get; set; }
    public bool SupportsCod { get; set; } = true;
}

public sealed class CarrierShipmentListViewModel
{
    public IReadOnlyList<CarrierShipmentItemViewModel> Items { get; init; } = [];
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; }
    public int TotalPages { get; init; }
}

public class CarrierShipmentItemViewModel
{
    public long ShipmentId { get; init; }
    public string TrackingCode { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string StoreOrderCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal ShippingFee { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class CarrierShipmentDetailsViewModel : CarrierShipmentItemViewModel
{
    public string OrderCode { get; init; } = string.Empty;
    public string StoreName { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string PickupAddress { get; init; } = string.Empty;
    public string DeliveryAddress { get; init; } = string.Empty;
    public DateTime? EstimatedDeliveryAt { get; init; }
    public DateTime? PickedUpAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public IReadOnlyList<ShipmentHistoryViewModel> History { get; init; } = [];
}

public sealed class ShipmentHistoryViewModel
{
    public string? OldStatus { get; init; }
    public string NewStatus { get; init; } = string.Empty;
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
}
