using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandGoods.Data.Entities
{
    /// <summary>
    /// Order representing a purchase transaction
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Unique order identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Unique order number for display purposes
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Final agreed price for the item
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal FinalPrice { get; set; }

        /// <summary>
        /// Current status of the order
        /// </summary>
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        /// <summary>
        /// Additional notes or comments about the order
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Payment method used (if specified)
        /// </summary>
        [MaxLength(100)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Meeting location for item exchange
        /// </summary>
        [MaxLength(300)]
        public string? MeetingLocation { get; set; }

        /// <summary>
        /// Scheduled meeting date and time
        /// </summary>
        public DateTime? ScheduledMeetingAt { get; set; }

        /// <summary>
        /// Date when the order was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when the order was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Date when the order was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Date when the order was cancelled
        /// </summary>
        public DateTime? CancelledAt { get; set; }

        /// <summary>
        /// Reason for cancellation (if applicable)
        /// </summary>
        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        // Foreign Keys
        /// <summary>
        /// ID of the user who is buying the item
        /// </summary>
        [Required]
        public string BuyerId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the user who is selling the item
        /// </summary>
        [Required]
        public string SellerId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the advertisement being purchased
        /// </summary>
        [Required]
        public int AdvertisementId { get; set; }

        // Navigation Properties
        /// <summary>
        /// The user who is buying the item
        /// </summary>
        public virtual ApplicationUser Buyer { get; set; } = null!;

        /// <summary>
        /// The user who is selling the item
        /// </summary>
        public virtual ApplicationUser Seller { get; set; } = null!;

        /// <summary>
        /// The advertisement being purchased
        /// </summary>
        public virtual Advertisement Advertisement { get; set; } = null!;

        /// <summary>
        /// Whether the order is in a final state (completed or cancelled)
        /// </summary>
        public bool IsFinal => Status == OrderStatus.Completed || Status == OrderStatus.Cancelled;

        /// <summary>
        /// Updates the UpdatedAt timestamp
        /// </summary>
        public void UpdateTimestamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the order as completed
        /// </summary>
        public void Complete()
        {
            Status = OrderStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        /// <summary>
        /// Cancels the order with a reason
        /// </summary>
        /// <param name="reason">Reason for cancellation</param>
        public void Cancel(string? reason = null)
        {
            Status = OrderStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
            CancellationReason = reason;
            UpdateTimestamp();
        }

        /// <summary>
        /// Generates a new order number
        /// </summary>
        /// <returns>A unique order number</returns>
        public static string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }
    }

    /// <summary>
    /// Possible order statuses
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// Order has been created but not yet confirmed
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Both parties have confirmed the order
        /// </summary>
        Confirmed = 2,

        /// <summary>
        /// Meeting is scheduled between buyer and seller
        /// </summary>
        MeetingScheduled = 3,

        /// <summary>
        /// Item has been exchanged and payment made
        /// </summary>
        Completed = 4,

        /// <summary>
        /// Order has been cancelled by either party
        /// </summary>
        Cancelled = 5
    }
}