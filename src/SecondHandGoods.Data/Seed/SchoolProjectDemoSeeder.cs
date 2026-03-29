using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecondHandGoods.Data.Entities;

namespace SecondHandGoods.Data.Seed
{
    /// <summary>
    /// Seeds a single completed order so the admin dashboard shows realistic totals for demos (e.g. school projects).
    /// Idempotent: only runs if the marker order does not exist.
    /// </summary>
    public static class SchoolProjectDemoSeeder
    {
        public const string DemoOrderNumber = "ORD-SCHOOL-DEMO";
        public const decimal DemoFinalPrice = 67.00m;

        public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Orders.AnyAsync(o => o.OrderNumber == DemoOrderNumber))
            {
                logger.LogDebug("School project demo order already present; skipping.");
                return;
            }

            var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@greenswap.com");
            if (admin == null)
            {
                logger.LogWarning("School project demo order skipped: admin@greenswap.com not found.");
                return;
            }

            var otherUser = await context.Users
                .Where(u => u.Id != admin.Id)
                .OrderBy(u => u.Email)
                .FirstOrDefaultAsync();

            if (otherUser == null)
            {
                logger.LogWarning(
                    "School project demo order skipped: need at least one non-admin user (run sample data seed or register a user).");
                return;
            }

            var sellerId = otherUser.Id;
            var buyerId = admin.Id;

            var ad = await context.Advertisements
                .FirstOrDefaultAsync(a => a.UserId == sellerId && !a.IsDeleted && !a.IsSold);

            if (ad == null)
            {
                var category = await context.Categories.FirstOrDefaultAsync();
                if (category == null)
                {
                    logger.LogWarning("School project demo order skipped: no categories.");
                    return;
                }

                ad = new Advertisement
                {
                    Title = "School project demo listing",
                    Description = "Seeded item for dashboard demo. Safe to delete after grading.",
                    Price = DemoFinalPrice,
                    Condition = ItemCondition.Used,
                    Location = otherUser.Location ?? "Campus",
                    IsPriceNegotiable = false,
                    ViewCount = 3,
                    IsActive = true,
                    IsDeleted = false,
                    IsSold = false,
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-14),
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    UserId = sellerId,
                    CategoryId = category.Id
                };
                context.Advertisements.Add(ad);
                await context.SaveChangesAsync();
            }

            var completedAt = DateTime.UtcNow.AddDays(-3);
            var order = new Order
            {
                OrderNumber = DemoOrderNumber,
                FinalPrice = DemoFinalPrice,
                Status = OrderStatus.Completed,
                BuyerId = buyerId,
                SellerId = sellerId,
                AdvertisementId = ad.Id,
                CreatedAt = completedAt.AddDays(-2),
                CompletedAt = completedAt,
                UpdatedAt = completedAt,
                Notes = "Seeded completed order for admin dashboard / school project demo."
            };

            ad.IsSold = true;
            ad.SoldAt = completedAt;
            ad.IsActive = false;
            ad.UpdatedAt = completedAt;

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            logger.LogInformation(
                "Seeded school project demo order {OrderNumber} (${Price}) for dashboard totals.",
                DemoOrderNumber, DemoFinalPrice);
        }
    }
}
