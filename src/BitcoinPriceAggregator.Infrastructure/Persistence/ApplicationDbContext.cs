using Microsoft.EntityFrameworkCore;
using BitcoinPriceAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BitcoinPriceAggregator.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // Ensure the database is created and the schema is up to date
            Database.EnsureCreated(); // Then recreate it with the correct schema
        }

        public DbSet<BitcoinPrice> BitcoinPrices { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BitcoinPrice>(entity =>
            {
                entity.ToTable("BitcoinPrices");
                
                // Configure Id as INTEGER PRIMARY KEY AUTOINCREMENT for SQLite
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnType("INTEGER")
                    .ValueGeneratedOnAdd();

                // Configure other properties
                entity.Property(e => e.Price)
                    .HasColumnType("decimal(18,8)")
                    .IsRequired();

                entity.Property(e => e.Pair)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(e => e.UtcTicks)
                    .HasColumnName("UtcTicks")
                    .HasColumnType("bigint")
                    .IsRequired();
                
                // Create a unique index on UtcTicks and Pair
                entity.HasIndex(e => new { e.UtcTicks, e.Pair })
                    .HasDatabaseName("IX_BitcoinPrices_UtcTicks_Pair");

                // Seed data for development
                if (Database.IsSqlite())
                {
                    var now = DateTime.UtcNow;
                    var twoHoursAgo = now.AddHours(-2);
                    var oneHourAgo = now.AddHours(-1);

                    entity.HasData(
                        new BitcoinPrice("BTC/USD")
                        {
                            Id = 1,
                            Price = 50000m,
                            UtcTicks = NormalizeTicksToHourPrecision(twoHoursAgo.Ticks)
                        },
                        new BitcoinPrice("BTC/USD")
                        {
                            Id = 2,
                            Price = 51000m,
                            UtcTicks = NormalizeTicksToHourPrecision(oneHourAgo.Ticks)
                        },
                        new BitcoinPrice("BTC/USD")
                        {
                            Id = 3,
                            Price = 52000m,
                            UtcTicks = NormalizeTicksToHourPrecision(now.Ticks)
                        }
                    );
                }
            });

            // Ensure SQLite uses INTEGER PRIMARY KEY AUTOINCREMENT
            modelBuilder.HasAnnotation("Sqlite:Autoincrement", true);
        }

        private static long NormalizeTicksToHourPrecision(long ticks)
        {
            const long ticksPerHour = TimeSpan.TicksPerHour;
            return ticks - (ticks % ticksPerHour);
        }
    }
} 