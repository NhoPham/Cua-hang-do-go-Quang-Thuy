namespace QLBH.ViewModels;

public class CartLineViewModel
{
    public int CartItemId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductImage { get; set; } = "/images/default/no-image.jpg";

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public int MaxStock { get; set; }

    public decimal SubTotal => UnitPrice * Quantity;
}
