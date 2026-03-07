using Microsoft.AspNetCore.Mvc.Rendering;
using SecondHandGoods.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Web.Models.Admin
{
    /// <summary>
    /// View model for managing forbidden words in admin panel
    /// </summary>
    public class ForbiddenWordsManagementViewModel
    {
        public List<ForbiddenWordItemViewModel> ForbiddenWords { get; set; } = new();
        public int TotalWords { get; set; }

        // Filters
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsBlocked { get; set; }
        public ModerationSeverity? Severity { get; set; }
        public string? Category { get; set; }
        public string SortBy { get; set; } = "newest";

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalWords / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        // Statistics
        public int ActiveWordsCount { get; set; }
        public int BlockedWordsCount { get; set; }
        public int FlaggedWordsCount { get; set; }
        public List<string> Categories { get; set; } = new();

        /// <summary>
        /// Get severity options for filtering
        /// </summary>
        public List<SelectListItem> SeverityOptions => new()
        {
            new SelectListItem { Value = "", Text = "All Severities" },
            new SelectListItem { Value = "1", Text = "Low" },
            new SelectListItem { Value = "2", Text = "Medium" },
            new SelectListItem { Value = "3", Text = "High" },
            new SelectListItem { Value = "4", Text = "Critical" }
        };

        /// <summary>
        /// Get sort options
        /// </summary>
        public List<SelectListItem> SortOptions => new()
        {
            new SelectListItem { Value = "newest", Text = "Newest First" },
            new SelectListItem { Value = "oldest", Text = "Oldest First" },
            new SelectListItem { Value = "word", Text = "Alphabetical" },
            new SelectListItem { Value = "severity-high", Text = "Highest Severity" },
            new SelectListItem { Value = "severity-low", Text = "Lowest Severity" },
            new SelectListItem { Value = "category", Text = "Category" }
        };
    }

    /// <summary>
    /// Individual forbidden word item for admin display
    /// </summary>
    public class ForbiddenWordItemViewModel
    {
        public int Id { get; set; }
        public string Word { get; set; } = string.Empty;
        public ModerationSeverity Severity { get; set; }
        public string? Category { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsExactMatch { get; set; }
        public bool IsActive { get; set; }
        public string? Replacement { get; set; }
        public string? AdminNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public string? UpdatedByUserName { get; set; }

        /// <summary>
        /// Severity display with color coding
        /// </summary>
        public string SeverityDisplay => Severity switch
        {
            ModerationSeverity.Low => "Low",
            ModerationSeverity.Medium => "Medium",
            ModerationSeverity.High => "High",
            ModerationSeverity.Critical => "Critical",
            _ => "Unknown"
        };

        /// <summary>
        /// Severity badge class for styling
        /// </summary>
        public string SeverityBadgeClass => Severity switch
        {
            ModerationSeverity.Low => "badge bg-info",
            ModerationSeverity.Medium => "badge bg-warning text-dark",
            ModerationSeverity.High => "badge bg-danger",
            ModerationSeverity.Critical => "badge bg-dark",
            _ => "badge bg-secondary"
        };

        /// <summary>
        /// Action display (blocked vs flagged)
        /// </summary>
        public string ActionDisplay => IsBlocked ? "Block" : "Flag";

        /// <summary>
        /// Action badge class
        /// </summary>
        public string ActionBadgeClass => IsBlocked ? "badge bg-danger" : "badge bg-warning text-dark";

        /// <summary>
        /// Match type display
        /// </summary>
        public string MatchTypeDisplay => IsExactMatch ? "Exact" : "Partial";

        /// <summary>
        /// Status badge class
        /// </summary>
        public string StatusBadgeClass => IsActive ? "badge bg-success" : "badge bg-secondary";

        /// <summary>
        /// Status text
        /// </summary>
        public string StatusText => IsActive ? "Active" : "Inactive";

        /// <summary>
        /// Time since creation
        /// </summary>
        public string CreatedAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CreatedAt;
                return timeSpan.TotalDays >= 1
                    ? $"{(int)timeSpan.TotalDays}d ago"
                    : timeSpan.TotalHours >= 1
                        ? $"{(int)timeSpan.TotalHours}h ago"
                        : $"{(int)timeSpan.TotalMinutes}m ago";
            }
        }
    }

    /// <summary>
    /// View model for creating/editing forbidden words
    /// </summary>
    public class CreateEditForbiddenWordViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Word must be between 1 and 100 characters.")]
        [Display(Name = "Forbidden Word")]
        public string Word { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Severity Level")]
        public ModerationSeverity Severity { get; set; } = ModerationSeverity.Medium;

        [StringLength(50)]
        [Display(Name = "Category")]
        public string? Category { get; set; }

        [Display(Name = "Block Content")]
        public bool IsBlocked { get; set; } = true;

        [Display(Name = "Exact Match Only")]
        public bool IsExactMatch { get; set; } = false;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        [Display(Name = "Replacement Text")]
        public string? Replacement { get; set; }

        [StringLength(500)]
        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        /// <summary>
        /// Get severity options for dropdown
        /// </summary>
        public List<SelectListItem> SeverityOptions => new()
        {
            new SelectListItem { Value = "1", Text = "Low - Warning only" },
            new SelectListItem { Value = "2", Text = "Medium - Flag for review" },
            new SelectListItem { Value = "3", Text = "High - Block content" },
            new SelectListItem { Value = "4", Text = "Critical - Block and log violation" }
        };

        /// <summary>
        /// Common category suggestions
        /// </summary>
        public List<string> CategorySuggestions => new()
        {
            "Profanity",
            "Hate Speech",
            "Violence",
            "Adult Content",
            "Spam",
            "Scam",
            "Illegal",
            "Personal Info",
            "Brand Names",
            "Other"
        };
    }

    /// <summary>
    /// View model for moderation logs
    /// </summary>
    public class ModerationLogsViewModel
    {
        public List<ModerationLogItemViewModel> Logs { get; set; } = new();
        public int TotalLogs { get; set; }

        // Filters
        public ModeratedEntityType? EntityType { get; set; }
        public ModerationResult? Result { get; set; }
        public ModerationAction? Action { get; set; }
        public ModerationSeverity? Severity { get; set; }
        public string? ContentAuthor { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsAutomatic { get; set; }
        public bool? IsAppealed { get; set; }
        public string SortBy { get; set; } = "newest";

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalPages => (int)Math.Ceiling((double)TotalLogs / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        // Statistics
        public int AutomaticModerations { get; set; }
        public int ManualModerations { get; set; }
        public int AppealsCount { get; set; }
        public int BlockedContent { get; set; }
        public int FlaggedContent { get; set; }

        /// <summary>
        /// Get entity type options
        /// </summary>
        public List<SelectListItem> EntityTypeOptions => new()
        {
            new SelectListItem { Value = "", Text = "All Content Types" },
            new SelectListItem { Value = "1", Text = "Advertisements" },
            new SelectListItem { Value = "2", Text = "Messages" },
            new SelectListItem { Value = "3", Text = "Reviews" },
            new SelectListItem { Value = "4", Text = "User Profiles" },
            new SelectListItem { Value = "5", Text = "Media" }
        };

        /// <summary>
        /// Get result options
        /// </summary>
        public List<SelectListItem> ResultOptions => new()
        {
            new SelectListItem { Value = "", Text = "All Results" },
            new SelectListItem { Value = "1", Text = "Passed" },
            new SelectListItem { Value = "2", Text = "Flagged" },
            new SelectListItem { Value = "3", Text = "Blocked" },
            new SelectListItem { Value = "4", Text = "Modified" },
            new SelectListItem { Value = "5", Text = "Pending Review" }
        };

        /// <summary>
        /// Get action options
        /// </summary>
        public List<SelectListItem> ActionOptions => new()
        {
            new SelectListItem { Value = "", Text = "All Actions" },
            new SelectListItem { Value = "1", Text = "Auto Flag" },
            new SelectListItem { Value = "2", Text = "Auto Block" },
            new SelectListItem { Value = "3", Text = "Auto Replace" },
            new SelectListItem { Value = "4", Text = "Manual Review" },
            new SelectListItem { Value = "5", Text = "Manual Approve" },
            new SelectListItem { Value = "6", Text = "Manual Reject" },
            new SelectListItem { Value = "7", Text = "User Report" }
        };

        /// <summary>
        /// Get sort options
        /// </summary>
        public List<SelectListItem> SortOptions => new()
        {
            new SelectListItem { Value = "newest", Text = "Newest First" },
            new SelectListItem { Value = "oldest", Text = "Oldest First" },
            new SelectListItem { Value = "severity-high", Text = "Highest Severity" },
            new SelectListItem { Value = "severity-low", Text = "Lowest Severity" },
            new SelectListItem { Value = "author", Text = "Content Author" },
            new SelectListItem { Value = "entity-type", Text = "Content Type" }
        };
    }

    /// <summary>
    /// Individual moderation log item
    /// </summary>
    public class ModerationLogItemViewModel
    {
        public int Id { get; set; }
        public ModeratedEntityType EntityType { get; set; }
        public int EntityId { get; set; }
        public string ContentAuthor { get; set; } = string.Empty;
        public string ContentAuthorEmail { get; set; } = string.Empty;
        public ModerationAction Action { get; set; }
        public ModerationResult Result { get; set; }
        public ModerationSeverity Severity { get; set; }
        public string OriginalContent { get; set; } = string.Empty;
        public string? ModeratedContent { get; set; }
        public string? DetectedWords { get; set; }
        public bool IsAutomatic { get; set; }
        public string? ModeratorName { get; set; }
        public string? ModerationReason { get; set; }
        public bool IsAppealed { get; set; }
        public string? AppealDecision { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UserIpAddress { get; set; }

        /// <summary>
        /// Truncated original content for display
        /// </summary>
        public string TruncatedContent
        {
            get
            {
                const int maxLength = 100;
                return OriginalContent.Length > maxLength
                    ? OriginalContent.Substring(0, maxLength) + "..."
                    : OriginalContent;
            }
        }

        /// <summary>
        /// Entity type display
        /// </summary>
        public string EntityTypeDisplay => EntityType switch
        {
            ModeratedEntityType.Advertisement => "Advertisement",
            ModeratedEntityType.Message => "Message",
            ModeratedEntityType.Review => "Review",
            ModeratedEntityType.UserProfile => "User Profile",
            ModeratedEntityType.Media => "Media",
            _ => "Unknown"
        };

        /// <summary>
        /// Action description with icon
        /// </summary>
        public string ActionDisplayWithIcon => Action switch
        {
            ModerationAction.AutoFlag => "🚩 Auto Flagged",
            ModerationAction.AutoBlock => "🚫 Auto Blocked",
            ModerationAction.AutoReplace => "📝 Auto Replaced",
            ModerationAction.ManualReview => "👀 Manual Review",
            ModerationAction.ManualApprove => "✅ Manual Approved",
            ModerationAction.ManualReject => "❌ Manual Rejected",
            ModerationAction.UserReport => "📝 User Reported",
            _ => "❓ Unknown"
        };

        /// <summary>
        /// Result badge class
        /// </summary>
        public string ResultBadgeClass => Result switch
        {
            ModerationResult.Passed => "badge bg-success",
            ModerationResult.Flagged => "badge bg-warning text-dark",
            ModerationResult.Blocked => "badge bg-danger",
            ModerationResult.Modified => "badge bg-info",
            ModerationResult.PendingReview => "badge bg-secondary",
            _ => "badge bg-light text-dark"
        };

        /// <summary>
        /// Severity badge class
        /// </summary>
        public string SeverityBadgeClass => Severity switch
        {
            ModerationSeverity.Low => "badge bg-info",
            ModerationSeverity.Medium => "badge bg-warning text-dark",
            ModerationSeverity.High => "badge bg-danger",
            ModerationSeverity.Critical => "badge bg-dark",
            _ => "badge bg-secondary"
        };

        /// <summary>
        /// Time since creation
        /// </summary>
        public string CreatedAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CreatedAt;
                return timeSpan.TotalDays >= 1
                    ? $"{(int)timeSpan.TotalDays}d ago"
                    : timeSpan.TotalHours >= 1
                        ? $"{(int)timeSpan.TotalHours}h ago"
                        : $"{(int)timeSpan.TotalMinutes}m ago";
            }
        }
    }

    /// <summary>
    /// View model for content moderation dashboard
    /// </summary>
    public class ContentModerationDashboardViewModel
    {
        // Summary Statistics
        public int TotalModerations { get; set; }
        public int ModerationsToday { get; set; }
        public int ModerationsThisWeek { get; set; }
        public int AutomaticActions { get; set; }
        public int ManualReviews { get; set; }
        public int PendingReviews { get; set; }

        // Content Statistics
        public int BlockedContent { get; set; }
        public int FlaggedContent { get; set; }
        public int ModifiedContent { get; set; }

        // Forbidden Words Statistics
        public int TotalForbiddenWords { get; set; }
        public int ActiveWords { get; set; }
        public int CriticalWords { get; set; }

        // Recent Activity
        public List<ModerationLogItemViewModel> RecentModerations { get; set; } = new();
        public List<ForbiddenWordItemViewModel> RecentWords { get; set; } = new();

        // Charts Data (for future implementation)
        public Dictionary<string, int> ModerationsByDay { get; set; } = new();
        public Dictionary<string, int> ModerationsByType { get; set; } = new();
        public Dictionary<string, int> DetectionsBySeverity { get; set; } = new();

        /// <summary>
        /// Get automation rate percentage
        /// </summary>
        public double AutomationRate => TotalModerations > 0 ? (double)AutomaticActions / TotalModerations * 100 : 0;

        /// <summary>
        /// Get effectiveness rate (blocked/flagged vs total)
        /// </summary>
        public double EffectivenessRate => TotalModerations > 0 ? (double)(BlockedContent + FlaggedContent) / TotalModerations * 100 : 0;
    }
}