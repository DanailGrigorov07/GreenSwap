using Microsoft.AspNetCore.Mvc.Rendering;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Web.Models.Ads;
using SecondHandGoods.Web.Models.Categories;
using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Web.Models.Search
{
    /// <summary>
    /// View model for advanced search functionality
    /// </summary>
    public class AdvancedSearchViewModel
    {
        // Search criteria
        [Display(Name = "Keywords")]
        [StringLength(200)]
        public string? Keywords { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Minimum Price")]
        [Range(0, 999999.99)]
        public decimal? MinPrice { get; set; }

        [Display(Name = "Maximum Price")]
        [Range(0, 999999.99)]
        public decimal? MaxPrice { get; set; }

        [Display(Name = "Item Conditions")]
        public List<ItemCondition>? Conditions { get; set; }

        [Display(Name = "Location")]
        [StringLength(200)]
        public string? Location { get; set; }

        [Display(Name = "Posted Before")]
        public string? PostedBeforeTime { get; set; } // e.g., "30min", "1hr", "1week", "none"

        [Display(Name = "Minimum Seller Rating")]
        [Range(0, 5)]
        public decimal? MinSellerRating { get; set; }

        [Display(Name = "Sort By")]
        public string SortBy { get; set; } = "newest";

        // Additional filters
        [Display(Name = "Has Images")]
        public bool HasImages { get; set; }

        [Display(Name = "Price Negotiable Only")]
        public bool PriceNegotiableOnly { get; set; }

        [Display(Name = "Featured Items Only")]
        public bool FeaturedOnly { get; set; }

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // Results
        public List<AdvertisementCardViewModel> Results { get; set; } = new();
        public int TotalResults { get; set; }
        public bool HasSearched { get; set; }

        // Dropdowns
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> SortOptions { get; set; } = new();

        // Computed properties
        public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        /// <summary>
        /// Gets all available condition options with checkboxes
        /// </summary>
        public List<ConditionCheckboxViewModel> GetConditionOptions()
        {
            return Enum.GetValues<ItemCondition>()
                .Select(c => new ConditionCheckboxViewModel
                {
                    Value = c,
                    Text = GetConditionDisplayName(c),
                    IsSelected = Conditions?.Contains(c) ?? false
                })
                .ToList();
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

        /// <summary>
        /// Generates search summary for display
        /// </summary>
        public string GetSearchSummary()
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Keywords))
                parts.Add($"'{Keywords}'");

            if (CategoryId.HasValue)
            {
                var categoryName = Categories.FirstOrDefault(c => c.Value == CategoryId.ToString())?.Text;
                if (!string.IsNullOrEmpty(categoryName))
                    parts.Add($"in {categoryName}");
            }

            if (MinPrice.HasValue || MaxPrice.HasValue)
            {
                if (MinPrice.HasValue && MaxPrice.HasValue)
                    parts.Add($"${MinPrice:F2} - ${MaxPrice:F2}");
                else if (MinPrice.HasValue)
                    parts.Add($"from ${MinPrice:F2}");
                else if (MaxPrice.HasValue)
                    parts.Add($"up to ${MaxPrice:F2}");
            }

            if (!string.IsNullOrWhiteSpace(Location))
                parts.Add($"near {Location}");

            return parts.Any() ? string.Join(", ", parts) : "All items";
        }
    }

    /// <summary>
    /// View model for condition checkbox options
    /// </summary>
    public class ConditionCheckboxViewModel
    {
        public ItemCondition Value { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// Response model for search suggestions API
    /// </summary>
    public class SearchSuggestionsResponse
    {
        public List<CategorySuggestion> Categories { get; set; } = new();
        public List<AdvertisementSuggestion> Advertisements { get; set; } = new();
        public List<string> PopularSearches { get; set; } = new();
        public List<string> RecentSearches { get; set; } = new();
    }

    /// <summary>
    /// View model for saved searches
    /// </summary>
    public class SavedSearchViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SearchUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool NotifyOnNewItems { get; set; }
        public int? LastResultCount { get; set; }

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

    /// <summary>
    /// Request model for saving searches
    /// </summary>
    public class SaveSearchRequest
    {
        public string Name { get; set; } = string.Empty;
        public string SearchUrl { get; set; } = string.Empty;
        public bool NotifyOnNewItems { get; set; }
    }

    /// <summary>
    /// View model for quick search widget
    /// </summary>
    public class QuickSearchViewModel
    {
        public string? Query { get; set; }
        public List<CategoryCardViewModel> PopularCategories { get; set; } = new();
        public List<AdvertisementCardViewModel> FeaturedItems { get; set; } = new();
        public List<string> TrendingSearches { get; set; } = new();
    }
}