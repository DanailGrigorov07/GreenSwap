namespace SecondHandGoods.Data.Entities
{
    /// <summary>
    /// EF-translatable filters for <see cref="Review"/> queries (mirrors <see cref="Review.IsDisplayable"/>).
    /// </summary>
    public static class ReviewQueryableExtensions
    {
        public static IQueryable<Review> WhereDisplayable(this IQueryable<Review> reviews) =>
            reviews.Where(r => r.IsPublic && r.IsApproved && !r.IsReported && r.Rating >= 1 && r.Rating <= 5);
    }
}
