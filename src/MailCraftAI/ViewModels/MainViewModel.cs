using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MailCraftAI.Models;
using MailCraftAI.Services.AI;
using MailCraftAI.Services.Outlook;
using MailCraftAI.Services.Settings;
using MailCraftAI.Services.SystemIntegration;

namespace MailCraftAI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IOutlookService _outlookService;
    private readonly ISettingsService _settingsService;
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly IGlobalHotkeyService _hotkeyService;

    public InputContextViewModel InputVm { get; }
    public ReplyEditorViewModel EditorVm { get; }
    public SettingsViewModel SettingsVm { get; }
    public HistoryViewModel HistoryVm { get; }

    [ObservableProperty]
    private OutlookStatusResult _outlookStatus = new(OutlookStatusType.NotRunning, "Checking Outlook...", false);

    [ObservableProperty]
    private string _activeAiProviderBadge = "Claude 3.7 Sonnet";

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _isHistoryOpen;

    [ObservableProperty]
    private bool _isPrivacyNoticeOpen;

    public SolidColorBrush OutlookStatusBrush => OutlookStatus.Status switch
    {
        OutlookStatusType.Connected => new SolidColorBrush(Color.FromRgb(46, 204, 113)),       // Emerald Green
        OutlookStatusType.NotRunning => new SolidColorBrush(Color.FromRgb(243, 156, 18)),     // Warm Orange
        OutlookStatusType.ElevationMismatch => new SolidColorBrush(Color.FromRgb(231, 76, 60)), // Crimson Red
        _ => new SolidColorBrush(Color.FromRgb(149, 165, 166))                                 // Slate Gray
    };

    public MainViewModel(
        IOutlookService outlookService,
        ISettingsService settingsService,
        IAiProviderFactory aiProviderFactory,
        IGlobalHotkeyService hotkeyService,
        InputContextViewModel inputVm,
        ReplyEditorViewModel editorVm,
        SettingsViewModel settingsVm,
        HistoryViewModel historyVm)
    {
        _outlookService = outlookService;
        _settingsService = settingsService;
        _aiProviderFactory = aiProviderFactory;
        _hotkeyService = hotkeyService;

        InputVm = inputVm;
        EditorVm = editorVm;
        SettingsVm = settingsVm;
        HistoryVm = historyVm;

        // Connect Input to Editor
        EditorVm.GetCurrentContextFunc = () => InputVm.CurrentContext;
        EditorVm.GetCurrentOptionsFunc = () => InputVm.BuildGenerationOptions();
        EditorVm.GetCurrentPersonaFunc = () => InputVm.SelectedPersona;

        // Connect History reuse to editor
        HistoryVm.LoadRecordRequested += OnHistoryRecordReused;

        // Register Global Hotkey listener
        _hotkeyService.HotkeyPressed += OnGlobalHotkeyPressed;
    }

    public async Task InitializeAsync()
    {
        await RefreshOutlookStatusAsync();
        await RefreshActiveProviderBadgeAsync();
        await LoadPersonasIntoInputVmAsync();
    }

    [RelayCommand]
    public async Task RefreshOutlookStatusAsync()
    {
        OutlookStatus = await _outlookService.CheckStatusAsync();
        OnPropertyChanged(nameof(OutlookStatusBrush));
    }

    [RelayCommand]
    public async Task ConnectOrLaunchOutlookAsync()
    {
        await _outlookService.EnsureOutlookRunningAsync();
        await Task.Delay(1000);
        await RefreshOutlookStatusAsync();
    }

    public async Task RefreshActiveProviderBadgeAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();
        string model = settings.ActiveProvider switch
        {
            AiProviderType.AnthropicClaude => settings.SelectedModelClaude,
            AiProviderType.OpenAi => settings.SelectedModelOpenAi,
            AiProviderType.GoogleGemini => settings.SelectedModelGemini,
            _ => "AI"
        };

        var provider = _aiProviderFactory.GetProvider(settings.ActiveProvider);
        ActiveAiProviderBadge = $"{provider.DisplayName} ({model})";
    }

    public async Task LoadPersonasIntoInputVmAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();
        InputVm.Personas.Clear();
        foreach (var p in settings.CustomPersonas)
        {
            InputVm.Personas.Add(p);
        }

        if (InputVm.Personas.Count > 0 && InputVm.SelectedPersona == null)
        {
            InputVm.SelectedPersona = InputVm.Personas[0];
        }
    }

    private void OnHistoryRecordReused(HistoryRecord record)
    {
        EditorVm.ReplyText = record.GeneratedReply;
        InputVm.CurrentContext = new EmailContext
        {
            Subject = record.OriginalSubject,
            SenderName = record.OriginalSender,
            BodyText = record.OriginalSummary
        };
        IsHistoryOpen = false;
    }

    private async void OnGlobalHotkeyPressed(object? sender, EventArgs e)
    {
        // Bring MainWindow to foreground
        Application.Current.Dispatcher.Invoke(() =>
        {
            var win = Application.Current.MainWindow;
            if (win != null)
            {
                if (win.WindowState == WindowState.Minimized)
                {
                    win.WindowState = WindowState.Normal;
                }
                win.Activate();
                win.Focus();
            }
        });

        // Pull latest selected email from Outlook and trigger AI generation
        await InputVm.PullFromOutlookAsync();
        if (!string.IsNullOrWhiteSpace(InputVm.CurrentContext.BodyText) || !string.IsNullOrWhiteSpace(InputVm.CurrentContext.Subject))
        {
            await EditorVm.GenerateReplyAsync();
        }
    }

    [RelayCommand]
    public void OpenSettings()
    {
        _ = SettingsVm.InitializeAsync();
        IsSettingsOpen = true;
    }

    [RelayCommand]
    public async Task CloseSettingsAndSaveAsync()
    {
        await SettingsVm.SaveAsync();
        await RefreshActiveProviderBadgeAsync();
        await LoadPersonasIntoInputVmAsync();
        IsSettingsOpen = false;
    }

    [RelayCommand]
    public void OpenHistory()
    {
        _ = HistoryVm.RefreshAsync();
        IsHistoryOpen = true;
    }

    [RelayCommand]
    public void OpenPrivacyNotice()
    {
        IsPrivacyNoticeOpen = true;
    }
}
