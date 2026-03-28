using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecondHandGoods.Data.Entities;

namespace SecondHandGoods.Data
{
    /// <summary>
    /// Application database context for GreenSwap platform
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets for domain entities
        public DbSet<Advertisement> Advertisements { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<AdvertisementImage> AdvertisementImages { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        
        // Content Moderation entities
        public DbSet<ForbiddenWord> ForbiddenWords { get; set; }
        public DbSet<ModerationLog> ModerationLogs { get; set; }

        // Site-paid advertisements (footer banners, etc.)
        public DbSet<SiteAdvertisement> SiteAdvertisements { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure Identity table names with custom prefix
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                
                // Configure ApplicationUser properties
                entity.Property(e => e.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();
                    
                entity.Property(e => e.LastName)
                    .HasMaxLength(100)
                    .IsRequired();
                    
                entity.Property(e => e.Location)
                    .HasMaxLength(200);
                    
                entity.Property(e => e.Bio)
                    .HasMaxLength(1000);
                    
                entity.Property(e => e.ProfilePictureUrl)
                    .HasMaxLength(500);
                    
                entity.Property(e => e.SellerRating)
                    .HasPrecision(3, 2); // Allows values like 4.75
                    
                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_Email");
                    
                entity.HasIndex(e => e.UserName)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_UserName");
            });
            
            // Configure entity relationships and constraints
            ConfigureEntityRelationships(modelBuilder);
            
            // Configure other Identity tables
            ConfigureIdentityTables(modelBuilder);
        }
        
        /// <summary>
        /// Configure entity relationships and database constraints
        /// </summary>
        private static void ConfigureEntityRelationships(ModelBuilder modelBuilder)
        {
            // Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("IX_Categories_Slug");
                entity.HasIndex(e => e.Name).HasDatabaseName("IX_Categories_Name");
            });

            // Advertisement configuration
            modelBuilder.Entity<Advertisement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.Price).HasPrecision(10, 2);
                
                // Foreign key relationships
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Advertisements)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Advertisements)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // Indexes for performance
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_Advertisements_UserId");
                entity.HasIndex(e => e.CategoryId).HasDatabaseName("IX_Advertisements_CategoryId");
                entity.HasIndex(e => new { e.IsActive, e.IsDeleted, e.IsSold }).HasDatabaseName("IX_Advertisements_Status");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Advertisements_CreatedAt");
            });

            // AdvertisementImage configuration
            modelBuilder.Entity<AdvertisementImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(500);
                
                entity.HasOne(e => e.Advertisement)
                    .WithMany(a => a.Images)
                    .HasForeignKey(e => e.AdvertisementId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasIndex(e => e.AdvertisementId).HasDatabaseName("IX_AdvertisementImages_AdvertisementId");
            });

            // Message configuration
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
                
                // Configure sender relationship
                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Configure receiver relationship
                entity.HasOne(e => e.Receiver)
                    .WithMany(u => u.ReceivedMessages)
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Advertisement)
                    .WithMany(a => a.Messages)
                    .HasForeignKey(e => e.AdvertisementId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Indexes for chat functionality
                entity.HasIndex(e => new { e.SenderId, e.ReceiverId, e.AdvertisementId }).HasDatabaseName("IX_Messages_Conversation");
                entity.HasIndex(e => e.SentAt).HasDatabaseName("IX_Messages_SentAt");
                entity.HasIndex(e => new { e.ReceiverId, e.IsRead }).HasDatabaseName("IX_Messages_Unread");
            });

            // Order configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FinalPrice).HasPrecision(10, 2);
                
                // Configure buyer relationship
                entity.HasOne(e => e.Buyer)
                    .WithMany(u => u.PurchaseOrders)
                    .HasForeignKey(e => e.BuyerId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Configure seller relationship
                entity.HasOne(e => e.Seller)
                    .WithMany(u => u.SalesOrders)
                    .HasForeignKey(e => e.SellerId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Advertisement)
                    .WithMany(a => a.Orders)
                    .HasForeignKey(e => e.AdvertisementId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // Unique order number
                entity.HasIndex(e => e.OrderNumber).IsUnique().HasDatabaseName("IX_Orders_OrderNumber");
                entity.HasIndex(e => e.BuyerId).HasDatabaseName("IX_Orders_BuyerId");
                entity.HasIndex(e => e.SellerId).HasDatabaseName("IX_Orders_SellerId");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_Orders_Status");
            });

            // Review configuration
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Rating).IsRequired();
                
                // Configure reviewer relationship
                entity.HasOne(e => e.Reviewer)
                    .WithMany(u => u.ReviewsGiven)
                    .HasForeignKey(e => e.ReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Configure reviewed user relationship
                entity.HasOne(e => e.ReviewedUser)
                    .WithMany(u => u.ReviewsReceived)
                    .HasForeignKey(e => e.ReviewedUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // One review per order per reviewer
                entity.HasIndex(e => new { e.OrderId, e.ReviewerId }).IsUnique().HasDatabaseName("IX_Reviews_OrderReviewer");
                entity.HasIndex(e => e.ReviewedUserId).HasDatabaseName("IX_Reviews_ReviewedUserId");
                entity.HasIndex(e => new { e.IsPublic, e.IsApproved }).HasDatabaseName("IX_Reviews_Visibility");
            });

            // Favorite configuration
            modelBuilder.Entity<Favorite>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Favorites)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Advertisement)
                    .WithMany(a => a.Favorites)
                    .HasForeignKey(e => e.AdvertisementId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // One favorite per user per advertisement
                entity.HasIndex(e => new { e.UserId, e.AdvertisementId }).IsUnique().HasDatabaseName("IX_Favorites_UserAdvertisement");
            });

            // ForbiddenWord configuration
            modelBuilder.Entity<ForbiddenWord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Word).IsRequired().HasMaxLength(100);
                entity.Property(e => e.NormalizedWord).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.Replacement).HasMaxLength(100);
                entity.Property(e => e.AdminNotes).HasMaxLength(500);
                entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
                entity.Property(e => e.UpdatedByUserId).HasMaxLength(450);

                // Configure relationships
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes for performance
                entity.HasIndex(e => e.NormalizedWord).HasDatabaseName("IX_ForbiddenWords_NormalizedWord");
                entity.HasIndex(e => new { e.IsActive, e.IsBlocked }).HasDatabaseName("IX_ForbiddenWords_Status");
                entity.HasIndex(e => e.Severity).HasDatabaseName("IX_ForbiddenWords_Severity");
                entity.HasIndex(e => e.Category).HasDatabaseName("IX_ForbiddenWords_Category");
            });

            // ModerationLog configuration
            modelBuilder.Entity<ModerationLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ContentAuthorId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.OriginalContent).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.ModeratedContent).HasMaxLength(2000);
                entity.Property(e => e.DetectedWords).HasMaxLength(500);
                entity.Property(e => e.ModeratorId).HasMaxLength(450);
                entity.Property(e => e.ModerationReason).HasMaxLength(1000);
                entity.Property(e => e.AdditionalInfo).HasMaxLength(1000);
                entity.Property(e => e.AppealDecision).HasMaxLength(1000);
                entity.Property(e => e.UserIpAddress).HasMaxLength(45);

                // Configure relationships
                entity.HasOne(e => e.ContentAuthor)
                    .WithMany()
                    .HasForeignKey(e => e.ContentAuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Moderator)
                    .WithMany()
                    .HasForeignKey(e => e.ModeratorId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ForbiddenWord)
                    .WithMany(fw => fw.ModerationLogs)
                    .HasForeignKey(e => e.ForbiddenWordId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes for performance and querying
                entity.HasIndex(e => new { e.EntityType, e.EntityId }).HasDatabaseName("IX_ModerationLogs_Entity");
                entity.HasIndex(e => e.ContentAuthorId).HasDatabaseName("IX_ModerationLogs_ContentAuthor");
                entity.HasIndex(e => new { e.Action, e.Result }).HasDatabaseName("IX_ModerationLogs_ActionResult");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_ModerationLogs_CreatedAt");
                entity.HasIndex(e => new { e.IsAutomatic, e.Severity }).HasDatabaseName("IX_ModerationLogs_AutomaticSeverity");
                entity.HasIndex(e => e.IsAppealed).HasDatabaseName("IX_ModerationLogs_Appeals");
            });

            // SiteAdvertisement configuration (paid ad slots)
            modelBuilder.Entity<SiteAdvertisement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SlotKey).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.TargetUrl).HasMaxLength(500);
                entity.Property(e => e.AltText).HasMaxLength(200);
                entity.HasIndex(e => new { e.SlotKey, e.IsActive }).HasDatabaseName("IX_SiteAdvertisements_SlotActive");
            });
        }
        
        /// <summary>
        /// Configure Identity table names and constraints
        /// </summary>
        private static void ConfigureIdentityTables(ModelBuilder modelBuilder)
        {
            // Customize Identity table names
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("Roles");
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
        }
    }
}
