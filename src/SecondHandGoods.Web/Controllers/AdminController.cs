using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SecondHandGoods.Data;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Web.Models.Admin;
using SecondHandGoods.Services;
using System.Security.Claims;

namespace SecondHandGoods.Web.Controllers
{
    /// <summary>
    /// Administrative controller for platform management
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IContentModerationService _moderationService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IContentModerationService moderationService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _moderationService = moderationService;
            _logger = logger;
        }

        /// <summary>
        /// Admin dashboard with platform overview
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var now = DateTime.UtcNow;
                var todayStart = now.Date;
                var weekStart = now.AddDays(-(int)now.DayOfWeek);

                // Get platform statistics
                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var newUsersToday = await _context.Users.CountAsync(u => u.CreatedAt.Date == todayStart);
                var newUsersThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= weekStart);

                var totalAds = await _context.Advertisements.CountAsync(a => !a.IsDeleted);
                var activeAds = await _context.Advertisements.CountAsync(a => a.IsActive && !a.IsDeleted && a.ExpiresAt > now);
                var soldAds = await _context.Advertisements.CountAsync(a => a.IsSold);
                var newAdsToday = await _context.Advertisements.CountAsync(a => a.CreatedAt.Date == todayStart && !a.IsDeleted);
                var newAdsThisWeek = await _context.Advertisements.CountAsync(a => a.CreatedAt >= weekStart && !a.IsDeleted);

                var totalOrders = await _context.Orders.CountAsync();
                var completedOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Completed);
                var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
                var totalRevenue = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Completed)
                    .SumAsync(o => o.FinalPrice);

                var totalReviews = await _context.Reviews.CountAsync();
                var reportedReviews = await _context.Reviews.CountAsync(r => r.IsReported);
                var unapprovedReviews = await _context.Reviews.CountAsync(r => !r.IsApproved);
                var averageRating = await _context.Reviews.AverageAsync(r => (double?)r.Rating) ?? 0;

                var totalMessages = await _context.Messages.CountAsync(m => !m.IsDeleted);
                var messagesToday = await _context.Messages.CountAsync(m => m.SentAt.Date == todayStart && !m.IsDeleted);

                // Get recent activities
                var recentUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new AdminActivityViewModel
                    {
                        Id = 0, // Not used for users
                        Title = u.FirstName + " " + u.LastName,
                        Description = u.Email,
                        UserName = u.FirstName + " " + u.LastName,
                        UserId = u.Id,
                        CreatedAt = u.CreatedAt,
                        Status = u.IsActive ? "Active" : "Inactive",
                        StatusBadgeClass = u.IsActive ? "badge bg-success" : "badge bg-danger"
                    })
                    .ToListAsync();

                var recentAds = await _context.Advertisements
                    .Include(a => a.User)
                    .Where(a => !a.IsDeleted)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .Select(a => new AdminActivityViewModel
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Description = a.Price.ToString("C"),
                        UserName = a.User.FirstName + " " + a.User.LastName,
                        UserId = a.UserId,
                        CreatedAt = a.CreatedAt,
                        Status = a.IsActive && !a.IsSold ? "Active" : a.IsSold ? "Sold" : "Inactive",
                        StatusBadgeClass = a.IsActive && !a.IsSold ? "badge bg-primary" : a.IsSold ? "badge bg-success" : "badge bg-secondary"
                    })
                    .ToListAsync();

                var recentOrders = await _context.Orders
                    .Include(o => o.Advertisement)
                    .Include(o => o.Buyer)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .Select(o => new AdminActivityViewModel
                    {
                        Id = o.Id,
                        Title = o.OrderNumber,
                        Description = o.Advertisement.Title,
                        UserName = o.Buyer.FirstName + " " + o.Buyer.LastName,
                        UserId = o.BuyerId,
                        CreatedAt = o.CreatedAt,
                        Status = o.Status.ToString(),
                        StatusBadgeClass = GetOrderStatusBadgeClass(o.Status)
                    })
                    .ToListAsync();

                // Get reported reviews
                var reportedReviewsList = await _context.Reviews
                    .Include(r => r.Reviewer)
                    .Include(r => r.ReviewedUser)
                    .Include(r => r.Order)
                        .ThenInclude(o => o.Advertisement)
                    .Where(r => r.IsReported)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .Select(r => new AdminReviewItemViewModel
                    {
                        Id = r.Id,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        ReviewType = r.ReviewType,
                        IsReported = r.IsReported,
                        IsApproved = r.IsApproved,
                        CreatedAt = r.CreatedAt,
                        ReviewerId = r.ReviewerId,
                        ReviewerName = r.Reviewer.FirstName + " " + r.Reviewer.LastName,
                        ReviewedUserId = r.ReviewedUserId,
                        ReviewedUserName = r.ReviewedUser.FirstName + " " + r.ReviewedUser.LastName,
                        OrderId = r.OrderId,
                        OrderNumber = r.Order.OrderNumber,
                        AdvertisementTitle = r.Order.Advertisement.Title
                    })
                    .ToListAsync();

                // Get ads by category breakdown
                var adsByCategory = await _context.Advertisements
                    .Include(a => a.Category)
                    .Where(a => !a.IsDeleted)
                    .GroupBy(a => a.Category.Name)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                var model = new AdminDashboardViewModel
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    NewUsersToday = newUsersToday,
                    NewUsersThisWeek = newUsersThisWeek,
                    TotalAdvertisements = totalAds,
                    ActiveAdvertisements = activeAds,
                    SoldAdvertisements = soldAds,
                    NewAdsToday = newAdsToday,
                    NewAdsThisWeek = newAdsThisWeek,
                    TotalOrders = totalOrders,
                    CompletedOrders = completedOrders,
                    PendingOrders = pendingOrders,
                    TotalRevenue = totalRevenue,
                    TotalReviews = totalReviews,
                    ReportedReviewsCount = reportedReviews,
                    UnapprovedReviews = unapprovedReviews,
                    AverageRating = (decimal)averageRating,
                    TotalMessages = totalMessages,
                    MessagesToday = messagesToday,
                    RecentUsers = recentUsers,
                    RecentAds = recentAds,
                    RecentOrders = recentOrders,
                    ReportedReviews = reportedReviewsList,
                    AdsByCategory = adsByCategory
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["Error"] = "An error occurred while loading the dashboard.";
                return View(new AdminDashboardViewModel());
            }
        }

        /// <summary>
        /// User management page
        /// </summary>
        public async Task<IActionResult> Users(AdminUserManagementViewModel model)
        {
            try
            {
                model.Page = model.Page == 0 ? 1 : model.Page;
                model.PageSize = 20;

                var query = _context.Users.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(model.SearchTerm))
                {
                    var searchTerm = model.SearchTerm.ToLower();
                    query = query.Where(u => u.FirstName.ToLower().Contains(searchTerm) ||
                                           u.LastName.ToLower().Contains(searchTerm) ||
                                           u.Email.ToLower().Contains(searchTerm));
                }

                // Apply filters
                if (model.IsActive.HasValue)
                    query = query.Where(u => u.IsActive == model.IsActive.Value);

                if (model.RegisteredAfter.HasValue)
                    query = query.Where(u => u.CreatedAt >= model.RegisteredAfter.Value);

                if (model.RegisteredBefore.HasValue)
                    query = query.Where(u => u.CreatedAt <= model.RegisteredBefore.Value);

                if (model.MinRating.HasValue)
                    query = query.Where(u => u.SellerRating >= model.MinRating.Value);

                // Get total count
                model.TotalUsers = await query.CountAsync();

                // Apply sorting
                query = model.SortBy?.ToLower() switch
                {
                    "oldest" => query.OrderBy(u => u.CreatedAt),
                    "name" => query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
                    "rating-high" => query.OrderByDescending(u => u.SellerRating),
                    "rating-low" => query.OrderBy(u => u.SellerRating),
                    "most-ads" => query.OrderByDescending(u => _context.Advertisements.Count(a => a.UserId == u.Id)),
                    "most-orders" => query.OrderByDescending(u => _context.Orders.Count(o => o.BuyerId == u.Id || o.SellerId == u.Id)),
                    "newest" or _ => query.OrderByDescending(u => u.CreatedAt)
                };

                // Get paginated users
                var users = await query
                    .Skip((model.Page - 1) * model.PageSize)
                    .Take(model.PageSize)
                    .ToListAsync();

                // Build user items with additional data
                model.Users = new List<AdminUserItemViewModel>();
                foreach (var user in users)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    var userStats = await GetUserStatistics(user.Id);

                    model.Users.Add(new AdminUserItemViewModel
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        IsActive = user.IsActive,
                        EmailConfirmed = user.EmailConfirmed,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = null, // LastLoginAt property not implemented yet
                        SellerRating = user.SellerRating,
                        Roles = userRoles.ToList(),
                        TotalAds = userStats.TotalAds,
                        ActiveAds = userStats.ActiveAds,
                        SoldAds = userStats.SoldAds,
                        TotalOrders = userStats.TotalOrders,
                        TotalReviews = userStats.TotalReviews,
                        TotalMessages = userStats.TotalMessages
                    });
                }

                // Get statistics
                model.ActiveUsersCount = await _context.Users.CountAsync(u => u.IsActive);
                model.InactiveUsersCount = model.TotalUsers - model.ActiveUsersCount;
                model.AdminUsersCount = await _context.UserRoles.CountAsync(ur => _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin"));
                model.RegularUsersCount = model.TotalUsers - model.AdminUsersCount;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin user management page");
                TempData["Error"] = "An error occurred while loading users.";
                return View(new AdminUserManagementViewModel());
            }
        }

        /// <summary>
        /// Advertisement management page
        /// </summary>
        public async Task<IActionResult> Advertisements(AdminAdManagementViewModel model)
        {
            try
            {
                model.Page = model.Page == 0 ? 1 : model.Page;
                model.PageSize = 15;

                var query = _context.Advertisements
                    .Include(a => a.Category)
                    .Include(a => a.User)
                    .Include(a => a.Images)
                    .Where(a => !a.IsDeleted)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(model.SearchTerm))
                {
                    var searchTerm = model.SearchTerm.ToLower();
                    query = query.Where(a => a.Title.ToLower().Contains(searchTerm) ||
                                           a.Description.ToLower().Contains(searchTerm));
                }

                // Apply filters
                if (model.IsActive.HasValue)
                    query = query.Where(a => a.IsActive == model.IsActive.Value);

                if (model.IsSold.HasValue)
                    query = query.Where(a => a.IsSold == model.IsSold.Value);

                if (model.IsFeatured.HasValue)
                    query = query.Where(a => a.IsFeatured == model.IsFeatured.Value);

                if (model.CategoryId.HasValue && model.CategoryId.Value > 0)
                    query = query.Where(a => a.CategoryId == model.CategoryId.Value);

                if (model.Condition.HasValue)
                    query = query.Where(a => a.Condition == model.Condition.Value);

                if (model.MinPrice.HasValue)
                    query = query.Where(a => a.Price >= model.MinPrice.Value);

                if (model.MaxPrice.HasValue)
                    query = query.Where(a => a.Price <= model.MaxPrice.Value);

                if (model.PostedAfter.HasValue)
                    query = query.Where(a => a.CreatedAt >= model.PostedAfter.Value);

                if (model.PostedBefore.HasValue)
                    query = query.Where(a => a.CreatedAt <= model.PostedBefore.Value);

                // Get total count
                model.TotalAds = await query.CountAsync();

                // Apply sorting
                query = model.SortBy?.ToLower() switch
                {
                    "oldest" => query.OrderBy(a => a.CreatedAt),
                    "price-high" => query.OrderByDescending(a => a.Price),
                    "price-low" => query.OrderBy(a => a.Price),
                    "most-views" => query.OrderByDescending(a => a.ViewCount),
                    "expiring" => query.OrderBy(a => a.ExpiresAt),
                    "newest" or _ => query.OrderByDescending(a => a.CreatedAt)
                };

                // Get paginated results
                var advertisements = await query
                    .Skip((model.Page - 1) * model.PageSize)
                    .Take(model.PageSize)
                    .Select(a => new AdminAdItemViewModel
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Description = a.Description,
                        Price = a.Price,
                        Condition = a.Condition,
                        IsActive = a.IsActive,
                        IsSold = a.IsSold,
                        IsFeatured = a.IsFeatured,
                        ViewCount = a.ViewCount,
                        CreatedAt = a.CreatedAt,
                        ExpiresAt = a.ExpiresAt,
                        UpdatedAt = a.UpdatedAt,
                        CategoryId = a.CategoryId,
                        CategoryName = a.Category.Name,
                        CategoryIconClass = a.Category.IconClass,
                        UserId = a.UserId,
                        UserName = a.User.FirstName + " " + a.User.LastName,
                        UserEmail = a.User.Email,
                        UserRating = a.User.SellerRating,
                        MainImageUrl = a.Images.FirstOrDefault(i => i.IsMainImage)!.ImageUrl ?? "/images/no-image.svg",
                        ImageCount = a.Images.Count(),
                        MessageCount = _context.Messages.Count(m => m.AdvertisementId == a.Id && !m.IsDeleted),
                        OrderCount = _context.Orders.Count(o => o.AdvertisementId == a.Id)
                    })
                    .ToListAsync();

                model.Advertisements = advertisements;

                // Get statistics
                var now = DateTime.UtcNow;
                model.ActiveAdsCount = await _context.Advertisements.CountAsync(a => a.IsActive && !a.IsDeleted && a.ExpiresAt > now);
                model.SoldAdsCount = await _context.Advertisements.CountAsync(a => a.IsSold);
                model.InactiveAdsCount = await _context.Advertisements.CountAsync(a => !a.IsActive && !a.IsDeleted);
                model.FeaturedAdsCount = await _context.Advertisements.CountAsync(a => a.IsFeatured && a.IsActive && !a.IsDeleted);
                model.ExpiredAdsCount = await _context.Advertisements.CountAsync(a => a.ExpiresAt <= now && !a.IsDeleted);

                // Populate categories dropdown
                model.Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = c.Id == model.CategoryId
                    })
                    .ToListAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin advertisement management page");
                TempData["Error"] = "An error occurred while loading advertisements.";
                return View(new AdminAdManagementViewModel());
            }
        }

        /// <summary>
        /// Platform statistics page
        /// </summary>
        public async Task<IActionResult> Statistics()
        {
            try
            {
                var model = new AdminStatsViewModel();

                // General platform stats
                model.GeneralStats = new Dictionary<string, object>
                {
                    ["TotalUsers"] = await _context.Users.CountAsync(),
                    ["ActiveUsers"] = await _context.Users.CountAsync(u => u.IsActive),
                    ["TotalAds"] = await _context.Advertisements.CountAsync(a => !a.IsDeleted),
                    ["ActiveAds"] = await _context.Advertisements.CountAsync(a => a.IsActive && !a.IsDeleted),
                    ["TotalOrders"] = await _context.Orders.CountAsync(),
                    ["CompletedOrders"] = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Completed),
                    ["TotalReviews"] = await _context.Reviews.CountAsync(),
                    ["AverageRating"] = await _context.Reviews.AverageAsync(r => (decimal?)r.Rating) ?? 0,
                    ["TotalMessages"] = await _context.Messages.CountAsync(m => !m.IsDeleted),
                    ["TotalRevenue"] = await _context.Orders.Where(o => o.Status == OrderStatus.Completed).SumAsync(o => o.FinalPrice)
                };

                // Category statistics
                model.CategoryStats = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new CategoryStatsItem
                    {
                        CategoryId = c.Id,
                        CategoryName = c.Name,
                        IconClass = c.IconClass,
                        TotalAds = _context.Advertisements.Count(a => a.CategoryId == c.Id && !a.IsDeleted),
                        ActiveAds = _context.Advertisements.Count(a => a.CategoryId == c.Id && a.IsActive && !a.IsDeleted),
                        SoldAds = _context.Advertisements.Count(a => a.CategoryId == c.Id && a.IsSold),
                        AveragePrice = _context.Advertisements.Where(a => a.CategoryId == c.Id && !a.IsDeleted).Average(a => (decimal?)a.Price) ?? 0,
                        TotalViews = _context.Advertisements.Where(a => a.CategoryId == c.Id && !a.IsDeleted).Sum(a => (long?)a.ViewCount) ?? 0
                    })
                    .OrderByDescending(cs => cs.ActiveAds)
                    .ToListAsync();

                // Top users by activity
                model.TopUsers = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderByDescending(u => u.SellerRating)
                    .Take(10)
                    .Select(u => new UserActivityItem
                    {
                        UserId = u.Id,
                        UserName = u.FirstName + " " + u.LastName,
                        Email = u.Email,
                        Rating = u.SellerRating,
                        TotalAds = _context.Advertisements.Count(a => a.UserId == u.Id && !a.IsDeleted),
                        TotalSales = _context.Orders.Count(o => o.SellerId == u.Id && o.Status == OrderStatus.Completed),
                        TotalPurchases = _context.Orders.Count(o => o.BuyerId == u.Id && o.Status == OrderStatus.Completed),
                        TotalRevenue = _context.Orders.Where(o => o.SellerId == u.Id && o.Status == OrderStatus.Completed).Sum(o => (decimal?)o.FinalPrice) ?? 0
                    })
                    .ToListAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin statistics page");
                TempData["Error"] = "An error occurred while loading statistics.";
                return View(new AdminStatsViewModel());
            }
        }

        /// <summary>
        /// User action endpoint (activate, deactivate, promote, etc.)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UserAction(AdminUserActionViewModel model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                    return Json(new { success = false, message = "User not found." });

                var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                switch (model.Action.ToLower())
                {
                    case "activate":
                        user.IsActive = true;
                        await _userManager.UpdateAsync(user);
                        break;

                    case "deactivate":
                        user.IsActive = false;
                        await _userManager.UpdateAsync(user);
                        break;

                    case "promote":
                        if (!await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            await _userManager.AddToRoleAsync(user, "Admin");
                        }
                        break;

                    case "demote":
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            // Don't allow demoting the last admin
                            var adminCount = await _context.UserRoles.CountAsync(ur => 
                                _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin"));
                            
                            if (adminCount > 1)
                            {
                                await _userManager.RemoveFromRoleAsync(user, "Admin");
                            }
                            else
                            {
                                return Json(new { success = false, message = "Cannot demote the last admin." });
                            }
                        }
                        break;

                    case "delete":
                        // Soft delete by deactivating
                        user.IsActive = false;
                        await _userManager.UpdateAsync(user);
                        break;

                    default:
                        return Json(new { success = false, message = "Invalid action." });
                }

                _logger.LogInformation("Admin {AdminId} performed action '{Action}' on user {UserId}. Reason: {Reason}", 
                    currentAdminId, model.Action, model.UserId, model.Reason);

                return Json(new { success = true, message = $"User {model.Action}d successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing admin action {Action} on user {UserId}", model.Action, model.UserId);
                return Json(new { success = false, message = "Failed to perform action." });
            }
        }

        /// <summary>
        /// Advertisement action endpoint (activate, deactivate, feature, etc.)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AdAction(AdminAdActionViewModel model)
        {
            try
            {
                var advertisement = await _context.Advertisements.FindAsync(model.AdId);
                if (advertisement == null)
                    return Json(new { success = false, message = "Advertisement not found." });

                var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                switch (model.Action.ToLower())
                {
                    case "activate":
                        advertisement.IsActive = true;
                        advertisement.UpdatedAt = DateTime.UtcNow;
                        break;

                    case "deactivate":
                        advertisement.IsActive = false;
                        advertisement.UpdatedAt = DateTime.UtcNow;
                        break;

                    case "feature":
                        advertisement.IsFeatured = true;
                        advertisement.UpdatedAt = DateTime.UtcNow;
                        break;

                    case "unfeature":
                        advertisement.IsFeatured = false;
                        advertisement.UpdatedAt = DateTime.UtcNow;
                        break;

                    case "delete":
                        advertisement.IsDeleted = true;
                        advertisement.IsActive = false;
                        break;

                    default:
                        return Json(new { success = false, message = "Invalid action." });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin {AdminId} performed action '{Action}' on advertisement {AdId}. Reason: {Reason}", 
                    currentAdminId, model.Action, model.AdId, model.Reason);

                return Json(new { success = true, message = $"Advertisement {model.Action}d successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing admin action {Action} on advertisement {AdId}", model.Action, model.AdId);
                return Json(new { success = false, message = "Failed to perform action." });
            }
        }

        /// <summary>
        /// Bulk actions for multiple items
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> BulkAction(AdminBulkActionViewModel model)
        {
            try
            {
                if (!model.SelectedIds.Any())
                    return Json(new { success = false, message = "No items selected." });

                var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var successCount = 0;

                if (model.EntityType.ToLower() == "ads")
                {
                    var ads = await _context.Advertisements
                        .Where(a => model.SelectedIds.Contains(a.Id))
                        .ToListAsync();

                    foreach (var ad in ads)
                    {
                        switch (model.Action.ToLower())
                        {
                            case "activate":
                                ad.IsActive = true;
                                ad.UpdatedAt = DateTime.UtcNow;
                                successCount++;
                                break;
                            case "deactivate":
                                ad.IsActive = false;
                                ad.UpdatedAt = DateTime.UtcNow;
                                successCount++;
                                break;
                            case "feature":
                                ad.IsFeatured = true;
                                ad.UpdatedAt = DateTime.UtcNow;
                                successCount++;
                                break;
                            case "delete":
                                ad.IsDeleted = true;
                                ad.IsActive = false;
                                successCount++;
                                break;
                        }
                    }
                }
                else if (model.EntityType.ToLower() == "users")
                {
                    var users = await _context.Users
                        .Where(u => model.SelectedIds.Select(id => id.ToString()).Contains(u.Id))
                        .ToListAsync();

                    foreach (var user in users)
                    {
                        switch (model.Action.ToLower())
                        {
                            case "activate":
                                user.IsActive = true;
                                await _userManager.UpdateAsync(user);
                                successCount++;
                                break;
                            case "deactivate":
                                user.IsActive = false;
                                await _userManager.UpdateAsync(user);
                                successCount++;
                                break;
                        }
                    }
                }

                if (model.EntityType.ToLower() == "ads")
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Admin {AdminId} performed bulk action '{Action}' on {Count} {EntityType}. Reason: {Reason}", 
                    currentAdminId, model.Action, successCount, model.EntityType, model.Reason);

                return Json(new { 
                    success = true, 
                    message = $"Successfully {model.Action}d {successCount} {model.EntityType}." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action {Action} on {EntityType}", model.Action, model.EntityType);
                return Json(new { success = false, message = "Failed to perform bulk action." });
            }
        }

        #region Helper Methods

        /// <summary>
        /// Get user statistics for admin display
        /// </summary>
        private async Task<(int TotalAds, int ActiveAds, int SoldAds, int TotalOrders, int TotalReviews, int TotalMessages)> GetUserStatistics(string userId)
        {
            var totalAds = await _context.Advertisements.CountAsync(a => a.UserId == userId && !a.IsDeleted);
            var activeAds = await _context.Advertisements.CountAsync(a => a.UserId == userId && a.IsActive && !a.IsDeleted);
            var soldAds = await _context.Advertisements.CountAsync(a => a.UserId == userId && a.IsSold);
            var totalOrders = await _context.Orders.CountAsync(o => o.BuyerId == userId || o.SellerId == userId);
            var totalReviews = await _context.Reviews.CountAsync(r => r.ReviewedUserId == userId);
            var totalMessages = await _context.Messages.CountAsync(m => (m.SenderId == userId || m.ReceiverId == userId) && !m.IsDeleted);

            return (totalAds, activeAds, soldAds, totalOrders, totalReviews, totalMessages);
        }

        /// <summary>
        /// Get order status badge class
        /// </summary>
        private static string GetOrderStatusBadgeClass(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "badge bg-warning text-dark",
                OrderStatus.Confirmed => "badge bg-info",
                OrderStatus.MeetingScheduled => "badge bg-primary",
                OrderStatus.Completed => "badge bg-success",
                OrderStatus.Cancelled => "badge bg-danger",
                _ => "badge bg-secondary"
            };
        }

        #endregion

        #region Content Moderation

        /// <summary>
        /// Content moderation dashboard
        /// </summary>
        public async Task<IActionResult> Moderation()
        {
            try
            {
                var now = DateTime.UtcNow;
                var todayStart = now.Date;
                var weekStart = now.AddDays(-(int)now.DayOfWeek);

                // Get moderation statistics
                var totalModerations = await _context.ModerationLogs.CountAsync();
                var moderationsToday = await _context.ModerationLogs.CountAsync(ml => ml.CreatedAt.Date == todayStart);
                var moderationsThisWeek = await _context.ModerationLogs.CountAsync(ml => ml.CreatedAt >= weekStart);
                var automaticActions = await _context.ModerationLogs.CountAsync(ml => ml.IsAutomatic);
                var manualReviews = await _context.ModerationLogs.CountAsync(ml => !ml.IsAutomatic);
                var pendingReviews = await _context.ModerationLogs.CountAsync(ml => ml.Result == Data.Entities.ModerationResult.PendingReview);

                var blockedContent = await _context.ModerationLogs.CountAsync(ml => ml.Result == Data.Entities.ModerationResult.Blocked);
                var flaggedContent = await _context.ModerationLogs.CountAsync(ml => ml.Result == Data.Entities.ModerationResult.Flagged);
                var modifiedContent = await _context.ModerationLogs.CountAsync(ml => ml.Result == Data.Entities.ModerationResult.Modified);

                var totalForbiddenWords = await _context.ForbiddenWords.CountAsync();
                var activeWords = await _context.ForbiddenWords.CountAsync(fw => fw.IsActive);
                var criticalWords = await _context.ForbiddenWords.CountAsync(fw => fw.IsActive && fw.Severity == ModerationSeverity.Critical);

                // Get recent moderations
                var recentModerations = await _context.ModerationLogs
                    .Include(ml => ml.ContentAuthor)
                    .Include(ml => ml.Moderator)
                    .OrderByDescending(ml => ml.CreatedAt)
                    .Take(10)
                    .Select(ml => new ModerationLogItemViewModel
                    {
                        Id = ml.Id,
                        EntityType = ml.EntityType,
                        EntityId = ml.EntityId,
                        ContentAuthor = ml.ContentAuthor.FirstName + " " + ml.ContentAuthor.LastName,
                        ContentAuthorEmail = ml.ContentAuthor.Email,
                        Action = ml.Action,
                        Result = ml.Result,
                        Severity = ml.Severity,
                        OriginalContent = ml.OriginalContent,
                        DetectedWords = ml.DetectedWords,
                        IsAutomatic = ml.IsAutomatic,
                        ModeratorName = ml.Moderator != null ? ml.Moderator.FirstName + " " + ml.Moderator.LastName : null,
                        IsAppealed = ml.IsAppealed,
                        CreatedAt = ml.CreatedAt
                    })
                    .ToListAsync();

                // Get recent forbidden words
                var recentWords = await _context.ForbiddenWords
                    .Include(fw => fw.CreatedByUser)
                    .OrderByDescending(fw => fw.CreatedAt)
                    .Take(5)
                    .Select(fw => new ForbiddenWordItemViewModel
                    {
                        Id = fw.Id,
                        Word = fw.Word,
                        Severity = fw.Severity,
                        Category = fw.Category,
                        IsBlocked = fw.IsBlocked,
                        IsActive = fw.IsActive,
                        CreatedAt = fw.CreatedAt,
                        CreatedByUserName = fw.CreatedByUser != null ? fw.CreatedByUser.FirstName + " " + fw.CreatedByUser.LastName : "System"
                    })
                    .ToListAsync();

                var model = new ContentModerationDashboardViewModel
                {
                    TotalModerations = totalModerations,
                    ModerationsToday = moderationsToday,
                    ModerationsThisWeek = moderationsThisWeek,
                    AutomaticActions = automaticActions,
                    ManualReviews = manualReviews,
                    PendingReviews = pendingReviews,
                    BlockedContent = blockedContent,
                    FlaggedContent = flaggedContent,
                    ModifiedContent = modifiedContent,
                    TotalForbiddenWords = totalForbiddenWords,
                    ActiveWords = activeWords,
                    CriticalWords = criticalWords,
                    RecentModerations = recentModerations,
                    RecentWords = recentWords
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading content moderation dashboard");
                TempData["Error"] = "An error occurred while loading the moderation dashboard.";
                return View(new ContentModerationDashboardViewModel());
            }
        }

        /// <summary>
        /// Forbidden words management
        /// </summary>
        public async Task<IActionResult> ForbiddenWords(ForbiddenWordsManagementViewModel model)
        {
            try
            {
                model.Page = model.Page == 0 ? 1 : model.Page;
                model.PageSize = 20;

                var query = _context.ForbiddenWords
                    .Include(fw => fw.CreatedByUser)
                    .Include(fw => fw.UpdatedByUser)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(model.SearchTerm))
                {
                    var searchTerm = model.SearchTerm.ToLower();
                    query = query.Where(fw => fw.Word.ToLower().Contains(searchTerm) ||
                                             (fw.Category != null && fw.Category.ToLower().Contains(searchTerm)));
                }

                // Apply filters
                if (model.IsActive.HasValue)
                    query = query.Where(fw => fw.IsActive == model.IsActive.Value);

                if (model.IsBlocked.HasValue)
                    query = query.Where(fw => fw.IsBlocked == model.IsBlocked.Value);

                if (model.Severity.HasValue)
                    query = query.Where(fw => fw.Severity == model.Severity.Value);

                if (!string.IsNullOrWhiteSpace(model.Category))
                    query = query.Where(fw => fw.Category == model.Category);

                // Get total count
                model.TotalWords = await query.CountAsync();

                // Apply sorting
                query = model.SortBy?.ToLower() switch
                {
                    "oldest" => query.OrderBy(fw => fw.CreatedAt),
                    "word" => query.OrderBy(fw => fw.Word),
                    "severity-high" => query.OrderByDescending(fw => fw.Severity),
                    "severity-low" => query.OrderBy(fw => fw.Severity),
                    "category" => query.OrderBy(fw => fw.Category),
                    "newest" or _ => query.OrderByDescending(fw => fw.CreatedAt)
                };

                // Get paginated results
                var forbiddenWords = await query
                    .Skip((model.Page - 1) * model.PageSize)
                    .Take(model.PageSize)
                    .Select(fw => new ForbiddenWordItemViewModel
                    {
                        Id = fw.Id,
                        Word = fw.Word,
                        Severity = fw.Severity,
                        Category = fw.Category,
                        IsBlocked = fw.IsBlocked,
                        IsExactMatch = fw.IsExactMatch,
                        IsActive = fw.IsActive,
                        Replacement = fw.Replacement,
                        AdminNotes = fw.AdminNotes,
                        CreatedAt = fw.CreatedAt,
                        UpdatedAt = fw.UpdatedAt,
                        CreatedByUserName = fw.CreatedByUser != null ? fw.CreatedByUser.FirstName + " " + fw.CreatedByUser.LastName : "System",
                        UpdatedByUserName = fw.UpdatedByUser != null ? fw.UpdatedByUser.FirstName + " " + fw.UpdatedByUser.LastName : null
                    })
                    .ToListAsync();

                model.ForbiddenWords = forbiddenWords;

                // Get statistics
                model.ActiveWordsCount = await _context.ForbiddenWords.CountAsync(fw => fw.IsActive);
                model.BlockedWordsCount = await _context.ForbiddenWords.CountAsync(fw => fw.IsBlocked && fw.IsActive);
                model.FlaggedWordsCount = await _context.ForbiddenWords.CountAsync(fw => !fw.IsBlocked && fw.IsActive);

                // Get categories
                model.Categories = await _context.ForbiddenWords
                    .Where(fw => fw.Category != null)
                    .Select(fw => fw.Category!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading forbidden words management");
                TempData["Error"] = "An error occurred while loading forbidden words.";
                return View(new ForbiddenWordsManagementViewModel());
            }
        }

        /// <summary>
        /// Create new forbidden word
        /// </summary>
        [HttpGet]
        public IActionResult CreateForbiddenWord()
        {
            return View(new CreateEditForbiddenWordViewModel());
        }

        /// <summary>
        /// Create new forbidden word
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateForbiddenWord(CreateEditForbiddenWordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var forbiddenWord = new ForbiddenWord
                {
                    Word = model.Word,
                    Severity = model.Severity,
                    Category = model.Category,
                    IsBlocked = model.IsBlocked,
                    IsExactMatch = model.IsExactMatch,
                    IsActive = model.IsActive,
                    Replacement = model.Replacement,
                    AdminNotes = model.AdminNotes,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = adminId
                };

                await _moderationService.AddForbiddenWordAsync(forbiddenWord, adminId);

                TempData["Success"] = $"Forbidden word '{model.Word}' has been added successfully.";
                return RedirectToAction(nameof(ForbiddenWords));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating forbidden word");
                TempData["Error"] = "An error occurred while creating the forbidden word.";
                return View(model);
            }
        }

        /// <summary>
        /// Edit forbidden word
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditForbiddenWord(int id)
        {
            try
            {
                var word = await _context.ForbiddenWords.FindAsync(id);
                if (word == null)
                {
                    TempData["Error"] = "Forbidden word not found.";
                    return RedirectToAction(nameof(ForbiddenWords));
                }

                var model = new CreateEditForbiddenWordViewModel
                {
                    Id = word.Id,
                    Word = word.Word,
                    Severity = word.Severity,
                    Category = word.Category,
                    IsBlocked = word.IsBlocked,
                    IsExactMatch = word.IsExactMatch,
                    IsActive = word.IsActive,
                    Replacement = word.Replacement,
                    AdminNotes = word.AdminNotes
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading forbidden word for editing");
                TempData["Error"] = "An error occurred while loading the forbidden word.";
                return RedirectToAction(nameof(ForbiddenWords));
            }
        }

        /// <summary>
        /// Edit forbidden word
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditForbiddenWord(CreateEditForbiddenWordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var word = await _context.ForbiddenWords.FindAsync(model.Id);
                if (word == null)
                {
                    TempData["Error"] = "Forbidden word not found.";
                    return RedirectToAction(nameof(ForbiddenWords));
                }

                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                word.Word = model.Word;
                word.Severity = model.Severity;
                word.Category = model.Category;
                word.IsBlocked = model.IsBlocked;
                word.IsExactMatch = model.IsExactMatch;
                word.IsActive = model.IsActive;
                word.Replacement = model.Replacement;
                word.AdminNotes = model.AdminNotes;
                word.UpdateTimestamp(adminId);

                await _moderationService.UpdateForbiddenWordAsync(word, adminId);

                TempData["Success"] = $"Forbidden word '{model.Word}' has been updated successfully.";
                return RedirectToAction(nameof(ForbiddenWords));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating forbidden word");
                TempData["Error"] = "An error occurred while updating the forbidden word.";
                return View(model);
            }
        }

        /// <summary>
        /// Delete forbidden word
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteForbiddenWord(int id)
        {
            try
            {
                var success = await _moderationService.DeleteForbiddenWordAsync(id);
                if (success)
                {
                    return Json(new { success = true, message = "Forbidden word deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Forbidden word not found." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting forbidden word {WordId}", id);
                return Json(new { success = false, message = "Failed to delete forbidden word." });
            }
        }

        /// <summary>
        /// Moderation logs
        /// </summary>
        public async Task<IActionResult> ModerationLogs(ModerationLogsViewModel model)
        {
            try
            {
                model.Page = model.Page == 0 ? 1 : model.Page;

                var (logs, totalCount) = await _moderationService.GetModerationLogsAsync(
                    entityType: model.EntityType,
                    result: model.Result,
                    fromDate: model.FromDate,
                    toDate: model.ToDate,
                    page: model.Page,
                    pageSize: model.PageSize);

                model.Logs = logs.Select(ml => new ModerationLogItemViewModel
                {
                    Id = ml.Id,
                    EntityType = ml.EntityType,
                    EntityId = ml.EntityId,
                    ContentAuthor = ml.ContentAuthor?.FirstName + " " + ml.ContentAuthor?.LastName ?? "Unknown",
                    ContentAuthorEmail = ml.ContentAuthor?.Email ?? "",
                    Action = ml.Action,
                    Result = ml.Result,
                    Severity = ml.Severity,
                    OriginalContent = ml.OriginalContent,
                    ModeratedContent = ml.ModeratedContent,
                    DetectedWords = ml.DetectedWords,
                    IsAutomatic = ml.IsAutomatic,
                    ModeratorName = ml.Moderator?.FirstName + " " + ml.Moderator?.LastName,
                    ModerationReason = ml.ModerationReason,
                    IsAppealed = ml.IsAppealed,
                    AppealDecision = ml.AppealDecision,
                    CreatedAt = ml.CreatedAt,
                    UserIpAddress = ml.UserIpAddress
                }).ToList();

                model.TotalLogs = totalCount;

                // Get statistics
                model.AutomaticModerations = await _context.ModerationLogs.CountAsync(ml => ml.IsAutomatic);
                model.ManualModerations = await _context.ModerationLogs.CountAsync(ml => !ml.IsAutomatic);
                model.AppealsCount = await _context.ModerationLogs.CountAsync(ml => ml.IsAppealed);
                model.BlockedContent = await _context.ModerationLogs.CountAsync(ml => ml.Result == Data.Entities.ModerationResult.Blocked);
                model.FlaggedContent = await _context.ModerationLogs.CountAsync(ml => ml.Result == Data.Entities.ModerationResult.Flagged);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading moderation logs");
                TempData["Error"] = "An error occurred while loading moderation logs.";
                return View(new ModerationLogsViewModel());
            }
        }

        #endregion

        #region Site Ads (Paid footer advertisements)

        /// <summary>
        /// List all site ad slots (footer-1, footer-2, footer-3) for admin management
        /// </summary>
        public async Task<IActionResult> SiteAds()
        {
            try
            {
                ViewData["ShowAdminNavigation"] = true;
                var ads = await _context.SiteAdvertisements
                    .OrderBy(a => a.DisplayOrder)
                    .ThenBy(a => a.SlotKey)
                    .Select(a => new SiteAdItemViewModel
                    {
                        Id = a.Id,
                        SlotKey = a.SlotKey,
                        ImageUrl = a.ImageUrl,
                        TargetUrl = a.TargetUrl,
                        AltText = a.AltText,
                        DisplayOrder = a.DisplayOrder,
                        IsActive = a.IsActive,
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();
                return View(ads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading site ads");
                TempData["Error"] = "An error occurred while loading site ads.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Edit a site ad slot (GET)
        /// </summary>
        public async Task<IActionResult> EditSiteAd(int id)
        {
            try
            {
                ViewData["ShowAdminNavigation"] = true;
                var ad = await _context.SiteAdvertisements.FindAsync(id);
                if (ad == null)
                {
                    TempData["Error"] = "Site ad not found.";
                    return RedirectToAction(nameof(SiteAds));
                }
                var model = new SiteAdEditViewModel
                {
                    Id = ad.Id,
                    SlotKey = ad.SlotKey,
                    ImageUrl = ad.ImageUrl ?? "",
                    TargetUrl = ad.TargetUrl,
                    AltText = ad.AltText,
                    DisplayOrder = ad.DisplayOrder,
                    IsActive = ad.IsActive
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading site ad {Id}", id);
                TempData["Error"] = "An error occurred.";
                return RedirectToAction(nameof(SiteAds));
            }
        }

        /// <summary>
        /// Save site ad (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSiteAd(SiteAdEditViewModel model)
        {
            try
            {
                ViewData["ShowAdminNavigation"] = true;
                if (model.Id <= 0)
                {
                    TempData["Error"] = "Invalid ad.";
                    return RedirectToAction(nameof(SiteAds));
                }
                var ad = await _context.SiteAdvertisements.FindAsync(model.Id);
                if (ad == null)
                {
                    TempData["Error"] = "Site ad not found.";
                    return RedirectToAction(nameof(SiteAds));
                }
                ad.ImageUrl = model.ImageUrl ?? "";
                ad.TargetUrl = string.IsNullOrWhiteSpace(model.TargetUrl) ? null : model.TargetUrl.Trim();
                ad.AltText = string.IsNullOrWhiteSpace(model.AltText) ? null : model.AltText.Trim();
                ad.DisplayOrder = model.DisplayOrder;
                ad.IsActive = model.IsActive;
                ad.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Site ad \"{model.SlotKey}\" updated successfully.";
                return RedirectToAction(nameof(SiteAds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving site ad {Id}", model.Id);
                TempData["Error"] = "An error occurred while saving.";
                return View(model);
            }
        }

        #endregion
    }
}