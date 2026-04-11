using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

                // Seed paid ad slots (footer-1, footer-2, footer-3) if none exist; backfill example on footer-1 if still empty
                await SeedSiteAdSlotsAsync(context, logger);
                await BackfillFooter1ExampleIfEmptyAsync(context, logger);
                
                // Sample/demo data: Development, or SQLite when SeedDemoData is true (default true for local .db demos)
                var environment = services.GetRequiredService<IHostEnvironment>();
                var configuration = services.GetRequiredService<IConfiguration>();
                var isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
                var seedDemoFlag = configuration["Database:SeedDemoData"];
                var seedDemoData = environment.IsDevelopment()
                    || (isSqlite && (string.IsNullOrEmpty(seedDemoFlag) || (bool.TryParse(seedDemoFlag, out var sd) && sd)));
                if (seedDemoData)
                {
                    await SampleDataSeeder.SeedAsync(context, userManager, logger);
                    await ExtendExpiredAdsForDevelopmentAsync(context, logger);
                    await SchoolProjectDemoSeeder.SeedAsync(context, logger);
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

        /// <summary>Default demo content for <c>footer-1</c> so fresh clones show a real ad, not only placeholders.</summary>
        private static class SiteAdSeedDefaults
        {
            public const string Footer1ExampleImageUrl =
                "https://www.gstatic.com/youtube/img/branding/favicon/favicon_144x144.png";
            public const string Footer1ExampleTargetUrl = "https://www.youtube.com";
            public const string Footer1ExampleAltText =
                "YouTube — example footer ad (replace under Admin → Site Ads)";
        }

        /// <summary>
        /// Ensures the three footer ad slots exist. If no site ads exist, creates slots with an example on <c>footer-1</c>.
        /// </summary>
        private static async Task SeedSiteAdSlotsAsync(ApplicationDbContext context, ILogger logger)
        {
            await EnsureSiteAdvertisementsTableExistsAsync(context, logger);
            if (await context.SiteAdvertisements.AnyAsync())
                return;
            var slots = new[] { ("footer-1", 0), ("footer-2", 1), ("footer-3", 2) };
            var now = DateTime.UtcNow;
            foreach (var (slotKey, order) in slots)
            {
                var isFooter1 = slotKey == "footer-1";
                context.SiteAdvertisements.Add(new SiteAdvertisement
                {
                    SlotKey = slotKey,
                    ImageUrl = isFooter1
                        ? SiteAdSeedDefaults.Footer1ExampleImageUrl
                        : string.Empty,
                    TargetUrl = isFooter1 ? SiteAdSeedDefaults.Footer1ExampleTargetUrl : null,
                    AltText = isFooter1
                        ? SiteAdSeedDefaults.Footer1ExampleAltText
                        : "Ad space",
                    DisplayOrder = order,
                    IsActive = true,
                    CreatedAt = now
                });
            }
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded 3 paid ad slots (footer-1 includes example YouTube link; footer-2/3 placeholders).");
        }

        /// <summary>
        /// Older DBs may have empty <c>footer-1</c> from prior seed — fill example so git pulls match.
        /// Skips if <see cref="SiteAdvertisement.ImageUrl"/> is already set.
        /// </summary>
        private static async Task BackfillFooter1ExampleIfEmptyAsync(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                var ad = await context.SiteAdvertisements.FirstOrDefaultAsync(s => s.SlotKey == "footer-1");
                if (ad == null || !string.IsNullOrWhiteSpace(ad.ImageUrl))
                    return;

                ad.ImageUrl = SiteAdSeedDefaults.Footer1ExampleImageUrl;
                ad.TargetUrl = SiteAdSeedDefaults.Footer1ExampleTargetUrl;
                ad.AltText = SiteAdSeedDefaults.Footer1ExampleAltText;
                ad.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                logger.LogInformation("Backfilled default example content for paid slot footer-1.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not backfill footer-1 example site ad.");
            }
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