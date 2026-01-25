using JournalApplicaton.Common;
using JournalApplicaton.Model;

namespace JournalApplicaton.Services;

public interface IUserService
{
    Task<ServiceResult<UserDisplayModel>> RegisterUserAsync(UserViewModel viewModel);
    Task<ServiceResult<UserDisplayModel>> LoginUserAsync(string username, string password);
    Task<ServiceResult<UserDisplayModel>> GetUserByIdAsync(int id);

    Task<ServiceResult<UserDisplayModel>> UpdateUserAsync(int id, UserViewModel viewModel);
    Task<ServiceResult<bool>> DeleteUserAsync(int id);
}