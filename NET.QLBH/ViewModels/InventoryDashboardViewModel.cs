using QLBH.Models;

namespace QLBH.ViewModels;

public class InventoryDashboardViewModel
{
    public List<InventoryProductRowViewModel> Products { get; set; } = new();

    public List<InventoryTransaction> RecentTransactions { get; set; } = new();

    public InventoryAdjustmentViewModel Adjustment { get; set; } = new();
}
