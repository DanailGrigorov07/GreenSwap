using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecondHandGoods.Data;
using SecondHandGoods.Data.Entities;
using System.Text.RegularExpressions;

namespace SecondHandGoods.Services
{
    /// <summary>
    /// Service for content moderation and forbidden word filtering
    /// </summary>
    public interface IContentModerationService
    {
        /// <summary>
        /// Moderates text content and returns the result
        /// </summary>
        Task<ContentModerationResult> ModerateContentAsync(string content, ModeratedEntityType entityType, int entityId, string authorId, string? ipAddress = null);

        /// <summary>
        /// Gets all active forbidden words from the database
        /// </summary>
        Task<List<ForbiddenWord>> GetForbiddenWordsAsync();

        /// <summary>
        /// Adds a new forbidden word
        /// </summary>
        Task<ForbiddenWord> AddForbiddenWordAsync(ForbiddenWord word, string adminId);

        /// <summary>
        /// Updates an existing forbidden word
        /// </summary>
        Task<ForbiddenWord> UpdateForbiddenWordAsync(ForbiddenWord word, string adminId);

        /// <summary>
        /// Deletes a forbidden word
        /// </summary>
        Task<bool> DeleteForbiddenWordAsync(int wordId);

        /// <summary>
        /// Gets moderation logs with filtering options
        /// </summary>
        Task<(List<ModerationLog> logs, int totalCount)> GetModerationLogsAsync(
            ModeratedEntityType? entityType = null, 
            Data.Entities.ModerationResult? result = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null,
            int page = 1, 
            int pageSize = 20);

        /// <summary>
        /// Appeals a moderation decision
        /// </summary>
        Task<bool> AppealModerationAsync(int logId, string appealReason, string userId);

        /// <summary>
        /// Processes an appeal (admin action)
        /// </summary>
        Task<bool> ProcessAppealAsync(int logId, bool approved, string decision, string adminId);
    }

    public class ContentModerationService : IContentModerationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContentModerationService> _logger;

        public ContentModerationService(ApplicationDbContext context, ILogger<ContentModerationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ContentModerationResult> ModerateContentAsync(string content, ModeratedEntityType entityType, int entityId, string authorId, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new ContentModerationResult { Passed = true, ModifiedContent = content };

            try
            {
                // Get active forbidden words
                var forbiddenWords = await GetActiveForbiddenWordsAsync();
                
                var result = new ContentModerationResult
                {
                    OriginalContent = content,
                    ModifiedContent = content,
                    Passed = true,
                    DetectedWords = new List<string>(),
                    MaxSeverity = ModerationSeverity.Low
                };

                string modifiedContent = content;
                var detectedWords = new List<string>();
                var maxSeverity = ModerationSeverity.Low;

                // Check content against forbidden words
                foreach (var word in forbiddenWords)
                {
                    var matches = FindWordMatches(content, word);
                    
                    if (matches.Any())
                    {
                        detectedWords.Add(word.Word);
                        
                        // Update max severity
                        if (word.Severity > maxSeverity)
                            maxSeverity = word.Severity;

                        // Apply moderation based on word configuration
                        if (word.IsBlocked)
                        {
                            result.Passed = false;
                            
                            // Replace with asterisks or replacement word
                            if (!string.IsNullOrEmpty(word.Replacement))
                            {
                                modifiedContent = ReplaceWordMatches(modifiedContent, word, word.Replacement);
                                result.WasModified = true;
                            }
                            else
                            {
                                // Replace with asterisks
                                modifiedContent = ReplaceWordMatches(modifiedContent, word, new string('*', word.Word.Length));
                                result.WasModified = true;
                            }
                        }
                        else
                        {
                            // Just flag for review
                            result.RequiresReview = true;
                        }
                    }
                }

                result.ModifiedContent = modifiedContent;
                result.DetectedWords = detectedWords;
                result.MaxSeverity = maxSeverity;

                // Determine final action based on severity and settings
                var action = DetermineAction(result);
                var finalResult = DetermineFinalResult(result);

                // Log the moderation
                await LogModerationAsync(new ModerationLog
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    ContentAuthorId = authorId,
                    Action = action,
                    Result = finalResult,
                    Severity = maxSeverity,
                    OriginalContent = content,
                    ModeratedContent = result.WasModified ? modifiedContent : null,
                    DetectedWords = detectedWords.Any() ? string.Join(", ", detectedWords) : null,
                    IsAutomatic = true,
                    UserIpAddress = ipAddress,
                    ModerationReason = GenerateModerationReason(result)
                });

                _logger.LogInformation("Content moderation completed for {EntityType} {EntityId}. Result: {Result}, Detected: {DetectedCount} words", 
                    entityType, entityId, finalResult, detectedWords.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during content moderation for {EntityType} {EntityId}", entityType, entityId);
                
                // Return safe result in case of error
                return new ContentModerationResult 
                { 
                    Passed = true, 
                    ModifiedContent = content,
                    HasError = true,
                    ErrorMessage = "Moderation service temporarily unavailable"
                };
            }
        }

        public async Task<List<ForbiddenWord>> GetForbiddenWordsAsync()
        {
            return await _context.ForbiddenWords
                .Where(fw => fw.IsActive)
                .OrderBy(fw => fw.Category)
                .ThenBy(fw => fw.Word)
                .ToListAsync();
        }

        public async Task<ForbiddenWord> AddForbiddenWordAsync(ForbiddenWord word, string adminId)
        {
            word.NormalizeWord();
            word.CreatedByUserId = adminId;
            word.CreatedAt = DateTime.UtcNow;

            _context.ForbiddenWords.Add(word);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Forbidden word '{Word}' added by admin {AdminId}", word.Word, adminId);
            return word;
        }

        public async Task<ForbiddenWord> UpdateForbiddenWordAsync(ForbiddenWord word, string adminId)
        {
            word.NormalizeWord();
            word.UpdateTimestamp(adminId);

            _context.ForbiddenWords.Update(word);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Forbidden word '{Word}' updated by admin {AdminId}", word.Word, adminId);
            return word;
        }

        public async Task<bool> DeleteForbiddenWordAsync(int wordId)
        {
            var word = await _context.ForbiddenWords.FindAsync(wordId);
            if (word == null)
                return false;

            // Soft delete by deactivating
            word.IsActive = false;
            word.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Forbidden word '{Word}' deactivated", word.Word);
            return true;
        }

        public async Task<(List<ModerationLog> logs, int totalCount)> GetModerationLogsAsync(
            ModeratedEntityType? entityType = null, 
            Data.Entities.ModerationResult? result = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null,
            int page = 1, 
            int pageSize = 20)
        {
            var query = _context.ModerationLogs
                .Include(ml => ml.ContentAuthor)
                .Include(ml => ml.Moderator)
                .Include(ml => ml.ForbiddenWord)
                .AsQueryable();

            // Apply filters
            if (entityType.HasValue)
                query = query.Where(ml => ml.EntityType == entityType.Value);

            if (result.HasValue)
                query = query.Where(ml => ml.Result == result.Value);

            if (fromDate.HasValue)
                query = query.Where(ml => ml.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(ml => ml.CreatedAt <= toDate.Value);

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(ml => ml.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }

        public async Task<bool> AppealModerationAsync(int logId, string appealReason, string userId)
        {
            var log = await _context.ModerationLogs.FindAsync(logId);
            if (log == null || log.ContentAuthorId != userId)
                return false;

            log.IsAppealed = true;
            log.AppealDecision = $"Appeal submitted: {appealReason}";
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Moderation appeal submitted for log {LogId} by user {UserId}", logId, userId);
            return true;
        }

        public async Task<bool> ProcessAppealAsync(int logId, bool approved, string decision, string adminId)
        {
            var log = await _context.ModerationLogs.FindAsync(logId);
            if (log == null)
                return false;

            log.AppealDecision = $"{(approved ? "APPROVED" : "DENIED")}: {decision}";
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Appeal {Status} for log {LogId} by admin {AdminId}", approved ? "approved" : "denied", logId, adminId);
            return true;
        }

        #region Private Helper Methods

        private async Task<List<ForbiddenWord>> GetActiveForbiddenWordsAsync()
        {
            return await _context.ForbiddenWords
                .Where(fw => fw.IsActive)
                .OrderByDescending(fw => fw.Severity) // Check higher severity words first
                .ToListAsync();
        }

        private List<Match> FindWordMatches(string content, ForbiddenWord word)
        {
            try
            {
                string pattern;
                
                if (word.IsExactMatch)
                {
                    // Exact word match with word boundaries
                    pattern = $@"\b{Regex.Escape(word.NormalizedWord)}\b";
                }
                else
                {
                    // Partial match
                    pattern = Regex.Escape(word.NormalizedWord);
                }

                return Regex.Matches(content.ToLowerInvariant(), pattern, RegexOptions.IgnoreCase).Cast<Match>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error creating regex pattern for word '{Word}'", word.Word);
                return new List<Match>();
            }
        }

        private string ReplaceWordMatches(string content, ForbiddenWord word, string replacement)
        {
            try
            {
                string pattern;
                
                if (word.IsExactMatch)
                {
                    pattern = $@"\b{Regex.Escape(word.Word)}\b";
                }
                else
                {
                    pattern = Regex.Escape(word.Word);
                }

                return Regex.Replace(content, pattern, replacement, RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error replacing word '{Word}' in content", word.Word);
                return content;
            }
        }

        private static ModerationAction DetermineAction(ContentModerationResult result)
        {
            if (!result.Passed && result.WasModified)
                return ModerationAction.AutoReplace;
            
            if (!result.Passed)
                return ModerationAction.AutoBlock;
                
            if (result.RequiresReview)
                return ModerationAction.AutoFlag;
                
            return ModerationAction.ManualApprove; // Content passed
        }

        private static Data.Entities.ModerationResult DetermineFinalResult(ContentModerationResult result)
        {
            if (!result.Passed && result.WasModified)
                return Data.Entities.ModerationResult.Modified;
                
            if (!result.Passed)
                return Data.Entities.ModerationResult.Blocked;
                
            if (result.RequiresReview)
                return Data.Entities.ModerationResult.Flagged;
                
            return Data.Entities.ModerationResult.Passed;
        }

        private static string GenerateModerationReason(ContentModerationResult result)
        {
            if (result.DetectedWords.Any())
            {
                return $"Detected forbidden words: {string.Join(", ", result.DetectedWords)}";
            }
            
            return "Content passed moderation checks";
        }

        private async Task LogModerationAsync(ModerationLog log)
        {
            try
            {
                _context.ModerationLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log moderation activity");
                // Don't throw - logging failure shouldn't break the moderation process
            }
        }

        #endregion
    }

    /// <summary>
    /// Result of content moderation
    /// </summary>
    public class ContentModerationResult
    {
        public string OriginalContent { get; set; } = string.Empty;
        public string ModifiedContent { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public bool WasModified { get; set; }
        public bool RequiresReview { get; set; }
        public List<string> DetectedWords { get; set; } = new();
        public ModerationSeverity MaxSeverity { get; set; }
        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }
    }
}