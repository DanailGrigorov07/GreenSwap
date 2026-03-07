using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandGoods.Data.Entities
{
    /// <summary>
    /// Review left by a user about another user after a transaction
    /// </summary>
    public class Review
    {
        /// <summary>
        /// Unique review identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Rating given (1-5 stars)
        /// </summary>
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; }

        /// <summary>
        /// Written review comment
        /// </summary>
        [MaxLength(1000)]
        public string? Comment { get; set; }

        /// <summary>
        /// Whether the review is visible to other users
        /// </summary>
        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// Whether the review has been reported for inappropriate content
        /// </summary>
        public bool IsReported { get; set; } = false;

        /// <summary>
        /// Whether the review has been approved by moderation
        /// </summary>
        public bool IsApproved { get; set; } = true;

        /// <summary>
        /// Type of review (seller review or buyer review)
        /// </summary>
        public ReviewType ReviewType { get; set; }

        /// <summary>
        /// Date when the review was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when the review was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Date when the review was approved (if applicable)
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        // Foreign Keys
        /// <summary>
        /// ID of the user who wrote this review
        /// </summary>
        [Required]
        public string ReviewerId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the user being reviewed
        /// </summary>
        [Required]
        public string ReviewedUserId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the order this review is related to
        /// </summary>
        [Required]
        public int OrderId { get; set; }

        // Navigation Properties
        /// <summary>
        /// The user who wrote this review
        /// </summary>
        public virtual ApplicationUser Reviewer { get; set; } = null!;

        /// <summary>
        /// The user being reviewed
        /// </summary>
        public virtual ApplicationUser ReviewedUser { get; set; } = null!;

        /// <summary>
        /// The order this review is related to
        /// </summary>
        public virtual Order Order { get; set; } = null!;

        /// <summary>
        /// Whether this review should be displayed publicly
        /// </summary>
        public bool IsDisplayable => IsPublic && IsApproved && !IsReported;

        /// <summary>
        /// Gets a formatted rating display (e.g., "4.5 stars")
        /// </summary>
        public string FormattedRating => $"{Rating} star{(Rating != 1 ? "s" : "")}";

        /// <summary>
        /// Gets star icons for display (★★★★☆)
        /// </summary>
        public string StarDisplay
        {
            get
            {
                var filled = new string('★', Rating);
                var empty = new string('☆', 5 - Rating);
                return filled + empty;
            }
        }

        /// <summary>
        /// Updates the UpdatedAt timestamp
        /// </summary>
        public void UpdateTimestamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Approves the review for public display
        /// </summary>
        public void Approve()
        {
            IsApproved = true;
            ApprovedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        /// <summary>
        /// Reports the review as inappropriate
        /// </summary>
        public void Report()
        {
            IsReported = true;
            UpdateTimestamp();
        }

        /// <summary>
        /// Hides the review from public display
        /// </summary>
        public void Hide()
        {
            IsPublic = false;
            UpdateTimestamp();
        }
    }

    /// <summary>
    /// Type of review based on the reviewer's role in the transaction
    /// </summary>
    public enum ReviewType
    {
        /// <summary>
        /// Review left by a buyer about a seller
        /// </summary>
        BuyerToSeller = 1,

        /// <summary>
        /// Review left by a seller about a buyer
        /// </summary>
        SellerToBuyer = 2
    }
}