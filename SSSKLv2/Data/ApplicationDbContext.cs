using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
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
        public DbSet<SSSKLv2.Data.AchievementImage> AchievementImage { get; set; } = default!;
        public DbSet<SSSKLv2.Data.Achievement> Achievement { get; set; } = default!;
        public DbSet<SSSKLv2.Data.AchievementEntry> AchievementEntry { get; set; } = default!;
        public DbSet<SSSKLv2.Data.Event> Event { get; set; } = default!;
        public DbSet<SSSKLv2.Data.EventResponse> EventResponse { get; set; } = default!;
        public DbSet<SSSKLv2.Data.EventImage> EventImage { get; set; } = default!;
        public DbSet<SSSKLv2.Data.UserImage> UserImage { get; set; } = default!;
        public DbSet<SSSKLv2.Data.GlobalSetting> GlobalSetting { get; set; } = default!;
        public DbSet<SSSKLv2.Data.Reaction> Reaction { get; set; } = default!;
        public DbSet<SSSKLv2.Data.Notification> Notification { get; set; } = default!;
        public DbSet<SSSKLv2.Data.PushSubscription> PushSubscription { get; set; } = default!;
        public DbSet<SSSKLv2.Data.Quote> Quote { get; set; } = default!;
        public DbSet<SSSKLv2.Data.QuoteAuthor> QuoteAuthor { get; set; } = default!;
        public DbSet<SSSKLv2.Data.QuoteVote> QuoteVote { get; set; } = default!;

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
                .Property(s => s.LastOrdered )
                .HasDefaultValueSql("GETDATE()");
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
            builder.Entity<OldUserMigration>()
                .Property(s => s.CreatedOn )
                .HasDefaultValueSql("GETDATE()");

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
            
            builder.Entity<AchievementImage>()
                .HasIndex(p => p.Id)
                .IsUnique();
            builder.Entity<AchievementImage>()
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
                .WithOne(e => e.Achievement)
                .IsRequired(false)
                .HasForeignKey<Achievement>("ImageId")
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Achievement>()
                .Property(e => e.Action)
                .HasConversion<int>();
            builder.Entity<Achievement>()
                .Property(e => e.ComparisonOperator)
                .HasConversion<int>();
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
                .Property(s => s.HasSeen )
                .HasDefaultValue(false);

            builder.Entity<Event>()
                .Property(s => s.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()")
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Entity<Event>()
                .Property(e => e.StartDateTime)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Entity<Event>()
                .Property(e => e.EndDateTime)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Entity<Event>()
                .HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Event>()
                .HasOne(e => e.Image)
                .WithOne(e => e.Event)
                .IsRequired(false)
                .HasForeignKey<Event>("ImageId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Event>()
                .HasMany(e => e.RequiredRoles)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "EventRequiredRoles",
                    j => j.HasOne<IdentityRole>().WithMany().HasForeignKey("RoleId"),
                    j => j.HasOne<Event>().WithMany().HasForeignKey("EventId")
                );

            builder.Entity<EventResponse>()
                .Property(s => s.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()")
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Entity<EventResponse>()
                .HasOne(e => e.Event)
                .WithMany(e => e.Responses)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<EventResponse>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<EventResponse>()
                .Property(e => e.Status)
                .HasConversion<int>();

            builder.Entity<EventImage>()
                .Property(s => s.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()")
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            builder.Entity<UserImage>()
                .HasIndex(p => p.Id)
                .IsUnique();
            builder.Entity<UserImage>()
                .Property(s => s.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()")
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Entity<UserImage>()
                .HasOne(e => e.User)
                .WithOne(e => e.ProfileImage)
                .HasForeignKey<ApplicationUser>(e => e.ProfileImageId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<GlobalSetting>()
                .HasIndex(p => p.Key)
                .IsUnique();
            builder.Entity<GlobalSetting>()
                .Property(s => s.CreatedOn )
                .HasDefaultValueSql("GETDATE()");
            builder.Entity<GlobalSetting>()
                .Property(s => s.UpdatedOn)
                .HasDefaultValueSql("GETDATE()");

            builder.Entity<Reaction>()
                .HasIndex(p => p.Id)
                .IsUnique();
            builder.Entity<Reaction>()
                .Property(s => s.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()")
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Entity<Reaction>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Reaction>()
                .HasIndex(p => new { p.TargetId, p.TargetType });

            builder.Entity<Notification>()
                .HasIndex(p => p.Id)
                .IsUnique();
            builder.Entity<Notification>()
                .Property(s => s.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()")
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Entity<Notification>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PushSubscription>()
                .HasIndex(p => p.Id)
                .IsUnique();
            builder.Entity<PushSubscription>()
                .HasIndex(p => new { p.UserId, p.Endpoint })
                .IsUnique();
            builder.Entity<PushSubscription>()
                .Property(s => s.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()")
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Entity<PushSubscription>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Quote>()
                .HasIndex(p => p.Id)
                .IsUnique();
            builder.Entity<Quote>()
                .Property(s => s.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()")
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Entity<Quote>()
                .Property(e => e.DateSaid)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Entity<Quote>()
                .HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Quote>()
                .HasMany(e => e.Authors)
                .WithOne(e => e.Quote)
                .HasForeignKey(e => e.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Quote>()
                .HasMany(e => e.VisibleToRoles)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "QuoteRequiredRoles",
                    j => j.HasOne<IdentityRole>().WithMany().HasForeignKey("RoleId"),
                    j => j.HasOne<Quote>().WithMany().HasForeignKey("QuoteId")
                );

            builder.Entity<QuoteAuthor>()
                .HasIndex(p => p.Id)
                .IsUnique();
            builder.Entity<QuoteAuthor>()
                .Property(s => s.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()")
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Entity<QuoteAuthor>()
                .HasOne(e => e.ApplicationUser)
                .WithMany()
                .HasForeignKey(e => e.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<QuoteVote>()
                .HasIndex(p => p.Id)
                .IsUnique();
            builder.Entity<QuoteVote>()
                .Property(s => s.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()")
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Entity<QuoteVote>()
                .HasOne(e => e.Quote)
                .WithMany()
                .HasForeignKey(e => e.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<QuoteVote>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<QuoteVote>()
                .HasIndex(p => new { p.QuoteId, p.UserId })
                .IsUnique();
        }
    }
}
