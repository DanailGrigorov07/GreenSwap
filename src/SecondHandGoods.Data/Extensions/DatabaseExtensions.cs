using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecondHandGoods.Data.Entities;
using SecondHandGoods.Data.Seed;

namespace SecondHandGoods.Data.Extensions
{
    /// <summary>
    /// Extension methods for database initialization and seeding
    /// </summary>
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Ensures the database is created and migrated to the latest version
        /// </summary>
        /// <param name="host">The web application host</param>
        /// <returns>The host for chaining</returns>
        public static async Task<IHost> InitializeDatabaseAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                
                logger.LogInformation("Ensuring database is created and up to date...");
                
                // Ensure database is created
                await context.Database.EnsureCreatedAsync();
                
                // Apply any pending migrations
                if ((await context.Database.GetPendingMigrationsAsync()).Any())
                {
                    logger.LogInformation("Applying pending migrations...");
                    await context.Database.MigrateAsync();
                }
                
                logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }

            return host;
        }
        
        /// <summary>
        /// Seeds the database with initial data if it's empty
        /// </summary>
        /// <param name="host">The web application host</param>
        /// <returns>The host for chaining</returns>
        public static async Task<IHost> SeedDatabaseAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                
                // Seed Identity data (roles and admin user)
                await IdentityDataSeeder.SeedAsync(services, logger);
                
                // Seed categories
                await CategoryDataSeeder.SeedAsync(context, logger);
                
                // Seed forbidden words for content moderation
                await ForbiddenWordsSeeder.SeedAsync(context, logger);

                // Seed paid ad slots (footer-1, footer-2, footer-3) if none exist
                await SeedSiteAdSlotsAsync(context, logger);
                
                // Seed sample data (demo users and advertisements) - only in development
                var environment = services.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();
                if (environment.IsDevelopment())
                {
                    await SampleDataSeeder.SeedAsync(context, userManager, logger);
                    // Extend expiration for any expired ads so listings stay visible in development
                    await ExtendExpiredAdsForDevelopmentAsync(context, logger);
                }
                
                logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }

            return host;
        }

        /// <summary>
        /// Ensures the three footer ad slots exist. If no site ads exist, creates placeholder slots.
        /// </summary>
        private static async Task SeedSiteAdSlotsAsync(ApplicationDbContext context, ILogger logger)
        {
            await EnsureSiteAdvertisementsTableExistsAsync(context, logger);
            if (await context.SiteAdvertisements.AnyAsync())
                return;
            var slots = new[] { ("footer-1", 0), ("footer-2", 1), ("footer-3", 2) };
            foreach (var (slotKey, order) in slots)
            {
                context.SiteAdvertisements.Add(new SiteAdvertisement
                {
                    SlotKey = slotKey,
                    ImageUrl = "", // Empty = show "Ad space" placeholder until you set a real image URL
                    TargetUrl = null,
                    AltText = "Ad space",
                    DisplayOrder = order,
                    IsActive = true
                });
            }
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded 3 paid ad slots (footer-1, footer-2, footer-3).");
        }

        /// <summary>
        /// Creates the SiteAdvertisements table if it does not exist (e.g. when DB was created with EnsureCreated before this migration existed).
        /// </summary>
        private static async Task EnsureSiteAdvertisementsTableExistsAsync(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS SiteAdvertisements (
                        Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        SlotKey TEXT NOT NULL,
                        ImageUrl TEXT NOT NULL,
                        TargetUrl TEXT NULL,
                        AltText TEXT NULL,
                        DisplayOrder INTEGER NOT NULL,
                        IsActive INTEGER NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NULL
                    )");
                await context.Database.ExecuteSqlRawAsync(
                    "CREATE INDEX IF NOT EXISTS IX_SiteAdvertisements_SlotActive ON SiteAdvertisements (SlotKey, IsActive)");
                logger.LogInformation("SiteAdvertisements table ensured.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not ensure SiteAdvertisements table (may already exist or use different provider).");
            }
        }

        /// <summary>
        /// In development, extends ExpiresAt for ads that have already expired so listings stay visible.
        /// </summary>
        private static async Task ExtendExpiredAdsForDevelopmentAsync(ApplicationDbContext context, ILogger logger)
        {
            var now = DateTime.UtcNow;
            var expired = await context.Advertisements
                .Where(a => !a.IsDeleted && a.ExpiresAt < now)
                .ToListAsync();
            if (expired.Count == 0)
                return;
            foreach (var ad in expired)
            {
                ad.ExpiresAt = now.AddDays(30);
            }
            await context.SaveChangesAsync();
            logger.LogInformation("Extended expiration for {Count} expired advertisement(s) so they appear in listings.", expired.Count);
        }
    }
}