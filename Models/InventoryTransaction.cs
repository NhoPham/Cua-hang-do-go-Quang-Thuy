namespace QLBH.Models;

public class InventoryTransaction
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int QuantityChanged { get; set; }

    public int QuantityAfter { get; set; }

    public string Type { get; set; } = string.Empty;

    public string? Note { get; set; }

    public string? ReferenceCode { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
