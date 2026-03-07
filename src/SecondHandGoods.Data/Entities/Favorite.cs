using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Data.Entities
{
    /// <summary>
    /// User's favorite/bookmarked advertisement
    /// </summary>
    public class Favorite
    {
        /// <summary>
        /// Unique favorite identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Date when the advertisement was added to favorites
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional notes the user can add about why they favorited this item
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        // Foreign Keys
        /// <summary>
        /// ID of the user who favorited the advertisement
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the favorited advertisement
        /// </summary>
        [Required]
        public int AdvertisementId { get; set; }

        // Navigation Properties
        /// <summary>
        /// The user who favorited the advertisement
        /// </summary>
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// The favorited advertisement
        /// </summary>
        public virtual Advertisement Advertisement { get; set; } = null!;
    }
}