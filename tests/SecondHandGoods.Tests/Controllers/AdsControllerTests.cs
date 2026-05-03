using Microsoft.AspNetCore.Hosting;
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
using SecondHandGoods.Web.Models.Ads;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace SecondHandGoods.Tests.Controllers
{
    public class AdsControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IWebHostEnvironment> _environmentMock;
        private readonly Mock<IContentModerationService> _moderationServiceMock;
        private readonly Mock<ILogger<AdsController>> _loggerMock;
        private readonly AdsController _controller;

        public AdsControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            _environmentMock = new Mock<IWebHostEnvironment>();
            _environmentMock.Setup(e => e.WebRootPath).Returns("/wwwroot");

            _moderationServiceMock = new Mock<IContentModerationService>();
            _loggerMock = new Mock<ILogger<AdsController>>();

            _controller = new AdsController(
                _context,
                _userManagerMock.Object,
                _environmentMock.Object,
                _moderationServiceMock.Object,
                _loggerMock.Object);

            // Setup temp data
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task Index_ShouldReturnViewWithAdvertisements()
        {
            // Arrange
            var category = new Category { Id = 1, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow };
            var user = new ApplicationUser { Id = "user1", UserName = "testuser", Email = "test@test.com", FirstName = "Test", LastName = "User", CreatedAt = DateTime.UtcNow };
            
            _context.Categories.Add(category);
            _context.Users.Add(user);
            
            var ad = new Advertisement
            {
                Id = 1,
                Title = "Test Ad",
                Description = "Test Description",
                Price = 100,
                CategoryId = 1,
                UserId = "user1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };
            _context.Advertisements.Add(ad);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index(new AdvertisementListViewModel());

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }

        [Fact]
        public async Task Details_WithValidId_ShouldReturnView()
        {
            // Arrange
            var category = new Category { Id = 1, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow };
            var user = new ApplicationUser { Id = "user1", UserName = "testuser", Email = "test@test.com", FirstName = "Test", LastName = "User", CreatedAt = DateTime.UtcNow };
            
            _context.Categories.Add(category);
            _context.Users.Add(user);
            
            var ad = new Advertisement
            {
                Id = 1,
                Title = "Test Ad",
                Description = "Test Description",
                Price = 100,
                CategoryId = 1,
                UserId = "user1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };
            _context.Advertisements.Add(ad);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AdvertisementDetailsViewModel>(viewResult.Model);
            Assert.Equal("Test Ad", model.Title);
        }

        [Fact]
        public async Task Details_WithInvalidId_ShouldReturnNotFound()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Get_ShouldReturnView()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1", UserName = "testuser", Email = "test@test.com", FirstName = "Test", LastName = "User", CreatedAt = DateTime.UtcNow };
            var category = new Category { Id = 1, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow };
            
            _context.Users.Add(user);
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user1") };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CreateAdvertisementViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task Create_Post_WithValidModel_ShouldCreateAdvertisement()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1", UserName = "testuser", Email = "test@test.com", FirstName = "Test", LastName = "User", CreatedAt = DateTime.UtcNow };
            var category = new Category { Id = 1, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow };
            
            _context.Users.Add(user);
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user1") };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _moderationServiceMock.Setup(m => m.ModerateContentAsync(
                It.IsAny<string>(),
                It.IsAny<ModeratedEntityType>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(new ContentModerationResult { Passed = true, ModifiedContent = "Test Title" });

            var model = new CreateAdvertisementViewModel
            {
                Title = "Test Ad",
                Description = "Test Description",
                Price = 100,
                CategoryId = 1,
                Condition = ItemCondition.Used
            };

            // Act
            var result = await _controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Single(await _context.Advertisements.ToListAsync());
        }

        [Fact]
        public async Task ToggleFavorite_AsBuyer_AddsThenRemovesFavorite()
        {
            var seller = new ApplicationUser { Id = "seller", UserName = "seller", Email = "s@test.com", FirstName = "S", LastName = "Eller", CreatedAt = DateTime.UtcNow };
            var buyer = new ApplicationUser { Id = "buyer", UserName = "buyer", Email = "b@test.com", FirstName = "B", LastName = "Uyer", CreatedAt = DateTime.UtcNow };
            var category = new Category { Id = 1, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow };
            _context.Users.AddRange(seller, buyer);
            _context.Categories.Add(category);
            _context.Advertisements.Add(new Advertisement
            {
                Id = 1,
                Title = "Item",
                Description = "Desc",
                Price = 50,
                CategoryId = 1,
                UserId = "seller",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });
            await _context.SaveChangesAsync();

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "buyer") };
            var identity = new ClaimsIdentity(claims, "Test");
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(buyer);

            var added = Assert.IsType<JsonResult>(await _controller.ToggleFavorite(1));
            using (var parsed = JsonDocument.Parse(JsonSerializer.Serialize(added.Value)))
            {
                Assert.True(parsed.RootElement.GetProperty("success").GetBoolean());
                Assert.True(parsed.RootElement.GetProperty("isFavorited").GetBoolean());
            }

            Assert.Single(await _context.Favorites.ToListAsync());

            var removed = Assert.IsType<JsonResult>(await _controller.ToggleFavorite(1));
            using (var parsed = JsonDocument.Parse(JsonSerializer.Serialize(removed.Value)))
            {
                Assert.True(parsed.RootElement.GetProperty("success").GetBoolean());
                Assert.False(parsed.RootElement.GetProperty("isFavorited").GetBoolean());
            }

            Assert.Empty(await _context.Favorites.ToListAsync());
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
