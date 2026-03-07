using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Web.Models.Account
{
    /// <summary>
    /// View model for user registration
    /// </summary>
    public class RegisterViewModel
    {
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
        /// User's email address
        /// </summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's location (optional)
        /// </summary>
        [StringLength(200, ErrorMessage = "Location cannot be longer than 200 characters.")]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        /// <summary>
        /// User's password
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} and at most {1} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Password confirmation
        /// </summary>
        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// Whether user agrees to terms and conditions
        /// </summary>
        [Required(ErrorMessage = "You must agree to the terms and conditions.")]
        [Display(Name = "I agree to the Terms and Conditions")]
        public bool AgreeToTerms { get; set; }
    }
}