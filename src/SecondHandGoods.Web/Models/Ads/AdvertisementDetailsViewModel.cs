using SecondHandGoods.Data.Entities;

namespace SecondHandGoods.Web.Models.Ads
{
    /// <summary>
    /// View model for displaying advertisement details
    /// </summary>
    public class AdvertisementDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsPriceNegotiable { get; set; }
        public ItemCondition Condition { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsSold { get; set; }
        public int ViewCount { get; set; }

        // Category info
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryIconClass { get; set; }

        // Seller info
        public string SellerId { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public string SellerLocation { get; set; } = string.Empty;
        public decimal SellerRating { get; set; }
        public int SellerRatingCount { get; set; }
        public DateTime SellerMemberSince { get; set; }
        public string? SellerBio { get; set; }

        // Images
        public List<AdvertisementImageViewModel> Images { get; set; } = new();

        // User permissions
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanContact { get; set; }
        public bool CanFavorite { get; set; }
        public bool IsFavorited { get; set; }

        /// <summary>
        /// Gets formatted price display
        /// </summary>
        public string FormattedPrice => IsPriceNegotiable ? $"${Price:F2} (negotiable)" : $"${Price:F2}";

        /// <summary>
        /// Gets condition badge CSS class
        /// </summary>
        public string ConditionBadgeClass => Condition switch
        {
            ItemCondition.New => "badge bg-success",
            ItemCondition.Used => "badge bg-primary", 
            ItemCondition.Damaged => "badge bg-warning text-dark",
            ItemCondition.Refurbished => "badge bg-info",
            _ => "badge bg-secondary"
        };

        /// <summary>
        /// Gets user-friendly condition display with description
        /// </summary>
        public string ConditionDisplay => Condition switch
        {
            ItemCondition.New => "New",
            ItemCondition.Used => "Used - Good Condition",
            ItemCondition.Damaged => "Damaged/For Parts",
            ItemCondition.Refurbished => "Refurbished/Restored",
            _ => Condition.ToString()
        };

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
                        : "Less than an hour ago";
            }
        }

        /// <summary>
        /// Gets seller rating stars for display
        /// </summary>
        public (int fullStars, bool hasHalfStar, int emptyStars) GetSellerRatingStars()
        {
            var fullStars = (int)SellerRating;
            var hasHalfStar = (SellerRating - fullStars) >= 0.5m;
            var emptyStars = 5 - fullStars - (hasHalfStar ? 1 : 0);
            
            return (fullStars, hasHalfStar, emptyStars);
        }

        /// <summary>
        /// Gets days until expiration
        /// </summary>
        public int DaysUntilExpiration
        {
            get
            {
                var timeSpan = ExpiresAt - DateTime.UtcNow;
                return Math.Max(0, (int)timeSpan.TotalDays);
            }
        }

        /// <summary>
        /// Gets status badge info
        /// </summary>
        public (string text, string cssClass) GetStatusBadge()
        {
            if (IsSold)
                return ("SOLD", "badge bg-danger");
            
            if (DaysUntilExpiration <= 0)
                return ("EXPIRED", "badge bg-secondary");
            
            if (DaysUntilExpiration <= 7)
                return ($"Expires in {DaysUntilExpiration} day{(DaysUntilExpiration == 1 ? "" : "s")}", "badge bg-warning text-dark");
            
            if (IsFeatured)
                return ("FEATURED", "badge bg-success");
            
            return ("ACTIVE", "badge bg-primary");
        }
    }
}