using SecondHandGoods.Data.Entities;
using Xunit;

namespace SecondHandGoods.Tests.Entities
{
    public class AdvertisementTests
    {
        [Fact]
        public void MarkAsSold_ShouldSetIsSoldAndSoldAt()
        {
            // Arrange
            var ad = new Advertisement
            {
                Id = 1,
                Title = "Test",
                Description = "Test",
                Price = 100,
                IsSold = false,
                SoldAt = null
            };

            // Act
            ad.MarkAsSold();

            // Assert
            Assert.True(ad.IsSold);
            Assert.NotNull(ad.SoldAt);
        }

        [Fact]
        public void IsPublic_WhenActiveAndNotDeleted_ShouldReturnTrue()
        {
            // Arrange
            var ad = new Advertisement
            {
                Id = 1,
                Title = "Test",
                Description = "Test",
                Price = 100,
                IsActive = true,
                IsDeleted = false,
                IsSold = false,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            // Act
            var result = ad.IsPublic;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IncrementViewCount_ShouldIncreaseViewCount()
        {
            // Arrange
            var ad = new Advertisement
            {
                Id = 1,
                Title = "Test",
                Description = "Test",
                Price = 100,
                ViewCount = 5
            };

            // Act
            ad.IncrementViewCount();

            // Assert
            Assert.Equal(6, ad.ViewCount);
        }

        [Fact]
        public void IsPublic_WhenExpired_ShouldReturnFalse()
        {
            // Arrange
            var ad = new Advertisement
            {
                Id = 1,
                Title = "Test",
                Description = "Test",
                Price = 100,
                IsActive = true,
                IsDeleted = false,
                IsSold = false,
                ExpiresAt = DateTime.UtcNow.AddDays(-1)
            };

            // Act
            var result = ad.IsPublic;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPublic_WhenSold_ShouldReturnFalse()
        {
            // Arrange
            var ad = new Advertisement
            {
                Id = 1,
                Title = "Test",
                Description = "Test",
                Price = 100,
                IsActive = true,
                IsDeleted = false,
                IsSold = true,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            // Act
            var result = ad.IsPublic;

            // Assert
            Assert.False(result);
        }
    }
}
