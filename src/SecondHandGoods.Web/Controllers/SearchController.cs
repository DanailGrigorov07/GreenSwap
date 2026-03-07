using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandGoods.Data;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Web.Models.Categories;
using SecondHandGoods.Web.Models.Search;
using System.Text.Json;

namespace SecondHandGoods.Web.Controllers
{
    /// <summary>
    /// Controller for enhanced search functionality, autocomplete, and suggestions
    /// </summary>
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ApplicationDbContext context, ILogger<SearchController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Provides search suggestions and autocomplete data
        /// </summary>
        /// <param name="q">Search query</param>
        /// <param name="limit">Number of suggestions to return</param>
        /// <returns>JSON response with suggestions</returns>
        [HttpGet]
        public async Task<IActionResult> Suggestions(string? q, int limit = 10)
        {
            try
            {
                var suggestions = new SearchSuggestionsResponse();

                if (!string.IsNullOrWhiteSpace(q) && q.Length >= 2)
                {
                    var searchTerm = q.ToLower().Trim();

                    // Get matching categories
                    var categories = await _context.Categories
                        .Where(c => c.IsActive && c.Name.ToLower().Contains(searchTerm))
                        .OrderBy(c => c.DisplayOrder)
                        .Take(limit / 2)
                        .Select(c => new CategorySuggestion
                        {
                            Id = c.Id,
                            Name = c.Name,
                            IconClass = c.IconClass,
                            AdCount = _context.Advertisements
                                .Where(a => a.CategoryId == c.Id && a.IsActive && !a.IsDeleted && !a.IsSold)
                                .Count()
                        })
                        .ToListAsync();

                    suggestions.Categories = categories;

                    // Get matching advertisement titles
                    var adTitles = await _context.Advertisements
                        .Where(a => a.IsActive && !a.IsDeleted && !a.IsSold && 
                                   a.ExpiresAt > DateTime.UtcNow &&
                                   a.Title.ToLower().Contains(searchTerm))
                        .OrderByDescending(a => a.ViewCount)
                        .Take(limit / 2)
                        .Select(a => new AdvertisementSuggestion
                        {
                            Id = a.Id,
                            Title = a.Title,
                            Price = a.Price,
                            CategoryName = a.Category.Name,
                            ImageUrl = a.Images
                                .Where(img => img.IsMainImage)
                                .Select(img => img.ImageUrl)
                                .FirstOrDefault() ?? "/images/no-image.svg"
                        })
                        .ToListAsync();

                    suggestions.Advertisements = adTitles;

                    // Get popular search terms (simplified - in production this would come from search analytics)
                    suggestions.PopularSearches = GetPopularSearchTerms(searchTerm);
                }

                return Json(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting search suggestions for query: {Query}", q);
                return Json(new SearchSuggestionsResponse());
            }
        }

        /// <summary>
        /// Advanced search page with detailed filters
        /// </summary>
        public async Task<IActionResult> Advanced()
        {
            try
            {
                var model = new AdvancedSearchViewModel();
                await PopulateAdvancedSearchData(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading advanced search page");
                TempData["Error"] = "An error occurred while loading the search page. Please try again.";
                return RedirectToAction("Index", "Ads");
            }
        }

        /// <summary>
        /// Execute advanced search with detailed criteria
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Advanced(AdvancedSearchViewModel model)
        {
            try
            {
                // Handle Conditions from form checkboxes
                var conditionsFromForm = Request.Form["Conditions"];
                if (conditionsFromForm.Count > 0)
                {
                    model.Conditions = conditionsFromForm
                        .Select(c => (ItemCondition)int.Parse(c))
                        .ToList();
                }

                await PopulateAdvancedSearchData(model);

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Build search query
                var query = _context.Advertisements
                    .Include(a => a.Category)
                    .Include(a => a.User)
                    .Include(a => a.Images)
                    .Where(a => a.IsActive && !a.IsDeleted && a.ExpiresAt > DateTime.UtcNow)
                    .AsQueryable();

                // Apply advanced filters
                query = ApplyAdvancedFilters(query, model);

                // Apply sorting
                query = ApplySorting(query, model.SortBy);

                // Save search if user is authenticated
                if (User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(model.Keywords))
                {
                    await SaveSearchHistory(model);
                }

                // Execute search with pagination
                var totalCount = await query.CountAsync();
                
                var advertisements = await query
                    .Skip((model.Page - 1) * model.PageSize)
                    .Take(model.PageSize)
                    .Select(a => new Models.Ads.AdvertisementCardViewModel
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Description = a.Description.Length > 150 ? a.Description.Substring(0, 150) + "..." : a.Description,
                        Price = a.Price,
                        IsPriceNegotiable = a.IsPriceNegotiable,
                        Condition = a.Condition,
                        Location = a.Location,
                        CreatedAt = a.CreatedAt,
                        IsFeatured = a.IsFeatured,
                        IsSold = a.IsSold,
                        ViewCount = a.ViewCount,
                        CategoryId = a.CategoryId,
                        CategoryName = a.Category.Name,
                        CategoryIconClass = a.Category.IconClass,
                        UserId = a.UserId,
                        UserName = a.User.FirstName + " " + a.User.LastName,
                        UserRating = a.User.SellerRating,
                        MainImageUrl = a.Images
                            .Where(img => img.IsMainImage)
                            .Select(img => img.ImageUrl)
                            .FirstOrDefault() ?? "/images/no-image.svg",
                        MainImageAlt = a.Images
                            .Where(img => img.IsMainImage)
                            .Select(img => img.AltText)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                model.Results = advertisements;
                model.TotalResults = totalCount;
                model.HasSearched = true;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during advanced search");
                TempData["Error"] = "An error occurred while searching. Please try again.";
                await PopulateAdvancedSearchData(model);
                return View(model);
            }
        }

        /// <summary>
        /// Get saved searches for the current user
        /// </summary>
        public async Task<IActionResult> SavedSearches()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Challenge();
            }

            try
            {
                // In a real implementation, you'd have a SavedSearch entity
                // For now, we'll simulate with session-based storage
                var savedSearchesJson = HttpContext.Session.GetString($"SavedSearches_{User.Identity.Name}");
                var savedSearches = string.IsNullOrEmpty(savedSearchesJson) 
                    ? new List<SavedSearchViewModel>() 
                    : JsonSerializer.Deserialize<List<SavedSearchViewModel>>(savedSearchesJson) ?? new List<SavedSearchViewModel>();

                return View(savedSearches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading saved searches");
                TempData["Error"] = "An error occurred while loading your saved searches.";
                return View(new List<SavedSearchViewModel>());
            }
        }

        /// <summary>
        /// Save a search for later use
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveSearch([FromBody] SaveSearchRequest request)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized();
            }

            try
            {
                var savedSearchesJson = HttpContext.Session.GetString($"SavedSearches_{User.Identity.Name}");
                var savedSearches = string.IsNullOrEmpty(savedSearchesJson) 
                    ? new List<SavedSearchViewModel>() 
                    : JsonSerializer.Deserialize<List<SavedSearchViewModel>>(savedSearchesJson) ?? new List<SavedSearchViewModel>();

                // Check if search already exists
                var existingSearch = savedSearches.FirstOrDefault(s => s.SearchUrl == request.SearchUrl);
                if (existingSearch != null)
                {
                    return Json(new { success = false, message = "Search already saved" });
                }

                // Add new saved search
                var newSavedSearch = new SavedSearchViewModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = request.Name,
                    SearchUrl = request.SearchUrl,
                    CreatedAt = DateTime.UtcNow,
                    NotifyOnNewItems = request.NotifyOnNewItems
                };

                savedSearches.Add(newSavedSearch);
                
                // Keep only last 20 searches
                if (savedSearches.Count > 20)
                {
                    savedSearches = savedSearches.OrderByDescending(s => s.CreatedAt).Take(20).ToList();
                }

                HttpContext.Session.SetString($"SavedSearches_{User.Identity.Name}", JsonSerializer.Serialize(savedSearches));

                return Json(new { success = true, message = "Search saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving search");
                return Json(new { success = false, message = "Failed to save search" });
            }
        }

        #region Helper Methods

        /// <summary>
        /// Apply advanced search filters to query
        /// </summary>
        private static IQueryable<Advertisement> ApplyAdvancedFilters(IQueryable<Advertisement> query, AdvancedSearchViewModel model)
        {
            // Keywords search in title and description
            if (!string.IsNullOrWhiteSpace(model.Keywords))
            {
                var keywords = model.Keywords.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var keyword in keywords)
                {
                    query = query.Where(a => a.Title.ToLower().Contains(keyword) || 
                                           a.Description.ToLower().Contains(keyword));
                }
            }

            // Category filter
            if (model.CategoryId.HasValue && model.CategoryId.Value > 0)
            {
                query = query.Where(a => a.CategoryId == model.CategoryId.Value);
            }

            // Price range
            if (model.MinPrice.HasValue)
            {
                query = query.Where(a => a.Price >= model.MinPrice.Value);
            }
            if (model.MaxPrice.HasValue)
            {
                query = query.Where(a => a.Price <= model.MaxPrice.Value);
            }

            // Condition filter
            if (model.Conditions?.Any() == true)
            {
                query = query.Where(a => model.Conditions.Contains(a.Condition));
            }

            // Location filter
            if (!string.IsNullOrWhiteSpace(model.Location))
            {
                var location = model.Location.ToLower();
                query = query.Where(a => a.Location != null && a.Location.ToLower().Contains(location));
            }

            // Date range - Posted Before Time
            if (!string.IsNullOrWhiteSpace(model.PostedBeforeTime) && model.PostedBeforeTime != "none")
            {
                var cutoffDate = GetCutoffDate(model.PostedBeforeTime);
                if (cutoffDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= cutoffDate.Value);
                }
            }

            // Seller rating
            if (model.MinSellerRating.HasValue)
            {
                query = query.Where(a => a.User.SellerRating >= model.MinSellerRating.Value);
            }

            // Additional filters
            if (model.HasImages)
            {
                query = query.Where(a => a.Images.Any());
            }
            if (model.PriceNegotiableOnly)
            {
                query = query.Where(a => a.IsPriceNegotiable);
            }
            if (model.FeaturedOnly)
            {
                query = query.Where(a => a.IsFeatured);
            }

            return query;
        }

        /// <summary>
        /// Apply sorting to query
        /// </summary>
        private static IQueryable<Advertisement> ApplySorting(IQueryable<Advertisement> query, string sortBy)
        {
            return sortBy?.ToLower() switch
            {
                "oldest" => query.OrderBy(a => a.CreatedAt),
                "price-low" => query.OrderBy(a => a.Price),
                "price-high" => query.OrderByDescending(a => a.Price),
                "title" => query.OrderBy(a => a.Title),
                "popular" => query.OrderByDescending(a => a.ViewCount),
                "rating" => query.OrderByDescending(a => a.User.SellerRating),
                "newest" or _ => query.OrderByDescending(a => a.CreatedAt)
            };
        }

        /// <summary>
        /// Populate dropdown data for advanced search
        /// </summary>
        private async Task PopulateAdvancedSearchData(AdvancedSearchViewModel model)
        {
            model.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();

            model.SortOptions = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "newest", Text = "Newest First" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "oldest", Text = "Oldest First" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "price-low", Text = "Price: Low to High" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "price-high", Text = "Price: High to Low" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "popular", Text = "Most Popular" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "rating", Text = "Highest Rated Sellers" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "title", Text = "Title A-Z" }
            };
        }

        /// <summary>
        /// Save search to user's history
        /// </summary>
        private async Task SaveSearchHistory(AdvancedSearchViewModel model)
        {
            try
            {
                // In production, you'd save to a SearchHistory table
                // For now, we'll use session storage
                var searchHistoryJson = HttpContext.Session.GetString($"SearchHistory_{User.Identity?.Name}");
                var searchHistory = string.IsNullOrEmpty(searchHistoryJson) 
                    ? new List<string>() 
                    : JsonSerializer.Deserialize<List<string>>(searchHistoryJson) ?? new List<string>();

                var searchTerm = model.Keywords.Trim();
                if (!searchHistory.Contains(searchTerm, StringComparer.OrdinalIgnoreCase))
                {
                    searchHistory.Insert(0, searchTerm);
                    
                    // Keep only last 10 searches
                    if (searchHistory.Count > 10)
                    {
                        searchHistory = searchHistory.Take(10).ToList();
                    }

                    HttpContext.Session.SetString($"SearchHistory_{User.Identity.Name}", JsonSerializer.Serialize(searchHistory));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save search history for user {User}", User.Identity?.Name);
            }
        }

        /// <summary>
        /// Get popular search terms (mock implementation)
        /// </summary>
        private static List<string> GetPopularSearchTerms(string currentSearch)
        {
            var popularTerms = new List<string>
            {
                "iPhone", "laptop", "furniture", "bicycle", "guitar", 
                "car", "camera", "watch", "book", "shoes"
            };

            return popularTerms
                .Where(term => term.StartsWith(currentSearch, StringComparison.OrdinalIgnoreCase))
                .Take(3)
                .ToList();
        }

        /// <summary>
        /// Get cutoff date based on posted before time option
        /// </summary>
        private static DateTime? GetCutoffDate(string? postedBeforeTime)
        {
            if (string.IsNullOrWhiteSpace(postedBeforeTime) || postedBeforeTime == "none")
                return null;

            var now = DateTime.UtcNow;
            return postedBeforeTime.ToLower() switch
            {
                "30min" => now.AddMinutes(-30),
                "1hr" => now.AddHours(-1),
                "6hr" => now.AddHours(-6),
                "24hr" => now.AddHours(-24),
                "2days" => now.AddDays(-2),
                "4days" => now.AddDays(-4),
                "1week" => now.AddDays(-7),
                "1month" => now.AddMonths(-1),
                "1year" => now.AddYears(-1),
                _ => null
            };
        }

        #endregion
    }
}