namespace QLBH.ViewModels;

public class CartViewModel
{
    public List<CartLineViewModel> Items { get; set; } = new();

    public decimal TotalAmount => Items.Sum(x => x.SubTotal);
}
