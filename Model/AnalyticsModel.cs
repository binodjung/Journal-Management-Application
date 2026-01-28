namespace JournalApplication.Model;

public class AnalyticsModel
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalEntries { get; set; }
    public int MissedDays { get; set; }
    public List<ChartData> MoodDistribution { get; set; } = new();
    public List<ChartData> TopMoods { get; set; } = new();
    public List<ChartData> TagUsage { get; set; } = new();
    public List<ChartData> WordCountTrend { get; set; } = new();
    public List<JournalDisplayModel> RecentEntries { get; set; } = new();
}

public class ChartData
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}
