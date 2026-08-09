using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Table("product_skus")]
[Index("ProductId", "Status", Name = "IX_product_skus_product_status")]
[Index("SkuCode", Name = "UQ__product___843F428F16D46CB8", IsUnique = true)]
public partial class ProductSku
{
    [Key]
    [Column("sku_id")]
    public long SkuId { get; set; }

    [Column("product_id")]
    public long ProductId { get; set; }

    [Column("sku_code")]
    [StringLength(80)]
    [Unicode(false)]
    public string SkuCode { get; set; } = null!;

    [Column("price", TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }

    [Column("sale_price", TypeName = "decimal(18, 2)")]
    public decimal? SalePrice { get; set; }

    [Column("sale_start")]
    public DateTime? SaleStart { get; set; }

    [Column("sale_end")]
    public DateTime? SaleEnd { get; set; }

    [Column("stock_quantity")]
    public int StockQuantity { get; set; }

    [Column("status")]
    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [InverseProperty("Sku")]
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [InverseProperty("Sku")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [ForeignKey("ProductId")]
    [InverseProperty("ProductSkus")]
    public virtual Product Product { get; set; } = null!;
}
