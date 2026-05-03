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
}