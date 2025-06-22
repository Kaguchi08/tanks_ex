using Microsoft.EntityFrameworkCore;
using Tanks.Shared.Models;

namespace Tanks.Server.Data
{
    public class TankGameDbContext : DbContext
    {
        public TankGameDbContext(DbContextOptions<TankGameDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.ToTable("user");
                
                entity.Property(e => e.UserId)
                    .ValueGeneratedOnAdd();
                
                entity.Property(e => e.HandleName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasCharSet("utf8mb4")
                    .UseCollation("utf8mb4_unicode_ci");
                
                entity.Property(e => e.WinCount)
                    .HasDefaultValue(0);
                    
                entity.Property(e => e.LoseCount)
                    .HasDefaultValue(0);
                
                entity.Property(e => e.Status)
                    .HasConversion<int>()
                    .HasDefaultValue(UserStatus.Active);
                
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasIndex(e => e.HandleName).HasDatabaseName("IX_Users_HandleName");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_Users_Status");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Users_CreatedAt");
                
                entity.Ignore(e => e.TotalGames);
                entity.Ignore(e => e.WinRate);
            });
        }
    }
}