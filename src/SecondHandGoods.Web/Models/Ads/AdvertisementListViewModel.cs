using Microsoft.AspNetCore.Mvc.Rendering;
using SecondHandGoods.Data.Entities;

namespace SecondHandGoods.Web.Models.Ads
{
    /// <summary>
    /// View model for displaying a list of advertisements with search and filtering
    /// </summary>
    public class AdvertisementListViewModel
    {
        // Search and filter parameters
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public ItemCondition? Condition { get; set; }
        public string? Location { get; set; }
        public bool? PriceNegotiable { get; set; }
        public string SortBy { get; set; } = "newest"; // newest, oldest, price-low, price-high

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        // Results
        public List<AdvertisementCardViewModel> Advertisements { get; set; } = new();

        // Dropdown options
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Conditions { get; set; } = new();
        public List<SelectListItem> SortOptions { get; set; } = new();

        // Additional info
        public string? CurrentCategoryName { get; set; }
        public bool IsUserAds { get; set; } // True when viewing current user's ads
        /// <summary>True when viewing the current user's bookmarked advertisements.</summary>
        public bool IsFavoritesPage { get; set; }

        public AdvertisementListViewModel()
        {
            PopulateSortOptions();
            PopulateConditions();
        }

        /// <summary>
        /// Populates sort dropdown options
        /// </summary>
        public void PopulateSortOptions()
        {
            SortOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "newest", Text = "Newest First" },
                new SelectListItem { Value = "oldest", Text = "Oldest First" },
                new SelectListItem { Value = "price-low", Text = "Price: Low to High" },
                new SelectListItem { Value = "price-high", Text = "Price: High to Low" },
                new SelectListItem { Value = "title", Text = "Title A-Z" }
            };
        }

        /// <summary>
        /// Populates condition dropdown options
        /// </summary>
        public void PopulateConditions()
        {
            Conditions = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Any Condition" }
            };

            Conditions.AddRange(Enum.GetValues<ItemCondition>()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = GetConditionDisplayName(c)
                }));
        }

        /// <summary>
        /// Gets user-friendly display name for condition
        /// </summary>
        private static string GetConditionDisplayName(ItemCondition condition)
        {
            return condition switch
            {
                ItemCondition.New => "New",
                ItemCondition.Used => "Used - Good Condition",
                ItemCondition.Damaged => "Damaged/For Parts",
                ItemCondition.Refurbished => "Refurbished/Restored",
                _ => condition.ToString()
            };
        }
    }

    /// <summary>
    /// View model for advertisement card display in list views
    /// </summary>
    public class AdvertisementCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsPriceNegotiable { get; set; }
        public ItemCondition Condition { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsSold { get; set; }
        public int ViewCount { get; set; }

        // Category info
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryIconClass { get; set; }

        // User info
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal UserRating { get; set; }

        // Main image
        public string? MainImageUrl { get; set; }
        public string? MainImageAlt { get; set; }

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
        /// Gets user-friendly condition display
        /// </summary>
        public string ConditionDisplay => Condition switch
        {
            ItemCondition.New => "New",
            ItemCondition.Used => "Used",
            ItemCondition.Damaged => "For Parts",
            ItemCondition.Refurbished => "Refurbished",
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
    }
}