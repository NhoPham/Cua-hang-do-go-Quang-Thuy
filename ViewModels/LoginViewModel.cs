using System.ComponentModel.DataAnnotations;

namespace QLBH.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Bạn chưa nhập tên đăng nhập hoặc email.")]
    [Display(Name = "Tên đăng nhập hoặc email")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bạn chưa nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }
}
