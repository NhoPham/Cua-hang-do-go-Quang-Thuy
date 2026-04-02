using System.ComponentModel.DataAnnotations;
using QLBH.Utils;

namespace QLBH.ViewModels;

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Bạn chưa nhập tên người nhận.")]
    [Display(Name = "Người nhận")]
    public string ReceiverName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bạn chưa nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bạn chưa nhập số điện thoại.")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bạn chưa nhập địa chỉ giao hàng.")]
    [Display(Name = "Địa chỉ giao hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [Required(ErrorMessage = "Bạn chưa chọn phương thức thanh toán.")]
    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; } = PaymentMethods.CashOnDelivery;

    public List<CartLineViewModel> Items { get; set; } = new();

    public decimal TotalAmount => Items.Sum(x => x.SubTotal);
}
