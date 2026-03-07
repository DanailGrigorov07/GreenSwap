using SecondHandGoods.Data.Entities;
using Xunit;

namespace SecondHandGoods.Tests.Entities
{
    public class ReviewTests
    {
        [Fact]
        public void Report_ShouldSetIsReportedToTrue()
        {
            // Arrange
            var review = new Review
            {
                Id = 1,
                Rating = 5,
                Comment = "Great!",
                ReviewerId = "reviewer1",
                ReviewedUserId = "reviewed1",
                OrderId = 1,
                IsReported = false
            };

            // Act
            review.Report();

            // Assert
            Assert.True(review.IsReported);
        }

        [Fact]
        public void Hide_ShouldSetIsPublicToFalse()
        {
            // Arrange
            var review = new Review
            {
                Id = 1,
                Rating = 5,
                Comment = "Great!",
                ReviewerId = "reviewer1",
                ReviewedUserId = "reviewed1",
                OrderId = 1,
                IsPublic = true
            };

            // Act
            review.Hide();

            // Assert
            Assert.False(review.IsPublic);
        }

        [Fact]
        public void IsDisplayable_WhenPublicAndApproved_ShouldReturnTrue()
        {
            // Arrange
            var review = new Review
            {
                Id = 1,
                Rating = 5,
                Comment = "Great!",
                ReviewerId = "reviewer1",
                ReviewedUserId = "reviewed1",
                OrderId = 1,
                IsPublic = true,
                IsApproved = true,
                IsReported = false
            };

            // Act
            var result = review.IsDisplayable;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDisplayable_WhenNotPublic_ShouldReturnFalse()
        {
            // Arrange
            var review = new Review
            {
                Id = 1,
                Rating = 5,
                Comment = "Great!",
                ReviewerId = "reviewer1",
                ReviewedUserId = "reviewed1",
                OrderId = 1,
                IsPublic = false,
                IsApproved = true,
                IsReported = false
            };

            // Act
            var result = review.IsDisplayable;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void FormattedRating_ShouldReturnCorrectFormat()
        {
            // Arrange
            var review = new Review
            {
                Id = 1,
                Rating = 4,
                Comment = "Great!",
                ReviewerId = "reviewer1",
                ReviewedUserId = "reviewed1",
                OrderId = 1
            };

            // Act
            var result = review.FormattedRating;

            // Assert
            Assert.Equal("4 stars", result);
        }

        [Fact]
        public void StarDisplay_ShouldReturnCorrectStars()
        {
            // Arrange
            var review = new Review
            {
                Id = 1,
                Rating = 4,
                Comment = "Great!",
                ReviewerId = "reviewer1",
                ReviewedUserId = "reviewed1",
                OrderId = 1
            };

            // Act
            var result = review.StarDisplay;

            // Assert
            Assert.Equal("★★★★☆", result);
        }
    }
}
