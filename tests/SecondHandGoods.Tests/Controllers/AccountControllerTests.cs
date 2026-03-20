using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Web.Controllers;
using SecondHandGoods.Web.Models.Account;
using Xunit;

namespace SecondHandGoods.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<ILogger<AccountController>> _loggerMock;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                null, null, null, null);

            _loggerMock = new Mock<ILogger<AccountController>>();
            _controller = new AccountController(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _loggerMock.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task Login_WhenLockedOut_ShouldReturnViewWithLockoutMessage()
        {
            var user = new ApplicationUser { Id = "u1", Email = "user@test.com", UserName = "user@test.com" };
            _userManagerMock.Setup(m => m.FindByEmailAsync("user@test.com"))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(m => m.PasswordSignInAsync(
                    user, "Password123", false, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            var model = new LoginViewModel
            {
                Email = "user@test.com",
                Password = "Password123",
                RememberMe = false
            };

            var result = await _controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Your account is temporarily locked due to multiple failed login attempts. Please try again later, or contact an administrator.",
                _controller.ViewData["LockoutMessage"]?.ToString());
            Assert.NotEmpty(_controller.ModelState[string.Empty].Errors);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task Login_WhenSucceeded_ShouldRedirectToHomeWhenReturnUrlMissing()
        {
            var user = new ApplicationUser { Id = "u1", Email = "user@test.com", UserName = "user@test.com" };
            _userManagerMock.Setup(m => m.FindByEmailAsync("user@test.com"))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(m => m.PasswordSignInAsync(
                    user, "Password123", false, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var model = new LoginViewModel
            {
                Email = "user@test.com",
                Password = "Password123",
                RememberMe = false
            };

            var result = await _controller.Login(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }
    }
}
