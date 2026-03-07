using Microsoft.Extensions.Logging;
using SecondHandGoods.Data.Entities;

namespace SecondHandGoods.Data.Seed
{
    /// <summary>
    /// Seeds initial product categories
    /// </summary>
    public static class CategoryDataSeeder
    {
        /// <summary>
        /// Seeds categories if they don't exist
        /// </summary>
        public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                if (context.Categories.Any())
                {
                    logger.LogDebug("Categories already exist, skipping category seeding.");
                    return;
                }

                logger.LogInformation("Seeding categories...");

                var categories = GetInitialCategories();

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();

                logger.LogInformation("Successfully seeded {Count} categories.", categories.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding categories.");
                throw;
            }
        }

        /// <summary>
        /// Gets the initial set of categories to seed
        /// </summary>
        private static List<Category> GetInitialCategories()
        {
            var now = DateTime.UtcNow;

            return new List<Category>
            {
                new Category
                {
                    Name = "Electronics",
                    Description = "Computers, phones, tablets, gaming consoles, and electronic accessories",
                    Slug = "electronics",
                    IconClass = "fas fa-laptop",
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Vehicles",
                    Description = "Cars, motorcycles, bicycles, and automotive parts",
                    Slug = "vehicles",
                    IconClass = "fas fa-car",
                    DisplayOrder = 2,
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Home & Garden",
                    Description = "Furniture, appliances, home decor, and gardening equipment",
                    Slug = "home-garden",
                    IconClass = "fas fa-home",
                    DisplayOrder = 3,
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Fashion & Beauty",
                    Description = "Clothing, shoes, jewelry, cosmetics, and accessories",
                    Slug = "fashion-beauty",
                    IconClass = "fas fa-tshirt",
                    DisplayOrder = 4,
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Sports & Recreation",
                    Description = "Sports equipment, outdoor gear, fitness items, and recreational activities",
                    Slug = "sports-recreation",
                    IconClass = "fas fa-football-ball",
                    DisplayOrder = 5,
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Books & Media",
                    Description = "Books, magazines, CDs, DVDs, and educational materials",
                    Slug = "books-media",
                    IconClass = "fas fa-book",
                    DisplayOrder = 6,
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Toys & Baby Items",
                    Description = "Children's toys, baby equipment, strollers, and educational games",
                    Slug = "toys-baby",
                    IconClass = "fas fa-baby",
                    DisplayOrder = 7,
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Tools & Equipment",
                    Description = "Hand tools, power tools, construction equipment, and workshop items",
                    Slug = "tools-equipment",
                    IconClass = "fas fa-hammer",
                    DisplayOrder = 8,
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Musical Instruments",
                    Description = "Guitars, keyboards, drums, and other musical equipment",
                    Slug = "musical-instruments",
                    IconClass = "fas fa-guitar",
                    DisplayOrder = 9,
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Pet Supplies",
                    Description = "Pet food, toys, cages, aquariums, and pet accessories",
                    Slug = "pet-supplies",
                    IconClass = "fas fa-paw",
                    DisplayOrder = 10,
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Art & Collectibles",
                    Description = "Artwork, antiques, collectible items, and handmade crafts",
                    Slug = "art-collectibles",
                    IconClass = "fas fa-palette",
                    DisplayOrder = 11,
                    IsActive = true,
                    CreatedAt = now
                },
                new Category
                {
                    Name = "Office & Business",
                    Description = "Office furniture, business equipment, and professional supplies",
                    Slug = "office-business",
                    IconClass = "fas fa-briefcase",
                    DisplayOrder = 12,
                    IsActive = true,
                    CreatedAt = now
                }
            };
        }
    }
}