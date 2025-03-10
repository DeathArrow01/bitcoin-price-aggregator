using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitcoinPriceAggregator.Domain.Entities
{
    /// <summary>
    /// Represents a Bitcoin price at a specific point in time.
    /// </summary>
    [Table("BitcoinPrices")]
    public class BitcoinPrice
    {
        /// <summary>
        /// Initializes a new instance of the BitcoinPrice class.
        /// </summary>
        public BitcoinPrice()
        {
            Pair = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the BitcoinPrice class.
        /// </summary>
        /// <param name="pair">The trading pair.</param>
        public BitcoinPrice(string pair) : this()
        {
            if (string.IsNullOrWhiteSpace(pair))
            {
                throw new ArgumentException("Pair cannot be null or empty.", nameof(pair));
            }
            Pair = pair.Trim();
        }

        /// <summary>
        /// Gets or sets the unique identifier for the price record
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the trading pair (e.g., "BTC/USD")
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Pair { get; set; }

        /// <summary>
        /// Gets or sets the price value
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the UTC ticks for timestamp (hour precision)
        /// </summary>
        [Required]
        [Column("UtcTicks")]
        public long UtcTicks { get; set; }

        /// <summary>
        /// Updates the price value.
        /// </summary>
        /// <param name="newPrice">The new price value.</param>
        /// <exception cref="ArgumentException">Thrown when price is less than or equal to zero.</exception>
        public void UpdatePrice(decimal newPrice)
        {
            if (newPrice <= 0)
            {
                throw new ArgumentException("Price must be greater than zero.", nameof(newPrice));
            }

            Price = newPrice;
        }

        /// <summary>
        /// Creates a new builder for BitcoinPrice.
        /// </summary>
        /// <returns>A new BitcoinPriceBuilder instance.</returns>
        public static BitcoinPriceBuilder CreateBuilder() => new BitcoinPriceBuilder();

        /// <summary>
        /// Builder class for creating BitcoinPrice instances.
        /// </summary>
        public class BitcoinPriceBuilder
        {
            private string? _pair;
            private decimal _price;
            private long _utcTicks;

            /// <summary>
            /// Sets the price for the BitcoinPrice instance.
            /// </summary>
            /// <param name="price">The price value.</param>
            /// <returns>The builder instance.</returns>
            /// <exception cref="ArgumentException">Thrown when price is less than or equal to zero.</exception>
            public BitcoinPriceBuilder WithPrice(decimal price)
            {
                if (price <= 0)
                {
                    throw new ArgumentException("Price must be greater than zero.", nameof(price));
                }
                _price = price;
                return this;
            }

            /// <summary>
            /// Sets the trading pair for the BitcoinPrice instance.
            /// </summary>
            /// <param name="pair">The trading pair.</param>
            /// <returns>The builder instance.</returns>
            public BitcoinPriceBuilder WithPair(string pair)
            {
                if (string.IsNullOrWhiteSpace(pair))
                {
                    throw new ArgumentException("Pair cannot be null or empty.", nameof(pair));
                }
                _pair = pair.Trim();
                return this;
            }

            /// <summary>
            /// Sets the timestamp for the BitcoinPrice instance.
            /// </summary>
            /// <param name="ticks">The UTC ticks (will be normalized to hour precision).</param>
            /// <returns>The builder instance.</returns>
            public BitcoinPriceBuilder WithUtcTicks(long ticks)
            {
                _utcTicks = NormalizeTicksToHourPrecision(ticks);
                return this;
            }

            /// <summary>
            /// Builds and returns a new BitcoinPrice instance.
            /// </summary>
            /// <returns>A new BitcoinPrice instance.</returns>
            /// <exception cref="InvalidOperationException">Thrown when required properties are not set.</exception>
            public BitcoinPrice Build()
            {
                if (_price <= 0)
                {
                    throw new InvalidOperationException("Price must be set and greater than zero.");
                }
                if (string.IsNullOrEmpty(_pair))
                {
                    throw new InvalidOperationException("Pair must be set.");
                }
                if (_utcTicks == 0)
                {
                    throw new InvalidOperationException("Timestamp must be set.");
                }

                var bitcoinPrice = new BitcoinPrice(_pair);
                bitcoinPrice.Price = _price;
                bitcoinPrice.UtcTicks = _utcTicks;

                return bitcoinPrice;
            }

            private static long NormalizeTicksToHourPrecision(long ticks)
            {
                const long ticksPerHour = TimeSpan.TicksPerHour;
                return ticks - (ticks % ticksPerHour);
            }
        }
    }
} 