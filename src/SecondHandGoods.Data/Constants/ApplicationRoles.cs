namespace SecondHandGoods.Data.Constants
{
    /// <summary>
    /// Application role constants
    /// </summary>
    public static class ApplicationRoles
    {
        /// <summary>
        /// System administrator with full access
        /// </summary>
        public const string Admin = "Admin";
        
        /// <summary>
        /// Moderator who can manage content and users
        /// </summary>
        public const string Moderator = "Moderator";
        
        /// <summary>
        /// Regular user who can buy and sell items
        /// </summary>
        public const string User = "User";
        
        /// <summary>
        /// Get all defined roles
        /// </summary>
        /// <returns>Array of all role names</returns>
        public static string[] GetAllRoles()
        {
            return new[] { Admin, Moderator, User };
        }
        
        /// <summary>
        /// Check if a role name is valid
        /// </summary>
        /// <param name="roleName">Role name to validate</param>
        /// <returns>True if the role is valid</returns>
        public static bool IsValidRole(string roleName)
        {
            return GetAllRoles().Contains(roleName, StringComparer.OrdinalIgnoreCase);
        }
    }
}