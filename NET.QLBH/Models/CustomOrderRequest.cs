namespace QLBH.Models;

public class CustomOrderRequest
{
    public int Id { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public int? ProductId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public string RequestedProductName { get; set; } = string.Empty;
    public string? WoodType { get; set; }
    public string? Dimensions { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? EstimatedBudget { get; set; }
    public DateTime? DesiredDeliveryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceImageUrls { get; set; }

    public string Status { get; set; } = "new";
    public string? AdminNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual User? User { get; set; }
    public virtual Product? Product { get; set; }
}
