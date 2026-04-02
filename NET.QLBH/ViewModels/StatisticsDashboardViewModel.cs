namespace QLBH.ViewModels;

public class StatisticsDashboardViewModel
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public int TotalOrders { get; set; }

    public int PendingOrders { get; set; }

    public int CompletedOrders { get; set; }

    public int TotalUnitsSold { get; set; }

    public decimal TotalRevenue { get; set; }

    public List<StatisticsTopProductRow> TopProducts { get; set; } = new();

    public List<StatisticsDailyRevenueRow> RevenueByDay { get; set; } = new();

    public List<StatisticsStatusRow> StatusBreakdown { get; set; } = new();

    public List<StatisticsLowStockRow> LowStockProducts { get; set; } = new();
}

public class StatisticsTopProductRow
{
    public string ProductName { get; set; } = string.Empty;

    public int QuantitySold { get; set; }

    public decimal Revenue { get; set; }
}

public class StatisticsDailyRevenueRow
{
    public DateTime Date { get; set; }

    public int Orders { get; set; }

    public decimal Revenue { get; set; }
}

public class StatisticsStatusRow
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class StatisticsLowStockRow
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Stock { get; set; }
}
