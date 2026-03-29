using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandGoods.Data;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Web.Models.Reviews;
using System.Security.Claims;

namespace SecondHandGoods.Web.Controllers
{
    /// <summary>
    /// Controller for handling reviews and ratings functionality
    /// </summary>
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ReviewsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Display reviews for a specific user
        /// </summary>
        /// <param name="userId">User ID to display reviews for</param>
        /// <param name="page">Page number for pagination</param>
        /// <param name="filterByType">Filter by review type</param>
        /// <param name="filterByRating">Filter by rating</param>
        /// <param name="sortBy">Sort option</param>
        [AllowAnonymous]
        public async Task<IActionResult> UserReviews(
            string userId, 
            int page = 1, 
            ReviewType? filterByType = null, 
            int? filterByRating = null, 
            string sortBy = "newest")
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound();

                const int pageSize = 10;

                // Build query for reviews about this user
                var query = _context.Reviews
                    .Include(r => r.Reviewer)
                    .Include(r => r.Order)
                        .ThenInclude(o => o.Advertisement)
                            .ThenInclude(a => a.Images)
                    .Where(r => r.ReviewedUserId == userId && r.IsDisplayable)
                    .AsQueryable();

                // Apply filters
                if (filterByType.HasValue)
                    query = query.Where(r => r.ReviewType == filterByType.Value);

                if (filterByRating.HasValue)
                    query = query.Where(r => r.Rating == filterByRating.Value);

                // Apply sorting
                query = sortBy?.ToLower() switch
                {
                    "oldest" => query.OrderBy(r => r.CreatedAt),
                    "rating-high" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
                    "rating-low" => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
                    "newest" or _ => query.OrderByDescending(r => r.CreatedAt)
                };

                var totalReviews = await query.CountAsync();
                var reviews = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ReviewDisplayViewModel
                    {
                        Id = r.Id,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        ReviewType = r.ReviewType,
                        CreatedAt = r.CreatedAt,
                        IsApproved = r.IsApproved,
                        IsReported = r.IsReported,
                        ReviewerId = r.ReviewerId,
                        ReviewerName = r.Reviewer.FirstName + " " + r.Reviewer.LastName,
                        ReviewerRating = r.Reviewer.SellerRating,
                        ReviewedUserId = r.ReviewedUserId,
                        ReviewedUserName = user.FirstName + " " + user.LastName,
                        OrderId = r.OrderId,
                        OrderNumber = r.Order.OrderNumber,
                        AdvertisementId = r.Order.AdvertisementId,
                        AdvertisementTitle = r.Order.Advertisement.Title,
                        AdvertisementImageUrl = r.Order.Advertisement.Images
                            .Where(img => img.IsMainImage)
                            .Select(img => img.ImageUrl)
                            .FirstOrDefault() ?? "/images/no-image.jpg",
                        FinalPrice = r.Order.FinalPrice
                    })
                    .ToListAsync();

                // Get rating breakdown
                var ratingBreakdown = await _context.Reviews
                    .Where(r => r.ReviewedUserId == userId && r.IsDisplayable)
                    .GroupBy(r => r.Rating)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                var model = new UserReviewsViewModel
                {
                    UserId = userId,
                    UserName = user.FirstName + " " + user.LastName,
                    OverallRating = user.SellerRating,
                    TotalReviews = totalReviews,
                    MemberSince = user.CreatedAt,
                    Reviews = reviews,
                    RatingBreakdown = ratingBreakdown,
                    Page = page,
                    PageSize = pageSize,
                    FilterByType = filterByType,
                    FilterByRating = filterByRating,
                    SortBy = sortBy
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reviews for user {UserId}", userId);
                TempData["Error"] = "An error occurred while loading reviews.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Display pending reviews for the current user
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Pending(int page = 1)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                const int pageSize = 10;

                // Find completed orders where the current user can leave a review
                var completedOrdersQuery = _context.Orders
                    .Include(o => o.Advertisement)
                        .ThenInclude(a => a.Images)
                    .Include(o => o.Buyer)
                    .Include(o => o.Seller)
                    .Where(o => o.Status == OrderStatus.Completed && 
                               (o.BuyerId == currentUserId || o.SellerId == currentUserId))
                    .Where(o => !_context.Reviews.Any(r => r.OrderId == o.Id && r.ReviewerId == currentUserId));

                var totalPending = await completedOrdersQuery.CountAsync();

                var pendingOrders = await completedOrdersQuery
                    .OrderByDescending(o => o.CompletedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var pendingReviews = pendingOrders.Select(order => new PendingReviewItemViewModel
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    CompletedAt = order.CompletedAt!.Value,
                    ReviewType = order.BuyerId == currentUserId ? ReviewType.BuyerToSeller : ReviewType.SellerToBuyer,
                    AdvertisementId = order.AdvertisementId,
                    AdvertisementTitle = order.Advertisement.Title,
                    AdvertisementImageUrl = order.Advertisement.Images
                        .FirstOrDefault(img => img.IsMainImage)?.ImageUrl ?? "/images/no-image.jpg",
                    FinalPrice = order.FinalPrice,
                    OtherUserId = order.BuyerId == currentUserId ? order.SellerId : order.BuyerId,
                    OtherUserName = order.BuyerId == currentUserId 
                        ? order.Seller.FirstName + " " + order.Seller.LastName
                        : order.Buyer.FirstName + " " + order.Buyer.LastName,
                    OtherUserRating = order.BuyerId == currentUserId 
                        ? order.Seller.SellerRating 
                        : order.Buyer.SellerRating
                }).ToList();

                var model = new PendingReviewsViewModel
                {
                    PendingReviews = pendingReviews,
                    TotalPending = totalPending,
                    Page = page,
                    PageSize = pageSize
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending reviews for user {UserId}", User.Identity?.Name);
                TempData["Error"] = "An error occurred while loading pending reviews.";
                return View(new PendingReviewsViewModel());
            }
        }

        /// <summary>
        /// Display form to create a new review
        /// </summary>
        /// <param name="orderId">Order ID to create review for</param>
        [Authorize]
        public async Task<IActionResult> Create(int orderId)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var order = await _context.Orders
                    .Include(o => o.Advertisement)
                        .ThenInclude(a => a.Images)
                    .Include(o => o.Buyer)
                    .Include(o => o.Seller)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.Status == OrderStatus.Completed &&
                                            (o.BuyerId == currentUserId || o.SellerId == currentUserId));

                if (order == null)
                {
                    TempData["Error"] = "Order not found or not eligible for review.";
                    return RedirectToAction(nameof(Pending));
                }

                // Check if review already exists
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.OrderId == orderId && r.ReviewerId == currentUserId);

                if (existingReview != null)
                {
                    TempData["Info"] = "You have already reviewed this transaction.";
                    return RedirectToAction(nameof(Pending));
                }

                var isBuyer = order.BuyerId == currentUserId;
                var reviewType = isBuyer ? ReviewType.BuyerToSeller : ReviewType.SellerToBuyer;
                var reviewedUser = isBuyer ? order.Seller : order.Buyer;

                var model = new CreateReviewViewModel
                {
                    OrderId = orderId,
                    ReviewedUserId = reviewedUser.Id,
                    ReviewType = reviewType,
                    OrderNumber = order.OrderNumber,
                    AdvertisementTitle = order.Advertisement.Title,
                    AdvertisementImageUrl = order.Advertisement.Images
                        .FirstOrDefault(img => img.IsMainImage)?.ImageUrl ?? "/images/no-image.jpg",
                    FinalPrice = order.FinalPrice,
                    ReviewedUserName = reviewedUser.FirstName + " " + reviewedUser.LastName,
                    OrderCompletedDate = order.CompletedAt!.Value
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading review creation form for order {OrderId}", orderId);
                TempData["Error"] = "An error occurred while loading the review form.";
                return RedirectToAction(nameof(Pending));
            }
        }

        /// <summary>
        /// Process review creation
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReviewViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await PopulateCreateReviewModel(model);
                    return View(model);
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                // Verify order and permissions
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == model.OrderId && o.Status == OrderStatus.Completed &&
                                            (o.BuyerId == currentUserId || o.SellerId == currentUserId));

                if (order == null)
                {
                    ModelState.AddModelError("", "Order not found or not eligible for review.");
                    await PopulateCreateReviewModel(model);
                    return View(model);
                }

                // Check for existing review
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.OrderId == model.OrderId && r.ReviewerId == currentUserId);

                if (existingReview != null)
                {
                    ModelState.AddModelError("", "You have already reviewed this transaction.");
                    await PopulateCreateReviewModel(model);
                    return View(model);
                }

                // Create the review
                var review = new Review
                {
                    OrderId = model.OrderId,
                    ReviewerId = currentUserId,
                    ReviewedUserId = model.ReviewedUserId,
                    Rating = model.Rating,
                    Comment = model.Comment?.Trim(),
                    ReviewType = model.ReviewType,
                    CreatedAt = DateTime.UtcNow,
                    IsApproved = true, // Auto-approve for now
                    IsPublic = true
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // Update user's overall rating
                await UpdateUserRating(model.ReviewedUserId);

                TempData["Success"] = "Thank you for your review! Your feedback helps build trust in our community.";
                return RedirectToAction(nameof(Pending));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review for order {OrderId}", model.OrderId);
                ModelState.AddModelError("", "An error occurred while saving your review. Please try again.");
                await PopulateCreateReviewModel(model);
                return View(model);
            }
        }

        /// <summary>
        /// Report a review as inappropriate
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Report(int reviewId, string? reason)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(reviewId);
                if (review == null)
                    return Json(new { success = false, message = "Review not found." });

                review.Report();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Review {ReviewId} reported by user {UserId}. Reason: {Reason}", 
                    reviewId, User.Identity?.Name, reason);

                return Json(new { success = true, message = "Review reported successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting review {ReviewId}", reviewId);
                return Json(new { success = false, message = "Failed to report review." });
            }
        }

        /// <summary>
        /// Get review statistics for a user (API endpoint)
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Stats(string userId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Where(r => r.ReviewedUserId == userId && r.IsDisplayable)
                    .ToListAsync();

                if (!reviews.Any())
                {
                    return Json(new
                    {
                        averageRating = 0,
                        totalReviews = 0,
                        ratingBreakdown = new Dictionary<int, int>()
                    });
                }

                var avgRating = reviews.Average(r => r.Rating);
                var ratingBreakdown = reviews
                    .GroupBy(r => r.Rating)
                    .ToDictionary(g => g.Key, g => g.Count());

                return Json(new
                {
                    averageRating = Math.Round(avgRating, 1),
                    totalReviews = reviews.Count,
                    ratingBreakdown = ratingBreakdown
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review stats for user {UserId}", userId);
                return Json(new { error = "Failed to load review statistics." });
            }
        }

        #region Admin Actions

        /// <summary>
        /// Admin review management page
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage(
            int page = 1,
            bool? isApproved = null,
            bool? isReported = null,
            ReviewType? filterByType = null,
            int? filterByRating = null,
            string sortBy = "newest")
        {
            try
            {
                const int pageSize = 20;

                var query = _context.Reviews
                    .Include(r => r.Reviewer)
                    .Include(r => r.ReviewedUser)
                    .Include(r => r.Order)
                        .ThenInclude(o => o.Advertisement)
                    .AsQueryable();

                // Apply filters
                if (isApproved.HasValue)
                    query = query.Where(r => r.IsApproved == isApproved.Value);

                if (isReported.HasValue)
                    query = query.Where(r => r.IsReported == isReported.Value);

                if (filterByType.HasValue)
                    query = query.Where(r => r.ReviewType == filterByType.Value);

                if (filterByRating.HasValue)
                    query = query.Where(r => r.Rating == filterByRating.Value);

                // Apply sorting
                query = sortBy?.ToLower() switch
                {
                    "oldest" => query.OrderBy(r => r.CreatedAt),
                    "rating-high" => query.OrderByDescending(r => r.Rating),
                    "rating-low" => query.OrderBy(r => r.Rating),
                    "reported" => query.OrderByDescending(r => r.IsReported).ThenByDescending(r => r.CreatedAt),
                    "newest" or _ => query.OrderByDescending(r => r.CreatedAt)
                };

                var totalReviews = await query.CountAsync();
                var reviews = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ReviewDisplayViewModel
                    {
                        Id = r.Id,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        ReviewType = r.ReviewType,
                        CreatedAt = r.CreatedAt,
                        IsApproved = r.IsApproved,
                        IsReported = r.IsReported,
                        ReviewerId = r.ReviewerId,
                        ReviewerName = r.Reviewer.FirstName + " " + r.Reviewer.LastName,
                        ReviewedUserId = r.ReviewedUserId,
                        ReviewedUserName = r.ReviewedUser.FirstName + " " + r.ReviewedUser.LastName,
                        OrderNumber = r.Order.OrderNumber,
                        AdvertisementTitle = r.Order.Advertisement.Title
                    })
                    .ToListAsync();

                var reviewDayStart = DateTime.UtcNow.Date;
                var reviewDayEnd = reviewDayStart.AddDays(1);
                var model = new ReviewManagementViewModel
                {
                    Reviews = reviews,
                    TotalReviews = totalReviews,
                    IsApproved = isApproved,
                    IsReported = isReported,
                    FilterByType = filterByType,
                    FilterByRating = filterByRating,
                    SortBy = sortBy,
                    Page = page,
                    PageSize = pageSize,
                    ReportedReviewsCount = await _context.Reviews.CountAsync(r => r.IsReported),
                    UnapprovedReviewsCount = await _context.Reviews.CountAsync(r => !r.IsApproved),
                    TotalReviewsToday = await _context.Reviews.CountAsync(r =>
                        r.CreatedAt >= reviewDayStart && r.CreatedAt < reviewDayEnd)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading review management page");
                TempData["Error"] = "An error occurred while loading reviews.";
                return View(new ReviewManagementViewModel());
            }
        }

        /// <summary>
        /// Admin action to approve/disapprove/delete reviews
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminAction(ReviewActionViewModel model)
        {
            try
            {
                var review = await _context.Reviews
                    .Include(r => r.ReviewedUser)
                    .FirstOrDefaultAsync(r => r.Id == model.ReviewId);

                if (review == null)
                    return Json(new { success = false, message = "Review not found." });

                switch (model.Action.ToLower())
                {
                    case "approve":
                        review.Approve();
                        break;
                    case "hide":
                        review.Hide();
                        break;
                    case "delete":
                        _context.Reviews.Remove(review);
                        break;
                    default:
                        return Json(new { success = false, message = "Invalid action." });
                }

                await _context.SaveChangesAsync();

                // Update user rating if review was approved or deleted
                if (model.Action.ToLower() == "approve" || model.Action.ToLower() == "delete")
                {
                    await UpdateUserRating(review.ReviewedUserId);
                }

                _logger.LogInformation("Admin {AdminId} performed action '{Action}' on review {ReviewId}", 
                    User.Identity?.Name, model.Action, model.ReviewId);

                return Json(new { success = true, message = $"Review {model.Action}d successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing admin action on review {ReviewId}", model.ReviewId);
                return Json(new { success = false, message = "Failed to perform action." });
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Populate display data for create review model
        /// </summary>
        private async Task PopulateCreateReviewModel(CreateReviewViewModel model)
        {
            var order = await _context.Orders
                .Include(o => o.Advertisement)
                    .ThenInclude(a => a.Images)
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .FirstOrDefaultAsync(o => o.Id == model.OrderId);

            if (order != null)
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var isBuyer = order.BuyerId == currentUserId;
                var reviewedUser = isBuyer ? order.Seller : order.Buyer;

                model.OrderNumber = order.OrderNumber;
                model.AdvertisementTitle = order.Advertisement.Title;
                model.AdvertisementImageUrl = order.Advertisement.Images
                    .FirstOrDefault(img => img.IsMainImage)?.ImageUrl ?? "/images/no-image.jpg";
                model.FinalPrice = order.FinalPrice;
                model.ReviewedUserName = reviewedUser.FirstName + " " + reviewedUser.LastName;
                model.OrderCompletedDate = order.CompletedAt!.Value;
            }
        }

        /// <summary>
        /// Update a user's overall rating based on their reviews
        /// </summary>
        private async Task UpdateUserRating(string userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return;

                var reviews = await _context.Reviews
                    .Where(r => r.ReviewedUserId == userId && r.IsDisplayable)
                    .ToListAsync();

                if (reviews.Any())
                {
                    var averageRating = reviews.Average(r => r.Rating);
                    user.SellerRating = Math.Round((decimal)averageRating, 1);
                }
                else
                {
                    user.SellerRating = 0;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user rating for {UserId}", userId);
            }
        }

        #endregion
    }
}