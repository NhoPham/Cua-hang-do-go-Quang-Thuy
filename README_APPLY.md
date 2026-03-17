# Cập nhật chức năng đăng nhập / đăng ký cho QLBH

## Repo hiện tại đang có gì
- Dự án đang là ASP.NET Core MVC + Entity Framework Core.
- Trong `Models/QLBHContext.cs` đã có `DbSet<User>` và model `User` gồm: `Username`, `Email`, `Password`, `Role`.
- `Program.cs` hiện chưa cấu hình Authentication nên chưa thể đăng nhập thực sự.
- `ProductController.cs` hiện chưa chặn quyền quản trị.

## Các file trong gói này
### File mới
- `Controllers/AccountController.cs`
- `ViewModels/LoginViewModel.cs`
- `ViewModels/RegisterViewModel.cs`
- `Utils/PasswordHelper.cs`
- `Views/Account/Login.cshtml`
- `Views/Account/Register.cshtml`
- `Views/Account/AccessDenied.cshtml`

### File cần thay thế
- `Program.cs`
- `Controllers/ProductController.cs`
- `Views/Shared/_Layout.cshtml`

## Cách áp dụng
1. Chép đúng các file vào project của bạn.
2. Build lại project:
   ```bash
   dotnet build
   ```
3. Chạy project:
   ```bash
   dotnet watch run
   ```

## Quyền tài khoản
- Tài khoản đăng ký mới mặc định có `role = customer`
- Chỉ tài khoản `admin` mới được thêm/sửa/xóa sản phẩm

Nếu muốn nâng một tài khoản thành admin, chạy SQL:
```sql
UPDATE [User]
SET [role] = 'admin'
WHERE [email] = 'email-cua-ban@gmail.com';
```

## Ghi chú bảo mật
- Mật khẩu mới sẽ được lưu theo định dạng hash PBKDF2.
- Nếu database cũ đang có tài khoản lưu plaintext, hệ thống vẫn cho đăng nhập và sẽ tự đổi sang hash sau lần đăng nhập đầu tiên.

## Nếu bạn muốn đẹp hơn nữa
Bạn có thể sửa tiếp `Views/Product/Index.cshtml` để ẩn các nút quản trị với đoạn điều kiện:
```cshtml
@if (User.IsInRole("admin"))
{
    <!-- nút thêm/xóa/load sản phẩm -->
}
```
