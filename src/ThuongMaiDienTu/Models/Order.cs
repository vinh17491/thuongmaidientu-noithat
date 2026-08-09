using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Table("orders")]
[Index("OrderStatus", Name = "IX_orders_status")]
[Index("UserId", "CreatedAt", Name = "IX_orders_user_created", IsDescending = new[] { false, true })]
[Index("OrderCode", Name = "UQ__orders__99D12D3FE04391DD", IsUnique = true)]
public partial class Order
{
    [Key]
    [Column("order_id")]
    public long OrderId { get; set; }

    [Column("order_code")]
    [StringLength(30)]
    [Unicode(false)]
    public string OrderCode { get; set; } = null!;

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("receiver_name")]
    [StringLength(120)]
    public string ReceiverName { get; set; } = null!;

    [Column("receiver_phone")]
    [StringLength(20)]
    [Unicode(false)]
    public string ReceiverPhone { get; set; } = null!;

    [Column("shipping_address")]
    [StringLength(300)]
    public string ShippingAddress { get; set; } = null!;

    [Column("subtotal", TypeName = "decimal(18, 2)")]
    public decimal Subtotal { get; set; }

    [Column("discount_amount", TypeName = "decimal(18, 2)")]
    public decimal DiscountAmount { get; set; }

    [Column("shipping_fee", TypeName = "decimal(18, 2)")]
    public decimal ShippingFee { get; set; }

    [Column("total_amount", TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; }

    [Column("payment_method")]
    [StringLength(20)]
    [Unicode(false)]
    public string PaymentMethod { get; set; } = null!;

    [Column("payment_status")]
    [StringLength(20)]
    [Unicode(false)]
    public string PaymentStatus { get; set; } = null!;

    [Column("order_status")]
    [StringLength(30)]
    [Unicode(false)]
    public string OrderStatus { get; set; } = null!;

    [Column("note")]
    [StringLength(500)]
    public string? Note { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Order")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [ForeignKey("UserId")]
    [InverseProperty("Orders")]
    public virtual User User { get; set; } = null!;
}
