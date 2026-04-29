namespace RemindMe.Models;

public class ReminderItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? ReminderTime { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsImportant { get; set; }
    public bool HasAlert { get; set; }
    public bool IsSelected { get; set; }
}