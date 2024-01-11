using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;

namespace SSSKLv2.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<SSSKLv2.Data.Product> Product { get; set; } = default!;
        public DbSet<SSSKLv2.Data.OldUserMigration> OldUserMigration { get; set; } = default!;
        public DbSet<SSSKLv2.Data.Order> Order { get; set; } = default!;
        public DbSet<SSSKLv2.Data.TopUp> TopUp { get; set; } = default!;
        
        public DbSet<SSSKLv2.Data.Announcement> Announcement { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            builder.Entity<Announcement>()
                .Property(s => s.CreatedOn )
                .HasDefaultValueSql("GETDATE()");

            builder.Entity<ApplicationUser>()
                .HasMany<Order>(e => e.Orders)
                .WithOne(e => e.User)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<ApplicationUser>()
                .HasMany<TopUp>(e => e.TopUps)
                .WithOne(e => e.User)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<Order>()
                .HasOne(e => e.Product)
                .WithMany()
                .OnDelete(DeleteBehavior.SetNull);
            
            builder.Entity<OldUserMigration>()
                .HasIndex(p => p.Username)
                .IsUnique();

            builder.Entity<Product>()
                .HasIndex(p => p.Name)
                .IsUnique();
            
            builder.Entity<Product>()
                .Property(s => s.CreatedOn )
                .HasDefaultValueSql("GETDATE()");
            
            builder.Entity<Order>()
                .Property(s => s.CreatedOn )
                .HasDefaultValueSql("GETDATE()");
            
            builder.Entity<TopUp>()
                .Property(s => s.CreatedOn )
                .HasDefaultValueSql("GETDATE()");
            
            builder.Entity<OldUserMigration>()
                .Property(s => s.CreatedOn )
                .HasDefaultValueSql("GETDATE()");
            
            builder.Entity<ApplicationUser>()
                .Property(s => s.LastOrdered )
                .HasDefaultValueSql("GETDATE()");
        }
    }
}
