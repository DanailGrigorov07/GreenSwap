using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Web.Models.Account
{
    /// <summary>
    /// View model for user profile
    /// </summary>
    public class ProfileViewModel
    {
        /// <summary>
        /// User's email address (read-only)
        /// </summary>
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's first name
        /// </summary>
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, ErrorMessage = "First name cannot be longer than 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name
        /// </summary>
        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, ErrorMessage = "Last name cannot be longer than 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User's location
        /// </summary>
        [StringLength(200, ErrorMessage = "Location cannot be longer than 200 characters.")]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        /// <summary>
        /// User's bio or description
        /// </summary>
        [StringLength(1000, ErrorMessage = "Bio cannot be longer than 1000 characters.")]
        [Display(Name = "Bio")]
        [DataType(DataType.MultilineText)]
        public string? Bio { get; set; }

        /// <summary>
        /// User's seller rating (read-only)
        /// </summary>
        [Display(Name = "Seller Rating")]
        public decimal SellerRating { get; set; }

        /// <summary>
        /// Number of ratings received (read-only)
        /// </summary>
        [Display(Name = "Number of Reviews")]
        public int RatingCount { get; set; }

        /// <summary>
        /// Account creation date (read-only)
        /// </summary>
        [Display(Name = "Member Since")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets formatted rating display
        /// </summary>
        public string FormattedRating => RatingCount > 0 ? $"{SellerRating:F1} ({RatingCount} reviews)" : "No reviews yet";

        /// <summary>
        /// Gets formatted member since display
        /// </summary>
        public string FormattedMemberSince => CreatedAt.ToString("MMMM yyyy");
    }
}