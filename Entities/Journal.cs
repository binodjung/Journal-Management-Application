namespace JournalApplication.Entities;
public class Journal
{
    public int JournalId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string PrimaryMood { get; set; } = string.Empty;
    public List<String> SecondaryMoods { get; set; } = new();
    public List<String> Tags { get; set; } = new();
    public int WordCount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
