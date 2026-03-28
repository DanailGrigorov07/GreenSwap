using Microsoft.AspNetCore.Identity;

namespace SecondHandGoods.Data.Entities
{
    /// <summary>
    /// Extended user entity for the GreenSwap platform
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// User's first name
        /// </summary>
        public string FirstName { get; set; } = string.Empty;
        
        /// <summary>
        /// User's last name
        /// </summary>
        public string LastName { get; set; } = string.Empty;
        
        /// <summary>
        /// User's full display name
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();
        
        /// <summary>
        /// Date when the user account was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date when the user profile was last updated
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }
        
        /// <summary>
        /// User's profile picture URL or file path
        /// </summary>
        public string? ProfilePictureUrl { get; set; }
        
        /// <summary>
        /// User's city or location
        /// </summary>
        public string? Location { get; set; }
        
        /// <summary>
        /// User's bio or description
        /// </summary>
        public string? Bio { get; set; }
        
        /// <summary>
        /// Whether the user account is active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// User's overall rating as a seller (0-5 stars)
        /// </summary>
        public decimal SellerRating { get; set; } = 0;
        
        /// <summary>
        /// Number of ratings received as a seller
        /// </summary>
        public int RatingCount { get; set; } = 0;
        
        // Navigation properties
        
        /// <summary>
        /// Advertisements created by this user
        /// </summary>
        public virtual ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
        
        /// <summary>
        /// Messages sent by this user
        /// </summary>
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        
        /// <summary>
        /// Messages received by this user
        /// </summary>
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        
        /// <summary>
        /// Orders where this user is the buyer
        /// </summary>
        public virtual ICollection<Order> PurchaseOrders { get; set; } = new List<Order>();
        
        /// <summary>
        /// Orders where this user is the seller
        /// </summary>
        public virtual ICollection<Order> SalesOrders { get; set; } = new List<Order>();
        
        /// <summary>
        /// Reviews written by this user
        /// </summary>
        public virtual ICollection<Review> ReviewsGiven { get; set; } = new List<Review>();
        
        /// <summary>
        /// Reviews received by this user
        /// </summary>
        public virtual ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
        
        /// <summary>
        /// Advertisements favorited by this user
        /// </summary>
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        
        /// <summary>
        /// Updates the LastUpdatedAt timestamp
        /// </summary>
        public void UpdateTimestamp()
        {
            LastUpdatedAt = DateTime.UtcNow;
        }
    }
}