using SecondHandGoods.Data.Entities;
using Xunit;

namespace SecondHandGoods.Tests.Entities
{
    public class CategoryTests
    {
        [Fact]
        public void Category_ShouldHaveRequiredProperties()
        {
            // Arrange & Act
            var category = new Category
            {
                Id = 1,
                Name = "Electronics",
                Slug = "electronics",
                Description = "Electronic items",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Assert
            Assert.Equal("Electronics", category.Name);
            Assert.Equal("electronics", category.Slug);
            Assert.True(category.IsActive);
        }
    }
}
