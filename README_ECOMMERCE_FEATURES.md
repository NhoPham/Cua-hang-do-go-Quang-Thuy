# Gói mở rộng tính năng cho repo `Cua-hang-do-go-Quang-Thuy`

## Các tính năng đã thêm
- Quên mật khẩu bằng token reset
- Giỏ hàng theo user đăng nhập
- Thanh toán / tạo đơn hàng
- Theo dõi đơn hàng theo tài khoản và tra cứu theo mã đơn + email
- Quản lý kho
- Thống kê bán hàng

## Cách áp dụng
Copy toàn bộ file trong gói này vào đúng path tương ứng của repo gốc.

## Cập nhật database
### Cách 1: dùng script SQL
Chạy file `sql/upgrade_ecommerce_features.sql`

### Cách 2: dùng EF Core Migration
```bash
dotnet ef migrations add EcommerceFeatures
dotnet ef database update
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

Nếu chưa cấu hình SMTP, chức năng quên mật khẩu vẫn tạo token và hiện `debug reset link` trên giao diện để test local.
