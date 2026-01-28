using JournalApplication.Entities;
using JournalApplication.Common;
using JournalApplication.Data;
using JournalApplication.Model;
using Microsoft.EntityFrameworkCore;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JournalApplication.Services;

public class JournalService : IJournalService
{
    private readonly AppDbContext _context;

    public JournalService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<Journal>> AddOrUpdateJournalAsync(int userId, JournalViewModel model)
    {
        try
        {
            if (model.EntryDate > DateTime.Today)
                return ServiceResult<Journal>.FailureResult("Cannot create journal for future dates");

            var journal = await _context.Journals
                .FirstOrDefaultAsync(j => j.UserId == userId && j.EntryDate.Date == model.EntryDate.Date);

            if (journal == null)
            {
                journal = new Journal
                {
                    UserId = userId,
                    Title = model.Title,
                    Content = model.Content,
                    EntryDate = model.EntryDate.Date,
                    PrimaryMood = model.PrimaryMood,
                    SecondaryMoods = model.SecondaryMoods,
                    Tags = model.Tags,
                    WordCount = CountWords(model.Content),
                    UpdatedAt = DateTime.Now
                };

                _context.Journals.Add(journal);
            }
            else
            {
                journal.Title = model.Title;
                journal.Content = model.Content;
                journal.PrimaryMood = model.PrimaryMood;
                journal.SecondaryMoods = model.SecondaryMoods;
                journal.Tags = model.Tags;
                journal.WordCount = CountWords(model.Content);
                journal.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return ServiceResult<Journal>.SuccessResult(journal);
        }
        catch (Exception ex)
        {
            return ServiceResult<Journal>.FailureResult($"Error saving journal: {ex.Message}");
        }
    }

    public async Task<JournalDisplayModel?> GetJournalByDateAsync(int userId, DateTime date)
    {
        var journal = await _context.Journals
            .FirstOrDefaultAsync(j =>
                j.UserId == userId &&
                j.EntryDate.Date == date.Date);

        if (journal == null)
            return null;

        return new JournalDisplayModel
        {
            Title = journal.Title,
            EntryDate = journal.EntryDate,
            Content = journal.Content,
            PrimaryMood = journal.PrimaryMood,
            SecondaryMoods = journal.SecondaryMoods ?? new(),
            Tags = journal.Tags ?? new(),
            WordCount = string.IsNullOrWhiteSpace(journal.Content)
                ? 0
                : journal.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
        };
    }

    private int CountWords(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        return content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public async Task<(List<JournalDisplayModel> Journals, int TotalCount)> GetAllJournalsByUserAsync(
        int userId, int page = 1, int pageSize = 10)
    {
        page = Math.Max(page, 1);

        var query = _context.Journals
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.EntryDate);

        int totalCount = await query.CountAsync();

        var journals = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JournalDisplayModel
            {
                JournalId = j.JournalId,
                EntryDate = j.EntryDate,
                Title = j.Title,
                PrimaryMood = j.PrimaryMood,
                SecondaryMoods = j.SecondaryMoods,
                Tags = j.Tags,
                WordCount = j.WordCount
            })
            .ToListAsync();

        return (journals, totalCount);
    }

    public async Task DeleteJournalAsync(int userId, DateTime date)
    {
        var journal = await _context.Journals
            .FirstOrDefaultAsync(j =>
                j.UserId == userId &&
                j.EntryDate.Date == date.Date);

        if (journal == null)
            return;

        _context.Journals.Remove(journal);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<JournalDisplayModel>, int)> SearchJournalsAsync(
        int userId,
        string titleSearch,
        string moodFilter,
        string tagSearch,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize)
    {
        page = Math.Max(page, 1);

        var query = _context.Journals
            .Where(j => j.UserId == userId);

        // ONE FILTER AT A TIME
        if (!string.IsNullOrWhiteSpace(titleSearch))
        {
            query = query.Where(j => j.Title.Contains(titleSearch));
        }
        else if (!string.IsNullOrWhiteSpace(moodFilter))
        {
            query = query.Where(j => j.PrimaryMood == moodFilter);
        }
        else if (!string.IsNullOrWhiteSpace(tagSearch))
        {
            query = query.Where(j => j.Tags.Any(t => t.Contains(tagSearch)));
        }

        if (fromDate.HasValue)
            query = query.Where(j => j.EntryDate.Date >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(j => j.EntryDate.Date <= toDate.Value.Date);

        int totalCount = await query.CountAsync();

        var journals = await query
            .OrderByDescending(j => j.EntryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JournalDisplayModel
            {
                JournalId = j.JournalId,
                EntryDate = j.EntryDate,
                Title = j.Title,
                Content = j.Content,
                PrimaryMood = j.PrimaryMood,
                SecondaryMoods = j.SecondaryMoods,
                Tags = j.Tags,
                WordCount = j.WordCount
            })
            .ToListAsync();

        return (journals, totalCount);
    }

    public async Task<byte[]> GenerateJournalPdfAsync(
        int userId,
        DateTime fromDate,
        DateTime toDate)
    {
        var journals = await _context.Journals
            .Where(j =>
                j.UserId == userId &&
                j.EntryDate >= fromDate &&
                j.EntryDate <= toDate)
            .OrderBy(j => j.EntryDate)
            .ToListAsync();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text($"Journal Report ({fromDate:dd MMM yyyy} - {toDate:dd MMM yyyy})")
                    .SemiBold().FontSize(16).AlignCenter();

                page.Content().Column(col =>
                {
                    foreach (var j in journals)
                    {
                        col.Item().PaddingBottom(10).BorderBottom(1).Column(c =>
                        {
                            c.Item().Text(j.EntryDate.ToString("dd MMM yyyy"))
                                .SemiBold().FontSize(12);

                            c.Item().Text(j.Title).SemiBold();
                            c.Item().Text($"Mood: {j.PrimaryMood}");
                            c.Item().Text($"Words: {j.WordCount}");
                            c.Item().Text(j.Content);
                        });
                    }
                });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Generated on ");
                        x.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm"));
                    });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<AnalyticsModel> GetAnalyticsAsync(int userId)
    {
        var allJournals = await _context.Journals
            .Where(j => j.UserId == userId)
            .OrderBy(j => j.EntryDate)
            .ToListAsync();

        var analytics = new AnalyticsModel
        {
            TotalEntries = allJournals.Count
        };

        // Calculate Streaks
        var (currentStreak, longestStreak) = CalculateStreaks(allJournals);
        analytics.CurrentStreak = currentStreak;
        analytics.LongestStreak = longestStreak;

        // Calculate Missed Days (current month)
        var daysInMonth = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
        var journalDatesThisMonth = allJournals
            .Where(j => j.EntryDate.Year == DateTime.Today.Year && j.EntryDate.Month == DateTime.Today.Month)
            .Select(j => j.EntryDate.Day)
            .ToHashSet();
        analytics.MissedDays = DateTime.Today.Day - journalDatesThisMonth.Count(d => d <= DateTime.Today.Day);

        // Mood Distribution by Category
        var moodCategories = new Dictionary<string, List<string>>
        {
            ["Positive"] = new() { "Happy", "Excited", "Relaxed", "Grateful", "Confident" },
            ["Neutral"] = new() { "Calm", "Thoughtful", "Curious", "Nostalgic", "Bored" },
            ["Negative"] = new() { "Sad", "Angry", "Stressed", "Lonely", "Anxious" }
        };

        var moodCounts = new Dictionary<string, int>
        {
            ["Positive"] = 0,
            ["Neutral"] = 0,
            ["Negative"] = 0
        };

        foreach (var journal in allJournals)
        {
            foreach (var category in moodCategories)
            {
                if (category.Value.Contains(journal.PrimaryMood))
                {
                    moodCounts[category.Key]++;
                    break;
                }
            }
        }

        analytics.MoodDistribution = moodCounts
            .Select(m => new ChartData { Label = m.Key, Value = m.Value })
            .ToList();

        // Top Moods
        var topMoods = allJournals
            .GroupBy(j => j.PrimaryMood)
            .Select(g => new ChartData { Label = g.Key, Value = g.Count() })
            .OrderByDescending(m => m.Value)
            .Take(5)
            .ToList();
        analytics.TopMoods = topMoods;

        // Tag Usage
        var tagCounts = allJournals
            .SelectMany(j => j.Tags)
            .GroupBy(t => t)
            .Select(g => new ChartData { Label = g.Key, Value = g.Count() })
            .OrderByDescending(t => t.Value)
            .Take(5)
            .ToList();
        analytics.TagUsage = tagCounts;

        // Word Count Trend (Last 7 days)
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => DateTime.Today.AddDays(-6 + i))
            .ToList();

        analytics.WordCountTrend = last7Days
            .Select(date =>
            {
                return new ChartData
                {
                    Label = date.ToString("ddd"),
                    Value = allJournals.Where(j => j.EntryDate.Date == date.Date).Sum(j => j.WordCount)
                };
            })
            .ToList();

        // Recent Entries
        analytics.RecentEntries = allJournals
            .OrderByDescending(j => j.EntryDate)
            .Take(5)
            .Select(j => new JournalDisplayModel
            {
                JournalId = j.JournalId,
                Title = j.Title,
                EntryDate = j.EntryDate,
                PrimaryMood = j.PrimaryMood,
                SecondaryMoods = j.SecondaryMoods,
                Tags = j.Tags,
                WordCount = j.WordCount
            })
            .ToList();

        return analytics;
    }

    private (int currentStreak, int longestStreak) CalculateStreaks(List<Journal> journals)
    {
        if (journals.Count == 0)
            return (0, 0);

        var dates = journals.Select(j => j.EntryDate.Date).Distinct().OrderBy(d => d).ToList();

        int currentStreak = 0;
        int longestStreak = 0;
        int tempStreak = 1;

        // Calculate longest streak
        for (int i = 1; i < dates.Count; i++)
        {
            if ((dates[i] - dates[i - 1]).Days == 1)
            {
                tempStreak++;
            }
            else
            {
                longestStreak = Math.Max(longestStreak, tempStreak);
                tempStreak = 1;
            }
        }
        longestStreak = Math.Max(longestStreak, tempStreak);

        // Calculate current streak
        if (dates.Count > 0)
        {
            var lastDate = dates[^1];
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            if (lastDate == today || lastDate == yesterday)
            {
                currentStreak = 1;
                for (int i = dates.Count - 2; i >= 0; i--)
                {
                    if ((dates[i + 1] - dates[i]).Days == 1)
                    {
                        currentStreak++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return (currentStreak, longestStreak);
    }
}

