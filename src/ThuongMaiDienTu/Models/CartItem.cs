using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Table("cart_items")]
[Index("CartId", Name = "IX_cart_items_cart")]
[Index("CartId", "SkuId", Name = "UQ_cart_items_cart_sku", IsUnique = true)]
public partial class CartItem
{
    [Key]
    [Column("cart_item_id")]
    public long CartItemId { get; set; }

    [Column("cart_id")]
    public long CartId { get; set; }

    [Column("sku_id")]
    public long SkuId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; }

    [ForeignKey("CartId")]
    [InverseProperty("CartItems")]
    public virtual Cart Cart { get; set; } = null!;

    [ForeignKey("SkuId")]
    [InverseProperty("CartItems")]
    public virtual ProductSku Sku { get; set; } = null!;
}
