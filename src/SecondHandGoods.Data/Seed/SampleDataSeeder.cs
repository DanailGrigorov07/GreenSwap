using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecondHandGoods.Data.Constants;
using SecondHandGoods.Data.Entities;

namespace SecondHandGoods.Data.Seed
{
    /// <summary>
    /// Seeds sample data for development and demonstration purposes
    /// </summary>
    public static class SampleDataSeeder
    {
        /// <summary>
        /// Seeds sample users and advertisements
        /// </summary>
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger logger)
        {
            try
            {
                // Check if sample data already exists
                if (await context.Users.AnyAsync(u => u.Email!.Contains("demo")))
                {
                    logger.LogDebug("Sample data already exists, skipping sample data seeding.");
                    return;
                }

                logger.LogInformation("Seeding sample data...");

                // Create demo users
                var demoUsers = await CreateDemoUsersAsync(userManager, logger);
                
                // Create sample advertisements
                await CreateSampleAdvertisementsAsync(context, demoUsers, logger);

                await context.SaveChangesAsync();
                logger.LogInformation("Sample data seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding sample data.");
                throw;
            }
        }

        /// <summary>
        /// Creates demo users for the application
        /// </summary>
        private static async Task<List<ApplicationUser>> CreateDemoUsersAsync(UserManager<ApplicationUser> userManager, ILogger logger)
        {
            var demoUsers = new List<ApplicationUser>();
            
            var usersData = new[]
            {
                new { Email = "john.demo@example.com", FirstName = "John", LastName = "Smith", Location = "New York, NY", Bio = "Tech enthusiast selling quality electronics and gadgets. All items tested and in working condition." },
                new { Email = "sarah.demo@example.com", FirstName = "Sarah", LastName = "Johnson", Location = "Los Angeles, CA", Bio = "Fashion lover with a curated collection of designer items. Excellent condition guaranteed!" },
                new { Email = "mike.demo@example.com", FirstName = "Mike", LastName = "Wilson", Location = "Chicago, IL", Bio = "Car enthusiast and collector. Selling well-maintained vehicles and automotive parts." },
                new { Email = "emily.demo@example.com", FirstName = "Emily", LastName = "Davis", Location = "Miami, FL", Bio = "Interior designer selling beautiful furniture and home decor items. Smoke-free home." },
                new { Email = "alex.demo@example.com", FirstName = "Alex", LastName = "Brown", Location = "Seattle, WA", Bio = "Outdoor adventure lover. Quality sports equipment and camping gear for sale." }
            };

            foreach (var userData in usersData)
            {
                var user = new ApplicationUser
                {
                    UserName = userData.Email,
                    Email = userData.Email,
                    EmailConfirmed = true,
                    FirstName = userData.FirstName,
                    LastName = userData.LastName,
                    Location = userData.Location,
                    Bio = userData.Bio,
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 365)), // Random creation date within the last year
                    IsActive = true,
                    SellerRating = Math.Round((decimal)(Random.Shared.NextDouble() * 2 + 3), 1), // Rating between 3.0 and 5.0
                    RatingCount = Random.Shared.Next(5, 50)
                };

                var result = await userManager.CreateAsync(user, "Demo123!");
                
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, ApplicationRoles.User);
                    demoUsers.Add(user);
                    logger.LogDebug("Created demo user: {Email}", userData.Email);
                }
                else
                {
                    logger.LogWarning("Failed to create demo user {Email}. Errors: {Errors}", 
                        userData.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            return demoUsers;
        }

        /// <summary>
        /// Creates sample advertisements for the demo users
        /// </summary>
        private static async Task CreateSampleAdvertisementsAsync(ApplicationDbContext context, List<ApplicationUser> demoUsers, ILogger logger)
        {
            if (!demoUsers.Any())
            {
                logger.LogWarning("No demo users available for creating sample advertisements.");
                return;
            }

            var categories = await context.Categories.ToListAsync();
            if (!categories.Any())
            {
                logger.LogWarning("No categories available for creating sample advertisements.");
                return;
            }

            var sampleAds = GetSampleAdvertisements(demoUsers, categories);
            await context.Advertisements.AddRangeAsync(sampleAds);
            
            logger.LogInformation("Created {Count} sample advertisements.", sampleAds.Count);
        }

        /// <summary>
        /// Gets sample advertisement data
        /// </summary>
        private static List<Advertisement> GetSampleAdvertisements(List<ApplicationUser> users, List<Category> categories)
        {
            var ads = new List<Advertisement>();
            var random = Random.Shared;

            var sampleItems = new[]
            {
                // Electronics
                new { Title = "iPhone 13 Pro - Excellent Condition", Description = "128GB iPhone 13 Pro in Space Gray. No scratches, always kept in a case. Original box and charger included. Battery health 95%. Perfect for anyone looking for a premium smartphone at a great price.", Price = 699.99m, CategorySlug = "electronics", Condition = ItemCondition.Used },
                new { Title = "Gaming Laptop - ASUS ROG Strix", Description = "High-performance gaming laptop with RTX 3060, 16GB RAM, 512GB SSD. Perfect for gaming and professional work. Runs all modern games smoothly. Minor wear on keyboard but fully functional.", Price = 1299.99m, CategorySlug = "electronics", Condition = ItemCondition.Used },
                new { Title = "iPad Air 4th Gen with Apple Pencil", Description = "64GB iPad Air in excellent condition with Apple Pencil (2nd gen). Great for digital art, note-taking, and productivity. Screen protector applied since day one. All accessories included.", Price = 549.99m, CategorySlug = "electronics", Condition = ItemCondition.Used },
                
                // Vehicles  
                new { Title = "2018 Honda Civic - Low Mileage", Description = "Well-maintained Honda Civic with only 35,000 miles. Regular oil changes, non-smoker car, garage kept. Excellent fuel economy and reliability. Perfect first car or commuter vehicle.", Price = 18500.00m, CategorySlug = "vehicles", Condition = ItemCondition.Used },
                new { Title = "Mountain Bike - Trek X-Caliber 8", Description = "Excellent mountain bike perfect for trails and recreational riding. 29er wheels, front suspension, 21-speed. Well maintained, some minor scuffs from normal use. Includes bike lock.", Price = 799.99m, CategorySlug = "vehicles", Condition = ItemCondition.Used },
                
                // Home & Garden
                new { Title = "Sectional Sofa - Modern Design", Description = "Beautiful gray sectional sofa in excellent condition. Very comfortable, pet-free and smoke-free home. Minor wear on cushions but overall great shape. Must sell due to moving.", Price = 899.99m, CategorySlug = "home-garden", Condition = ItemCondition.Used },
                new { Title = "KitchenAid Stand Mixer - Classic", Description = "Red KitchenAid Artisan stand mixer, barely used. Includes dough hook, wire whip, and flat beater. Perfect for baking enthusiasts. Purchased last year but moving to smaller apartment.", Price = 279.99m, CategorySlug = "home-garden", Condition = ItemCondition.Used },
                
                // Fashion & Beauty
                new { Title = "Designer Handbag - Coach Leather", Description = "Authentic Coach leather handbag in excellent condition. Beautiful brown leather with minimal signs of wear. No pets, no smoking household. Comes with dust bag and authenticity card.", Price = 199.99m, CategorySlug = "fashion-beauty", Condition = ItemCondition.Used },
                new { Title = "Men's Winter Coat - North Face", Description = "Large size North Face winter coat in black. Very warm and waterproof. Perfect for cold weather. Some minor pilling but otherwise in great shape. Retail price was $350.", Price = 149.99m, CategorySlug = "fashion-beauty", Condition = ItemCondition.Used },
                
                // Sports & Recreation
                new { Title = "Tennis Racket Set - Wilson Pro Staff", Description = "Professional tennis racket set with 2 Wilson Pro Staff rackets and carrying case. Great for intermediate to advanced players. Strings recently replaced. Minor grip wear.", Price = 189.99m, CategorySlug = "sports-recreation", Condition = ItemCondition.Used },
                new { Title = "Camping Tent - 4 Person Coleman", Description = "Spacious 4-person camping tent in excellent condition. Used only 3 times. Waterproof, easy setup, includes all stakes and guy lines. Perfect for family camping trips.", Price = 129.99m, CategorySlug = "sports-recreation", Condition = ItemCondition.Used },
                
                // Books & Media
                new { Title = "Computer Science Textbook Collection", Description = "Collection of 12 computer science textbooks covering algorithms, data structures, and programming. Great condition, minimal highlighting. Perfect for CS students or professionals.", Price = 199.99m, CategorySlug = "books-media", Condition = ItemCondition.Used },
                
                // Tools & Equipment
                new { Title = "Dewalt Drill Set with Case", Description = "Professional Dewalt cordless drill set with 2 batteries, charger, and various drill bits. Excellent for home improvement projects. All tools in working condition.", Price = 179.99m, CategorySlug = "tools-equipment", Condition = ItemCondition.Used },
                
                // Musical Instruments
                new { Title = "Acoustic Guitar - Yamaha FG830", Description = "Beautiful acoustic guitar in excellent condition. Rich, warm tone perfect for beginners and intermediate players. Includes soft case and extra strings. Well maintained, no damage.", Price = 299.99m, CategorySlug = "musical-instruments", Condition = ItemCondition.Used }
            };

            foreach (var item in sampleItems)
            {
                var category = categories.FirstOrDefault(c => c.Slug == item.CategorySlug);
                var user = users[random.Next(users.Count)];
                
                if (category != null)
                {
                    var ad = new Advertisement
                    {
                        Title = item.Title,
                        Description = item.Description,
                        Price = item.Price,
                        Condition = item.Condition,
                        Location = user.Location,
                        IsPriceNegotiable = random.Next(0, 2) == 1, // 50% chance
                        ViewCount = random.Next(1, 50),
                        IsActive = true,
                        IsDeleted = false,
                        IsSold = false,
                        IsFeatured = random.Next(0, 5) == 0, // 20% chance of being featured
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30)), // Random date within last 30 days
                        ExpiresAt = DateTime.UtcNow.AddDays(random.Next(15, 45)), // Expires 15-45 days from now
                        UserId = user.Id,
                        CategoryId = category.Id
                    };

                    ads.Add(ad);
                }
            }

            return ads;
        }
    }
}