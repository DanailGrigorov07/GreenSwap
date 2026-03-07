using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandGoods.Data.Entities
{
    /// <summary>
    /// Advertisement for selling second-hand goods
    /// </summary>
    public class Advertisement
    {
        /// <summary>
        /// Unique advertisement identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Advertisement title
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the item
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Item price in the local currency
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        /// <summary>
        /// Item condition
        /// </summary>
        public ItemCondition Condition { get; set; } = ItemCondition.Used;

        /// <summary>
        /// Location where the item is located
        /// </summary>
        [MaxLength(200)]
        public string? Location { get; set; }

        /// <summary>
        /// Whether price is negotiable
        /// </summary>
        public bool IsPriceNegotiable { get; set; } = false;

        /// <summary>
        /// Number of views this ad has received
        /// </summary>
        public int ViewCount { get; set; } = 0;

        /// <summary>
        /// Whether the advertisement is active and visible
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether the advertisement has been soft-deleted
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Whether the item has been sold
        /// </summary>
        public bool IsSold { get; set; } = false;

        /// <summary>
        /// Whether the ad is featured (highlighted)
        /// </summary>
        public bool IsFeatured { get; set; } = false;

        /// <summary>
        /// Date when the advertisement was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when the advertisement was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Date when the advertisement expires
        /// </summary>
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);

        /// <summary>
        /// Date when the item was sold (if applicable)
        /// </summary>
        public DateTime? SoldAt { get; set; }

        // Foreign Keys
        /// <summary>
        /// ID of the user who created this advertisement
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the category this advertisement belongs to
        /// </summary>
        [Required]
        public int CategoryId { get; set; }

        // Navigation Properties
        /// <summary>
        /// The user who created this advertisement
        /// </summary>
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// The category this advertisement belongs to
        /// </summary>
        public virtual Category Category { get; set; } = null!;

        /// <summary>
        /// Images associated with this advertisement
        /// </summary>
        public virtual ICollection<AdvertisementImage> Images { get; set; } = new List<AdvertisementImage>();

        /// <summary>
        /// Messages related to this advertisement
        /// </summary>
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

        /// <summary>
        /// Users who have favorited this advertisement
        /// </summary>
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

        /// <summary>
        /// Orders for this advertisement
        /// </summary>
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        /// <summary>
        /// Whether the advertisement is currently active and viewable
        /// </summary>
        public bool IsPublic => IsActive && !IsDeleted && !IsSold && ExpiresAt > DateTime.UtcNow;

        /// <summary>
        /// Gets the main image URL or a default placeholder
        /// </summary>
        public string MainImageUrl => Images.FirstOrDefault(i => i.IsMainImage)?.ImageUrl ?? "/images/no-image.jpg";

        /// <summary>
        /// Updates the UpdatedAt timestamp
        /// </summary>
        public void UpdateTimestamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the item as sold
        /// </summary>
        public void MarkAsSold()
        {
            IsSold = true;
            SoldAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        /// <summary>
        /// Increments the view count
        /// </summary>
        public void IncrementViewCount()
        {
            ViewCount++;
        }
    }

    /// <summary>
    /// Possible item conditions
    /// </summary>
    public enum ItemCondition
    {
        /// <summary>
        /// Item is brand new, never used
        /// </summary>
        New = 1,

        /// <summary>
        /// Item has been used but is in good condition
        /// </summary>
        Used = 2,

        /// <summary>
        /// Item has visible damage but is still functional
        /// </summary>
        Damaged = 3,

        /// <summary>
        /// Item has been refurbished or restored
        /// </summary>
        Refurbished = 4
    }
}