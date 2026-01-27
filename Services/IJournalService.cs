using JournalApplication.Entities;
using JournalApplication.Common;
using JournalApplication.Model;

namespace JournalApplication.Services;

public interface IJournalService
{
    Task<ServiceResult<Journal>> AddOrUpdateJournalAsync(int userId, JournalViewModel model);
    Task<JournalDisplayModel?> GetJournalByDateAsync(int userId, DateTime date);
    Task<(List<JournalDisplayModel> Journals, int TotalCount)> GetAllJournalsByUserAsync(
    int userId, int page = 1, int pageSize = 10);
    Task DeleteJournalAsync(int userId, DateTime date);
    Task<(List<JournalDisplayModel>, int)>
    SearchJournalsAsync(
        int userId,
        string titleSearch,
        string moodFilter,
        string tagSearch,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize);
    
        Task<byte[]> GenerateJournalPdfAsync(
    int userId,
    DateTime fromDate,
    DateTime toDate);

}