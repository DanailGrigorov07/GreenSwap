using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Data.Entities
{
    /// <summary>
    /// Message in a conversation between buyer and seller
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Unique message identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Message content
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Whether the message has been read by the recipient
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Whether the message has been deleted by the sender
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Date when the message was sent
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when the message was read (if applicable)
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// Date when the message was deleted (if applicable)
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Type of message (text, system notification, etc.)
        /// </summary>
        public MessageType MessageType { get; set; } = MessageType.Text;

        // Foreign Keys
        /// <summary>
        /// ID of the user who sent the message
        /// </summary>
        [Required]
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the user who should receive the message
        /// </summary>
        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the advertisement this message is about
        /// </summary>
        [Required]
        public int AdvertisementId { get; set; }

        // Navigation Properties
        /// <summary>
        /// The user who sent this message
        /// </summary>
        public virtual ApplicationUser Sender { get; set; } = null!;

        /// <summary>
        /// The user who should receive this message
        /// </summary>
        public virtual ApplicationUser Receiver { get; set; } = null!;

        /// <summary>
        /// The advertisement this message is related to
        /// </summary>
        public virtual Advertisement Advertisement { get; set; } = null!;

        /// <summary>
        /// Marks the message as read
        /// </summary>
        public void MarkAsRead()
        {
            if (!IsRead)
            {
                IsRead = true;
                ReadAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Soft deletes the message
        /// </summary>
        public void Delete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the conversation partner ID for a given user
        /// </summary>
        /// <param name="currentUserId">Current user's ID</param>
        /// <returns>The other participant's ID</returns>
        public string GetConversationPartnerId(string currentUserId)
        {
            return currentUserId == SenderId ? ReceiverId : SenderId;
        }
    }

    /// <summary>
    /// Types of messages in the system
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Regular text message from user
        /// </summary>
        Text = 1,

        /// <summary>
        /// System-generated notification
        /// </summary>
        System = 2,

        /// <summary>
        /// Price offer message
        /// </summary>
        PriceOffer = 3,

        /// <summary>
        /// Meeting arrangement message
        /// </summary>
        MeetingRequest = 4
    }
}