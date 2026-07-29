using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Models;

namespace ThuongMaiDienTu.Data;

public partial class ThuongMaiDienTuDbContext : DbContext
{
    public ThuongMaiDienTuDbContext(DbContextOptions<ThuongMaiDienTuDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }
    public virtual DbSet<StoreOrder> StoreOrders { get; set; }
    public virtual DbSet<ShippingProvider> ShippingProviders { get; set; }
    public virtual DbSet<ShippingService> ShippingServices { get; set; }
    public virtual DbSet<ShippingRateRule> ShippingRateRules { get; set; }
    public virtual DbSet<ShippingQuote> ShippingQuotes { get; set; }
    public virtual DbSet<Shipment> Shipments { get; set; }
    public virtual DbSet<ShipmentStatusHistory> ShipmentStatusHistories { get; set; }
    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductSku> ProductSkus { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Store> Stores { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwBestSellingProduct> VwBestSellingProducts { get; set; }

    public virtual DbSet<VwProductCatalog> VwProductCatalogs { get; set; }

    public virtual DbSet<VwRevenueByMonth> VwRevenueByMonths { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__carts__2EF52A2730BAC23C");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithOne(p => p.Cart)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_carts_users");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__cart_ite__5D9A6C6E5B37E3CE");

            entity.Property(e => e.AddedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems).HasConstraintName("FK_cart_items_carts");

            entity.HasOne(d => d.Sku).WithMany(p => p.CartItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_cart_items_skus");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__orders__465962295B9644A1");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.OrderStatus).HasDefaultValue("PENDING");
            entity.Property(e => e.PaymentMethod).HasDefaultValue("COD");
            entity.Property(e => e.PaymentStatus).HasDefaultValue("UNPAID");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_orders_users");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__order_it__3764B6BC14E86687");

            entity.Property(e => e.LineTotal).HasComputedColumnSql("([unit_price]*[quantity])", true);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems).HasConstraintName("FK_order_items_orders");

            entity.HasOne(d => d.Sku).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_order_items_skus");

            entity.HasOne(d => d.StoreOrder).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.StoreOrderId)
                .HasConstraintName("FK_order_items_store_orders");
        });

        modelBuilder.Entity<StoreOrder>(entity =>
        {
            entity.HasOne(e => e.Order).WithMany()
                .HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Store).WithMany()
                .HasForeignKey(e => e.StoreId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<ShippingProvider>(entity =>
            entity.HasOne(e => e.OwnerUser).WithMany()
                .HasForeignKey(e => e.OwnerUserId).OnDelete(DeleteBehavior.NoAction));
        modelBuilder.Entity<ShippingService>(entity =>
            entity.HasOne(e => e.Provider).WithMany(e => e.Services)
                .HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.NoAction));
        modelBuilder.Entity<ShippingRateRule>(entity =>
            entity.HasOne(e => e.Service).WithMany(e => e.RateRules)
                .HasForeignKey(e => e.ServiceId).OnDelete(DeleteBehavior.NoAction));
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasOne(e => e.StoreOrder).WithMany(e => e.Shipments)
                .HasForeignKey(e => e.StoreOrderId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Provider).WithMany()
                .HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Service).WithMany()
                .HasForeignKey(e => e.ServiceId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__products__47027DF514EBFA42");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_products_categories");

            entity.HasOne(d => d.Store).WithMany(p => p.Products)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_products_stores");
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__product___D54EE9B4291C42C6");

            entity.Property(e => e.Status).HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent).HasConstraintName("FK_product_categories_parent");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__product___DC9AC95590E2B294");

            entity.HasIndex(e => e.ProductId, "UX_product_images_one_primary")
                .IsUnique()
                .HasFilter("([is_primary]=(1))");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_product_images_products");
        });

        modelBuilder.Entity<ProductSku>(entity =>
        {
            entity.HasKey(e => e.SkuId).HasName("PK__product___EAC95375FCB9B799");

            entity.Property(e => e.Status).HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductSkus)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_product_skus_products");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__reviews__60883D901A4F89AB");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("VISIBLE");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.Reviews).HasConstraintName("FK_reviews_order_items");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_reviews_products");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_reviews_users");
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.StoreId).HasName("PK__stores__A2F2A30C467A1068");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.OwnerUser).WithMany(p => p.Stores)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stores_users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__B9BE370FA0230235");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("ACTIVE");
        });

        modelBuilder.Entity<VwBestSellingProduct>(entity =>
        {
            entity.ToView("vw_best_selling_products");
        });

        modelBuilder.Entity<VwProductCatalog>(entity =>
        {
            entity.ToView("vw_product_catalog");
        });

        modelBuilder.Entity<VwRevenueByMonth>(entity =>
        {
            entity.ToView("vw_revenue_by_month");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
