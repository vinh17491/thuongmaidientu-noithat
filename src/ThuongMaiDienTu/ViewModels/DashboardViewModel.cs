namespace ThuongMaiDienTu.ViewModels;

public class DashboardViewModel
{
    public int TotalProducts { get; set; }

    public int TotalCustomers { get; set; }

    public int TotalOrders { get; set; }

    public decimal TotalRevenue { get; set; }

    public IReadOnlyList<DashboardOrderStatusViewModel> OrdersByStatus { get; set; } = [];

    public IReadOnlyList<DashboardTopProductViewModel> TopSellingProducts { get; set; } = [];

    public IReadOnlyList<DashboardRevenueMonthViewModel> RevenueByMonth { get; set; } = [];

    public IReadOnlyList<DashboardLowStockViewModel> LowStockProducts { get; set; } = [];
}

public class DashboardOrderStatusViewModel
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class DashboardTopProductViewModel
{
    public long SkuId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int TotalQuantitySold { get; set; }

    public decimal TotalRevenue { get; set; }
}

public class DashboardRevenueMonthViewModel
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int TotalOrders { get; set; }

    public decimal TotalRevenue { get; set; }
}

public class DashboardLowStockViewModel
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SkuCode { get; set; } = string.Empty;

    public int StockQuantity { get; set; }
}
