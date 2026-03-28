using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Web.Models.Account;

namespace SecondHandGoods.Web.Controllers
{
    /// <summary>
    /// Handles user authentication and account management
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// Display login page
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        /// <summary>
        /// Process login request
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find user by email first (login form uses email field; Identity's PasswordSignInAsync(userName, ...) looks up by UserName).
            // The seeded admin has UserName = "admin" and Email = "admin@greenswap.com", so we must resolve by email and sign in with the user object.
            var user = await _userManager.FindByEmailAsync(model.Email);
            Microsoft.AspNetCore.Identity.SignInResult result;
            if (user != null)
            {
                result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
            }
            else
            {
                // Fallback: treat input as username (e.g. "admin") for backward compatibility
                result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
            }

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in.", model.Email);
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account {Email} is locked out.", model.Email);
                ViewData["LockoutMessage"] = "Your account is temporarily locked due to multiple failed login attempts. Please try again later, or contact an administrator.";
                ModelState.AddModelError("", "Account locked due to multiple failed login attempts.");
                return View(model);
            }

            _logger.LogWarning("Invalid login attempt for {Email}.", model.Email);
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        /// <summary>
        /// Display registration page
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel());
        }

        /// <summary>
        /// Process registration request
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("register")]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Validate Terms and Conditions agreement
            if (!model.AgreeToTerms)
            {
                ModelState.AddModelError(nameof(model.AgreeToTerms), "You must agree to the Terms and Conditions to create an account.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Location = model.Location,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                // Add user to the default "User" role
                await _userManager.AddToRoleAsync(user, "User");

                // Sign in the user
                await _signInManager.SignInAsync(user, isPersistent: false);
                
                _logger.LogInformation("User {Email} registered and signed in.", model.Email);
                
                return RedirectToLocal(returnUrl);
            }

            // Add errors to model state
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        /// <summary>
        /// Process logout request
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Display access denied page
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Display user profile
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Location = user.Location,
                Bio = user.Bio,
                SellerRating = user.SellerRating,
                RatingCount = user.RatingCount,
                CreatedAt = user.CreatedAt
            };

            return View(model);
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Location = model.Location;
            user.Bio = model.Bio;
            user.UpdateTimestamp();

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Your profile has been updated successfully.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        /// <summary>
        /// Only redirect to local URLs to prevent open redirect attacks (returnUrl must be app-relative or same-origin).
        /// </summary>
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
    }
}