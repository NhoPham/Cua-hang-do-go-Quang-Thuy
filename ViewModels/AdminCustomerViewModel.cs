namespace QLBH.ViewModels;

public class AdminCustomerViewModel
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "customer";

    public int OrderCount { get; set; }

    public decimal TotalSpent { get; set; }
}