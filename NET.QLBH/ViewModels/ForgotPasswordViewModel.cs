using System.ComponentModel.DataAnnotations;

namespace QLBH.ViewModels;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Bạn chưa nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;
}
