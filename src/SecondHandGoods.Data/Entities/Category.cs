using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Data.Entities
{
    /// <summary>
    /// Product category for organizing advertisements
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Unique category identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Category name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Category description
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// URL-friendly slug for category
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Icon class for UI display (e.g., "fas fa-car")
        /// </summary>
        [MaxLength(50)]
        public string? IconClass { get; set; }

        /// <summary>
        /// Display order for sorting categories
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Whether this category is active and visible
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date when the category was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when the category was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        /// <summary>
        /// Advertisements in this category
        /// </summary>
        public virtual ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();

        /// <summary>
        /// Gets the number of active advertisements in this category
        /// </summary>
        public int ActiveAdvertisementCount => Advertisements.Count(a => a.IsActive && !a.IsDeleted);
    }
}