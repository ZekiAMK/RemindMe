using SQLite;

namespace RemindMe.Models;

public class ReminderItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime? ReminderTime { get; set; }

    public bool IsCompleted { get; set; }
    public bool IsImportant { get; set; }
    public bool HasAlert { get; set; }

    [Ignore]
    public bool IsSelected { get; set; }

    [Ignore]
    public bool IsPastDue =>
    HasAlert &&
    !IsCompleted &&
    ReminderTime.HasValue &&
    ReminderTime.Value < DateTime.Now;
}