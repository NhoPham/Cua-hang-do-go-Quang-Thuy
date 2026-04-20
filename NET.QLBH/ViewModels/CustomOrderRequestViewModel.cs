using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace QLBH.ViewModels;

public class CustomOrderRequestViewModel
{
    public int? ProductId { get; set; }

    [Required(ErrorMessage = "Bạn chưa nhập họ và tên.")]
    [Display(Name = "Họ và tên")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bạn chưa nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bạn chưa nhập số điện thoại.")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bạn chưa nhập tên sản phẩm mong muốn.")]
    [Display(Name = "Tên sản phẩm mong muốn")]
    public string RequestedProductName { get; set; } = string.Empty;

    [Display(Name = "Loại gỗ mong muốn")]
    public string? WoodType { get; set; }

    [Display(Name = "Kích thước mong muốn")]
    public string? Dimensions { get; set; }

    [Range(1, 999, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    [Display(Name = "Số lượng")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Ngân sách dự kiến")]
    [Range(0, 999999999, ErrorMessage = "Ngân sách không hợp lệ.")]
    public decimal? EstimatedBudget { get; set; }

    [Display(Name = "Ngày mong muốn nhận hàng")]
    public DateTime? DesiredDeliveryDate { get; set; }

    [Required(ErrorMessage = "Bạn chưa nhập mô tả yêu cầu.")]
    [Display(Name = "Mô tả chi tiết")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Link ảnh tham khảo (nếu có, nhiều link cách nhau bằng dấu phẩy hoặc xuống dòng)")]
    public string? ReferenceImageUrls { get; set; }

    [Display(Name = "Tải ảnh tham khảo từ máy")]
    public List<IFormFile>? ReferenceImages { get; set; }

    public string? ProductName { get; set; }
    public string? ProductImage { get; set; }
}
