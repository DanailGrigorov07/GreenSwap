using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SecondHandGoods.Data;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Services;
using SecondHandGoods.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SecondHandGoods.Tests.Controllers
{
    public class AdminControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly Mock<IContentModerationService> _moderationServiceMock;
        private readonly Mock<ILogger<AdminController>> _loggerMock;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                roleStore.Object, null, null, null, null);

            _moderationServiceMock = new Mock<IContentModerationService>();
            _loggerMock = new Mock<ILogger<AdminController>>();

            _controller = new AdminController(
                _context,
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _moderationServiceMock.Object,
                _loggerMock.Object);

            // Setup admin user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "admin1"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task Index_ShouldReturnDashboardView()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1", UserName = "test", Email = "test@test.com", FirstName = "Test", LastName = "User", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }

        [Fact]
        public async Task Users_ShouldReturnUserManagementView()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1", UserName = "test", Email = "test@test.com", FirstName = "Test", LastName = "User", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Users(new Web.Models.Admin.AdminUserManagementViewModel());

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }

        [Fact]
        public async Task Advertisements_ShouldReturnAdManagementView()
        {
            // Arrange
            var category = new Category { Id = 1, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow };
            var user = new ApplicationUser { Id = "user1", UserName = "test", Email = "test@test.com", FirstName = "Test", LastName = "User", CreatedAt = DateTime.UtcNow };
            _context.Categories.Add(category);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Advertisements(new Web.Models.Admin.AdminAdManagementViewModel());

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }

        [Fact]
        public async Task Statistics_ShouldReturnStatisticsView()
        {
            // Act
            var result = await _controller.Statistics();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }

        [Fact]
        public async Task Moderation_ShouldReturnModerationDashboard()
        {
            // Act
            var result = await _controller.Moderation();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }

        [Fact]
        public async Task ForbiddenWords_ShouldReturnForbiddenWordsView()
        {
            // Arrange
            var word = new ForbiddenWord
            {
                Word = "test",
                NormalizedWord = "test",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.ForbiddenWords.Add(word);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ForbiddenWords(new Web.Models.Admin.ForbiddenWordsManagementViewModel());

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
