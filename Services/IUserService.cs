
using JournalApplication.Common;
using JournalApplication.Model;

namespace JournalApplication.Services;

public interface IUserService
{
    Task<ServiceResult<UserDisplayModel>> RegisterUserAsync(UserViewModel viewModel);
    Task<ServiceResult<UserDisplayModel>> LoginUserAsync(string username, string password);
}