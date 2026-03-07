using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandGoods.Data;
using SecondHandGoods.Web.Models.Ads;

namespace SecondHandGoods.Web.ViewComponents
{
    public class FeaturedAdsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public FeaturedAdsViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var featuredAds = await _context.Advertisements
                .Include(a => a.Category)
                .Include(a => a.User)
                .Include(a => a.Images)
                .Where(a => a.IsActive && !a.IsDeleted && !a.IsSold && a.IsFeatured && a.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(a => a.CreatedAt)
                .Take(12)
                .Select(a => new AdvertisementCardViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Price = a.Price,
                    IsPriceNegotiable = a.IsPriceNegotiable,
                    Location = a.Location,
                    CategoryName = a.Category.Name,
                    MainImageUrl = a.Images
                        .Where(img => img.IsMainImage)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault() ?? "/images/no-image.jpg",
                    MainImageAlt = a.Images
                        .Where(img => img.IsMainImage)
                        .Select(img => img.AltText)
                        .FirstOrDefault() ?? a.Title
                })
                .ToListAsync();

            return View(featuredAds);
        }
    }
}
