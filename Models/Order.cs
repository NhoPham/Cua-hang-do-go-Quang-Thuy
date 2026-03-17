namespace QLBH.Models;

public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public string ReceiverName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string ShippingAddress { get; set; } = string.Empty;

    public string? Note { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public string OrderStatus { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
