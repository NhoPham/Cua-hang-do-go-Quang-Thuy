using Microsoft.EntityFrameworkCore;

namespace QLBH.Models;

public partial class QlbhContext : DbContext
{
    public QlbhContext()
    {
    }

    public QlbhContext(DbContextOptions<QlbhContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories => Set<Category>();

    public virtual DbSet<Product> Products => Set<Product>();

    public virtual DbSet<User> Users => Set<User>();

    public virtual DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public virtual DbSet<CartItem> CartItems => Set<CartItem>();

    public virtual DbSet<Order> Orders => Set<Order>();

    public virtual DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public virtual DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public virtual DbSet<CustomOrderRequest> CustomOrderRequests { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Category__3213E83FE468FFE2");

            entity.ToTable("Category");

            entity.HasIndex(e => e.Name, "UQ__Category__72E12F1B9315DDD9").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Description)
                .HasColumnName("description");

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Product__3213E83F1064F91F");

            entity.ToTable("Product");

            entity.HasIndex(e => e.CategoryId, "IX_Product_category_id");

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.CategoryId)
                .HasColumnName("category_id");

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            entity.Property(e => e.Images)
                .HasColumnName("images");

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");

            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");

            entity.Property(e => e.Stock)
                .HasColumnName("stock");

            entity.Property(e => e.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);

            entity.HasOne(d => d.Category)
                .WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Product__categor__3C69FB99");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3213E83F1F53BD0F");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__AB6E6164EF7B18C9").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__User__F3DBC5727EB7D181").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");

            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");

            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValue("customer")
                .HasColumnName("role");

            entity.Property(e => e.Username)
                .HasMaxLength(255)
                .HasColumnName("username");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetToken");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Token).IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.Token)
                .HasColumnName("token")
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");

            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");

            entity.Property(e => e.UsedAt)
                .HasColumnName("used_at");

            entity.HasOne(d => d.User)
                .WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItem");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.ProductId)
                .HasColumnName("product_id");

            entity.Property(e => e.Quantity)
                .HasColumnName("quantity");

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("unit_price");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");

            entity.HasOne(d => d.User)
                .WithMany(p => p.CartItems)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("OrderHeader");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.OrderCode).IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.OrderCode)
                .HasColumnName("order_code")
                .HasMaxLength(100);

            entity.Property(e => e.ReceiverName)
                .HasColumnName("receiver_name")
                .HasMaxLength(255);

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(255);

            entity.Property(e => e.Phone)
                .HasColumnName("phone")
                .HasMaxLength(50);

            entity.Property(e => e.ShippingAddress)
                .HasColumnName("shipping_address")
                .HasMaxLength(500);

            entity.Property(e => e.Note)
                .HasColumnName("note")
                .HasMaxLength(1000);

            entity.Property(e => e.PaymentMethod)
                .HasColumnName("payment_method")
                .HasMaxLength(50);

            entity.Property(e => e.PaymentStatus)
                .HasColumnName("payment_status")
                .HasMaxLength(50);

            entity.Property(e => e.OrderStatus)
                .HasColumnName("order_status")
                .HasMaxLength(50);

            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItem");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.OrderId)
                .HasColumnName("order_id");

            entity.Property(e => e.ProductId)
                .HasColumnName("product_id");

            entity.Property(e => e.ProductName)
                .HasColumnName("product_name")
                .HasMaxLength(255);

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("unit_price");

            entity.Property(e => e.Quantity)
                .HasColumnName("quantity");

            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("subtotal");

            entity.HasOne(d => d.Order)
                .WithMany(p => p.Items)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("InventoryTransaction");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.ProductId)
                .HasColumnName("product_id");

            entity.Property(e => e.QuantityChanged)
                .HasColumnName("quantity_changed");

            entity.Property(e => e.QuantityAfter)
                .HasColumnName("quantity_after");

            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasMaxLength(50);

            entity.Property(e => e.Note)
                .HasColumnName("note")
                .HasMaxLength(500);

            entity.Property(e => e.ReferenceCode)
                .HasColumnName("reference_code")
                .HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomOrderRequest>(entity =>
        {
            entity.ToTable("CustomOrderRequest");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.RequestCode).IsUnique();

            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.RequestCode)
                .HasColumnName("request_code")
                .HasMaxLength(100);

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.ProductId)
                .HasColumnName("product_id");

            entity.Property(e => e.CustomerName)
                .HasColumnName("customer_name")
                .HasMaxLength(255);

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(255);

            entity.Property(e => e.Phone)
                .HasColumnName("phone")
                .HasMaxLength(50);

            entity.Property(e => e.RequestedProductName)
                .HasColumnName("requested_product_name")
                .HasMaxLength(255);

            entity.Property(e => e.WoodType)
                .HasColumnName("wood_type")
                .HasMaxLength(255);

            entity.Property(e => e.Dimensions)
                .HasColumnName("dimensions")
                .HasMaxLength(255);

            entity.Property(e => e.Quantity)
                .HasColumnName("quantity");

            entity.Property(e => e.EstimatedBudget)
                .HasColumnName("estimated_budget")
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.DesiredDeliveryDate)
                .HasColumnName("desired_delivery_date");

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(2000);

            entity.Property(e => e.ReferenceImageUrls)
                .HasColumnName("reference_image_urls")
                .HasMaxLength(2000);

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(50);

            entity.Property(e => e.AdminNote)
                .HasColumnName("admin_note")
                .HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}