using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Data.Entities
{
    /// <summary>
    /// A paid/site advertisement slot (e.g. footer banner). Used for real ads that make money.
    /// </summary>
    public class SiteAdvertisement
    {
        public int Id { get; set; }

        /// <summary>
        /// Slot identifier (e.g. "footer-1", "footer-2", "footer-3"). One active ad per slot.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string SlotKey { get; set; } = string.Empty;

        /// <summary>
        /// URL to the ad image (or external ad creative).
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Link URL when the ad is clicked.
        /// </summary>
        [MaxLength(500)]
        public string? TargetUrl { get; set; }

        /// <summary>
        /// Alt text for the image (accessibility).
        /// </summary>
        [MaxLength(200)]
        public string? AltText { get; set; }

        /// <summary>
        /// Display order when multiple slots are shown (lower = first).
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Whether this ad is currently active and should be shown.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
