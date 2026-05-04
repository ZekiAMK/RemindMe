using System.ComponentModel;
using System.Runtime.CompilerServices;
using RemindMe.Services;

namespace RemindMe.Pages;

public partial class PomodoroPage : ContentPage, INotifyPropertyChanged
{
    private const int PomodoroNotificationId = 900001;

    private const string FocusMinutesKey = "PomodoroFocusMinutes";
    private const string BreakMinutesKey = "PomodoroBreakMinutes";
    private const string IsRunningKey = "PomodoroIsRunning";
    private const string IsFocusModeKey = "PomodoroIsFocusMode";
    private const string RemainingSecondsKey = "PomodoroRemainingSeconds";
    private const string LastUpdatedKey = "PomodoroLastUpdated";

    private IDispatcherTimer? _timer;

    private bool _isRunning;
    private bool _isFocusMode = true;
    private int _remainingSeconds;

    private string _focusMinutesText = "25";
    private string _breakMinutesText = "5";

    public new event PropertyChangedEventHandler? PropertyChanged;

    public string FocusMinutesText
    {
        get => _focusMinutesText;
        set
        {
            _focusMinutesText = value;
            Preferences.Set(FocusMinutesKey, GetFocusMinutes());

            if (!_isRunning && _isFocusMode)
                _remainingSeconds = GetFocusMinutes() * 60;

            SaveCurrentPomodoroState();
            RefreshAll();
            OnPropertyChanged();
        }
    }

    public string BreakMinutesText
    {
        get => _breakMinutesText;
        set
        {
            _breakMinutesText = value;
            Preferences.Set(BreakMinutesKey, GetBreakMinutes());

            if (!_isRunning && !_isFocusMode)
                _remainingSeconds = GetBreakMinutes() * 60;

            SaveCurrentPomodoroState();
            RefreshAll();
            OnPropertyChanged();
        }
    }

    public string TimerText =>
        $"{_remainingSeconds / 60:00}:{_remainingSeconds % 60:00}";

    public string CurrentModeText =>
        _isFocusMode ? "Focus Mode" : "Break Time";

    public string MotivationText =>
        _isFocusMode
            ? "One focused session at a time."
            : "Relax. You earned this break.";

    public string StartPauseButtonText =>
        _isRunning ? "Pause" : "Start";

    public string SkipButtonText =>
        _isFocusMode ? "Skip to Break" : "Skip to Focus";

    public PomodoroPage()
    {
        InitializeComponent();

        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);

        int savedFocusMinutes = Preferences.Get(FocusMinutesKey, 25);
        int savedBreakMinutes = Preferences.Get(BreakMinutesKey, 5);

        _focusMinutesText = savedFocusMinutes.ToString();
        _breakMinutesText = savedBreakMinutes.ToString();

        RestorePomodoroState();

        BindingContext = this;

        SetupTimer();

        if (_isRunning)
            _timer?.Start();

        RefreshAll();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        RestorePomodoroState();

        if (_isRunning)
            _timer?.Start();

        RefreshAll();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        SaveCurrentPomodoroState();
        _timer?.Stop();
    }

    private void SetupTimer()
    {
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);

        _timer.Tick += async (s, e) =>
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                SaveCurrentPomodoroState();
                OnPropertyChanged(nameof(TimerText));
                return;
            }

            _timer.Stop();
            _isRunning = false;

            await NotificationService.CancelPomodoroNotification();

            SaveCurrentPomodoroState();
            RefreshAll();

            if (_isFocusMode)
            {
                bool takeBreak = await DisplayAlertAsync(
                    "Focus completed",
                    "Great job. Do you want to take a break now?",
                    "Break",
                    "Continue");

                if (takeBreak)
                    StartBreak();
                else
                    StartFocus();
            }
            else
            {
                await DisplayAlertAsync(
                    "Break finished",
                    "Time to get back to work.",
                    "Start Focus");

                StartFocus();
            }
        };
    }

    private async void OnStartPauseClicked(object? sender, EventArgs e)
    {
        if (_isRunning)
        {
            _timer?.Stop();
            _isRunning = false;

            await NotificationService.CancelNotification(PomodoroNotificationId);
        }
        else
        {
            if (_remainingSeconds <= 0)
                _remainingSeconds = (_isFocusMode ? GetFocusMinutes() : GetBreakMinutes()) * 60;

            _timer?.Start();
            _isRunning = true;

            await SchedulePomodoroAlarmAsync();
        }

        SaveCurrentPomodoroState();
        RefreshAll();
    }

    private async void OnResetClicked(object? sender, EventArgs e)
    {
        _timer?.Stop();

        _isRunning = false;
        _isFocusMode = true;
        _remainingSeconds = GetFocusMinutes() * 60;

        await NotificationService.CancelNotification(PomodoroNotificationId);

        SaveCurrentPomodoroState();
        RefreshAll();
    }

    private async void OnSkipClicked(object? sender, EventArgs e)
    {
        await NotificationService.CancelNotification(PomodoroNotificationId);

        if (_isFocusMode)
            StartBreak();
        else
            StartFocus();
    }

    private void StartFocus()
    {
        _timer?.Stop();

        _isRunning = false;
        _isFocusMode = true;
        _remainingSeconds = GetFocusMinutes() * 60;

        SaveCurrentPomodoroState();
        RefreshAll();
    }

    private void StartBreak()
    {
        _timer?.Stop();

        _isRunning = false;
        _isFocusMode = false;
        _remainingSeconds = GetBreakMinutes() * 60;

        SaveCurrentPomodoroState();
        RefreshAll();
    }

    private async Task SchedulePomodoroAlarmAsync()
    {
        DateTime finishTime = DateTime.Now.AddSeconds(_remainingSeconds);

        await NotificationService.SchedulePomodoroNotification(
            finishTime,
            _isFocusMode);
    }

    private void SaveCurrentPomodoroState()
    {
        Preferences.Set(IsRunningKey, _isRunning);
        Preferences.Set(IsFocusModeKey, _isFocusMode);
        Preferences.Set(RemainingSecondsKey, _remainingSeconds);
        Preferences.Set(LastUpdatedKey, DateTime.Now.ToString("O"));
    }

    private void RestorePomodoroState()
    {
        _isRunning = Preferences.Get(IsRunningKey, false);
        _isFocusMode = Preferences.Get(IsFocusModeKey, true);

        int defaultSeconds = GetFocusMinutes() * 60;
        _remainingSeconds = Preferences.Get(RemainingSecondsKey, defaultSeconds);

        string lastUpdatedText = Preferences.Get(LastUpdatedKey, DateTime.Now.ToString("O"));

        if (_isRunning && DateTime.TryParse(lastUpdatedText, out DateTime lastUpdated))
        {
            int elapsedSeconds = (int)(DateTime.Now - lastUpdated).TotalSeconds;

            if (elapsedSeconds > 0)
                _remainingSeconds -= elapsedSeconds;

            if (_remainingSeconds <= 0)
            {
                _remainingSeconds = 0;
                _isRunning = false;
            }
        }

        SaveCurrentPomodoroState();
    }

    private int GetFocusMinutes()
    {
        if (int.TryParse(_focusMinutesText, out int minutes) && minutes > 0)
            return minutes;

        return 25;
    }

    private int GetBreakMinutes()
    {
        if (int.TryParse(_breakMinutesText, out int minutes) && minutes > 0)
            return minutes;

        return 5;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        SaveCurrentPomodoroState();
        await Navigation.PopAsync();
    }

    private void RefreshAll()
    {
        OnPropertyChanged(nameof(TimerText));
        OnPropertyChanged(nameof(CurrentModeText));
        OnPropertyChanged(nameof(MotivationText));
        OnPropertyChanged(nameof(StartPauseButtonText));
        OnPropertyChanged(nameof(SkipButtonText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}