using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace QLBH.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    [ValidateNever]
    public string Images { get; set; } = "[]";

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public bool IsDeleted { get; set; } = false;

    public int? CategoryId { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}