using Microsoft.AspNetCore.Mvc.Rendering;
using SecondHandGoods.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Web.Models.Reviews
{
    /// <summary>
    /// View model for creating a new review
    /// </summary>
    public class CreateReviewViewModel
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public string ReviewedUserId { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "Please select a rating between 1 and 5 stars.")]
        [Display(Name = "Rating")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        [Display(Name = "Comment (Optional)")]
        public string? Comment { get; set; }

        [Required]
        public ReviewType ReviewType { get; set; }

        // Read-only display properties
        public string OrderNumber { get; set; } = string.Empty;
        public string AdvertisementTitle { get; set; } = string.Empty;
        public string AdvertisementImageUrl { get; set; } = string.Empty;
        public decimal FinalPrice { get; set; }
        public string ReviewedUserName { get; set; } = string.Empty;
        public DateTime OrderCompletedDate { get; set; }

        /// <summary>
        /// Gets the review type display text
        /// </summary>
        public string ReviewTypeDisplay => ReviewType switch
        {
            ReviewType.BuyerToSeller => "Rate this Seller",
            ReviewType.SellerToBuyer => "Rate this Buyer",
            _ => "Leave Review"
        };

        /// <summary>
        /// Gets the formatted price
        /// </summary>
        public string FormattedPrice => $"${FinalPrice:F2}";
    }

    /// <summary>
    /// View model for displaying a review
    /// </summary>
    public class ReviewDisplayViewModel
    {
        public int Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public ReviewType ReviewType { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }
        public bool IsReported { get; set; }

        // Reviewer information
        public string ReviewerId { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public decimal ReviewerRating { get; set; }

        // Reviewed user information  
        public string ReviewedUserId { get; set; } = string.Empty;
        public string ReviewedUserName { get; set; } = string.Empty;

        // Order/Advertisement information
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int AdvertisementId { get; set; }
        public string AdvertisementTitle { get; set; } = string.Empty;
        public string AdvertisementImageUrl { get; set; } = string.Empty;
        public decimal FinalPrice { get; set; }

        /// <summary>
        /// Gets star rating display
        /// </summary>
        public string StarDisplay
        {
            get
            {
                var filled = new string('★', Rating);
                var empty = new string('☆', 5 - Rating);
                return filled + empty;
            }
        }

        /// <summary>
        /// Gets time ago display
        /// </summary>
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CreatedAt;
                return timeSpan.TotalDays >= 1
                    ? $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago"
                    : timeSpan.TotalHours >= 1
                        ? $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours == 1 ? "" : "s")} ago"
                        : timeSpan.TotalMinutes >= 1
                            ? $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes == 1 ? "" : "s")} ago"
                            : "Just now";
            }
        }

        /// <summary>
        /// Gets formatted price
        /// </summary>
        public string FormattedPrice => $"${FinalPrice:F2}";

        /// <summary>
        /// Gets review type display
        /// </summary>
        public string ReviewTypeDisplay => ReviewType switch
        {
            ReviewType.BuyerToSeller => "Seller Review",
            ReviewType.SellerToBuyer => "Buyer Review",
            _ => "Review"
        };

        /// <summary>
        /// Gets CSS class for rating display
        /// </summary>
        public string RatingClass => Rating switch
        {
            5 => "text-success",
            4 => "text-info", 
            3 => "text-warning",
            2 => "text-danger",
            1 => "text-danger",
            _ => "text-muted"
        };
    }

    /// <summary>
    /// View model for user's reviews list
    /// </summary>
    public class UserReviewsViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal OverallRating { get; set; }
        public int TotalReviews { get; set; }
        public DateTime MemberSince { get; set; }

        // Reviews breakdown
        public List<ReviewDisplayViewModel> Reviews { get; set; } = new();
        public Dictionary<int, int> RatingBreakdown { get; set; } = new();
        
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => (int)Math.Ceiling((double)TotalReviews / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        // Filter options
        public ReviewType? FilterByType { get; set; }
        public int? FilterByRating { get; set; }
        public string? SortBy { get; set; } = "newest";

        /// <summary>
        /// Gets formatted overall rating
        /// </summary>
        public string FormattedOverallRating => OverallRating > 0 ? OverallRating.ToString("F1") : "No ratings yet";

        /// <summary>
        /// Gets star rating for overall score
        /// </summary>
        public string OverallStarDisplay
        {
            get
            {
                var fullStars = (int)Math.Floor(OverallRating);
                var hasHalfStar = OverallRating - fullStars >= 0.5m;
                var emptyStars = 5 - fullStars - (hasHalfStar ? 1 : 0);

                var stars = new string('★', fullStars);
                if (hasHalfStar) stars += "☆";
                stars += new string('☆', emptyStars);

                return stars;
            }
        }

        /// <summary>
        /// Gets percentage for each rating level
        /// </summary>
        public Dictionary<int, double> RatingPercentages
        {
            get
            {
                var result = new Dictionary<int, double>();
                for (int i = 1; i <= 5; i++)
                {
                    var count = RatingBreakdown.GetValueOrDefault(i, 0);
                    result[i] = TotalReviews > 0 ? (double)count / TotalReviews * 100 : 0;
                }
                return result;
            }
        }
    }

    /// <summary>
    /// View model for review statistics and summary
    /// </summary>
    public class ReviewStatsViewModel
    {
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingCounts { get; set; } = new();
        public List<ReviewDisplayViewModel> RecentReviews { get; set; } = new();

        // Detailed statistics
        public int SellerReviewsCount { get; set; }
        public int BuyerReviewsCount { get; set; }
        public decimal SellerAverageRating { get; set; }
        public decimal BuyerAverageRating { get; set; }

        /// <summary>
        /// Gets formatted average rating
        /// </summary>
        public string FormattedAverageRating => AverageRating > 0 ? AverageRating.ToString("F1") : "N/A";
    }

    /// <summary>
    /// View model for pending reviews (orders awaiting reviews)
    /// </summary>
    public class PendingReviewsViewModel
    {
        public List<PendingReviewItemViewModel> PendingReviews { get; set; } = new();
        public int TotalPending { get; set; }

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => (int)Math.Ceiling((double)TotalPending / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }

    /// <summary>
    /// View model for a single pending review item
    /// </summary>
    public class PendingReviewItemViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public ReviewType ReviewType { get; set; }

        // Advertisement info
        public int AdvertisementId { get; set; }
        public string AdvertisementTitle { get; set; } = string.Empty;
        public string AdvertisementImageUrl { get; set; } = string.Empty;
        public decimal FinalPrice { get; set; }

        // Other party info
        public string OtherUserId { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public decimal OtherUserRating { get; set; }

        /// <summary>
        /// Gets time since order completion
        /// </summary>
        public string TimeSinceCompletion
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CompletedAt;
                return timeSpan.TotalDays >= 1
                    ? $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago"
                    : timeSpan.TotalHours >= 1
                        ? $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours == 1 ? "" : "s")} ago"
                        : "Recently";
            }
        }

        /// <summary>
        /// Gets formatted price
        /// </summary>
        public string FormattedPrice => $"${FinalPrice:F2}";

        /// <summary>
        /// Gets review type display
        /// </summary>
        public string ReviewTypeDisplay => ReviewType switch
        {
            ReviewType.BuyerToSeller => "Rate Seller",
            ReviewType.SellerToBuyer => "Rate Buyer",
            _ => "Leave Review"
        };
    }

    /// <summary>
    /// View model for review management (admin)
    /// </summary>
    public class ReviewManagementViewModel
    {
        public List<ReviewDisplayViewModel> Reviews { get; set; } = new();
        public int TotalReviews { get; set; }

        // Filter options
        public bool? IsApproved { get; set; }
        public bool? IsReported { get; set; }
        public ReviewType? FilterByType { get; set; }
        public int? FilterByRating { get; set; }
        public string? SortBy { get; set; } = "newest";

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalReviews / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        // Statistics
        public int ReportedReviewsCount { get; set; }
        public int UnapprovedReviewsCount { get; set; }
        public int TotalReviewsToday { get; set; }

        /// <summary>
        /// Gets filter options for approval status
        /// </summary>
        public List<SelectListItem> ApprovalStatusOptions => new()
        {
            new SelectListItem { Value = "", Text = "All Reviews" },
            new SelectListItem { Value = "true", Text = "Approved Only" },
            new SelectListItem { Value = "false", Text = "Unapproved Only" }
        };

        /// <summary>
        /// Gets filter options for reported status
        /// </summary>
        public List<SelectListItem> ReportedStatusOptions => new()
        {
            new SelectListItem { Value = "", Text = "All Reviews" },
            new SelectListItem { Value = "false", Text = "Not Reported" },
            new SelectListItem { Value = "true", Text = "Reported Only" }
        };

        /// <summary>
        /// Gets sort options
        /// </summary>
        public List<SelectListItem> SortOptions => new()
        {
            new SelectListItem { Value = "newest", Text = "Newest First" },
            new SelectListItem { Value = "oldest", Text = "Oldest First" },
            new SelectListItem { Value = "rating-high", Text = "Highest Rating" },
            new SelectListItem { Value = "rating-low", Text = "Lowest Rating" },
            new SelectListItem { Value = "reported", Text = "Reported First" }
        };
    }

    /// <summary>
    /// View model for quick review actions
    /// </summary>
    public class ReviewActionViewModel
    {
        public int ReviewId { get; set; }
        public string Action { get; set; } = string.Empty; // approve, report, hide, delete
        public string? Reason { get; set; }
        public string? AdminNotes { get; set; }
    }
}