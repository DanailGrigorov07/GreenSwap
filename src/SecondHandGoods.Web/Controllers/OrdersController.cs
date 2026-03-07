using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandGoods.Data;
using SecondHandGoods.Data.Entities;
using System.Security.Claims;

namespace SecondHandGoods.Web.Controllers
{
    /// <summary>
    /// Controller for managing orders and transactions
    /// </summary>
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Create an order from an advertisement (simplified for review system)
        /// </summary>
        /// <param name="advertisementId">Advertisement to create order for</param>
        /// <param name="sellerId">Seller's user ID</param>
        /// <param name="agreedPrice">Agreed price for the item</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int advertisementId, string sellerId, decimal agreedPrice)
        {
            try
            {
                var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                // Verify advertisement exists and is available
                var advertisement = await _context.Advertisements
                    .FirstOrDefaultAsync(a => a.Id == advertisementId && 
                                            a.UserId == sellerId && 
                                            a.IsActive && 
                                            !a.IsDeleted && 
                                            !a.IsSold);

                if (advertisement == null)
                {
                    TempData["Error"] = "Advertisement not found or not available.";
                    return RedirectToAction("Details", "Ads", new { id = advertisementId });
                }

                // Don't allow users to buy their own items
                if (advertisement.UserId == buyerId)
                {
                    TempData["Error"] = "You cannot purchase your own advertisement.";
                    return RedirectToAction("Details", "Ads", new { id = advertisementId });
                }

                // Check if order already exists for this buyer/seller/ad combination
                var existingOrder = await _context.Orders
                    .FirstOrDefaultAsync(o => o.AdvertisementId == advertisementId && 
                                            o.BuyerId == buyerId && 
                                            o.SellerId == sellerId &&
                                            o.Status != OrderStatus.Cancelled);

                if (existingOrder != null)
                {
                    TempData["Info"] = "An order already exists for this item.";
                    return RedirectToAction(nameof(Details), new { id = existingOrder.Id });
                }

                // Create the order
                var order = new Order
                {
                    OrderNumber = Order.GenerateOrderNumber(),
                    BuyerId = buyerId,
                    SellerId = sellerId,
                    AdvertisementId = advertisementId,
                    FinalPrice = agreedPrice,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    Notes = "Order created through chat conversation"
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Order created successfully! Order number: {order.OrderNumber}";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for advertisement {AdvertisementId}", advertisementId);
                TempData["Error"] = "An error occurred while creating the order.";
                return RedirectToAction("Details", "Ads", new { id = advertisementId });
            }
        }

        /// <summary>
        /// View order details
        /// </summary>
        /// <param name="id">Order ID</param>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var order = await _context.Orders
                    .Include(o => o.Advertisement)
                        .ThenInclude(a => a.Images)
                    .Include(o => o.Buyer)
                    .Include(o => o.Seller)
                    .FirstOrDefaultAsync(o => o.Id == id && 
                                            (o.BuyerId == currentUserId || o.SellerId == currentUserId));

                if (order == null)
                    return NotFound();

                ViewBag.CurrentUserId = currentUserId;
                ViewBag.IsBuyer = order.BuyerId == currentUserId;
                ViewBag.IsSeller = order.SellerId == currentUserId;

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for {OrderId}", id);
                TempData["Error"] = "An error occurred while loading order details.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Complete an order (mark as finished)
        /// </summary>
        /// <param name="id">Order ID</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var order = await _context.Orders
                    .Include(o => o.Advertisement)
                    .FirstOrDefaultAsync(o => o.Id == id && 
                                            (o.BuyerId == currentUserId || o.SellerId == currentUserId) &&
                                            o.Status == OrderStatus.Confirmed);

                if (order == null)
                {
                    TempData["Error"] = "Order not found or cannot be completed.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Complete the order
                order.Complete();

                // Mark advertisement as sold
                order.Advertisement.IsSold = true;
                order.Advertisement.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Order completed successfully! You can now leave reviews.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing order {OrderId}", id);
                TempData["Error"] = "An error occurred while completing the order.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// Confirm an order (accept the transaction)
        /// </summary>
        /// <param name="id">Order ID</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id && 
                                            (o.BuyerId == currentUserId || o.SellerId == currentUserId) &&
                                            o.Status == OrderStatus.Pending);

                if (order == null)
                {
                    TempData["Error"] = "Order not found or cannot be confirmed.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                order.Status = OrderStatus.Confirmed;
                order.UpdateTimestamp();

                await _context.SaveChangesAsync();

                TempData["Success"] = "Order confirmed! You can now proceed with the transaction.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming order {OrderId}", id);
                TempData["Error"] = "An error occurred while confirming the order.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// Cancel an order
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="reason">Reason for cancellation</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id && 
                                            (o.BuyerId == currentUserId || o.SellerId == currentUserId) &&
                                            o.Status != OrderStatus.Completed);

                if (order == null)
                {
                    TempData["Error"] = "Order not found or cannot be cancelled.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                order.Cancel(reason);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Order cancelled successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                TempData["Error"] = "An error occurred while cancelling the order.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// List orders for the current user
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, OrderStatus? status = null)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                const int pageSize = 10;

                var query = _context.Orders
                    .Include(o => o.Advertisement)
                        .ThenInclude(a => a.Images)
                    .Include(o => o.Buyer)
                    .Include(o => o.Seller)
                    .Where(o => o.BuyerId == currentUserId || o.SellerId == currentUserId)
                    .AsQueryable();

                if (status.HasValue)
                    query = query.Where(o => o.Status == status.Value);

                var totalOrders = await query.CountAsync();
                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
                ViewBag.CurrentUserId = currentUserId;
                ViewBag.FilterStatus = status;

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders for user {UserId}", User.Identity?.Name);
                TempData["Error"] = "An error occurred while loading your orders.";
                return View(new List<Order>());
            }
        }

        /// <summary>
        /// Quick order creation API (for testing purposes)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> QuickCreate(int advertisementId)
        {
            try
            {
                var advertisement = await _context.Advertisements
                    .FirstOrDefaultAsync(a => a.Id == advertisementId);

                if (advertisement == null)
                    return Json(new { success = false, message = "Advertisement not found." });

                var order = new Order
                {
                    OrderNumber = Order.GenerateOrderNumber(),
                    BuyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    SellerId = advertisement.UserId,
                    AdvertisementId = advertisementId,
                    FinalPrice = advertisement.Price,
                    Status = OrderStatus.Completed, // Auto-complete for testing
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    Notes = "Quick order for testing reviews"
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Order created successfully!", 
                    orderId = order.Id,
                    orderNumber = order.OrderNumber 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quick order for advertisement {AdvertisementId}", advertisementId);
                return Json(new { success = false, message = "Failed to create order." });
            }
        }
    }
}