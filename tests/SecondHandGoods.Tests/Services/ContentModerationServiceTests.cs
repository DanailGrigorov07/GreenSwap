using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SecondHandGoods.Data;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Services;
using Xunit;

namespace SecondHandGoods.Tests.Services
{
    public class ContentModerationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ContentModerationService>> _loggerMock;
        private readonly ContentModerationService _service;

        public ContentModerationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<ContentModerationService>>();
            _service = new ContentModerationService(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task ModerateContentAsync_WithNoForbiddenWords_ShouldPass()
        {
            // Arrange
            var content = "This is a clean message with no issues.";
            var authorId = "user123";

            // Act
            var result = await _service.ModerateContentAsync(
                content, 
                ModeratedEntityType.Message, 
                1, 
                authorId);

            // Assert
            Assert.True(result.Passed);
            Assert.False(result.WasModified);
            Assert.Equal(content, result.ModifiedContent);
            Assert.Empty(result.DetectedWords);
        }

        [Fact]
        public async Task ModerateContentAsync_WithBlockedWord_ShouldBlock()
        {
            // Arrange
            var forbiddenWord = new ForbiddenWord
            {
                Word = "spam",
                NormalizedWord = "spam",
                Severity = ModerationSeverity.High,
                IsBlocked = true,
                IsActive = true,
                IsExactMatch = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.ForbiddenWords.Add(forbiddenWord);
            await _context.SaveChangesAsync();

            var content = "This is a spam message";
            var authorId = "user123";

            // Act
            var result = await _service.ModerateContentAsync(
                content,
                ModeratedEntityType.Message,
                1,
                authorId);

            // Assert
            Assert.False(result.Passed);
            Assert.Contains("spam", result.DetectedWords);
            Assert.Equal(ModerationSeverity.High, result.MaxSeverity);
        }

        [Fact]
        public async Task ModerateContentAsync_WithReplacement_ShouldModify()
        {
            // Arrange
            var forbiddenWord = new ForbiddenWord
            {
                Word = "spam",
                NormalizedWord = "spam",
                Severity = ModerationSeverity.Medium,
                IsBlocked = true,
                IsActive = true,
                IsExactMatch = true,
                Replacement = "[promotional]",
                CreatedAt = DateTime.UtcNow
            };
            _context.ForbiddenWords.Add(forbiddenWord);
            await _context.SaveChangesAsync();

            var content = "This is a spam message";
            var authorId = "user123";

            // Act
            var result = await _service.ModerateContentAsync(
                content,
                ModeratedEntityType.Message,
                1,
                authorId);

            // Assert
            Assert.False(result.Passed);
            Assert.True(result.WasModified);
            Assert.Contains("[promotional]", result.ModifiedContent);
        }

        [Fact]
        public async Task ModerateContentAsync_WithFlaggedWord_ShouldFlag()
        {
            // Arrange
            var forbiddenWord = new ForbiddenWord
            {
                Word = "fake",
                NormalizedWord = "fake",
                Severity = ModerationSeverity.Medium,
                IsBlocked = false, // Just flag, don't block
                IsActive = true,
                IsExactMatch = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.ForbiddenWords.Add(forbiddenWord);
            await _context.SaveChangesAsync();

            var content = "This is a fake product";
            var authorId = "user123";

            // Act
            var result = await _service.ModerateContentAsync(
                content,
                ModeratedEntityType.Advertisement,
                1,
                authorId);

            // Assert
            Assert.True(result.Passed); // Content passes but is flagged
            Assert.True(result.RequiresReview);
            Assert.Contains("fake", result.DetectedWords);
        }

        [Fact]
        public async Task AddForbiddenWordAsync_ShouldAddAndNormalize()
        {
            // Arrange
            var word = new ForbiddenWord
            {
                Word = "  TestWord  ",
                Severity = ModerationSeverity.Medium,
                IsActive = true,
                IsBlocked = true
            };
            var adminId = "admin123";

            // Act
            var result = await _service.AddForbiddenWordAsync(word, adminId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("testword", result.NormalizedWord);
            Assert.Equal(adminId, result.CreatedByUserId);
            Assert.NotEqual(default(DateTime), result.CreatedAt);
        }

        [Fact]
        public async Task GetForbiddenWordsAsync_ShouldReturnOnlyActive()
        {
            // Arrange
            var activeWord = new ForbiddenWord
            {
                Word = "active",
                NormalizedWord = "active",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var inactiveWord = new ForbiddenWord
            {
                Word = "inactive",
                NormalizedWord = "inactive",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.ForbiddenWords.AddRange(activeWord, inactiveWord);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetForbiddenWordsAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("active", result.First().Word);
        }

        [Fact]
        public async Task DeleteForbiddenWordAsync_ShouldSoftDelete()
        {
            // Arrange
            var word = new ForbiddenWord
            {
                Word = "todelete",
                NormalizedWord = "todelete",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.ForbiddenWords.Add(word);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteForbiddenWordAsync(word.Id);

            // Assert
            Assert.True(result);
            var deleted = await _context.ForbiddenWords.FindAsync(word.Id);
            Assert.False(deleted!.IsActive);
        }

        [Fact]
        public async Task ModerateContentAsync_WithEmptyContent_ShouldPass()
        {
            // Arrange
            var content = "";
            var authorId = "user123";

            // Act
            var result = await _service.ModerateContentAsync(
                content,
                ModeratedEntityType.Message,
                1,
                authorId);

            // Assert
            Assert.True(result.Passed);
            Assert.Equal("", result.ModifiedContent);
        }

        [Fact]
        public async Task ModerateContentAsync_WithPartialMatch_ShouldDetect()
        {
            // Arrange
            var forbiddenWord = new ForbiddenWord
            {
                Word = "bad",
                NormalizedWord = "bad",
                Severity = ModerationSeverity.Medium,
                IsBlocked = true,
                IsActive = true,
                IsExactMatch = false, // Partial match
                CreatedAt = DateTime.UtcNow
            };
            _context.ForbiddenWords.Add(forbiddenWord);
            await _context.SaveChangesAsync();

            var content = "This is a badly written message";
            var authorId = "user123";

            // Act
            var result = await _service.ModerateContentAsync(
                content,
                ModeratedEntityType.Message,
                1,
                authorId);

            // Assert
            Assert.False(result.Passed);
            Assert.Contains("bad", result.DetectedWords);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
