using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Table("order_items")]
[Index("OrderId", Name = "IX_order_items_order")]
public partial class OrderItem
{
    [Key]
    [Column("order_item_id")]
    public long OrderItemId { get; set; }

    [Column("order_id")]
    public long OrderId { get; set; }

    [Column("store_order_id")]
    public long? StoreOrderId { get; set; }

    [Column("store_name_snapshot")]
    [StringLength(200)]
    public string? StoreNameSnapshot { get; set; }

    [Column("sku_id")]
    public long SkuId { get; set; }

    [Column("product_name")]
    [StringLength(200)]
    public string ProductName { get; set; } = null!;

    [Column("sku_code")]
    [StringLength(80)]
    [Unicode(false)]
    public string SkuCode { get; set; } = null!;

    [Column("unit_price", TypeName = "decimal(18, 2)")]
    public decimal UnitPrice { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("line_total", TypeName = "decimal(29, 2)")]
    public decimal? LineTotal { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("OrderItems")]
    public virtual Order Order { get; set; } = null!;

    public virtual StoreOrder? StoreOrder { get; set; }

    [InverseProperty("OrderItem")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [ForeignKey("SkuId")]
    [InverseProperty("OrderItems")]
    public virtual ProductSku Sku { get; set; } = null!;
}
