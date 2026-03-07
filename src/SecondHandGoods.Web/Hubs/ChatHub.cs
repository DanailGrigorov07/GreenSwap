using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SecondHandGoods.Data;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Services;
using System.Security.Claims;

namespace SecondHandGoods.Web.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time chat functionality
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly IContentModerationService _moderationService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ApplicationDbContext context, IContentModerationService moderationService, ILogger<ChatHub> logger)
        {
            _context = context;
            _moderationService = moderationService;
            _logger = logger;
        }

        /// <summary>
        /// Handles user connection to the chat hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Anonymous";

            _logger.LogInformation("User {UserName} ({UserId}) connected to chat hub", userName, userId);

            // Join user to their personal group for direct notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Notify others that user is online (optional)
            await Clients.All.SendAsync("UserConnected", userId, userName);

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Handles user disconnection from the chat hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Anonymous";

            _logger.LogInformation("User {UserName} ({UserId}) disconnected from chat hub", userName, userId);

            // Remove user from their personal group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Notify others that user is offline (optional)
            await Clients.All.SendAsync("UserDisconnected", userId, userName);

            if (exception != null)
            {
                _logger.LogError(exception, "User {UserName} ({UserId}) disconnected with error", userName, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a conversation room for a specific advertisement
        /// </summary>
        /// <param name="advertisementId">The advertisement ID</param>
        /// <param name="otherUserId">The other user in the conversation</param>
        public async Task JoinConversation(int advertisementId, string otherUserId)
        {
            try
            {
                var currentUserId = Context.UserIdentifier!;
                var conversationId = GetConversationId(currentUserId, otherUserId, advertisementId);

                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
                
                _logger.LogInformation("User {UserId} joined conversation {ConversationId}", currentUserId, conversationId);

                // Notify the user they've joined the conversation
                await Clients.Caller.SendAsync("ConversationJoined", conversationId, advertisementId);

                // Load and send recent messages
                await SendRecentMessages(conversationId, advertisementId, currentUserId, otherUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining conversation for advertisement {AdvertisementId}", advertisementId);
                await Clients.Caller.SendAsync("Error", "Failed to join conversation");
            }
        }

        /// <summary>
        /// Leave a conversation room
        /// </summary>
        /// <param name="advertisementId">The advertisement ID</param>
        /// <param name="otherUserId">The other user in the conversation</param>
        public async Task LeaveConversation(int advertisementId, string otherUserId)
        {
            try
            {
                var currentUserId = Context.UserIdentifier!;
                var conversationId = GetConversationId(currentUserId, otherUserId, advertisementId);

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
                
                _logger.LogInformation("User {UserId} left conversation {ConversationId}", currentUserId, conversationId);

                await Clients.Caller.SendAsync("ConversationLeft", conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving conversation for advertisement {AdvertisementId}", advertisementId);
                await Clients.Caller.SendAsync("Error", "Failed to leave conversation");
            }
        }

        /// <summary>
        /// Send a message in a conversation
        /// </summary>
        /// <param name="advertisementId">The advertisement ID</param>
        /// <param name="receiverId">The receiver's user ID</param>
        /// <param name="content">The message content</param>
        /// <param name="messageType">The type of message (optional, defaults to Text)</param>
        public async Task SendMessage(int advertisementId, string receiverId, string content, MessageType messageType = MessageType.Text)
        {
            try
            {
                var senderId = Context.UserIdentifier!;
                var senderName = Context.User?.Identity?.Name ?? "Anonymous";

                // Validate input
                if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
                {
                    await Clients.Caller.SendAsync("Error", "Message content is invalid");
                    return;
                }

                // Moderate message content
                var moderation = await _moderationService.ModerateContentAsync(
                    content.Trim(), 
                    ModeratedEntityType.Message, 
                    0, // We don't have the message ID yet
                    senderId,
                    Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString());

                // Handle moderation results
                string finalContent = content.Trim();
                if (!moderation.Passed)
                {
                    if (moderation.WasModified)
                    {
                        finalContent = moderation.ModifiedContent;
                        await Clients.Caller.SendAsync("ModerationWarning", 
                            "Your message contained inappropriate content and has been modified.");
                    }
                    else
                    {
                        // Message was blocked
                        await Clients.Caller.SendAsync("Error", 
                            "Your message contains inappropriate content and cannot be sent.");
                        return;
                    }
                }

                // Verify the advertisement exists
                var advertisement = await _context.Advertisements
                    .FirstOrDefaultAsync(a => a.Id == advertisementId && !a.IsDeleted);

                if (advertisement == null)
                {
                    await Clients.Caller.SendAsync("Error", "Advertisement not found");
                    return;
                }

                // Verify the receiver exists
                var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Id == receiverId);
                if (receiver == null)
                {
                    await Clients.Caller.SendAsync("Error", "Receiver not found");
                    return;
                }

                // Create the message with moderated content
                var message = new Message
                {
                    Content = finalContent,
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    AdvertisementId = advertisementId,
                    MessageType = messageType,
                    SentAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Update moderation log with the correct message ID
                if (moderation.WasModified || !moderation.Passed)
                {
                    var moderationLog = await _context.ModerationLogs
                        .Where(ml => ml.EntityType == ModeratedEntityType.Message 
                                  && ml.ContentAuthorId == senderId 
                                  && ml.EntityId == 0
                                  && ml.OriginalContent == content.Trim())
                        .OrderByDescending(ml => ml.CreatedAt)
                        .FirstOrDefaultAsync();
                    
                    if (moderationLog != null)
                    {
                        moderationLog.EntityId = message.Id;
                        await _context.SaveChangesAsync();
                    }
                }

                // Get conversation ID
                var conversationId = GetConversationId(senderId, receiverId, advertisementId);

                // Prepare message data for clients (SentAtDisplay avoids client-side date parsing issues)
                var sentAt = message.SentAt;
                var sentAtDisplay = (DateTime.UtcNow - sentAt).TotalDays >= 1 ? sentAt.ToString("MMM dd") :
                    (DateTime.UtcNow - sentAt).TotalHours >= 1 ? sentAt.ToString("h:mm tt") : "Just now";
                var messageData = new
                {
                    Id = message.Id,
                    Content = message.Content,
                    SenderId = senderId,
                    SenderName = senderName,
                    ReceiverId = receiverId,
                    AdvertisementId = advertisementId,
                    MessageType = message.MessageType,
                    SentAt = message.SentAt,
                    SentAtDisplay = sentAtDisplay,
                    IsRead = false,
                    ConversationId = conversationId
                };

                // Send to conversation group (sender and anyone else in the room see it)
                await Clients.Group(conversationId).SendAsync("ReceiveMessage", messageData);

                // Also send directly to receiver so they get it even if not in the room (e.g. just connected)
                await Clients.User(receiverId).SendAsync("ReceiveMessage", messageData);

                // Send notification to receiver's personal group (if they're not in the conversation room)
                await Clients.Group($"user_{receiverId}").SendAsync("MessageNotification", new
                {
                    MessageId = message.Id,
                    SenderId = senderId,
                    SenderName = senderName,
                    AdvertisementId = advertisementId,
                    AdvertisementTitle = advertisement.Title,
                    Content = content.Length > 50 ? content.Substring(0, 50) + "..." : content,
                    SentAt = message.SentAt,
                    ConversationId = conversationId
                });

                _logger.LogInformation("Message sent from {SenderId} to {ReceiverId} for advertisement {AdvertisementId}", 
                    senderId, receiverId, advertisementId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message for advertisement {AdvertisementId}", advertisementId);
                await Clients.Caller.SendAsync("Error", "Failed to send message");
            }
        }

        /// <summary>
        /// Mark messages as read
        /// </summary>
        /// <param name="messageIds">Array of message IDs to mark as read</param>
        public async Task MarkMessagesAsRead(int[] messageIds)
        {
            try
            {
                var currentUserId = Context.UserIdentifier!;

                var messages = await _context.Messages
                    .Where(m => messageIds.Contains(m.Id) && m.ReceiverId == currentUserId && !m.IsRead)
                    .ToListAsync();

                foreach (var message in messages)
                {
                    message.MarkAsRead();
                }

                if (messages.Any())
                {
                    await _context.SaveChangesAsync();

                    // Notify sender that their messages have been read
                    var senderIds = messages.Select(m => m.SenderId).Distinct();
                    foreach (var senderId in senderIds)
                    {
                        await Clients.Group($"user_{senderId}").SendAsync("MessagesRead", messageIds, currentUserId);
                    }

                    _logger.LogInformation("Marked {Count} messages as read for user {UserId}", messages.Count, currentUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read for user {UserId}", Context.UserIdentifier);
                await Clients.Caller.SendAsync("Error", "Failed to mark messages as read");
            }
        }

        /// <summary>
        /// Send typing indicator
        /// </summary>
        /// <param name="advertisementId">The advertisement ID</param>
        /// <param name="otherUserId">The other user in the conversation</param>
        /// <param name="isTyping">Whether the user is typing</param>
        public async Task SendTypingIndicator(int advertisementId, string otherUserId, bool isTyping)
        {
            try
            {
                var currentUserId = Context.UserIdentifier!;
                var userName = Context.User?.Identity?.Name ?? "Anonymous";
                var conversationId = GetConversationId(currentUserId, otherUserId, advertisementId);

                // Send typing indicator to the conversation group (excluding the sender)
                await Clients.GroupExcept(conversationId, Context.ConnectionId).SendAsync("TypingIndicator", new
                {
                    UserId = currentUserId,
                    UserName = userName,
                    IsTyping = isTyping,
                    ConversationId = conversationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending typing indicator");
            }
        }

        #region Helper Methods

        /// <summary>
        /// Generate a consistent conversation ID for two users and an advertisement
        /// </summary>
        private static string GetConversationId(string userId1, string userId2, int advertisementId)
        {
            // Create a consistent ID regardless of user order
            var sortedUserIds = new[] { userId1, userId2 }.OrderBy(id => id);
            return $"conversation_{advertisementId}_{string.Join("_", sortedUserIds)}";
        }

        /// <summary>
        /// Send recent messages for a conversation
        /// </summary>
        private async Task SendRecentMessages(string conversationId, int advertisementId, string currentUserId, string otherUserId)
        {
            try
            {
                var recentMessages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.AdvertisementId == advertisementId &&
                               ((m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                                (m.SenderId == otherUserId && m.ReceiverId == currentUserId)) &&
                               !m.IsDeleted)
                    .OrderByDescending(m => m.SentAt)
                    .Take(50)
                    .Select(m => new
                    {
                        Id = m.Id,
                        Content = m.Content,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.FirstName + " " + m.Sender.LastName,
                        ReceiverId = m.ReceiverId,
                        AdvertisementId = m.AdvertisementId,
                        MessageType = m.MessageType,
                        SentAt = m.SentAt,
                        SentAtDisplay = (DateTime.UtcNow - m.SentAt).TotalDays >= 1 ? m.SentAt.ToString("MMM dd") :
                                        (DateTime.UtcNow - m.SentAt).TotalHours >= 1 ? m.SentAt.ToString("h:mm tt") : "Just now",
                        IsRead = m.IsRead,
                        ConversationId = conversationId
                    })
                    .ToListAsync();

                // Send messages in chronological order (oldest first)
                var orderedMessages = recentMessages.OrderBy(m => m.SentAt).ToList();

                await Clients.Caller.SendAsync("LoadRecentMessages", orderedMessages, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent messages for conversation {ConversationId}", conversationId);
            }
        }

        #endregion
    }
}