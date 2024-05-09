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
        public DbSet<SSSKLv2.Data.BlobStorageItem> BlobStorageItem { get; set; } = default!;
        public DbSet<SSSKLv2.Data.Achievement> Achievement { get; set; } = default!;
        public DbSet<SSSKLv2.Data.AchievementEntry> AchievementEntry { get; set; } = default!;

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
                .HasMany<AchievementEntry>(e => e.CompletedAchievements)
                .WithOne(e => e.User)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<Order>()
                .HasOne(e => e.Product)
                .WithMany(e => e.Orders)
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
            
            builder.Entity<BlobStorageItem>()
                .HasIndex(p => p.Id)
                .IsUnique();
            builder.Entity<BlobStorageItem>()
                .Property(s => s.CreatedOn )
                .HasDefaultValueSql("GETDATE()");
            
            builder.Entity<Achievement>()
                .HasIndex(p => p.Id)
                .IsUnique();
            builder.Entity<Achievement>()
                .Property(s => s.CreatedOn )
                .HasDefaultValueSql("GETDATE()");
            builder.Entity<Achievement>()
                .HasOne(e => e.Image)
                .WithMany()
                .OnDelete(DeleteBehavior.SetNull);
            builder.Entity<Achievement>()
                .HasMany<AchievementEntry>(e => e.CompletedEntries)
                .WithOne(e => e.Achievement)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<AchievementEntry>()
                .HasIndex(p => p.Id)
                .IsUnique();
            builder.Entity<AchievementEntry>()
                .Property(s => s.CreatedOn )
                .HasDefaultValueSql("GETDATE()");
            builder.Entity<AchievementEntry>()
                .HasOne(e => e.Achievement)
                .WithMany(e => e.CompletedEntries)
                .OnDelete(DeleteBehavior.SetNull);
            builder.Entity<AchievementEntry>()
                .HasOne(e => e.User)
                .WithMany(e => e.CompletedAchievements)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
