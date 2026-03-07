using System.ComponentModel.DataAnnotations;
using SecondHandGoods.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SecondHandGoods.Web.Models.Ads
{
    /// <summary>
    /// View model for creating a new advertisement
    /// </summary>
    public class CreateAdvertisementViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between $0.01 and $999,999.99")]
        [Display(Name = "Price ($)")]
        public decimal Price { get; set; }

        [Display(Name = "Price is negotiable")]
        public bool IsPriceNegotiable { get; set; }

        [Required(ErrorMessage = "Condition is required")]
        [Display(Name = "Item Condition")]
        public ItemCondition Condition { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        [Display(Name = "Location (optional)")]
        public string? Location { get; set; }

        [Display(Name = "Images (up to 20)")]
        public List<IFormFile>? Images { get; set; }

        // Navigation properties for dropdowns
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Conditions { get; set; } = new();

        /// <summary>
        /// Populates condition dropdown options
        /// </summary>
        public void PopulateConditions()
        {
            Conditions = Enum.GetValues<ItemCondition>()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = GetConditionDisplayName(c)
                })
                .ToList();
        }

        /// <summary>
        /// Gets user-friendly display name for condition
        /// </summary>
        private static string GetConditionDisplayName(ItemCondition condition)
        {
            return condition switch
            {
                ItemCondition.New => "New",
                ItemCondition.Used => "Used - Good Condition",
                ItemCondition.Damaged => "Damaged/For Parts",
                ItemCondition.Refurbished => "Refurbished/Restored",
                _ => condition.ToString()
            };
        }
    }
}