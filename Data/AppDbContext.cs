using FastFoodShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FastFoodShop.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartDetail> CartDetails => Set<CartDetail>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision for money fields
            modelBuilder.Entity<Order>().Property(o => o.TotalPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderDetail>().Property(od => od.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<CartDetail>().Property(cd => cd.Price).HasColumnType("decimal(18,2)");
            
            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "ADMIN", Description = "Administrator role" },
                new Role { Id = 2, Name = "USER", Description = "Normal user role" }
            );

            // Configure relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails!)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(v => new { v.ProductId, v.VariantName })
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product relationships with OrderDetail/CartDetail
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Variant)
                .WithMany()
                .HasForeignKey(od => od.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartDetail>()
                .HasOne(cd => cd.Product)
                .WithMany()
                .HasForeignKey(cd => cd.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartDetail>()
                .HasOne(cd => cd.Variant)
                .WithMany()
                .HasForeignKey(cd => cd.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartDetail>()
                .HasOne(cd => cd.Cart)
                .WithMany(c => c.CartDetails!)
                .HasForeignKey(cd => cd.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
