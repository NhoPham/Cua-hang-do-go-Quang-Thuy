# 📊 Diagrams - Hệ thống Quản lý Bán hàng Đồ Gỗ Quảng Thủy

## 📁 Danh sách các tệp Diagram

Thư mục này chứa các diagram PlantUML mô tả kiến trúc và quy trình làm việc của hệ thống.

### 1. **usecase_diagram.puml** - Use Case Diagram
Mô tả tất cả các chức năng chính của hệ thống từ góc nhìn người dùng.

**Các actor chính:**
- 👤 **Khách chưa đăng nhập (Guest)** - Có thể duyệt sản phẩm, đăng ký, quên mật khẩu
- 👤 **Khách đã đăng nhập (Customer)** - Mua hàng, quản lý giỏ, xem đơn hàng
- 👨‍💼 **Quản trị viên (Admin)** - Quản lý toàn bộ hệ thống

**Các chức năng chính:**
- 📦 **Quản lý Sản phẩm:** Duyệt, tìm kiếm, lọc, xem chi tiết
- 🔐 **Quản lý Tài khoản:** Đăng ký, đăng nhập, đặt lại mật khẩu
- 🛒 **Quản lý Giỏ hàng:** Thêm, sửa, xóa sản phẩm
- 💳 **Quản lý Đơn hàng:** Thanh toán, xem lịch sử, theo dõi
- 👨‍💼 **Admin - Sản phẩm:** Thêm, sửa, xóa, nạp dữ liệu
- 📋 **Admin - Đơn hàng:** Xem, cập nhật trạng thái
- 📊 **Admin - Tồn kho:** Quản lý, điều chỉnh, xem lịch sử
- 📈 **Admin - Thống kê:** Doanh số, sản phẩm bán chạy, tồn kho thấp

---

### 2. **sequence_checkout.puml** - Sequence Diagram: Thanh toán / Đặt hàng
Mô tả chi tiết quy trình khi khách hàng thanh toán một đơn hàng.

**Các bước chính:**
1. Khách hàng đã đăng nhập bấm nút "Thanh toán"
2. Hệ thống kiểm tra:
   - ✅ Giỏ hàng không trống
   - ✅ Sản phẩm còn stock
   - ✅ Dữ liệu hợp lệ
3. Tạo đơn hàng trong cơ sở dữ liệu:
   - Tạo Order
   - Tạo OrderItems
   - Cập nhật Stock sản phẩm
   - Ghi lịch sử InventoryTransaction
   - Xóa CartItems
4. Nếu có lỗi → RollBack toàn bộ, hiển thị thông báo lỗi
5. Thành công → Chuyển hướng tới trang xác nhận đơn hàng

**Công nghệ:**
- ✅ Sử dụng **Transaction** để đảm bảo tính toàn vẹn dữ liệu

---

### 3. **sequence_login.puml** - Sequence Diagram: Đăng nhập
Mô tả quy trình xác thực người dùng khi đăng nhập.

**Các bước chính:**
1. Khách nhập Username/Email + Password
2. Hệ thống:
   - Tìm user trong DB
   - So sánh password (hash hoặc legacy plain text)
   - Nếu là plain text → Hash lại (migration)
   - Tạo Claims Principal (UserId, Username, Email, Role)
   - Ghi cookie xác thực (remember me: 7 ngày hoặc khi đóng trình duyệt)
3. Chuyển hướng về trang yêu cầu hoặc trang chủ

**Bảo mật:**
- 🔐 Sử dụng **Cookie Authentication**
- 🔐 Password hash an toàn

---

### 4. **sequence_add_to_cart.puml** - Sequence Diagram: Thêm sản phẩm vào giỏ
Mô tả quy trình thêm sản phẩm vào giỏ hàng.

**Các bước chính:**
1. Khách chọn sản phẩm + số lượng
2. Hệ thống:
   - ✅ Kiểm tra stock sản phẩm
   - ✅ Nếu sản phẩm chưa có trong giỏ → Tạo CartItem mới
   - ✅ Nếu sản phẩm đã có → Cộng số lượng (capped tại stock có sẵn)
3. Thành công → Quay lại trang sản phẩm + thông báo

**Đặc điểm:**
- 🛒 Tự động merge nếu sản phẩm đã trong giỏ
- 🛒 Giới hạn số lượng theo stock

---

### 5. **sequence_admin_update_order.puml** - Sequence Diagram: Admin cập nhật đơn hàng
Mô tả quy trình Admin cập nhật trạng thái đơn hàng.

**Các bước chính:**
1. Admin xem danh sách đơn → Chọn đơn để xem chi tiết
2. Admin chọn trạng thái mới (Pending → Confirmed → Delivered → Completed)
3. Hệ thống:
   - ✅ Validate trạng thái mới hợp lệ
   - ✅ Kiểm tra: Đơn hủy không thể quay lại trạng thái khác
   - 🔄 Nếu hủy đơn → Hoàn lại toàn bộ stock sản phẩm
   - 📝 Ghi lịch sử InventoryTransaction
4. Update DB + Commit
5. Chuyển hướng về chi tiết đơn với thông báo thành công

**Đặc điểm:**
- 🔄 Tự động đảo ngược stock khi hủy đơn
- 📝 Ghi chép lịch sử chi tiết

---

## 🚀 Cách xem Diagram

### Tùy chọn 1: Online PlantUML Editor
1. Truy cập: https://www.plantuml.com/plantuml
2. Dán nội dung file `.puml`
3. Click "Render"

### Tùy chọn 2: VSCode Extension
1. Cài đặt extension: **PlantUML** (jebbs.plantuml)
2. Mở file `.puml`
3. Right-click → "Preview Current Diagram"

### Tùy chọn 3: Command Line
```bash
# Cài PlantUML
brew install plantuml  # macOS
# hoặc apt install plantuml # Linux

# Render sang PNG
plantuml diagrams/usecase_diagram.puml -o output/
```

---

## 📊 Tóm tắt Diagram

| Diagram | Mục đích | Actor | Dòng chú |
|---------|---------|-------|---------|
| **UseCase** | Tổng quan chức năng | Guest, Customer, Admin | 3 actor chính, 18+ use case |
| **Checkout** | Đặt hàng & thanh toán | Customer | Transaction, Inventory sync |
| **Login** | Xác thực người dùng | Guest → Customer | Cookie + Claims auth |
| **AddCart** | Thêm vào giỏ | Customer | Auto-merge, stock check |
| **AdminOrderStatus** | Quản lý đơn hàng | Admin | Auto-reverse stock |

---

## 🔗 Liên hệ giữa các Diagram

```
UseCase Diagram
    ├── Checkout (sequence_checkout.puml)
    ├── AddToCart (sequence_add_to_cart.puml)
    ├── Login (sequence_login.puml)
    └── AdminUpdateOrder (sequence_admin_update_order.puml)
```

---

## 📝 Ghi chú quan trọng

1. **Xác thực:** Tất cả routes (trừ Product.Index, Login, Register) yêu cầu Authorize
2. **Admin:** Yêu cầu role = "admin"
3. **Transaction:** Checkout và UpdateOrder sử dụng database transaction để đảm bảo atomicity
4. **Stock:** Tự động cập nhật khi checkout hoặc hủy đơn
5. **Inventory:** Ghi lịch sử chi tiết mỗi thay đổi stock

---

**Generated:** 2026-03-17  
**System:** Hệ thống Quản lý Bán hàng Đồ Gỗ Quảng Thủy
