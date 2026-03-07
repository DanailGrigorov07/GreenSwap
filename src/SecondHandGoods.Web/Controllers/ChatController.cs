using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandGoods.Data;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Web.Models.Chat;
using System.Security.Claims;

namespace SecondHandGoods.Web.Controllers
{
    /// <summary>
    /// Controller for chat functionality and conversation management
    /// </summary>
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ChatController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Display conversations inbox
        /// </summary>
        public async Task<IActionResult> Index(int page = 1)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                const int pageSize = 20;

                // Load all messages for this user (with includes), then group in memory so the query works on SQLite/EF Core
                var messages = await _context.Messages
                    .Include(m => m.Advertisement)
                    .ThenInclude(a => a!.Images)
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Where(m => (m.SenderId == currentUserId || m.ReceiverId == currentUserId) && !m.IsDeleted)
                    .OrderByDescending(m => m.SentAt)
                    .ToListAsync();

                var grouped = messages
                    .GroupBy(m => new { m.AdvertisementId, OtherUserId = m.SenderId == currentUserId ? m.ReceiverId : m.SenderId })
                    .Select(g => new
                    {
                        g.Key.AdvertisementId,
                        g.Key.OtherUserId,
                        LastMessage = g.First(),
                        UnreadCount = g.Count(m => m.ReceiverId == currentUserId && !m.IsRead)
                    })
                    .OrderByDescending(c => c.LastMessage.SentAt)
                    .ToList();

                var totalCount = grouped.Count;
                var paged = grouped
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c =>
                    {
                        var ad = c.LastMessage.Advertisement;
                        var last = c.LastMessage;
                        var otherUser = last.SenderId == currentUserId ? last.Receiver : last.Sender;
                        return new ConversationSummaryViewModel
                        {
                            AdvertisementId = c.AdvertisementId,
                            AdvertisementTitle = ad?.Title ?? "",
                            AdvertisementImageUrl = ad?.Images?.FirstOrDefault(img => img.IsMainImage)?.ImageUrl ?? "/images/no-image.svg",
                            AdvertisementPrice = ad?.Price ?? 0,
                            IsAdvertisementActive = ad?.IsActive ?? false,
                            IsAdvertisementSold = ad?.IsSold ?? false,
                            OtherUserId = c.OtherUserId,
                            OtherUserName = otherUser != null ? $"{otherUser.FirstName} {otherUser.LastName}" : "",
                            OtherUserRating = otherUser?.SellerRating ?? 0,
                            LastMessageContent = last.Content,
                            LastMessageSenderId = last.SenderId,
                            LastMessageTime = last.SentAt,
                            LastMessageFromCurrentUser = last.SenderId == currentUserId,
                            UnreadCount = c.UnreadCount
                        };
                    })
                    .ToList();

                var totalUnreadCount = await _context.Messages
                    .Where(m => m.ReceiverId == currentUserId && !m.IsRead && !m.IsDeleted)
                    .CountAsync();

                var model = new ConversationListViewModel
                {
                    Conversations = paged,
                    TotalUnreadCount = totalUnreadCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalConversations = totalCount
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversations for user {UserId}", User.Identity?.Name);
                TempData["Error"] = "An error occurred while loading your conversations.";
                return View(new ConversationListViewModel());
            }
        }

        /// <summary>
        /// Display a specific conversation
        /// </summary>
        /// <param name="advertisementId">The advertisement ID</param>
        /// <param name="otherUserId">The other user's ID</param>
        public async Task<IActionResult> Conversation(int advertisementId, string otherUserId)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                // Verify the advertisement exists and user has permission to chat about it
                var advertisement = await _context.Advertisements
                    .Include(a => a.User)
                    .Include(a => a.Images)
                    .FirstOrDefaultAsync(a => a.Id == advertisementId && !a.IsDeleted);

                if (advertisement == null)
                {
                    TempData["Error"] = "Advertisement not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Verify the other user exists
                var otherUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == otherUserId);
                if (otherUser == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Verify user can chat (must be ad owner or interested buyer)
                if (advertisement.UserId != currentUserId && advertisement.UserId != otherUserId)
                {
                    TempData["Error"] = "You don't have permission to view this conversation.";
                    return RedirectToAction(nameof(Index));
                }

                // Don't allow users to chat with themselves
                if (currentUserId == otherUserId)
                {
                    TempData["Error"] = "You cannot start a conversation with yourself.";
                    return RedirectToAction("Details", "Ads", new { id = advertisementId });
                }

                // Load messages for this conversation
                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.AdvertisementId == advertisementId &&
                               ((m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                                (m.SenderId == otherUserId && m.ReceiverId == currentUserId)) &&
                               !m.IsDeleted)
                    .OrderBy(m => m.SentAt)
                    .Select(m => new MessageViewModel
                    {
                        Id = m.Id,
                        Content = m.Content,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.FirstName + " " + m.Sender.LastName,
                        ReceiverId = m.ReceiverId,
                        MessageType = m.MessageType,
                        SentAt = m.SentAt,
                        IsRead = m.IsRead,
                        ReadAt = m.ReadAt,
                        IsFromCurrentUser = m.SenderId == currentUserId
                    })
                    .ToListAsync();

                // Mark messages as read
                await MarkConversationMessagesAsRead(advertisementId, currentUserId, otherUserId);

                var unreadCount = messages.Count(m => !m.IsFromCurrentUser && !m.IsRead);

                var model = new ConversationViewModel
                {
                    AdvertisementId = advertisementId,
                    AdvertisementTitle = advertisement.Title,
                    AdvertisementImageUrl = advertisement.Images
                        .FirstOrDefault(img => img.IsMainImage)?.ImageUrl ?? "/images/no-image.svg",
                    AdvertisementPrice = advertisement.Price,
                    AdvertisementLocation = advertisement.Location ?? "",
                    IsAdvertisementActive = advertisement.IsActive,
                    IsAdvertisementSold = advertisement.IsSold,
                    OtherUserId = otherUserId,
                    OtherUserName = otherUser.FirstName + " " + otherUser.LastName,
                    OtherUserRating = otherUser.SellerRating,
                    OtherUserJoinedDate = otherUser.CreatedAt,
                    CurrentUserId = currentUserId,
                    CurrentUserName = User.Identity?.Name ?? "",
                    Messages = messages,
                    UnreadCount = unreadCount,
                    LastMessageTime = messages.LastOrDefault()?.SentAt
                };

                ViewBag.MessageTemplates = MessageTemplateViewModel.GetDefaultTemplates();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation for advertisement {AdvertisementId}", advertisementId);
                TempData["Error"] = "An error occurred while loading the conversation.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Start a new conversation from an advertisement
        /// </summary>
        /// <param name="advertisementId">The advertisement ID</param>
        public async Task<IActionResult> StartConversation(int advertisementId)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var advertisement = await _context.Advertisements
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == advertisementId && a.IsActive && !a.IsDeleted && !a.IsSold);

                if (advertisement == null)
                {
                    TempData["Error"] = "Advertisement not found or not available for messaging.";
                    return RedirectToAction("Details", "Ads", new { id = advertisementId });
                }

                // Don't allow owner to message themselves
                if (advertisement.UserId == currentUserId)
                {
                    TempData["Info"] = "You cannot start a conversation about your own advertisement.";
                    return RedirectToAction("Details", "Ads", new { id = advertisementId });
                }

                // Check if conversation already exists
                var existingMessage = await _context.Messages
                    .FirstOrDefaultAsync(m => m.AdvertisementId == advertisementId &&
                                            ((m.SenderId == currentUserId && m.ReceiverId == advertisement.UserId) ||
                                             (m.SenderId == advertisement.UserId && m.ReceiverId == currentUserId)));

                if (existingMessage != null)
                {
                    return RedirectToAction(nameof(Conversation), new { advertisementId, otherUserId = advertisement.UserId });
                }

                // Redirect to conversation page to start new conversation
                return RedirectToAction(nameof(Conversation), new { advertisementId, otherUserId = advertisement.UserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversation for advertisement {AdvertisementId}", advertisementId);
                TempData["Error"] = "An error occurred while starting the conversation.";
                return RedirectToAction("Details", "Ads", new { id = advertisementId });
            }
        }

        /// <summary>
        /// Send a message via HTTP POST (fallback for non-SignalR clients)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(SendMessageViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Invalid message data.";
                    return RedirectToAction(nameof(Conversation), new { model.AdvertisementId, otherUserId = model.ReceiverId });
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                // Verify advertisement and permissions
                var advertisement = await _context.Advertisements
                    .FirstOrDefaultAsync(a => a.Id == model.AdvertisementId && !a.IsDeleted);

                if (advertisement == null)
                {
                    TempData["Error"] = "Advertisement not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (advertisement.UserId != currentUserId && model.ReceiverId != advertisement.UserId)
                {
                    TempData["Error"] = "You don't have permission to send this message.";
                    return RedirectToAction(nameof(Index));
                }

                // Prepare message content based on type
                var messageContent = model.Content;
                if (model.MessageType == MessageType.PriceOffer && model.OfferPrice.HasValue)
                {
                    messageContent = $"{model.Content}\n\nOffered Price: ${model.OfferPrice:F2}";
                }
                else if (model.MessageType == MessageType.MeetingRequest && model.ProposedMeetingTime.HasValue)
                {
                    messageContent = $"{model.Content}\n\nProposed Meeting: {model.ProposedMeetingTime:MMM dd, yyyy 'at' h:mm tt}";
                    if (!string.IsNullOrWhiteSpace(model.MeetingLocation))
                    {
                        messageContent += $"\nLocation: {model.MeetingLocation}";
                    }
                }

                // Create message
                var message = new Message
                {
                    Content = messageContent,
                    SenderId = currentUserId,
                    ReceiverId = model.ReceiverId,
                    AdvertisementId = model.AdvertisementId,
                    MessageType = model.MessageType,
                    SentAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Message sent successfully!";
                return RedirectToAction(nameof(Conversation), new { model.AdvertisementId, otherUserId = model.ReceiverId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message for advertisement {AdvertisementId}", model.AdvertisementId);
                TempData["Error"] = "An error occurred while sending the message.";
                return RedirectToAction(nameof(Conversation), new { model.AdvertisementId, otherUserId = model.ReceiverId });
            }
        }

        /// <summary>
        /// Get unread message count (AJAX endpoint)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                
                var count = await _context.Messages
                    .Where(m => m.ReceiverId == currentUserId && !m.IsRead && !m.IsDeleted)
                    .CountAsync();

                return Json(new { unreadCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread message count for user {UserId}", User.Identity?.Name);
                return Json(new { unreadCount = 0 });
            }
        }

        /// <summary>
        /// Delete a conversation (HTTP POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConversation(int advertisementId, string otherUserId)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var messages = await _context.Messages
                    .Where(m => m.AdvertisementId == advertisementId &&
                               ((m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                                (m.SenderId == otherUserId && m.ReceiverId == currentUserId)) &&
                               (m.SenderId == currentUserId || m.ReceiverId == currentUserId))
                    .ToListAsync();

                // Soft delete messages where current user is involved
                foreach (var message in messages)
                {
                    if (message.SenderId == currentUserId || message.ReceiverId == currentUserId)
                    {
                        message.Delete();
                    }
                }

                if (messages.Any())
                {
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Conversation deleted successfully.";
                }
                else
                {
                    TempData["Info"] = "No conversation found to delete.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation for advertisement {AdvertisementId}", advertisementId);
                TempData["Error"] = "An error occurred while deleting the conversation.";
                return RedirectToAction(nameof(Index));
            }
        }

        #region Helper Methods

        /// <summary>
        /// Mark messages in a conversation as read
        /// </summary>
        private async Task MarkConversationMessagesAsRead(int advertisementId, string currentUserId, string otherUserId)
        {
            try
            {
                var unreadMessages = await _context.Messages
                    .Where(m => m.AdvertisementId == advertisementId &&
                               m.SenderId == otherUserId &&
                               m.ReceiverId == currentUserId &&
                               !m.IsRead &&
                               !m.IsDeleted)
                    .ToListAsync();

                foreach (var message in unreadMessages)
                {
                    message.MarkAsRead();
                }

                if (unreadMessages.Any())
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read for conversation {AdvertisementId}", advertisementId);
            }
        }

        #endregion
    }
}