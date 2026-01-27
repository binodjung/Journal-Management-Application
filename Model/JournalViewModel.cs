namespace JournalApplication.Model;

using System.ComponentModel.DataAnnotations;
public class JournalViewModel
{
    public int JournalId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title can be max 100 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Entry Date is required")]
    public DateTime EntryDate { get; set; }

    [Required(ErrorMessage = "Primary Mood is required")]
    public string PrimaryMood { get; set; } = string.Empty;

    public List<String> SecondaryMoods { get; set; } = new();

    public List<String> Tags { get; set; } = new();
}