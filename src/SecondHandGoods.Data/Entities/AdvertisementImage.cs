using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Data.Entities
{
    /// <summary>
    /// Image associated with an advertisement
    /// </summary>
    public class AdvertisementImage
    {
        /// <summary>
        /// Unique image identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// URL or path to the image file
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Alternative text for the image (accessibility)
        /// </summary>
        [MaxLength(200)]
        public string? AltText { get; set; }

        /// <summary>
        /// Whether this is the main/primary image for the advertisement
        /// </summary>
        public bool IsMainImage { get; set; } = false;

        /// <summary>
        /// Display order of the image
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Original filename of the uploaded image
        /// </summary>
        [MaxLength(255)]
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long? FileSizeBytes { get; set; }

        /// <summary>
        /// MIME type of the image (e.g., "image/jpeg")
        /// </summary>
        [MaxLength(100)]
        public string? MimeType { get; set; }

        /// <summary>
        /// Date when the image was uploaded
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key
        /// <summary>
        /// ID of the advertisement this image belongs to
        /// </summary>
        public int AdvertisementId { get; set; }

        // Navigation Property
        /// <summary>
        /// The advertisement this image belongs to
        /// </summary>
        public virtual Advertisement Advertisement { get; set; } = null!;
    }
}