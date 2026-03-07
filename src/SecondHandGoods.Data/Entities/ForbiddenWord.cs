using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Data.Entities
{
    /// <summary>
    /// Represents a forbidden word or phrase for content moderation
    /// </summary>
    public class ForbiddenWord
    {
        /// <summary>
        /// Unique identifier for the forbidden word
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The word or phrase that is forbidden
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Word { get; set; } = string.Empty;

        /// <summary>
        /// Normalized version for case-insensitive matching
        /// </summary>
        [Required]
        [StringLength(100)]
        public string NormalizedWord { get; set; } = string.Empty;

        /// <summary>
        /// Severity level of the forbidden word
        /// </summary>
        public ModerationSeverity Severity { get; set; } = ModerationSeverity.Medium;

        /// <summary>
        /// Category of the forbidden word for organization
        /// </summary>
        [StringLength(50)]
        public string? Category { get; set; }

        /// <summary>
        /// Whether this word should be completely blocked or just flagged
        /// </summary>
        public bool IsBlocked { get; set; } = true;

        /// <summary>
        /// Whether the word matching should be exact or partial
        /// </summary>
        public bool IsExactMatch { get; set; } = false;

        /// <summary>
        /// Whether this forbidden word entry is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional replacement word for automatic substitution
        /// </summary>
        [StringLength(100)]
        public string? Replacement { get; set; }

        /// <summary>
        /// Admin notes about this forbidden word
        /// </summary>
        [StringLength(500)]
        public string? AdminNotes { get; set; }

        /// <summary>
        /// Date when this forbidden word was added
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ID of the admin who added this word
        /// </summary>
        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        /// <summary>
        /// Date when this word was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// ID of the admin who last updated this word
        /// </summary>
        [StringLength(450)]
        public string? UpdatedByUserId { get; set; }

        // Navigation properties

        /// <summary>
        /// Admin user who created this forbidden word
        /// </summary>
        public virtual ApplicationUser? CreatedByUser { get; set; }

        /// <summary>
        /// Admin user who last updated this forbidden word
        /// </summary>
        public virtual ApplicationUser? UpdatedByUser { get; set; }

        /// <summary>
        /// Moderation logs related to this forbidden word
        /// </summary>
        public virtual ICollection<ModerationLog> ModerationLogs { get; set; } = new List<ModerationLog>();

        /// <summary>
        /// Updates the timestamp and user for modifications
        /// </summary>
        public void UpdateTimestamp(string userId)
        {
            UpdatedAt = DateTime.UtcNow;
            UpdatedByUserId = userId;
        }

        /// <summary>
        /// Normalizes the word for consistent matching
        /// </summary>
        public void NormalizeWord()
        {
            NormalizedWord = Word.Trim().ToLowerInvariant();
        }
    }

    /// <summary>
    /// Severity levels for content moderation
    /// </summary>
    public enum ModerationSeverity
    {
        /// <summary>
        /// Low severity - typically results in warnings
        /// </summary>
        Low = 1,

        /// <summary>
        /// Medium severity - may result in content flagging
        /// </summary>
        Medium = 2,

        /// <summary>
        /// High severity - typically results in content blocking
        /// </summary>
        High = 3,

        /// <summary>
        /// Critical severity - may result in account restrictions
        /// </summary>
        Critical = 4
    }
}