namespace JournalApplication.Model;

public class JournalDisplayModel
{
    public int JournalId { get; set; }
    public DateTime EntryDate { get; set; }
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public string PrimaryMood { get; set; } = string.Empty;

    public List<String> SecondaryMoods { get; set; } = new();

    public List<String> Tags { get; set; } = new();
    public int WordCount { get; set; }
}