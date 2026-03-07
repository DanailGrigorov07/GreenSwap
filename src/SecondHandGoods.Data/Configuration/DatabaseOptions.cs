namespace SecondHandGoods.Data.Configuration
{
    /// <summary>
    /// Configuration options for database connection
    /// </summary>
    public class DatabaseOptions
    {
        public const string SectionName = "Database";
        
        /// <summary>
        /// SQL Server connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
        
        /// <summary>
        /// Enable detailed logging for EF Core queries (for development)
        /// </summary>
        public bool EnableSensitiveDataLogging { get; set; } = false;
        
        /// <summary>
        /// Enable detailed query logging (for development)
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;
    }
}