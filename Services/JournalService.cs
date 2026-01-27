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
}