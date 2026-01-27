using JournalApplication.Model;

namespace JournalApplication.Services;

public class UserSessionService
{
    public UserDisplayModel? CurrentUser { get; private set; }

    public void SetCurrentUser(UserDisplayModel user)
    {
        CurrentUser = user;
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    public bool IsLoggedIn => CurrentUser != null;
}
