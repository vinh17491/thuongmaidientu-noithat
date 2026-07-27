using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Keyless]
public partial class VwBestSellingProduct
{
    [Column("sku_id")]
    public long SkuId { get; set; }

    [Column("product_name")]
    [StringLength(200)]
    public string ProductName { get; set; } = null!;

    [Column("total_quantity_sold")]
    public int? TotalQuantitySold { get; set; }

    [Column("total_revenue", TypeName = "decimal(38, 2)")]
    public decimal? TotalRevenue { get; set; }
}
