using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandGoods.Data;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Web.Models.Ads;
using SecondHandGoods.Web.Models.Categories;

namespace SecondHandGoods.Web.Controllers
{
    /// <summary>
    /// Controller for category browsing and category-specific functionality
    /// </summary>
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Display all categories in a grid layout
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new CategoryCardViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        Slug = c.Slug,
                        IconClass = c.IconClass,
                        DisplayOrder = c.DisplayOrder,
                        AdCount = _context.Advertisements
                            .Where(a => a.CategoryId == c.Id && a.IsActive && !a.IsDeleted && !a.IsSold && a.ExpiresAt > DateTime.UtcNow)
                            .Count()
                    })
                    .ToListAsync();

                var model = new CategoriesIndexViewModel
                {
                    Categories = categories,
                    TotalCategories = categories.Count,
                    TotalActiveAds = categories.Sum(c => c.AdCount)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading categories");
                TempData["Error"] = "An error occurred while loading categories. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Browse advertisements in a specific category
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <param name="slug">Category slug for SEO-friendly URLs</param>
        public async Task<IActionResult> Browse(int id, string? slug, AdvertisementListViewModel? model)
        {
            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (category == null)
                    return NotFound();

                // Initialize model if not provided
                model ??= new AdvertisementListViewModel();
                
                // Set category filter
                model.CategoryId = id;
                model.CurrentCategoryName = category.Name;
                model.Page = model.Page == 0 ? 1 : model.Page;
                model.PageSize = 12;

                // Get advertisements for this category
                var query = _context.Advertisements
                    .Include(a => a.Category)
                    .Include(a => a.User)
                    .Include(a => a.Images)
                    .Where(a => a.CategoryId == id && a.IsActive && !a.IsDeleted && a.ExpiresAt > DateTime.UtcNow)
                    .AsQueryable();

                // Apply additional filters
                query = ApplyFilters(query, model);
                query = ApplySorting(query, model.SortBy);

                // Get total count for pagination
                model.TotalCount = await query.CountAsync();

                // Apply pagination
                var advertisements = await query
                    .Skip((model.Page - 1) * model.PageSize)
                    .Take(model.PageSize)
                    .Select(a => new AdvertisementCardViewModel
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

                model.Advertisements = advertisements;

                // Populate dropdown data
                await PopulateDropdownsAsync(model);

                // Create category-specific breadcrumb
                ViewBag.CategoryName = category.Name;
                ViewBag.CategoryIcon = category.IconClass;
                ViewBag.CategoryDescription = category.Description;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while browsing category {CategoryId}", id);
                TempData["Error"] = "An error occurred while loading advertisements. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Get category statistics for admin/analytics purposes
        /// </summary>
        public async Task<IActionResult> Stats()
        {
            try
            {
                var categoryStats = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new CategoryStatsViewModel
                    {
                        CategoryId = c.Id,
                        CategoryName = c.Name,
                        IconClass = c.IconClass,
                        TotalAds = _context.Advertisements
                            .Where(a => a.CategoryId == c.Id && !a.IsDeleted)
                            .Count(),
                        ActiveAds = _context.Advertisements
                            .Where(a => a.CategoryId == c.Id && a.IsActive && !a.IsDeleted && !a.IsSold && a.ExpiresAt > DateTime.UtcNow)
                            .Count(),
                        SoldAds = _context.Advertisements
                            .Where(a => a.CategoryId == c.Id && a.IsSold)
                            .Count(),
                        AveragePrice = _context.Advertisements
                            .Where(a => a.CategoryId == c.Id && a.IsActive && !a.IsDeleted && !a.IsSold)
                            .Average(a => (decimal?)a.Price) ?? 0,
                        TotalViews = _context.Advertisements
                            .Where(a => a.CategoryId == c.Id && !a.IsDeleted)
                            .Sum(a => (long?)a.ViewCount) ?? 0
                    })
                    .OrderByDescending(cs => cs.ActiveAds)
                    .ToListAsync();

                return View(categoryStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading category statistics");
                TempData["Error"] = "An error occurred while loading statistics. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        #region Helper Methods

        /// <summary>
        /// Apply search and filter conditions to query (excluding category filter)
        /// </summary>
        private static IQueryable<Advertisement> ApplyFilters(IQueryable<Advertisement> query, AdvertisementListViewModel model)
        {
            // Search term
            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                var searchTerm = model.SearchTerm.ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(searchTerm) ||
                                         a.Description.ToLower().Contains(searchTerm));
            }

            // Price filters
            if (model.MinPrice.HasValue)
            {
                query = query.Where(a => a.Price >= model.MinPrice.Value);
            }

            if (model.MaxPrice.HasValue)
            {
                query = query.Where(a => a.Price <= model.MaxPrice.Value);
            }

            // Condition filter
            if (model.Condition.HasValue)
            {
                query = query.Where(a => a.Condition == model.Condition.Value);
            }

            // Location filter
            if (!string.IsNullOrWhiteSpace(model.Location))
            {
                var location = model.Location.ToLower();
                query = query.Where(a => a.Location != null && a.Location.ToLower().Contains(location));
            }

            // Negotiable price filter
            if (model.PriceNegotiable.HasValue)
            {
                query = query.Where(a => a.IsPriceNegotiable == model.PriceNegotiable.Value);
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
                "newest" or _ => query.OrderByDescending(a => a.CreatedAt)
            };
        }

        /// <summary>
        /// Populate dropdown data for filters
        /// </summary>
        private async Task PopulateDropdownsAsync(AdvertisementListViewModel model)
        {
            // Categories (for consistency with main ads controller)
            model.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = c.Id == model.CategoryId
                })
                .ToListAsync();
        }

        #endregion
    }
}