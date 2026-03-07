using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecondHandGoods.Data.Entities;

namespace SecondHandGoods.Data.Seed
{
    /// <summary>
    /// Seeder for default forbidden words for content moderation
    /// </summary>
    public static class ForbiddenWordsSeeder
    {
        /// <summary>
        /// Seeds default forbidden words for content moderation
        /// </summary>
        public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                // Check if forbidden words already exist
                if (await context.ForbiddenWords.AnyAsync())
                {
                    logger.LogDebug("Forbidden words already exist, skipping seeding.");
                    return;
                }

                logger.LogInformation("Seeding default forbidden words...");

                var defaultWords = new List<ForbiddenWord>
                {
                    // Profanity and inappropriate language
                    new ForbiddenWord
                    {
                        Word = "spam",
                        Severity = ModerationSeverity.High,
                        Category = "Spam",
                        IsBlocked = true,
                        IsExactMatch = true,
                        Replacement = "[promotional]",
                        AdminNotes = "Common spam indicator"
                    },
                    new ForbiddenWord
                    {
                        Word = "scam",
                        Severity = ModerationSeverity.Critical,
                        Category = "Scam",
                        IsBlocked = true,
                        IsExactMatch = true,
                        AdminNotes = "Potential fraudulent activity"
                    },
                    new ForbiddenWord
                    {
                        Word = "fake",
                        Severity = ModerationSeverity.High,
                        Category = "Fraud",
                        IsBlocked = false, // Just flag for review
                        IsExactMatch = true,
                        AdminNotes = "May indicate counterfeit goods"
                    },
                    new ForbiddenWord
                    {
                        Word = "stolen",
                        Severity = ModerationSeverity.Critical,
                        Category = "Illegal",
                        IsBlocked = true,
                        IsExactMatch = true,
                        AdminNotes = "Indicates stolen merchandise"
                    },
                    new ForbiddenWord
                    {
                        Word = "drugs",
                        Severity = ModerationSeverity.Critical,
                        Category = "Illegal",
                        IsBlocked = true,
                        IsExactMatch = false,
                        AdminNotes = "Illegal substances"
                    },

                    // Personal information protection
                    new ForbiddenWord
                    {
                        Word = "password",
                        Severity = ModerationSeverity.Medium,
                        Category = "Personal Info",
                        IsBlocked = false,
                        IsExactMatch = true,
                        AdminNotes = "May contain sensitive information"
                    },
                    new ForbiddenWord
                    {
                        Word = "credit card",
                        Severity = ModerationSeverity.Critical,
                        Category = "Personal Info",
                        IsBlocked = true,
                        IsExactMatch = false,
                        AdminNotes = "Financial information protection"
                    },
                    new ForbiddenWord
                    {
                        Word = "social security",
                        Severity = ModerationSeverity.Critical,
                        Category = "Personal Info",
                        IsBlocked = true,
                        IsExactMatch = false,
                        AdminNotes = "SSN protection"
                    },

                    // Common inappropriate words (examples)
                    new ForbiddenWord
                    {
                        Word = "hate",
                        Severity = ModerationSeverity.Medium,
                        Category = "Hate Speech",
                        IsBlocked = false,
                        IsExactMatch = true,
                        AdminNotes = "Context-dependent, flag for review"
                    },
                    new ForbiddenWord
                    {
                        Word = "kill",
                        Severity = ModerationSeverity.High,
                        Category = "Violence",
                        IsBlocked = false,
                        IsExactMatch = true,
                        AdminNotes = "May be violent content"
                    },

                    // Spam and commercial abuse
                    new ForbiddenWord
                    {
                        Word = "guaranteed money",
                        Severity = ModerationSeverity.High,
                        Category = "Spam",
                        IsBlocked = true,
                        IsExactMatch = false,
                        Replacement = "[promotional offer]",
                        AdminNotes = "Common spam phrase"
                    },
                    new ForbiddenWord
                    {
                        Word = "work from home",
                        Severity = ModerationSeverity.Medium,
                        Category = "Spam",
                        IsBlocked = false,
                        IsExactMatch = false,
                        AdminNotes = "Often used in pyramid schemes"
                    },
                    new ForbiddenWord
                    {
                        Word = "get rich quick",
                        Severity = ModerationSeverity.High,
                        Category = "Scam",
                        IsBlocked = true,
                        IsExactMatch = false,
                        AdminNotes = "Classic scam indicator"
                    },

                    // Brand protection (examples)
                    new ForbiddenWord
                    {
                        Word = "replica",
                        Severity = ModerationSeverity.Medium,
                        Category = "Brand Names",
                        IsBlocked = false,
                        IsExactMatch = true,
                        AdminNotes = "May indicate counterfeit products"
                    },
                    new ForbiddenWord
                    {
                        Word = "counterfeit",
                        Severity = ModerationSeverity.Critical,
                        Category = "Illegal",
                        IsBlocked = true,
                        IsExactMatch = true,
                        AdminNotes = "Illegal counterfeit goods"
                    },

                    // Test word for demonstration
                    new ForbiddenWord
                    {
                        Word = "badword",
                        Severity = ModerationSeverity.Low,
                        Category = "Testing",
                        IsBlocked = false,
                        IsExactMatch = true,
                        Replacement = "[censored]",
                        AdminNotes = "Test word for demonstration purposes"
                    }
                };

                // Normalize all words
                foreach (var word in defaultWords)
                {
                    word.NormalizeWord();
                }

                await context.ForbiddenWords.AddRangeAsync(defaultWords);
                await context.SaveChangesAsync();

                logger.LogInformation("Successfully seeded {Count} default forbidden words", defaultWords.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding forbidden words");
                throw;
            }
        }
    }
}