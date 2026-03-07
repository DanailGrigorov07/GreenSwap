using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandGoods.Data;

namespace SecondHandGoods.Web.ViewComponents
{
    /// <summary>
    /// Loads and displays paid/site advertisements for the footer slots (footer-1, footer-2, footer-3).
    /// </summary>
    public class PaidAdsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public PaidAdsViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var slots = new[] { "footer-1", "footer-2", "footer-3" };
            var ads = await _context.SiteAdvertisements
                .Where(a => a.IsActive && slots.Contains(a.SlotKey))
                .OrderBy(a => a.DisplayOrder)
                .ThenBy(a => a.SlotKey)
                .Select(a => new PaidAdViewModel
                {
                    SlotKey = a.SlotKey,
                    ImageUrl = a.ImageUrl,
                    TargetUrl = a.TargetUrl,
                    AltText = a.AltText ?? "Advertisement"
                })
                .ToListAsync();

            // Ensure we have an entry per slot (placeholder if no ad)
            var result = slots.Select(slot =>
                ads.FirstOrDefault(a => a.SlotKey == slot) ?? new PaidAdViewModel
                {
                    SlotKey = slot,
                    ImageUrl = null,
                    TargetUrl = null,
                    AltText = "Ad space"
                }
            ).ToList();

            return View(result);
        }
    }

    public class PaidAdViewModel
    {
        public string SlotKey { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? TargetUrl { get; set; }
        public string AltText { get; set; } = "Advertisement";
    }
}
