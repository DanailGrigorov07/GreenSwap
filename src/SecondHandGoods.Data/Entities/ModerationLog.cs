using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Data.Entities
{
    /// <summary>
    /// Represents a log entry for content moderation activities
    /// </summary>
    public class ModerationLog
    {
        /// <summary>
        /// Unique identifier for the moderation log entry
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Type of entity being moderated
        /// </summary>
        public ModeratedEntityType EntityType { get; set; }

        /// <summary>
        /// ID of the entity being moderated (Advertisement, Message, Review, etc.)
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// ID of the user who created the content being moderated
        /// </summary>
        [Required]
        [StringLength(450)]
        public string ContentAuthorId { get; set; } = string.Empty;

        /// <summary>
        /// Type of moderation action taken
        /// </summary>
        public ModerationAction Action { get; set; }

        /// <summary>
        /// Result of the moderation check
        /// </summary>
        public ModerationResult Result { get; set; }

        /// <summary>
        /// Severity level of the content violation
        /// </summary>
        public ModerationSeverity Severity { get; set; }

        /// <summary>
        /// The original content that was moderated
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string OriginalContent { get; set; } = string.Empty;

        /// <summary>
        /// Content after moderation (if modified)
        /// </summary>
        [StringLength(2000)]
        public string? ModeratedContent { get; set; }

        /// <summary>
        /// List of forbidden words that were detected
        /// </summary>
        [StringLength(500)]
        public string? DetectedWords { get; set; }

        /// <summary>
        /// ID of the forbidden word that triggered this log (if applicable)
        /// </summary>
        public int? ForbiddenWordId { get; set; }

        /// <summary>
        /// Whether the moderation was automatic or manual
        /// </summary>
        public bool IsAutomatic { get; set; } = true;

        /// <summary>
        /// ID of the moderator who performed manual review (if applicable)
        /// </summary>
        [StringLength(450)]
        public string? ModeratorId { get; set; }

        /// <summary>
        /// Reason or notes for the moderation decision
        /// </summary>
        [StringLength(1000)]
        public string? ModerationReason { get; set; }

        /// <summary>
        /// Additional context about the moderation
        /// </summary>
        [StringLength(1000)]
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// Whether this moderation decision was appealed
        /// </summary>
        public bool IsAppealed { get; set; } = false;

        /// <summary>
        /// Appeal decision if applicable
        /// </summary>
        [StringLength(1000)]
        public string? AppealDecision { get; set; }

        /// <summary>
        /// Date when the moderation occurred
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// IP address of the user who created the content
        /// </summary>
        [StringLength(45)]
        public string? UserIpAddress { get; set; }

        // Navigation properties

        /// <summary>
        /// User who created the content being moderated
        /// </summary>
        public virtual ApplicationUser? ContentAuthor { get; set; }

        /// <summary>
        /// Moderator who performed manual review
        /// </summary>
        public virtual ApplicationUser? Moderator { get; set; }

        /// <summary>
        /// Forbidden word that triggered this moderation
        /// </summary>
        public virtual ForbiddenWord? ForbiddenWord { get; set; }

        /// <summary>
        /// Gets a formatted summary of detected words
        /// </summary>
        public string DetectedWordsDisplay => 
            string.IsNullOrEmpty(DetectedWords) ? "None" : DetectedWords;

        /// <summary>
        /// Gets a user-friendly action description
        /// </summary>
        public string ActionDescription => Action switch
        {
            ModerationAction.AutoFlag => "Automatically flagged",
            ModerationAction.AutoBlock => "Automatically blocked",
            ModerationAction.AutoReplace => "Automatically replaced",
            ModerationAction.ManualReview => "Sent for manual review",
            ModerationAction.ManualApprove => "Manually approved",
            ModerationAction.ManualReject => "Manually rejected",
            ModerationAction.UserReport => "Reported by user",
            _ => "Unknown action"
        };

        /// <summary>
        /// Gets a user-friendly result description
        /// </summary>
        public string ResultDescription => Result switch
        {
            ModerationResult.Passed => "Content approved",
            ModerationResult.Flagged => "Content flagged for review",
            ModerationResult.Blocked => "Content blocked",
            ModerationResult.Modified => "Content modified",
            ModerationResult.PendingReview => "Pending manual review",
            _ => "Unknown result"
        };
    }

    /// <summary>
    /// Types of entities that can be moderated
    /// </summary>
    public enum ModeratedEntityType
    {
        /// <summary>
        /// Advertisement title or description
        /// </summary>
        Advertisement = 1,

        /// <summary>
        /// Chat or direct message
        /// </summary>
        Message = 2,

        /// <summary>
        /// Review comment
        /// </summary>
        Review = 3,

        /// <summary>
        /// User profile information
        /// </summary>
        UserProfile = 4,

        /// <summary>
        /// Image or media content
        /// </summary>
        Media = 5
    }

    /// <summary>
    /// Types of moderation actions
    /// </summary>
    public enum ModerationAction
    {
        /// <summary>
        /// Content was automatically flagged for review
        /// </summary>
        AutoFlag = 1,

        /// <summary>
        /// Content was automatically blocked
        /// </summary>
        AutoBlock = 2,

        /// <summary>
        /// Content was automatically replaced/censored
        /// </summary>
        AutoReplace = 3,

        /// <summary>
        /// Content was sent for manual review
        /// </summary>
        ManualReview = 4,

        /// <summary>
        /// Content was manually approved by moderator
        /// </summary>
        ManualApprove = 5,

        /// <summary>
        /// Content was manually rejected by moderator
        /// </summary>
        ManualReject = 6,

        /// <summary>
        /// Content was reported by a user
        /// </summary>
        UserReport = 7
    }

    /// <summary>
    /// Results of moderation actions
    /// </summary>
    public enum ModerationResult
    {
        /// <summary>
        /// Content passed moderation checks
        /// </summary>
        Passed = 1,

        /// <summary>
        /// Content was flagged but not blocked
        /// </summary>
        Flagged = 2,

        /// <summary>
        /// Content was blocked from publication
        /// </summary>
        Blocked = 3,

        /// <summary>
        /// Content was modified (censored/replaced)
        /// </summary>
        Modified = 4,

        /// <summary>
        /// Content is pending manual review
        /// </summary>
        PendingReview = 5
    }
}