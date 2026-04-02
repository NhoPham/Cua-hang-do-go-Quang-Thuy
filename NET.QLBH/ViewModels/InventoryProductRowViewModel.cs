namespace QLBH.ViewModels;

public class InventoryProductRowViewModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public int SoldQuantity { get; set; }

    public bool IsLowStock { get; set; }
}
