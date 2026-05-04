using SQLite;

namespace RemindMe.Models;

public class PomodoroSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime EndedAt { get; set; }

    public int FocusMinutes { get; set; }

    public int BreakMinutes { get; set; }

    public bool CompletedFocus { get; set; }

    public bool CompletedBreak { get; set; }

    public bool WasContinued { get; set; }

    public DateTime SessionDate { get; set; }

    [Ignore]
    public bool IsSelected { get; set; }
}