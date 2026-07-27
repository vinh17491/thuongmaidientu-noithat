using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Models;

[Keyless]
public partial class VwRevenueByMonth
{
    [Column("revenue_year")]
    public int? RevenueYear { get; set; }

    [Column("revenue_month")]
    public int? RevenueMonth { get; set; }

    [Column("total_orders")]
    public int? TotalOrders { get; set; }

    [Column("total_revenue", TypeName = "decimal(38, 2)")]
    public decimal? TotalRevenue { get; set; }
}
