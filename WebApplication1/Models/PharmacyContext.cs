using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models;

public partial class PharmacyContext : DbContext
{
    public PharmacyContext()
    {
    }

    public PharmacyContext(DbContextOptions<PharmacyContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Customer> Customers { get; set; }
    public virtual DbSet<CustomerOrder> CustomerOrders { get; set; }
    public virtual DbSet<OrderItem> OrderItems { get; set; }
    public virtual DbSet<Pharmacy> Pharmacies { get; set; }
    public virtual DbSet<PharmacyStock> PharmacyStocks { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Pharmacist> Pharmacists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=Pharmacy.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customer");

            entity.HasIndex(e => e.Phone, "IX_customer_phone").IsUnique();
            entity.HasIndex(e => e.UserId, "IX_customer_user_id").IsUnique();

            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.DefaultAddress).HasColumnName("default_address");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Customer).HasForeignKey<Customer>(d => d.UserId);
        });

        modelBuilder.Entity<CustomerOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId);

            entity.ToTable("customer_orders");

            entity.HasIndex(e => e.CustomerId, "idx_customer_orders_customer");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("TIMESTAMP")
                .HasColumnName("expires_at");
            entity.Property(e => e.Status)
                .HasDefaultValue("новый")
                .HasColumnName("status");
            entity.Property(e => e.TotalCost)
                .HasColumnType("DECIMAL(10, 2)")
                .HasColumnName("total_cost");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerOrders).HasForeignKey(d => d.CustomerId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");

            entity.HasIndex(e => e.OrderId, "idx_order_items_order");
            entity.HasIndex(e => e.StockId, "idx_order_items_stock");

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.StockId).HasColumnName("stock_id");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems).HasForeignKey(d => d.OrderId);
            entity.HasOne(d => d.Stock).WithMany(p => p.OrderItems).HasForeignKey(d => d.StockId);
        });

        modelBuilder.Entity<Pharmacy>(entity =>
        {
            entity.ToTable("pharmacy");

            entity.HasIndex(e => e.UserId, "IX_pharmacy_user_id").IsUnique();

            entity.Property(e => e.PharmacyId).HasColumnName("pharmacy_id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.DirectorName).HasColumnName("director_name");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WorkingHours).HasColumnName("working_hours");

            entity.HasOne(d => d.User).WithOne(p => p.Pharmacy).HasForeignKey<Pharmacy>(d => d.UserId);
        });

        modelBuilder.Entity<PharmacyStock>(entity =>
        {
            entity.HasKey(e => e.StockId);

            entity.ToTable("pharmacy_stock");

            entity.HasIndex(e => e.PharmacyId, "idx_pharmacy_stock_pharmacy");
            entity.HasIndex(e => e.ProductId, "idx_pharmacy_stock_product");

            entity.Property(e => e.StockId).HasColumnName("stock_id");
            entity.Property(e => e.PharmacyId).HasColumnName("pharmacy_id");
            entity.Property(e => e.Price)
                .HasColumnType("DECIMAL(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(0)
                .HasColumnName("quantity");

            entity.HasOne(d => d.Pharmacy).WithMany(p => p.PharmacyStocks).HasForeignKey(d => d.PharmacyId);
            entity.HasOne(d => d.Product).WithMany(p => p.PharmacyStocks).HasForeignKey(d => d.ProductId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ActiveSubstance).HasColumnName("active_substance");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Dosage).HasColumnName("dosage");
            entity.Property(e => e.Form).HasColumnName("form");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.PrescriptionRequired)
                .HasDefaultValue(0)
                .HasColumnName("prescription_required");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.Role).HasColumnName("role");

            // Вот это ограничение позволит добавлять фармацевта
            entity.HasCheckConstraint("CK_User_Role", "role IN ('customer', 'owner', 'admin', 'pharmacist')");
        });

        modelBuilder.Entity<Pharmacist>(entity =>
        {
            entity.ToTable("pharmacists");

            entity.HasKey(e => e.PharmacistId);
            entity.Property(e => e.PharmacistId).HasColumnName("pharmacist_id");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.PharmacyId).HasColumnName("pharmacy_id");
            entity.Property(e => e.WorkingHours).HasColumnName("working_hours");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User)
                .WithOne()
                .HasForeignKey<Pharmacist>(d => d.UserId);

            entity.HasOne(d => d.Pharmacy)
                .WithMany()
                .HasForeignKey(d => d.PharmacyId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}