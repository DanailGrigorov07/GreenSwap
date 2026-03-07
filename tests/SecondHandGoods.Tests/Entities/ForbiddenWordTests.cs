using SecondHandGoods.Data.Entities;
using Xunit;

namespace SecondHandGoods.Tests.Entities
{
    public class ForbiddenWordTests
    {
        [Fact]
        public void NormalizeWord_ShouldConvertToLowercase()
        {
            // Arrange
            var word = new ForbiddenWord
            {
                Word = "  SPAM  ",
                Severity = ModerationSeverity.High
            };

            // Act
            word.NormalizeWord();

            // Assert
            Assert.Equal("spam", word.NormalizedWord);
        }

        [Fact]
        public void UpdateTimestamp_ShouldSetUpdatedAtAndUserId()
        {
            // Arrange
            var word = new ForbiddenWord
            {
                Word = "test",
                NormalizedWord = "test",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            var adminId = "admin123";

            // Act
            word.UpdateTimestamp(adminId);

            // Assert
            Assert.NotNull(word.UpdatedAt);
            Assert.Equal(adminId, word.UpdatedByUserId);
            Assert.True(word.UpdatedAt > word.CreatedAt);
        }
    }
}
