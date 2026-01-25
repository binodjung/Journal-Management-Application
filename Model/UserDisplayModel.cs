namespace JournalApplicaton.Model;


public class UserDisplayModel
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}