namespace QLBH.ViewModels;

public class OrderHistoryIndexViewModel
{
    public List<OrderHistoryItemViewModel> Items { get; set; } = new();
}

public class OrderHistoryItemViewModel
{
    public bool IsCustomOrder { get; set; }
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = "bg-secondary";
    public string? PaymentStatusLabel { get; set; }
    public string? PaymentBadgeClass { get; set; }
    public decimal? TotalAmount { get; set; }
    public int Quantity { get; set; }
}
