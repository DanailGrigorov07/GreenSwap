using System.ComponentModel.DataAnnotations;

namespace SecondHandGoods.Web.Models.Account
{
    /// <summary>
    /// View model for user login
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// User's email address
        /// </summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's password
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Whether to remember the user's login
        /// </summary>
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}