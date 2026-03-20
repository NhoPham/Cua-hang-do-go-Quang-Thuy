## Nếu muốn nâng một tài khoản thành admin, chạy SQL:
```sql
UPDATE [User]
SET [role] = 'admin'
WHERE [email] = 'email-cua-ban@gmail.com';
```

## Ghi chú bảo mật
- Mật khẩu mới sẽ được lưu theo định dạng hash PBKDF2.
- Nếu database cũ đang có tài khoản lưu plaintext, hệ thống vẫn cho đăng nhập và sẽ tự đổi sang hash sau lần đăng nhập đầu tiên.

## dùng EF Core Migration
```bash
dotnet ef migrations add EcommerceFeatures
dotnet ef database update
```
## Chạy ứng dụng
```CMD
  "dotnet watch run"
```

## Cấu hình SMTP để quên mật khẩu gửi mail thật
Thêm vào `appsettings.json`:
```json
"SmtpSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Username": "your_email@gmail.com",
  "Password": "app_password",
  "EnableSsl": true,
  "FromEmail": "your_email@gmail.com",
  "FromName": "Đồ Gỗ Quảng Thủy"
}
```
