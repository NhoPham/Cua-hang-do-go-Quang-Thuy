using System.ComponentModel.DataAnnotations;

namespace QLBH.ViewModels;

public class InventoryAdjustmentViewModel
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int Quantity { get; set; }

    [Required]
    public string Type { get; set; } = "IMPORT";

    public string? Note { get; set; }
}
