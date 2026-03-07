using SecondHandGoods.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Web.Models.Chat
{
    /// <summary>
    /// View model for displaying a conversation between two users
    /// </summary>
    public class ConversationViewModel
    {
        public int AdvertisementId { get; set; }
        public string AdvertisementTitle { get; set; } = string.Empty;
        public string AdvertisementImageUrl { get; set; } = string.Empty;
        public decimal AdvertisementPrice { get; set; }
        public string AdvertisementLocation { get; set; } = string.Empty;
        public bool IsAdvertisementActive { get; set; }
        public bool IsAdvertisementSold { get; set; }

        public string OtherUserId { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public decimal OtherUserRating { get; set; }
        public DateTime OtherUserJoinedDate { get; set; }
        public bool IsOtherUserOnline { get; set; }

        public string CurrentUserId { get; set; } = string.Empty;
        public string CurrentUserName { get; set; } = string.Empty;

        public List<MessageViewModel> Messages { get; set; } = new();
        public int UnreadCount { get; set; }
        public DateTime? LastMessageTime { get; set; }

        /// <summary>
        /// Gets the conversation ID for SignalR
        /// </summary>
        public string ConversationId => GetConversationId(CurrentUserId, OtherUserId, AdvertisementId);

        /// <summary>
        /// Formatted advertisement price
        /// </summary>
        public string FormattedPrice => $"${AdvertisementPrice:F2}";

        /// <summary>
        /// Time ago display for last message
        /// </summary>
        public string LastMessageTimeAgo
        {
            get
            {
                if (!LastMessageTime.HasValue) return "No messages yet";

                var timeSpan = DateTime.UtcNow - LastMessageTime.Value;
                return timeSpan.TotalDays >= 1
                    ? $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago"
                    : timeSpan.TotalHours >= 1
                        ? $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours == 1 ? "" : "s")} ago"
                        : timeSpan.TotalMinutes >= 1
                            ? $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes == 1 ? "" : "s")} ago"
                            : "Just now";
            }
        }

        /// <summary>
        /// Generate a consistent conversation ID
        /// </summary>
        private static string GetConversationId(string userId1, string userId2, int advertisementId)
        {
            var sortedUserIds = new[] { userId1, userId2 }.OrderBy(id => id);
            return $"conversation_{advertisementId}_{string.Join("_", sortedUserIds)}";
        }
    }

    /// <summary>
    /// View model for individual messages
    /// </summary>
    public class MessageViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public MessageType MessageType { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// Whether this message was sent by the current user
        /// </summary>
        public bool IsFromCurrentUser { get; set; }

        /// <summary>
        /// Formatted send time
        /// </summary>
        public string FormattedSentAt => SentAt.ToString("MMM dd, yyyy 'at' h:mm tt");

        /// <summary>
        /// Short time format for recent messages
        /// </summary>
        public string ShortTimeFormat
        {
            get
            {
                var now = DateTime.UtcNow;
                var diff = now - SentAt;

                if (diff.TotalDays >= 1)
                    return SentAt.ToString("MMM dd");
                else if (diff.TotalHours >= 1)
                    return SentAt.ToString("h:mm tt");
                else
                    return "Just now";
            }
        }

        /// <summary>
        /// CSS classes for message styling
        /// </summary>
        public string MessageClasses => IsFromCurrentUser 
            ? "message-sent bg-primary text-white" 
            : "message-received bg-light";

        /// <summary>
        /// Message type display
        /// </summary>
        public string TypeDisplay => MessageType switch
        {
            MessageType.Text => "",
            MessageType.System => "System",
            MessageType.PriceOffer => "Price Offer",
            MessageType.MeetingRequest => "Meeting Request",
            _ => MessageType.ToString()
        };

        /// <summary>
        /// Badge class for message type
        /// </summary>
        public string TypeBadgeClass => MessageType switch
        {
            MessageType.PriceOffer => "badge bg-success",
            MessageType.MeetingRequest => "badge bg-info",
            MessageType.System => "badge bg-secondary",
            _ => ""
        };
    }

    /// <summary>
    /// View model for sending new messages
    /// </summary>
    public class SendMessageViewModel
    {
        [Required]
        public int AdvertisementId { get; set; }

        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        [Required]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 1000 characters")]
        public string Content { get; set; } = string.Empty;

        public MessageType MessageType { get; set; } = MessageType.Text;

        // Price offer specific fields
        [Range(0.01, 999999.99, ErrorMessage = "Offer price must be between $0.01 and $999,999.99")]
        public decimal? OfferPrice { get; set; }

        // Meeting request specific fields
        public DateTime? ProposedMeetingTime { get; set; }
        
        [StringLength(500)]
        public string? MeetingLocation { get; set; }
    }

    /// <summary>
    /// View model for conversation list/inbox
    /// </summary>
    public class ConversationListViewModel
    {
        public List<ConversationSummaryViewModel> Conversations { get; set; } = new();
        public int TotalUnreadCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalConversations { get; set; }

        /// <summary>
        /// Pagination helper properties
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalConversations / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }

    /// <summary>
    /// View model for conversation summary in the list
    /// </summary>
    public class ConversationSummaryViewModel
    {
        public int AdvertisementId { get; set; }
        public string AdvertisementTitle { get; set; } = string.Empty;
        public string AdvertisementImageUrl { get; set; } = string.Empty;
        public decimal AdvertisementPrice { get; set; }
        public bool IsAdvertisementActive { get; set; }
        public bool IsAdvertisementSold { get; set; }

        public string OtherUserId { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public decimal OtherUserRating { get; set; }
        public bool IsOtherUserOnline { get; set; }

        public string LastMessageContent { get; set; } = string.Empty;
        public string LastMessageSenderId { get; set; } = string.Empty;
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }

        /// <summary>
        /// Formatted price
        /// </summary>
        public string FormattedPrice => $"${AdvertisementPrice:F2}";

        /// <summary>
        /// Whether the last message was from the current user
        /// </summary>
        public bool LastMessageFromCurrentUser { get; set; }

        /// <summary>
        /// Truncated last message for display
        /// </summary>
        public string TruncatedLastMessage
        {
            get
            {
                if (string.IsNullOrEmpty(LastMessageContent)) return "Start a conversation...";
                
                const int maxLength = 60;
                return LastMessageContent.Length > maxLength 
                    ? LastMessageContent.Substring(0, maxLength) + "..." 
                    : LastMessageContent;
            }
        }

        /// <summary>
        /// Time ago for last message
        /// </summary>
        public string LastMessageTimeAgo
        {
            get
            {
                if (!LastMessageTime.HasValue) return "";

                var timeSpan = DateTime.UtcNow - LastMessageTime.Value;
                return timeSpan.TotalDays >= 1
                    ? $"{(int)timeSpan.TotalDays}d"
                    : timeSpan.TotalHours >= 1
                        ? $"{(int)timeSpan.TotalHours}h"
                        : timeSpan.TotalMinutes >= 1
                            ? $"{(int)timeSpan.TotalMinutes}m"
                            : "now";
            }
        }

        /// <summary>
        /// CSS classes for conversation item
        /// </summary>
        public string ConversationClasses => UnreadCount > 0 
            ? "conversation-item conversation-unread" 
            : "conversation-item";

        /// <summary>
        /// Badge for advertisement status
        /// </summary>
        public string StatusBadgeClass
        {
            get
            {
                if (IsAdvertisementSold) return "badge bg-success";
                if (!IsAdvertisementActive) return "badge bg-secondary";
                return "";
            }
        }

        /// <summary>
        /// Badge text for advertisement status
        /// </summary>
        public string StatusBadgeText
        {
            get
            {
                if (IsAdvertisementSold) return "SOLD";
                if (!IsAdvertisementActive) return "INACTIVE";
                return "";
            }
        }
    }

    /// <summary>
    /// View model for quick message templates
    /// </summary>
    public class MessageTemplateViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public MessageType MessageType { get; set; }
        public string IconClass { get; set; } = string.Empty;

        /// <summary>
        /// Gets predefined message templates
        /// </summary>
        public static List<MessageTemplateViewModel> GetDefaultTemplates()
        {
            return new List<MessageTemplateViewModel>
            {
                new MessageTemplateViewModel
                {
                    Title = "Interested",
                    Content = "Hi! I'm interested in your item. Is it still available?",
                    MessageType = MessageType.Text,
                    IconClass = "fas fa-heart"
                },
                new MessageTemplateViewModel
                {
                    Title = "Ask Question",
                    Content = "Hi! I have a question about your item. Could you tell me more about its condition?",
                    MessageType = MessageType.Text,
                    IconClass = "fas fa-question-circle"
                },
                new MessageTemplateViewModel
                {
                    Title = "Make Offer",
                    Content = "Hi! I'd like to make an offer on your item.",
                    MessageType = MessageType.PriceOffer,
                    IconClass = "fas fa-hand-holding-usd"
                },
                new MessageTemplateViewModel
                {
                    Title = "Arrange Meeting",
                    Content = "Hi! I'd like to arrange a time to see the item. When would be convenient for you?",
                    MessageType = MessageType.MeetingRequest,
                    IconClass = "fas fa-calendar-alt"
                }
            };
        }
    }
}