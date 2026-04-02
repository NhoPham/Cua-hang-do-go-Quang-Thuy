using QLBH.Models;

namespace QLBH.ViewModels;

public class TrackOrderViewModel
{
    public string? OrderCode { get; set; }

    public string? Email { get; set; }

    public bool HasSearched { get; set; }

    public Order? Order { get; set; }
}
