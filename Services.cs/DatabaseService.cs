using SQLite;
using RemindMe.Models;

namespace RemindMe.Services;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _db;

    public DatabaseService(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);

        _db.CreateTableAsync<ReminderItem>().Wait();
        _db.CreateTableAsync<PomodoroSession>().Wait();
    }

    public Task<List<ReminderItem>> GetRemindersAsync()
    {
        return _db.Table<ReminderItem>().ToListAsync();
    }

    public Task<int> SaveReminderAsync(ReminderItem reminder)
    {
        if (reminder.Id != 0)
            return _db.UpdateAsync(reminder);

        return _db.InsertAsync(reminder);
    }

    public Task<int> DeleteReminderAsync(ReminderItem reminder)
    {
        return _db.DeleteAsync(reminder);
    }

    public Task<List<PomodoroSession>> GetPomodoroSessionsAsync()
    {
        return _db.Table<PomodoroSession>()
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
    }

    public Task<List<PomodoroSession>> GetPomodoroSessionsForDateAsync(DateTime date)
    {
        DateTime day = date.Date;

        return _db.Table<PomodoroSession>()
            .Where(s => s.SessionDate == day)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
    }

    public Task<int> SavePomodoroSessionAsync(PomodoroSession session)
    {
        if (session.Id != 0)
            return _db.UpdateAsync(session);

        return _db.InsertAsync(session);
    }

    public Task<int> DeletePomodoroSessionAsync(PomodoroSession session)
    {
        return _db.DeleteAsync(session);
    }
}