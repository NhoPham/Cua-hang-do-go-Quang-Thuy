IF OBJECT_ID(N'dbo.CustomOrderRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomOrderRequest
    (
        id INT IDENTITY(1,1) PRIMARY KEY,
        request_code NVARCHAR(100) NOT NULL,
        user_id INT NULL,
        product_id INT NULL,
        customer_name NVARCHAR(255) NOT NULL,
        email NVARCHAR(255) NOT NULL,
        phone NVARCHAR(50) NOT NULL,
        requested_product_name NVARCHAR(255) NOT NULL,
        wood_type NVARCHAR(255) NULL,
        dimensions NVARCHAR(255) NULL,
        quantity INT NOT NULL DEFAULT 1,
        estimated_budget DECIMAL(18,2) NULL,
        desired_delivery_date DATETIME NULL,
        description NVARCHAR(2000) NOT NULL,
        reference_image_urls NVARCHAR(2000) NULL,
        status NVARCHAR(50) NOT NULL DEFAULT N'new',
        admin_note NVARCHAR(1000) NULL,
        created_at DATETIME NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME NULL
    );

    CREATE UNIQUE INDEX IX_CustomOrderRequest_request_code ON dbo.CustomOrderRequest(request_code);
    CREATE INDEX IX_CustomOrderRequest_status ON dbo.CustomOrderRequest(status);

    ALTER TABLE dbo.CustomOrderRequest
        ADD CONSTRAINT FK_CustomOrderRequest_User
        FOREIGN KEY (user_id) REFERENCES dbo.[User](id);

    ALTER TABLE dbo.CustomOrderRequest
        ADD CONSTRAINT FK_CustomOrderRequest_Product
        FOREIGN KEY (product_id) REFERENCES dbo.Product(id);
END
