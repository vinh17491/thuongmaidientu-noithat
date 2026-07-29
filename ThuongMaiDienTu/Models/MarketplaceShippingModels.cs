using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThuongMaiDienTu.Models;

[Table("store_orders")]
public sealed class StoreOrder
{
    [Key, Column("store_order_id")] public long StoreOrderId { get; set; }
    [Column("order_id")] public long OrderId { get; set; }
    [Column("store_id")] public long StoreId { get; set; }
    [Column("store_order_code")] public string StoreOrderCode { get; set; } = string.Empty;
    [Column("subtotal", TypeName = "decimal(18,2)")] public decimal Subtotal { get; set; }
    [Column("discount_amount", TypeName = "decimal(18,2)")] public decimal DiscountAmount { get; set; }
    [Column("shipping_fee", TypeName = "decimal(18,2)")] public decimal ShippingFee { get; set; }
    [Column("total_amount", TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    [Column("status")] public string Status { get; set; } = "PENDING";
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
    [Timestamp, Column("row_version")] public byte[] RowVersion { get; set; } = [];
    public Order Order { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public ICollection<Shipment> Shipments { get; set; } = [];
}

[Table("shipping_providers")]
public sealed class ShippingProvider
{
    [Key, Column("provider_id")] public long ProviderId { get; set; }
    [Column("owner_user_id")] public long OwnerUserId { get; set; }
    [Column("provider_name")] public string ProviderName { get; set; } = string.Empty;
    [Column("slug")] public string Slug { get; set; } = string.Empty;
    [Column("phone")] public string? Phone { get; set; }
    [Column("email")] public string? Email { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("status")] public string Status { get; set; } = "PENDING";
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
    public User OwnerUser { get; set; } = null!;
    public ICollection<ShippingService> Services { get; set; } = [];
}

[Table("shipping_services")]
public sealed class ShippingService
{
    [Key, Column("service_id")] public long ServiceId { get; set; }
    [Column("provider_id")] public long ProviderId { get; set; }
    [Column("service_code")] public string ServiceCode { get; set; } = string.Empty;
    [Column("service_name")] public string ServiceName { get; set; } = string.Empty;
    [Column("base_fee", TypeName = "decimal(18,2)")] public decimal BaseFee { get; set; }
    [Column("estimated_min_days")] public int EstimatedMinDays { get; set; }
    [Column("estimated_max_days")] public int EstimatedMaxDays { get; set; }
    [Column("max_weight", TypeName = "decimal(18,3)")] public decimal? MaxWeight { get; set; }
    [Column("status")] public string Status { get; set; } = "ACTIVE";
    public ShippingProvider Provider { get; set; } = null!;
    public ICollection<ShippingRateRule> RateRules { get; set; } = [];
}

[Table("shipping_rate_rules")]
public sealed class ShippingRateRule
{
    [Key, Column("rule_id")] public long RuleId { get; set; }
    [Column("service_id")] public long ServiceId { get; set; }
    [Column("origin_area")] public string OriginArea { get; set; } = string.Empty;
    [Column("destination_area")] public string DestinationArea { get; set; } = string.Empty;
    [Column("min_weight", TypeName = "decimal(18,3)")] public decimal MinWeight { get; set; }
    [Column("max_weight", TypeName = "decimal(18,3)")] public decimal MaxWeight { get; set; }
    [Column("fee", TypeName = "decimal(18,2)")] public decimal Fee { get; set; }
    [Column("extra_fee_per_kg", TypeName = "decimal(18,2)")] public decimal ExtraFeePerKg { get; set; }
    [Column("supports_cod")] public bool SupportsCod { get; set; }
    [Column("status")] public string Status { get; set; } = "ACTIVE";
    public ShippingService Service { get; set; } = null!;
}

[Table("shipping_quotes")]
public sealed class ShippingQuote
{
    [Key, Column("quote_id")] public Guid QuoteId { get; set; }
    [Column("provider_id")] public long ProviderId { get; set; }
    [Column("service_id")] public long ServiceId { get; set; }
    [Column("fee", TypeName = "decimal(18,2)")] public decimal Fee { get; set; }
    [Column("estimated_min_days")] public int EstimatedMinDays { get; set; }
    [Column("estimated_max_days")] public int EstimatedMaxDays { get; set; }
    [Column("expires_at")] public DateTime ExpiresAt { get; set; }
    [Column("status")] public string Status { get; set; } = "ACTIVE";
    [Column("selected_at")] public DateTime? SelectedAt { get; set; }
    public ShippingProvider Provider { get; set; } = null!;
    public ShippingService Service { get; set; } = null!;
}

[Table("shipments")]
public sealed class Shipment
{
    [Key, Column("shipment_id")] public long ShipmentId { get; set; }
    [Column("store_order_id")] public long StoreOrderId { get; set; }
    [Column("provider_id")] public long ProviderId { get; set; }
    [Column("service_id")] public long ServiceId { get; set; }
    [Column("shipping_fee", TypeName = "decimal(18,2)")] public decimal ShippingFee { get; set; }
    [Column("tracking_code")] public string TrackingCode { get; set; } = string.Empty;
    [Column("status")] public string Status { get; set; } = "CREATED";
    [Column("pickup_address")] public string PickupAddress { get; set; } = string.Empty;
    [Column("delivery_address")] public string DeliveryAddress { get; set; } = string.Empty;
    [Column("estimated_delivery_at")] public DateTime? EstimatedDeliveryAt { get; set; }
    [Column("picked_up_at")] public DateTime? PickedUpAt { get; set; }
    [Column("delivered_at")] public DateTime? DeliveredAt { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
    [Timestamp, Column("row_version")] public byte[] RowVersion { get; set; } = [];
    public StoreOrder StoreOrder { get; set; } = null!;
    public ShippingProvider Provider { get; set; } = null!;
    public ShippingService Service { get; set; } = null!;
}

[Table("shipment_status_histories")]
public sealed class ShipmentStatusHistory
{
    [Key, Column("history_id")] public long HistoryId { get; set; }
    [Column("shipment_id")] public long ShipmentId { get; set; }
    [Column("old_status")] public string? OldStatus { get; set; }
    [Column("new_status")] public string NewStatus { get; set; } = string.Empty;
    [Column("changed_by_user_id")] public long ChangedByUserId { get; set; }
    [Column("note")] public string? Note { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}

[Table("order_status_histories")]
public sealed class OrderStatusHistory
{
    [Key, Column("history_id")] public long HistoryId { get; set; }
    [Column("store_order_id")] public long StoreOrderId { get; set; }
    [Column("old_status")] public string? OldStatus { get; set; }
    [Column("new_status")] public string NewStatus { get; set; } = string.Empty;
    [Column("changed_by_user_id")] public long ChangedByUserId { get; set; }
    [Column("note")] public string? Note { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}

[Table("audit_logs")]
public sealed class AuditLog
{
    [Key, Column("audit_id")] public long AuditId { get; set; }
    [Column("actor_user_id")] public long? ActorUserId { get; set; }
    [Column("action")] public string Action { get; set; } = string.Empty;
    [Column("entity_name")] public string EntityName { get; set; } = string.Empty;
    [Column("entity_id")] public string? EntityId { get; set; }
    [Column("before_json")] public string? BeforeJson { get; set; }
    [Column("after_json")] public string? AfterJson { get; set; }
    [Column("ip_address")] public string? IpAddress { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}
