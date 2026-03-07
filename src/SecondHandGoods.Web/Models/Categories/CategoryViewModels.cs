namespace SecondHandGoods.Web.Models.Categories
{
    /// <summary>
    /// View model for the categories index page
    /// </summary>
    public class CategoriesIndexViewModel
    {
        public List<CategoryCardViewModel> Categories { get; set; } = new();
        public int TotalCategories { get; set; }
        public int TotalActiveAds { get; set; }

        /// <summary>
        /// Groups categories by display order for layout purposes
        /// </summary>
        public IEnumerable<IGrouping<int, CategoryCardViewModel>> CategoryGroups => 
            Categories.GroupBy(c => (c.DisplayOrder - 1) / 4); // 4 categories per row
    }

    /// <summary>
    /// View model for individual category cards
    /// </summary>
    public class CategoryCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string? IconClass { get; set; }
        public int DisplayOrder { get; set; }
        public int AdCount { get; set; }

        /// <summary>
        /// Gets URL-friendly category browse link
        /// </summary>
        public string BrowseUrl => $"/Categories/Browse/{Id}/{Slug}";

        /// <summary>
        /// Gets formatted advertisement count
        /// </summary>
        public string FormattedAdCount => AdCount switch
        {
            0 => "No items",
            1 => "1 item",
            _ => $"{AdCount:N0} items"
        };

        /// <summary>
        /// Gets CSS class for category card based on ad count
        /// </summary>
        public string CardClass => AdCount switch
        {
            0 => "category-card-empty",
            < 10 => "category-card-low",
            < 50 => "category-card-medium",
            _ => "category-card-high"
        };
    }

    /// <summary>
    /// View model for category statistics (admin/analytics)
    /// </summary>
    public class CategoryStatsViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? IconClass { get; set; }
        public int TotalAds { get; set; }
        public int ActiveAds { get; set; }
        public int SoldAds { get; set; }
        public decimal AveragePrice { get; set; }
        public long TotalViews { get; set; }

        /// <summary>
        /// Gets the success rate (sold/total) as percentage
        /// </summary>
        public double SuccessRate => TotalAds > 0 ? (double)SoldAds / TotalAds * 100 : 0;

        /// <summary>
        /// Gets formatted average price
        /// </summary>
        public string FormattedAveragePrice => AveragePrice > 0 ? $"${AveragePrice:F2}" : "N/A";

        /// <summary>
        /// Gets activity level based on active ads count
        /// </summary>
        public string ActivityLevel => ActiveAds switch
        {
            0 => "No Activity",
            < 5 => "Low Activity", 
            < 20 => "Medium Activity",
            < 50 => "High Activity",
            _ => "Very High Activity"
        };

        /// <summary>
        /// Gets activity CSS class for styling
        /// </summary>
        public string ActivityClass => ActiveAds switch
        {
            0 => "badge bg-secondary",
            < 5 => "badge bg-danger",
            < 20 => "badge bg-warning",
            < 50 => "badge bg-info",
            _ => "badge bg-success"
        };
    }

    /// <summary>
    /// View model for search suggestions and autocomplete
    /// </summary>
    public class SearchSuggestionViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<CategorySuggestion> Categories { get; set; } = new();
        public List<string> PopularSearches { get; set; } = new();
        public List<string> RecentSearches { get; set; } = new();
        public List<AdvertisementSuggestion> FeaturedItems { get; set; } = new();
    }

    /// <summary>
    /// Category suggestion for search autocomplete
    /// </summary>
    public class CategorySuggestion
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IconClass { get; set; }
        public int AdCount { get; set; }
        public string BrowseUrl => $"/Categories/Browse/{Id}";
    }

    /// <summary>
    /// Advertisement suggestion for search
    /// </summary>
    public class AdvertisementSuggestion
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string DetailUrl => $"/Ads/Details/{Id}";
        public string FormattedPrice => $"${Price:F2}";
    }
}