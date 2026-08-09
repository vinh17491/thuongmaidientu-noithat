using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Keyless]
public partial class VwProductCatalog
{
    [Column("product_id")]
    public long ProductId { get; set; }

    [Column("product_name")]
    [StringLength(200)]
    public string ProductName { get; set; } = null!;

    [Column("slug")]
    [StringLength(220)]
    [Unicode(false)]
    public string Slug { get; set; } = null!;

    [Column("brand")]
    [StringLength(100)]
    public string? Brand { get; set; }

    [Column("short_description")]
    [StringLength(500)]
    public string? ShortDescription { get; set; }

    [Column("category_id")]
    public long CategoryId { get; set; }

    [Column("category_name")]
    [StringLength(150)]
    public string CategoryName { get; set; } = null!;

    [Column("store_id")]
    public long StoreId { get; set; }

    [Column("store_name")]
    [StringLength(150)]
    public string StoreName { get; set; } = null!;

    [Column("sku_id")]
    public long SkuId { get; set; }

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

    [Column("display_price", TypeName = "decimal(18, 2)")]
    public decimal? DisplayPrice { get; set; }

    [Column("discount_percent")]
    public int? DiscountPercent { get; set; }

    [Column("stock_quantity")]
    public int StockQuantity { get; set; }

    [Column("image_url")]
    [StringLength(500)]
    [Unicode(false)]
    public string? ImageUrl { get; set; }

    [Column("alt_text")]
    [StringLength(180)]
    public string? AltText { get; set; }

    [Column("average_rating", TypeName = "decimal(38, 6)")]
    public decimal AverageRating { get; set; }

    [Column("review_count")]
    public int ReviewCount { get; set; }
}
