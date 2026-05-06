using System.ComponentModel;
using System.Runtime.CompilerServices;
using SQLite;

namespace RemindMe.Models;

public class ReminderItem : INotifyPropertyChanged
{
    private bool _isSelected;

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime? ReminderTime { get; set; }

    public bool IsCompleted { get; set; }
    public bool IsImportant { get; set; }
    public bool HasAlert { get; set; }

    [Ignore]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    [Ignore]
    public bool IsPastDue =>
        HasAlert &&
        !IsCompleted &&
        ReminderTime.HasValue &&
        ReminderTime.Value < DateTime.Now;

    [Ignore]
    public bool ShowImportantIcon =>
        IsImportant && !IsCompleted;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}