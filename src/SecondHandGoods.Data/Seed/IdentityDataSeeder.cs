using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecondHandGoods.Data.Constants;
using SecondHandGoods.Data.Entities;

namespace SecondHandGoods.Data.Seed
{
    /// <summary>
    /// Seeds initial Identity data (roles and admin user)
    /// </summary>
    public static class IdentityDataSeeder
    {
        /// <summary>
        /// Seeds roles and creates the initial admin user
        /// </summary>
        public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            try
            {
                // Seed roles
                await SeedRolesAsync(roleManager, logger);
                
                // Seed admin user
                await SeedAdminUserAsync(userManager, logger);
                
                logger.LogInformation("Identity data seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding Identity data.");
                throw;
            }
        }
        
        /// <summary>
        /// Creates application roles if they don't exist
        /// </summary>
        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            logger.LogInformation("Seeding application roles...");
            
            var roles = ApplicationRoles.GetAllRoles();
            
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant()
                    };
                    
                    var result = await roleManager.CreateAsync(role);
                    
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Created role: {RoleName}", roleName);
                    }
                    else
                    {
                        logger.LogError("Failed to create role: {RoleName}. Errors: {Errors}", 
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogDebug("Role already exists: {RoleName}", roleName);
                }
            }
        }
        
        /// <summary>
        /// Creates the initial admin user if it doesn't exist
        /// </summary>
        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
        {
            logger.LogInformation("Seeding admin user...");
            
            const string adminEmail = "admin@greenswap.com";
            const string legacyAdminEmail = "admin@secondhandgoods.com";
            const string adminUserName = "admin";
            const string adminPassword = "Admin123!"; // In production, use a secure password from configuration
            
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var legacyUser = await userManager.FindByEmailAsync(legacyAdminEmail);
                if (legacyUser != null)
                {
                    var emailResult = await userManager.SetEmailAsync(legacyUser, adminEmail);
                    if (emailResult.Succeeded)
                    {
                        logger.LogInformation("Migrated admin email from {Legacy} to {Email}", legacyAdminEmail, adminEmail);
                        adminUser = legacyUser;
                    }
                    else
                    {
                        logger.LogError("Failed to migrate admin email. Errors: {Errors}",
                            string.Join(", ", emailResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "System",
                    LastName = "Administrator",
                    Location = "System",
                    Bio = "System administrator account",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                
                if (result.Succeeded)
                {
                    logger.LogInformation("Created admin user: {Email}", adminEmail);
                    
                    // Add admin to Admin role
                    var roleResult = await userManager.AddToRoleAsync(adminUser, ApplicationRoles.Admin);
                    
                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation("Added admin user to Admin role");
                    }
                    else
                    {
                        logger.LogError("Failed to add admin user to Admin role. Errors: {Errors}", 
                            string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogError("Failed to create admin user. Errors: {Errors}", 
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogDebug("Admin user already exists: {Email}", adminUser.Email);
                
                // Ensure admin user has Admin role
                if (!await userManager.IsInRoleAsync(adminUser, ApplicationRoles.Admin))
                {
                    var roleResult = await userManager.AddToRoleAsync(adminUser, ApplicationRoles.Admin);
                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation("Added existing admin user to Admin role");
                    }
                }
            }
        }
    }
}