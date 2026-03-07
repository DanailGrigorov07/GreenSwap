using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SecondHandGoods.Data;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Web.Models.Ads;
using SecondHandGoods.Services;

namespace SecondHandGoods.Web.Controllers
{
    /// <summary>
    /// Controller for advertisement CRUD operations
    /// </summary>
    public class AdsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IContentModerationService _moderationService;
        private readonly ILogger<AdsController> _logger;

        private const int DefaultPageSize = 12;
        private const int MaxImageCount = 20;
        private const long MaxImageSize = 10 * 1024 * 1024; // 10MB
        private readonly string[] _allowedImageTypes = { "image/jpeg", "image/jpg", "image/png", "image/webp" };

        public AdsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            IContentModerationService moderationService,
            ILogger<AdsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _moderationService = moderationService;
            _logger = logger;
        }

        #region List/Index Actions

        /// <summary>
        /// Display list of advertisements with search and filtering
        /// </summary>
        public async Task<IActionResult> Index(AdvertisementListViewModel model)
        {
            try
            {
                var query = _context.Advertisements
                    .Include(a => a.Category)
                    .Include(a => a.User)
                    .Include(a => a.Images)
                    .Where(a => a.IsActive && !a.IsDeleted && a.ExpiresAt > DateTime.UtcNow)
                    .AsQueryable();

                // Apply filters
                query = ApplyFilters(query, model);

                // Apply sorting
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
                            .FirstOrDefault() ?? "/images/no-image.jpg",
                        MainImageAlt = a.Images
                            .Where(img => img.IsMainImage)
                            .Select(img => img.AltText)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                model.Advertisements = advertisements;

                // Populate dropdown data
                await PopulateDropdownsAsync(model);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading advertisements");
                TempData["Error"] = "An error occurred while loading advertisements. Please try again.";
                return View(new AdvertisementListViewModel());
            }
        }

        /// <summary>
        /// Display current user's advertisements
        /// </summary>
        [Authorize]
        public async Task<IActionResult> MyAds(AdvertisementListViewModel model)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Challenge();

                model.IsUserAds = true;

                var query = _context.Advertisements
                    .Include(a => a.Category)
                    .Include(a => a.User)
                    .Include(a => a.Images)
                    .Where(a => a.UserId == currentUser.Id && !a.IsDeleted)
                    .AsQueryable();

                // Apply filters (except user filter)
                query = ApplyFilters(query, model, excludeUserFilter: true);

                // Apply sorting
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
                            .FirstOrDefault() ?? "/images/no-image.jpg",
                        MainImageAlt = a.Images
                            .Where(img => img.IsMainImage)
                            .Select(img => img.AltText)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                model.Advertisements = advertisements;

                // Populate dropdown data
                await PopulateDropdownsAsync(model);

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading user's advertisements");
                TempData["Error"] = "An error occurred while loading your advertisements. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Details Action

        /// <summary>
        /// Display advertisement details
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                var advertisement = await _context.Advertisements
                    .Include(a => a.Category)
                    .Include(a => a.User)
                    .Include(a => a.Images.OrderBy(img => img.DisplayOrder))
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

                if (advertisement == null)
                    return NotFound();

                // Increment view count (only for non-owners)
                if (currentUser?.Id != advertisement.UserId)
                {
                    advertisement.ViewCount++;
                    await _context.SaveChangesAsync();
                }

                var model = new AdvertisementDetailsViewModel
                {
                    Id = advertisement.Id,
                    Title = advertisement.Title,
                    Description = advertisement.Description,
                    Price = advertisement.Price,
                    IsPriceNegotiable = advertisement.IsPriceNegotiable,
                    Condition = advertisement.Condition,
                    Location = advertisement.Location,
                    CreatedAt = advertisement.CreatedAt,
                    UpdatedAt = advertisement.UpdatedAt,
                    ExpiresAt = advertisement.ExpiresAt,
                    IsFeatured = advertisement.IsFeatured,
                    IsSold = advertisement.IsSold,
                    ViewCount = advertisement.ViewCount,
                    CategoryId = advertisement.CategoryId,
                    CategoryName = advertisement.Category.Name,
                    CategoryIconClass = advertisement.Category.IconClass,
                    SellerId = advertisement.UserId,
                    SellerName = advertisement.User.FirstName + " " + advertisement.User.LastName,
                    SellerLocation = advertisement.User.Location ?? "Location not specified",
                    SellerRating = advertisement.User.SellerRating,
                    SellerRatingCount = advertisement.User.RatingCount,
                    SellerMemberSince = advertisement.User.CreatedAt,
                    SellerBio = advertisement.User.Bio,
                    Images = advertisement.Images.Select(img => new AdvertisementImageViewModel
                    {
                        Id = img.Id,
                        ImageUrl = img.ImageUrl,
                        AltText = img.AltText,
                        IsMainImage = img.IsMainImage,
                        DisplayOrder = img.DisplayOrder
                    }).ToList(),
                    CanEdit = currentUser?.Id == advertisement.UserId,
                    CanDelete = currentUser?.Id == advertisement.UserId,
                    CanContact = currentUser != null && currentUser.Id != advertisement.UserId,
                    CanFavorite = currentUser != null && currentUser.Id != advertisement.UserId,
                    IsFavorited = currentUser != null && await _context.Favorites
                        .AnyAsync(f => f.UserId == currentUser.Id && f.AdvertisementId == advertisement.Id)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading advertisement details for ID: {Id}", id);
                TempData["Error"] = "An error occurred while loading advertisement details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Create Actions

        /// <summary>
        /// Display create advertisement form
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new CreateAdvertisementViewModel();
                await PopulateCreateViewModelAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while preparing create advertisement form");
                TempData["Error"] = "An error occurred while preparing the form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Handle create advertisement form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(CreateAdvertisementViewModel model)
        {
            try
            {
                // Validate images
                if (model.Images != null && model.Images.Count > 0)
                {
                    var imageValidation = ValidateImages(model.Images);
                    if (!imageValidation.isValid)
                    {
                        ModelState.AddModelError(nameof(model.Images), imageValidation.error);
                    }
                }

                if (!ModelState.IsValid)
                {
                    await PopulateCreateViewModelAsync(model);
                    return View(model);
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Challenge();

                // Get negotiable text if provided (for specific categories)
                var negotiableText = GetFormValue("NegotiableText");
                var isNegotiable = model.IsPriceNegotiable;
                var description = model.Description.Trim();
                
                // If negotiable text is provided, set IsPriceNegotiable to true and append to description
                if (!string.IsNullOrWhiteSpace(negotiableText))
                {
                    isNegotiable = true;
                    if (!string.IsNullOrWhiteSpace(negotiableText.Trim()))
                    {
                        description += $"\n\n[Negotiation Details: {negotiableText.Trim()}]";
                    }
                }
                
                // Create advertisement
                var advertisement = new Advertisement
                {
                    Title = model.Title.Trim(),
                    Description = description,
                    Price = model.Price,
                    IsPriceNegotiable = isNegotiable,
                    Condition = model.Condition,
                    CategoryId = model.CategoryId,
                    Location = model.Location?.Trim() ?? currentUser.Location,
                    UserId = currentUser.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30) // Default 30 days expiration
                };

                // Add to context first to get the ID
                _context.Advertisements.Add(advertisement);
                await _context.SaveChangesAsync();

                // Perform content moderation on title and description
                var titleModeration = await _moderationService.ModerateContentAsync(
                    advertisement.Title, 
                    ModeratedEntityType.Advertisement, 
                    advertisement.Id, 
                    currentUser.Id,
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                var descriptionModeration = await _moderationService.ModerateContentAsync(
                    advertisement.Description, 
                    ModeratedEntityType.Advertisement, 
                    advertisement.Id, 
                    currentUser.Id,
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                // Handle moderation results
                bool contentWasModerated = false;
                if (!titleModeration.Passed || titleModeration.WasModified)
                {
                    if (titleModeration.WasModified)
                    {
                        advertisement.Title = titleModeration.ModifiedContent;
                        contentWasModerated = true;
                        TempData["Warning"] = "Your advertisement title contained inappropriate content and has been modified.";
                    }
                    else if (!titleModeration.Passed)
                    {
                        // Content was blocked - deactivate the ad
                        advertisement.IsActive = false;
                        TempData["Error"] = "Your advertisement title contains inappropriate content and cannot be published. Please edit and try again.";
                    }
                }

                if (!descriptionModeration.Passed || descriptionModeration.WasModified)
                {
                    if (descriptionModeration.WasModified)
                    {
                        advertisement.Description = descriptionModeration.ModifiedContent;
                        contentWasModerated = true;
                        var existingWarning = TempData["Warning"] as string;
                        TempData["Warning"] = existingWarning != null 
                            ? existingWarning + " Your description was also modified."
                            : "Your advertisement description contained inappropriate content and has been modified.";
                    }
                    else if (!descriptionModeration.Passed)
                    {
                        // Content was blocked - deactivate the ad
                        advertisement.IsActive = false;
                        TempData["Error"] = "Your advertisement description contains inappropriate content and cannot be published. Please edit and try again.";
                    }
                }

                // Save any content modifications
                if (contentWasModerated || !advertisement.IsActive)
                {
                    await _context.SaveChangesAsync();
                }

                // Handle image uploads with ordering
                if (model.Images != null && model.Images.Count > 0)
                {
                    // Get main image index and order from form
                    var mainImageIndexStr = GetFormValue("MainImageIndex");
                    var imageOrderStr = GetFormValue("ImageOrder");
                    int mainImageIndex = 0;
                    List<int> imageOrder = new();
                    
                    if (!string.IsNullOrEmpty(mainImageIndexStr) && int.TryParse(mainImageIndexStr, out var parsedMainIndex))
                    {
                        mainImageIndex = parsedMainIndex;
                    }
                    
                    if (!string.IsNullOrEmpty(imageOrderStr))
                    {
                        imageOrder = imageOrderStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => int.TryParse(s, out var idx) ? idx : -1)
                            .Where(idx => idx >= 0)
                            .ToList();
                    }
                    
                    await SaveAdvertisementImagesAsync(advertisement.Id, model.Images, mainImageIndex, imageOrder);
                }

                TempData["Success"] = "Your advertisement has been created successfully!";
                return RedirectToAction(nameof(Details), new { id = advertisement.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating advertisement");
                TempData["Error"] = "An error occurred while creating your advertisement. Please try again.";
                await PopulateCreateViewModelAsync(model);
                return View(model);
            }
        }

        #endregion

        #region Edit Actions

        /// <summary>
        /// Display edit advertisement form
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Challenge();

                var advertisement = await _context.Advertisements
                    .Include(a => a.Images.OrderBy(img => img.DisplayOrder))
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

                if (advertisement == null)
                    return NotFound();

                if (advertisement.UserId != currentUser.Id)
                    return Forbid();

                var model = new EditAdvertisementViewModel
                {
                    Id = advertisement.Id,
                    Title = advertisement.Title,
                    Description = advertisement.Description,
                    Price = advertisement.Price,
                    IsPriceNegotiable = advertisement.IsPriceNegotiable,
                    Condition = advertisement.Condition,
                    CategoryId = advertisement.CategoryId,
                    Location = advertisement.Location,
                    IsActive = advertisement.IsActive,
                    IsSold = advertisement.IsSold,
                    CurrentImages = advertisement.Images.Select(img => new AdvertisementImageViewModel
                    {
                        Id = img.Id,
                        ImageUrl = img.ImageUrl,
                        AltText = img.AltText,
                        IsMainImage = img.IsMainImage,
                        DisplayOrder = img.DisplayOrder
                    }).ToList()
                };

                await PopulateEditViewModelAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while preparing edit form for advertisement ID: {Id}", id);
                TempData["Error"] = "An error occurred while loading the advertisement for editing. Please try again.";
                return RedirectToAction(nameof(MyAds));
            }
        }

        /// <summary>
        /// Handle edit advertisement form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, EditAdvertisementViewModel model)
        {
            try
            {
                if (id != model.Id)
                    return BadRequest();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Challenge();

                // Validate new images if any
                if (model.NewImages != null && model.NewImages.Count > 0)
                {
                    var imageValidation = ValidateImages(model.NewImages);
                    if (!imageValidation.isValid)
                    {
                        ModelState.AddModelError(nameof(model.NewImages), imageValidation.error);
                    }
                }

                if (!ModelState.IsValid)
                {
                    await PopulateEditViewModelAsync(model);
                    return View(model);
                }

                var advertisement = await _context.Advertisements
                    .Include(a => a.Images)
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

                if (advertisement == null)
                    return NotFound();

                if (advertisement.UserId != currentUser.Id)
                    return Forbid();

                // Update advertisement
                advertisement.Title = model.Title.Trim();
                advertisement.Description = model.Description.Trim();
                advertisement.Price = model.Price;
                advertisement.IsPriceNegotiable = model.IsPriceNegotiable;
                advertisement.Condition = model.Condition;
                advertisement.CategoryId = model.CategoryId;
                advertisement.Location = model.Location?.Trim();
                advertisement.IsActive = model.IsActive;
                advertisement.IsSold = model.IsSold;
                advertisement.UpdatedAt = DateTime.UtcNow;

                // Handle image removal
                if (model.ImagesToRemove != null && model.ImagesToRemove.Any())
                {
                    var imagesToRemove = advertisement.Images
                        .Where(img => model.ImagesToRemove.Contains(img.Id))
                        .ToList();

                    foreach (var image in imagesToRemove)
                    {
                        DeleteImageFile(image.ImageUrl);
                        _context.AdvertisementImages.Remove(image);
                    }
                }

                // Handle new images
                if (model.NewImages != null && model.NewImages.Count > 0)
                {
                    var currentImageCount = advertisement.Images.Count - (model.ImagesToRemove?.Count ?? 0);
                    if (currentImageCount + model.NewImages.Count > MaxImageCount)
                    {
                        TempData["Error"] = $"You can only have up to {MaxImageCount} images per advertisement.";
                        await PopulateEditViewModelAsync(model);
                        return View(model);
                    }

                    await SaveAdvertisementImagesAsync(advertisement.Id, model.NewImages);
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Your advertisement has been updated successfully!";
                return RedirectToAction(nameof(Details), new { id = advertisement.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating advertisement ID: {Id}", id);
                TempData["Error"] = "An error occurred while updating your advertisement. Please try again.";
                await PopulateEditViewModelAsync(model);
                return View(model);
            }
        }

        #endregion

        #region Delete Actions

        /// <summary>
        /// Display delete confirmation
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Challenge();

                var advertisement = await _context.Advertisements
                    .Include(a => a.Category)
                    .Include(a => a.User)
                    .Include(a => a.Images)
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

                if (advertisement == null)
                    return NotFound();

                if (advertisement.UserId != currentUser.Id)
                    return Forbid();

                var model = new AdvertisementDetailsViewModel
                {
                    Id = advertisement.Id,
                    Title = advertisement.Title,
                    Description = advertisement.Description,
                    Price = advertisement.Price,
                    IsPriceNegotiable = advertisement.IsPriceNegotiable,
                    Condition = advertisement.Condition,
                    Location = advertisement.Location,
                    CreatedAt = advertisement.CreatedAt,
                    CategoryName = advertisement.Category.Name,
                    CategoryIconClass = advertisement.Category.IconClass,
                    SellerName = advertisement.User.FirstName + " " + advertisement.User.LastName,
                    Images = advertisement.Images.Select(img => new AdvertisementImageViewModel
                    {
                        Id = img.Id,
                        ImageUrl = img.ImageUrl,
                        AltText = img.AltText,
                        IsMainImage = img.IsMainImage,
                        DisplayOrder = img.DisplayOrder
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while preparing delete confirmation for advertisement ID: {Id}", id);
                TempData["Error"] = "An error occurred while loading the advertisement. Please try again.";
                return RedirectToAction(nameof(MyAds));
            }
        }

        /// <summary>
        /// Handle delete confirmation
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Challenge();

                var advertisement = await _context.Advertisements
                    .Include(a => a.Images)
                    .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

                if (advertisement == null)
                    return NotFound();

                if (advertisement.UserId != currentUser.Id)
                    return Forbid();

                // Soft delete
                advertisement.IsDeleted = true;
                advertisement.IsActive = false;
                advertisement.UpdatedAt = DateTime.UtcNow;

                // Delete image files
                foreach (var image in advertisement.Images)
                {
                    DeleteImageFile(image.ImageUrl);
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Your advertisement has been deleted successfully.";
                return RedirectToAction(nameof(MyAds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting advertisement ID: {Id}", id);
                TempData["Error"] = "An error occurred while deleting your advertisement. Please try again.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Apply search and filter conditions to query
        /// </summary>
        private static IQueryable<Advertisement> ApplyFilters(IQueryable<Advertisement> query, AdvertisementListViewModel model, bool excludeUserFilter = false)
        {
            // Search term
            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                var searchTerm = model.SearchTerm.ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(searchTerm) ||
                                         a.Description.ToLower().Contains(searchTerm));
            }

            // Category filter
            if (model.CategoryId.HasValue && model.CategoryId.Value > 0)
            {
                query = query.Where(a => a.CategoryId == model.CategoryId.Value);
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
                "newest" or _ => query.OrderByDescending(a => a.CreatedAt)
            };
        }

        /// <summary>
        /// Populate dropdown data for list views
        /// </summary>
        private async Task PopulateDropdownsAsync(AdvertisementListViewModel model)
        {
            model.Categories = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Categories" }
            };

            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();

            model.Categories.AddRange(categories);

            if (model.CategoryId.HasValue && model.CategoryId.Value > 0)
            {
                var categoryName = await _context.Categories
                    .Where(c => c.Id == model.CategoryId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();
                model.CurrentCategoryName = categoryName;
            }
        }

        /// <summary>
        /// Populate dropdown data for create form
        /// </summary>
        private async Task PopulateCreateViewModelAsync(CreateAdvertisementViewModel model)
        {
            model.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();

            model.PopulateConditions();
        }

        /// <summary>
        /// Populate dropdown data for edit form
        /// </summary>
        private async Task PopulateEditViewModelAsync(EditAdvertisementViewModel model)
        {
            model.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();

            model.PopulateConditions();

            // Reload current images
            var currentImages = await _context.AdvertisementImages
                .Where(img => img.AdvertisementId == model.Id)
                .OrderBy(img => img.DisplayOrder)
                .Select(img => new AdvertisementImageViewModel
                {
                    Id = img.Id,
                    ImageUrl = img.ImageUrl,
                    AltText = img.AltText,
                    IsMainImage = img.IsMainImage,
                    DisplayOrder = img.DisplayOrder
                })
                .ToListAsync();

            model.CurrentImages = currentImages;
        }

        /// <summary>
        /// Validate uploaded images
        /// </summary>
        private (bool isValid, string error) ValidateImages(List<IFormFile> images)
        {
            if (images.Count > MaxImageCount)
                return (false, $"You can upload up to {MaxImageCount} images.");

            foreach (var image in images)
            {
                if (image.Length > MaxImageSize)
                    return (false, $"Image '{image.FileName}' is too large. Maximum size is 10MB.");

                if (!_allowedImageTypes.Contains(image.ContentType.ToLower()))
                    return (false, $"'{image.FileName}' is not a supported image format. Please use JPEG, PNG, or WebP.");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Save uploaded images for advertisement with ordering support
        /// </summary>
        private async Task SaveAdvertisementImagesAsync(int advertisementId, List<IFormFile> images, int mainImageIndex = 0, List<int>? imageOrder = null)
        {
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "ads", advertisementId.ToString());
            Directory.CreateDirectory(uploadsPath);

            var currentImageCount = await _context.AdvertisementImages.CountAsync(img => img.AdvertisementId == advertisementId);
            var isFirstImage = currentImageCount == 0;

            // If image order is provided, use it; otherwise use default order
            var orderedImages = new List<(IFormFile file, int order)>();
            if (imageOrder != null && imageOrder.Count == images.Count)
            {
                // Map files to their order
                for (int i = 0; i < images.Count; i++)
                {
                    var orderIndex = imageOrder.IndexOf(i);
                    if (orderIndex >= 0 && orderIndex < images.Count)
                    {
                        orderedImages.Add((images[orderIndex], i));
                    }
                    else
                    {
                        orderedImages.Add((images[i], i));
                    }
                }
                orderedImages = orderedImages.OrderBy(x => x.order).ToList();
            }
            else
            {
                // Default order
                for (int i = 0; i < images.Count; i++)
                {
                    orderedImages.Add((images[i], i));
                }
            }

            for (int i = 0; i < orderedImages.Count; i++)
            {
                var (image, originalIndex) = orderedImages[i];
                var fileExtension = Path.GetExtension(image.FileName).ToLower();
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Determine if this is the main image
                bool isMain = false;
                if (isFirstImage && i == 0)
                {
                    isMain = true; // First image ever is main by default
                }
                else if (mainImageIndex >= 0 && mainImageIndex < orderedImages.Count)
                {
                    // Check if this image's original index matches the main image index
                    isMain = (originalIndex == mainImageIndex);
                }

                var advertisementImage = new AdvertisementImage
                {
                    AdvertisementId = advertisementId,
                    ImageUrl = $"/uploads/ads/{advertisementId}/{fileName}",
                    AltText = $"Image for advertisement",
                    IsMainImage = isMain,
                    DisplayOrder = currentImageCount + i + 1,
                    OriginalFileName = image.FileName,
                    FileSizeBytes = image.Length,
                    MimeType = image.ContentType,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AdvertisementImages.Add(advertisementImage);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Delete image file from disk
        /// </summary>
        private void DeleteImageFile(string imageUrl)
        {
            try
            {
                if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("/uploads/"))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete image file: {ImageUrl}", imageUrl);
            }
        }

        /// <summary>
        /// Safely get a form value (returns null when form is not available, e.g. in unit tests).
        /// </summary>
        private string? GetFormValue(string key)
        {
            if (HttpContext?.Request == null || !HttpContext.Request.HasFormContentType)
                return null;
            try
            {
                return HttpContext.Request.Form[key].FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}