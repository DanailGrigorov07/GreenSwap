using Microsoft.AspNetCore.Mvc.Rendering;
using SecondHandGoods.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Web.Models.Admin
{
    /// <summary>
    /// Dashboard view model with platform statistics
    /// </summary>
    public class AdminDashboardViewModel
    {
        // Platform Statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int NewUsersThisWeek { get; set; }

        public int TotalAdvertisements { get; set; }
        public int ActiveAdvertisements { get; set; }
        public int SoldAdvertisements { get; set; }
        public int NewAdsToday { get; set; }
        public int NewAdsThisWeek { get; set; }

        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public int TotalReviews { get; set; }
        public int ReportedReviewsCount { get; set; }
        public int UnapprovedReviews { get; set; }
        public decimal AverageRating { get; set; }

        public int TotalMessages { get; set; }
        public int MessagesToday { get; set; }

        // Recent Activities
        public List<AdminActivityViewModel> RecentUsers { get; set; } = new();
        public List<AdminActivityViewModel> RecentAds { get; set; } = new();
        public List<AdminActivityViewModel> RecentOrders { get; set; } = new();
        public List<AdminReviewItemViewModel> ReportedReviews { get; set; } = new();
        public List<AdminAdItemViewModel> FlaggedAds { get; set; } = new();

        // Performance Metrics
        public Dictionary<string, int> UsersByMonth { get; set; } = new();
        public Dictionary<string, int> AdsByCategory { get; set; } = new();
        public Dictionary<int, int> ReviewsByRating { get; set; } = new();

        /// <summary>
        /// Get user growth rate
        /// </summary>
        public double UserGrowthRate
        {
            get
            {
                if (TotalUsers == 0) return 0;
                return NewUsersThisWeek > 0 ? (double)NewUsersThisWeek / TotalUsers * 100 : 0;
            }
        }

        /// <summary>
        /// Get advertisement conversion rate (sold/total)
        /// </summary>
        public double ConversionRate
        {
            get
            {
                if (TotalAdvertisements == 0) return 0;
                return (double)SoldAdvertisements / TotalAdvertisements * 100;
            }
        }

        /// <summary>
        /// Get order completion rate
        /// </summary>
        public double OrderCompletionRate
        {
            get
            {
                if (TotalOrders == 0) return 0;
                return (double)CompletedOrders / TotalOrders * 100;
            }
        }
    }

    /// <summary>
    /// Generic activity item for dashboard lists
    /// </summary>
    public class AdminActivityViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CreatedAt;
                return timeSpan.TotalDays >= 1
                    ? $"{(int)timeSpan.TotalDays}d ago"
                    : timeSpan.TotalHours >= 1
                        ? $"{(int)timeSpan.TotalHours}h ago"
                        : timeSpan.TotalMinutes >= 1
                            ? $"{(int)timeSpan.TotalMinutes}m ago"
                            : "Just now";
            }
        }
    }

    /// <summary>
    /// User management view model
    /// </summary>
    public class AdminUserManagementViewModel
    {
        public List<AdminUserItemViewModel> Users { get; set; } = new();
        public int TotalUsers { get; set; }

        // Filters
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public string? Role { get; set; }
        public DateTime? RegisteredAfter { get; set; }
        public DateTime? RegisteredBefore { get; set; }
        public decimal? MinRating { get; set; }
        public string SortBy { get; set; } = "newest";

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalUsers / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        // Statistics
        public int ActiveUsersCount { get; set; }
        public int InactiveUsersCount { get; set; }
        public int AdminUsersCount { get; set; }
        public int RegularUsersCount { get; set; }

        /// <summary>
        /// Get role options for filtering
        /// </summary>
        public List<SelectListItem> RoleOptions => new()
        {
            new SelectListItem { Value = "", Text = "All Roles" },
            new SelectListItem { Value = "Admin", Text = "Administrators" },
            new SelectListItem { Value = "User", Text = "Regular Users" }
        };

        /// <summary>
        /// Get sort options
        /// </summary>
        public List<SelectListItem> SortOptions => new()
        {
            new SelectListItem { Value = "newest", Text = "Newest First" },
            new SelectListItem { Value = "oldest", Text = "Oldest First" },
            new SelectListItem { Value = "name", Text = "Name A-Z" },
            new SelectListItem { Value = "rating-high", Text = "Highest Rating" },
            new SelectListItem { Value = "rating-low", Text = "Lowest Rating" },
            new SelectListItem { Value = "most-ads", Text = "Most Advertisements" },
            new SelectListItem { Value = "most-orders", Text = "Most Orders" }
        };
    }

    /// <summary>
    /// Individual user item in admin management
    /// </summary>
    public class AdminUserItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public decimal SellerRating { get; set; }
        public List<string> Roles { get; set; } = new();

        // Statistics
        public int TotalAds { get; set; }
        public int ActiveAds { get; set; }
        public int SoldAds { get; set; }
        public int TotalOrders { get; set; }
        public int TotalReviews { get; set; }
        public int TotalMessages { get; set; }

        /// <summary>
        /// Full display name
        /// </summary>
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>
        /// Registration time ago
        /// </summary>
        public string MemberSince
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CreatedAt;
                return timeSpan.TotalDays >= 30
                    ? $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) == 1 ? "" : "s")} ago"
                    : timeSpan.TotalDays >= 1
                        ? $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago"
                        : "Today";
            }
        }

        /// <summary>
        /// Last activity display
        /// </summary>
        public string LastActivity
        {
            get
            {
                if (!LastLoginAt.HasValue) return "Never";
                
                var timeSpan = DateTime.UtcNow - LastLoginAt.Value;
                return timeSpan.TotalDays >= 1
                    ? $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago"
                    : timeSpan.TotalHours >= 1
                        ? $"{(int)timeSpan.TotalHours}h ago"
                        : "Recently";
            }
        }

        /// <summary>
        /// User status badge class
        /// </summary>
        public string StatusBadgeClass => IsActive ? "badge bg-success" : "badge bg-danger";

        /// <summary>
        /// User status text
        /// </summary>
        public string StatusText => IsActive ? "Active" : "Inactive";

        /// <summary>
        /// Indicates whether the account is currently locked out.
        /// </summary>
        public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;

        /// <summary>
        /// Primary role display
        /// </summary>
        public string PrimaryRole => Roles.Contains("Admin") ? "Admin" : "User";

        /// <summary>
        /// Role badge class
        /// </summary>
        public string RoleBadgeClass => PrimaryRole == "Admin" ? "badge bg-primary" : "badge bg-secondary";
    }

    /// <summary>
    /// Advertisement management view model
    /// </summary>
    public class AdminAdManagementViewModel
    {
        public List<AdminAdItemViewModel> Advertisements { get; set; } = new();
        public int TotalAds { get; set; }

        // Filters
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsSold { get; set; }
        public bool? IsFeatured { get; set; }
        public int? CategoryId { get; set; }
        public ItemCondition? Condition { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public DateTime? PostedAfter { get; set; }
        public DateTime? PostedBefore { get; set; }
        public string SortBy { get; set; } = "newest";

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalPages => (int)Math.Ceiling((double)TotalAds / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        // Statistics
        public int ActiveAdsCount { get; set; }
        public int SoldAdsCount { get; set; }
        public int InactiveAdsCount { get; set; }
        public int FeaturedAdsCount { get; set; }
        public int ExpiredAdsCount { get; set; }

        // Dropdowns
        public List<SelectListItem> Categories { get; set; } = new();

        /// <summary>
        /// Get condition options
        /// </summary>
        public List<SelectListItem> ConditionOptions => new()
        {
            new SelectListItem { Value = "", Text = "All Conditions" },
            new SelectListItem { Value = "1", Text = "New" },
            new SelectListItem { Value = "2", Text = "Used" },
            new SelectListItem { Value = "3", Text = "Damaged" },
            new SelectListItem { Value = "4", Text = "Refurbished" }
        };

        /// <summary>
        /// Get sort options
        /// </summary>
        public List<SelectListItem> SortOptions => new()
        {
            new SelectListItem { Value = "newest", Text = "Newest First" },
            new SelectListItem { Value = "oldest", Text = "Oldest First" },
            new SelectListItem { Value = "price-high", Text = "Highest Price" },
            new SelectListItem { Value = "price-low", Text = "Lowest Price" },
            new SelectListItem { Value = "most-views", Text = "Most Views" },
            new SelectListItem { Value = "expiring", Text = "Expiring Soon" }
        };
    }

    /// <summary>
    /// Individual advertisement item in admin management
    /// </summary>
    public class AdminAdItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public ItemCondition Condition { get; set; }
        public bool IsActive { get; set; }
        public bool IsSold { get; set; }
        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Category info
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryIconClass { get; set; }

        // User info
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public decimal UserRating { get; set; }

        // Images
        public string MainImageUrl { get; set; } = string.Empty;
        public int ImageCount { get; set; }

        // Metrics
        public int MessageCount { get; set; }
        public int OrderCount { get; set; }

        /// <summary>
        /// Formatted price
        /// </summary>
        public string FormattedPrice => $"${Price:F2}";

        /// <summary>
        /// Truncated description
        /// </summary>
        public string TruncatedDescription
        {
            get
            {
                const int maxLength = 100;
                return Description.Length > maxLength 
                    ? Description.Substring(0, maxLength) + "..." 
                    : Description;
            }
        }

        /// <summary>
        /// Time since posted
        /// </summary>
        public string PostedAgo
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

        /// <summary>
        /// Days until expiration
        /// </summary>
        public int DaysUntilExpiration => (int)Math.Ceiling((ExpiresAt - DateTime.UtcNow).TotalDays);

        /// <summary>
        /// Status badge class
        /// </summary>
        public string StatusBadgeClass
        {
            get
            {
                if (IsSold) return "badge bg-success";
                if (!IsActive) return "badge bg-secondary";
                if (DaysUntilExpiration <= 0) return "badge bg-danger";
                if (DaysUntilExpiration <= 3) return "badge bg-warning text-dark";
                return "badge bg-primary";
            }
        }

        /// <summary>
        /// Status text
        /// </summary>
        public string StatusText
        {
            get
            {
                if (IsSold) return "SOLD";
                if (!IsActive) return "INACTIVE";
                if (DaysUntilExpiration <= 0) return "EXPIRED";
                if (DaysUntilExpiration <= 3) return $"EXPIRES {DaysUntilExpiration}D";
                return "ACTIVE";
            }
        }

        /// <summary>
        /// Condition display
        /// </summary>
        public string ConditionDisplay => Condition switch
        {
            ItemCondition.New => "New",
            ItemCondition.Used => "Used",
            ItemCondition.Damaged => "Damaged",
            ItemCondition.Refurbished => "Refurbished",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Review item for admin review management
    /// </summary>
    public class AdminReviewItemViewModel
    {
        public int Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public ReviewType ReviewType { get; set; }
        public bool IsReported { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }

        // User info
        public string ReviewerId { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewedUserId { get; set; } = string.Empty;
        public string ReviewedUserName { get; set; } = string.Empty;

        // Order info
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string AdvertisementTitle { get; set; } = string.Empty;

        /// <summary>
        /// Truncated comment
        /// </summary>
        public string TruncatedComment
        {
            get
            {
                if (string.IsNullOrEmpty(Comment)) return "No comment";
                const int maxLength = 80;
                return Comment.Length > maxLength 
                    ? Comment.Substring(0, maxLength) + "..." 
                    : Comment;
            }
        }

        /// <summary>
        /// Review type display
        /// </summary>
        public string ReviewTypeDisplay => ReviewType switch
        {
            ReviewType.BuyerToSeller => "Buyer → Seller",
            ReviewType.SellerToBuyer => "Seller → Buyer",
            _ => "Unknown"
        };

        /// <summary>
        /// Status display
        /// </summary>
        public string StatusDisplay
        {
            get
            {
                if (IsReported) return "Reported";
                if (!IsApproved) return "Pending";
                return "Approved";
            }
        }

        /// <summary>
        /// Status badge class
        /// </summary>
        public string StatusBadgeClass
        {
            get
            {
                if (IsReported) return "badge bg-danger";
                if (!IsApproved) return "badge bg-warning text-dark";
                return "badge bg-success";
            }
        }
    }

    /// <summary>
    /// User action view model for admin operations
    /// </summary>
    public class AdminUserActionViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Action { get; set; } = string.Empty; // activate, deactivate, promote, demote, delete

        public string? Reason { get; set; }
        public string? AdminNotes { get; set; }
    }

    /// <summary>
    /// Advertisement action view model for admin operations
    /// </summary>
    public class AdminAdActionViewModel
    {
        [Required]
        public int AdId { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // activate, deactivate, feature, unfeature, delete

        public string? Reason { get; set; }
        public string? AdminNotes { get; set; }
    }

    /// <summary>
    /// Platform statistics view model
    /// </summary>
    public class AdminStatsViewModel
    {
        public Dictionary<string, object> GeneralStats { get; set; } = new();
        public List<CategoryStatsItem> CategoryStats { get; set; } = new();
        public List<UserActivityItem> TopUsers { get; set; } = new();
        public List<PopularSearchItem> PopularSearches { get; set; } = new();
        public Dictionary<string, int> MonthlySignups { get; set; } = new();
        public Dictionary<string, int> MonthlyAds { get; set; } = new();
        public Dictionary<string, decimal> MonthlyRevenue { get; set; } = new();
    }

    /// <summary>
    /// Category statistics item
    /// </summary>
    public class CategoryStatsItem
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? IconClass { get; set; }
        public int TotalAds { get; set; }
        public int ActiveAds { get; set; }
        public int SoldAds { get; set; }
        public decimal AveragePrice { get; set; }
        public long TotalViews { get; set; }

        public double ConversionRate => TotalAds > 0 ? (double)SoldAds / TotalAds * 100 : 0;
        public string FormattedAveragePrice => AveragePrice > 0 ? $"${AveragePrice:F2}" : "N/A";
    }

    /// <summary>
    /// User activity item for top users
    /// </summary>
    public class UserActivityItem
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public int TotalAds { get; set; }
        public int TotalSales { get; set; }
        public int TotalPurchases { get; set; }
        public decimal TotalRevenue { get; set; }

        public string FormattedRevenue => $"${TotalRevenue:F2}";
    }

    /// <summary>
    /// Popular search item for analytics
    /// </summary>
    public class PopularSearchItem
    {
        public string SearchTerm { get; set; } = string.Empty;
        public int SearchCount { get; set; }
        public int ResultCount { get; set; }
        public DateTime LastSearched { get; set; }
    }

    /// <summary>
    /// Bulk actions view model
    /// </summary>
    public class AdminBulkActionViewModel
    {
        [Required]
        public List<int> SelectedIds { get; set; } = new();

        [Required]
        public string Action { get; set; } = string.Empty;

        public string? Reason { get; set; }
        public string EntityType { get; set; } = string.Empty; // users, ads, reviews
    }

    /// <summary>
    /// View model for a site ad slot in the admin list
    /// </summary>
    public class SiteAdItemViewModel
    {
        public int Id { get; set; }
        public string SlotKey { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? TargetUrl { get; set; }
        public string? AltText { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// View model for editing a site ad (footer paid ad slot)
    /// </summary>
    public class SiteAdEditViewModel
    {
        public int Id { get; set; }
        public string SlotKey { get; set; } = string.Empty;
        [Display(Name = "Image URL")]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;
        [Url]
        [Display(Name = "Link URL")]
        [StringLength(500)]
        public string? TargetUrl { get; set; }
        [Display(Name = "Alt text")]
        [StringLength(200)]
        public string? AltText { get; set; }
        public int DisplayOrder { get; set; }
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}