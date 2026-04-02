/*
    Script mở rộng DB cho các tính năng:
    - Quên mật khẩu
    - Giỏ hàng
    - Thanh toán / đơn hàng
    - Theo dõi đơn hàng
    - Kho
    - Thống kê
*/

IF OBJECT_ID('dbo.PasswordResetToken', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetToken
    (
        id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        token NVARCHAR(200) NOT NULL UNIQUE,
        created_at DATETIME2 NOT NULL,
        expires_at DATETIME2 NOT NULL,
        used_at DATETIME2 NULL,
        CONSTRAINT FK_PasswordResetToken_User FOREIGN KEY (user_id) REFERENCES dbo.[User](id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.CartItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CartItem
    (
        id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        product_id INT NOT NULL,
        quantity INT NOT NULL,
        unit_price DECIMAL(18,2) NOT NULL,
        created_at DATETIME2 NOT NULL,
        CONSTRAINT UQ_CartItem_User_Product UNIQUE (user_id, product_id),
        CONSTRAINT FK_CartItem_User FOREIGN KEY (user_id) REFERENCES dbo.[User](id) ON DELETE CASCADE,
        CONSTRAINT FK_CartItem_Product FOREIGN KEY (product_id) REFERENCES dbo.Product(id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.OrderHeader', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderHeader
    (
        id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        order_code NVARCHAR(100) NOT NULL UNIQUE,
        receiver_name NVARCHAR(255) NOT NULL,
        email NVARCHAR(255) NOT NULL,
        phone NVARCHAR(50) NOT NULL,
        shipping_address NVARCHAR(500) NOT NULL,
        note NVARCHAR(1000) NULL,
        payment_method NVARCHAR(50) NOT NULL,
        payment_status NVARCHAR(50) NOT NULL,
        order_status NVARCHAR(50) NOT NULL,
        total_amount DECIMAL(18,2) NOT NULL,
        created_at DATETIME2 NOT NULL,
        updated_at DATETIME2 NULL,
        CONSTRAINT FK_OrderHeader_User FOREIGN KEY (user_id) REFERENCES dbo.[User](id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.OrderItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItem
    (
        id INT IDENTITY(1,1) PRIMARY KEY,
        order_id INT NOT NULL,
        product_id INT NOT NULL,
        product_name NVARCHAR(255) NOT NULL,
        unit_price DECIMAL(18,2) NOT NULL,
        quantity INT NOT NULL,
        subtotal DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_OrderItem_OrderHeader FOREIGN KEY (order_id) REFERENCES dbo.OrderHeader(id) ON DELETE CASCADE,
        CONSTRAINT FK_OrderItem_Product FOREIGN KEY (product_id) REFERENCES dbo.Product(id)
    );
END
GO

IF OBJECT_ID('dbo.InventoryTransaction', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryTransaction
    (
        id INT IDENTITY(1,1) PRIMARY KEY,
        product_id INT NOT NULL,
        quantity_changed INT NOT NULL,
        quantity_after INT NOT NULL,
        type NVARCHAR(50) NOT NULL,
        note NVARCHAR(500) NULL,
        reference_code NVARCHAR(100) NULL,
        created_at DATETIME2 NOT NULL,
        CONSTRAINT FK_InventoryTransaction_Product FOREIGN KEY (product_id) REFERENCES dbo.Product(id) ON DELETE CASCADE
    );
END
GO
