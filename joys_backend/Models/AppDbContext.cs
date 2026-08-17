
// ------------------------------
// DbContext
// ------------------------------
using Hlouwa.Models;
using Hlouwa.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleImage> ArticleImages => Set<ArticleImage>();
    public DbSet<ArticleVariant> ArticleVariants => Set<ArticleVariant>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<StockReservationItem> StockReservationItems => Set<StockReservationItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Reclamation> Reclamations => Set<Reclamation>();

    public override int SaveChanges()
    {
        ApplyAudit();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAudit();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAudit()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = null;
                entry.Entity.IsDeleted = false;
                entry.Entity.DeletedAt = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---- Decimal precision (TND often uses 3 decimals)
        builder.Entity<Article>().Property(x => x.Price).HasPrecision(18, 3);
        builder.Entity<Article>().Property(x => x.OldPrice).HasPrecision(18, 3);

        builder.Entity<ArticleVariant>().Property(x => x.Price).HasPrecision(18, 3);

        builder.Entity<Order>().Property(x => x.Subtotal).HasPrecision(18, 3);
        builder.Entity<Order>().Property(x => x.DiscountTotal).HasPrecision(18, 3);
        builder.Entity<Order>().Property(x => x.DeliveryFee).HasPrecision(18, 3);
        builder.Entity<Order>().Property(x => x.Total).HasPrecision(18, 3);

        builder.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(18, 3);

        builder.Entity<PaymentTransaction>().Property(x => x.Amount).HasPrecision(18, 3);

        builder.Entity<Cart>().Property(x => x.Subtotal).HasPrecision(18, 3);
        builder.Entity<Cart>().Property(x => x.DiscountTotal).HasPrecision(18, 3);
        builder.Entity<Cart>().Property(x => x.DeliveryFee).HasPrecision(18, 3);
        builder.Entity<Cart>().Property(x => x.Total).HasPrecision(18, 3);

        builder.Entity<CartItem>().Property(x => x.UnitPrice).HasPrecision(18, 3);

        builder.Entity<StockItem>().Property(x => x.OnHand).HasPrecision(18, 3);
        builder.Entity<StockItem>().Property(x => x.Reserved).HasPrecision(18, 3);

        builder.Entity<StockReservationItem>().Property(x => x.Quantity).HasPrecision(18, 3);

        builder.Entity<StockMovement>().Property(x => x.Quantity).HasPrecision(18, 3);

        // ---- Unique indexes
        builder.Entity<Category>()
            .HasIndex(x => x.Name).IsUnique();

        builder.Entity<Category>()
            .HasIndex(x => x.Slug).IsUnique();

        builder.Entity<Article>()
            .HasIndex(x => x.Slug).IsUnique();

        builder.Entity<Order>()
            .HasIndex(x => x.OrderNumber).IsUnique();

        builder.Entity<PaymentTransaction>()
            .HasIndex(x => x.ProviderPaymentId);

        // ---- Relationships
        builder.Entity<Category>()
            .HasMany(c => c.Articles)
            .WithOne(a => a.Category)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Article>()
            .HasMany(a => a.Images)
            .WithOne(i => i.Article)
            .HasForeignKey(i => i.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Article>()
            .HasMany(a => a.Variants)
            .WithOne(v => v.Article)
            .HasForeignKey(v => v.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Order>()
            .HasMany(o => o.Payments)
            .WithOne(p => p.Order)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Order>()
            .HasMany(o => o.StatusHistory)
            .WithOne(h => h.Order)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ApplicationUser>()
            .HasMany(u => u.Orders)
            .WithOne(o => o.User)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ApplicationUser>()
            .HasMany(u => u.Reclamations)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ApplicationUser>()
            .HasMany(u => u.Addresses)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Cart>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        builder.Entity<CartItem>()
            .HasIndex(x => new { x.CartId, x.ArticleId, x.ArticleVariantId });

        builder.Entity<Cart>()
            .HasMany(c => c.Items)
            .WithOne(i => i.Cart)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StockItem>()
            .HasIndex(x => new { x.ArticleId, x.ArticleVariantId })
            .IsUnique();

        builder.Entity<StockReservation>()
            .HasIndex(x => new { x.OrderId })
            .IsUnique();

        builder.Entity<StockReservationItem>()
            .HasIndex(x => new { x.StockReservationId, x.ArticleId, x.ArticleVariantId });

        builder.Entity<StockMovement>()
            .HasIndex(x => new { x.ArticleId, x.ArticleVariantId, x.CreatedAt });

        builder.Entity<StockReservation>()
            .HasMany(r => r.Items)
            .WithOne(i => i.StockReservation)
            .HasForeignKey(i => i.StockReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Reclamation>()
            .HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        // ---- Owned: ShippingAddress snapshot inside Orders table
        builder.Entity<Order>()
            .OwnsOne(o => o.ShippingAddress, sa =>
            {
                sa.Property(p => p.CountryCode).HasDefaultValue("TN");
            });

        // ---- Soft delete filters (apply per entity)
        builder.Entity<Category>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Article>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<ArticleImage>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<ArticleVariant>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Order>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<OrderItem>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<UserAddress>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<PaymentTransaction>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<OrderStatusHistory>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Cart>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<CartItem>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<StockItem>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<StockReservation>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<StockReservationItem>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<StockMovement>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Reclamation>().HasQueryFilter(x => !x.IsDeleted);
    }
}