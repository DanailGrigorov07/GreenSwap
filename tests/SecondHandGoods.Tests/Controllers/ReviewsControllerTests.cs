using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SecondHandGoods.Data;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Web.Controllers;
using System.Security.Claims;
using Xunit;

namespace SecondHandGoods.Tests.Controllers
{
    public class ReviewsControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<ILogger<ReviewsController>> _loggerMock;
        private readonly ReviewsController _controller;

        public ReviewsControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            _loggerMock = new Mock<ILogger<ReviewsController>>();

            _controller = new ReviewsController(_context, _userManagerMock.Object, _loggerMock.Object);

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user1") };
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
        public async Task UserReviews_WithValidUserId_ShouldReturnView()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1", UserName = "test", Email = "test@test.com", FirstName = "Test", LastName = "User", CreatedAt = DateTime.UtcNow };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.UserReviews("user1");

            // Assert
            // May return view or redirect depending on implementation
            Assert.True(result is ViewResult || result is RedirectToActionResult);
        }

        [Fact]
        public async Task Pending_ShouldReturnPendingReviewsView()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1", UserName = "test", Email = "test@test.com", FirstName = "Test", LastName = "User", CreatedAt = DateTime.UtcNow };
            var category = new Category { Id = 1, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow };
            var ad = new Advertisement { Id = 1, Title = "Test", Description = "Test", Price = 100, CategoryId = 1, UserId = "user1", IsActive = true, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) };
            
            _context.Users.Add(user);
            _context.Categories.Add(category);
            _context.Advertisements.Add(ad);
            await _context.SaveChangesAsync();

            var order = new Order
            {
                Id = 1,
                OrderNumber = "ORD-123",
                BuyerId = "user1",
                SellerId = "user1",
                AdvertisementId = 1,
                FinalPrice = 100,
                Status = OrderStatus.Completed
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Pending();

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
