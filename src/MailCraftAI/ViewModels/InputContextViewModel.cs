using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MailCraftAI.Models;
using MailCraftAI.Services.Context;
using MailCraftAI.Services.Outlook;
using Microsoft.Win32;

namespace MailCraftAI.ViewModels;

public partial class InputContextViewModel : ObservableObject
{
    private readonly IOutlookService _outlookService;
    private readonly IDocumentExtractorService _documentExtractor;
    private readonly IContextTrimmerService _contextTrimmer;

    [ObservableProperty]
    private InputSourceType _selectedSourceType = InputSourceType.Outlook;

    [ObservableProperty]
    private EmailContext _currentContext = new();

    [ObservableProperty]
    private string _pastedText = string.Empty;

    [ObservableProperty]
    private string _customInstruction = string.Empty;

    [ObservableProperty]
    private ToneOption _selectedTone = ToneOption.FriendlyProfessional;

    [ObservableProperty]
    private ReplyLengthOption _selectedLength = ReplyLengthOption.Medium;

    [ObservableProperty]
    private string _selectedLanguage = "Auto-detect";

    [ObservableProperty]
    private PersonaTemplate? _selectedPersona;

    [ObservableProperty]
    private bool _maskPii = true;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _contextWarning;

    [ObservableProperty]
    private string _contextStatsText = "0 chars (~0 tokens)";

    public ObservableCollection<PersonaTemplate> Personas { get; } = [];

    public ObservableCollection<string> AvailableLanguages { get; } =
    [
        "Auto-detect",
        "English",
        "Spanish",
        "French",
        "German",
        "Italian",
        "Portuguese",
        "Dutch",
        "Chinese (Simplified)",
        "Japanese"
    ];

    public event EventHandler? ContextChanged;

    public InputContextViewModel(
        IOutlookService outlookService,
        IDocumentExtractorService documentExtractor,
        IContextTrimmerService contextTrimmer)
    {
        _outlookService = outlookService;
        _documentExtractor = documentExtractor;
        _contextTrimmer = contextTrimmer;
    }

    partial void OnPastedTextChanged(string value)
    {
        if (SelectedSourceType == InputSourceType.PastedText)
        {
            CurrentContext = new EmailContext
            {
                SourceType = InputSourceType.PastedText,
                Subject = ExtractSubjectFromText(value),
                BodyText = value
            };
            UpdateContextStats();
            ContextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnCurrentContextChanged(EmailContext value)
    {
        UpdateContextStats();
        ContextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateContextStats()
    {
        string text = CurrentContext.BodyText ?? string.Empty;
        var trimResult = _contextTrimmer.ProcessContext(text);
        ContextStatsText = $"{trimResult.OriginalCharCount:N0} chars (~{trimResult.EstimatedTokens:N0} tokens)";
        ContextWarning = trimResult.WarningMessage;
    }

    [RelayCommand]
    public async Task PullFromOutlookAsync()
    {
        IsLoading = true;
        try
        {
            var email = await _outlookService.GetCurrentSelectedEmailAsync();
            if (email != null)
            {
                CurrentContext = email;
                SelectedSourceType = InputSourceType.Outlook;
            }
            else
            {
                ContextWarning = "No email is currently selected in classic Outlook. Select an email and try again.";
            }
        }
        catch (Exception ex)
        {
            ContextWarning = $"Failed to pull from Outlook: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task BrowseFileAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Import Email or Context Document",
            Filter = "All Supported Files|*.msg;*.eml;*.pdf;*.docx;*.txt;*.md|Outlook Messages (*.msg)|*.msg|Email Files (*.eml)|*.eml|PDF Documents (*.pdf)|*.pdf|Word Documents (*.docx)|*.docx|Text Files (*.txt;*.md)|*.txt;*.md"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            await LoadFileContextAsync(openFileDialog.FileName);
        }
    }

    public async Task LoadFileContextAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;

        IsLoading = true;
        try
        {
            var context = await _documentExtractor.ExtractFromFileAsync(filePath);
            CurrentContext = context;
            SelectedSourceType = InputSourceType.FileAttachment;
        }
        catch (Exception ex)
        {
            ContextWarning = $"Could not load file context: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ApplyQuickChip(string chipText)
    {
        if (string.IsNullOrWhiteSpace(CustomInstruction))
        {
            CustomInstruction = chipText;
        }
        else if (!CustomInstruction.Contains(chipText, StringComparison.OrdinalIgnoreCase))
        {
            CustomInstruction += $", and {chipText.ToLowerInvariant()}";
        }
    }

    [RelayCommand]
    public void ClearContext()
    {
        CurrentContext = new EmailContext();
        PastedText = string.Empty;
        CustomInstruction = string.Empty;
        ContextWarning = null;
        UpdateContextStats();
    }

    private static string ExtractSubjectFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "(Pasted Context)";
        var firstLine = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (firstLine.Length > 0)
        {
            string line = firstLine[0].Trim();
            if (line.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
            {
                return line[8..].Trim();
            }
            return line.Length > 60 ? line[..60] + "..." : line;
        }
        return "(Pasted Context)";
    }

    public GenerationOptions BuildGenerationOptions()
    {
        return new GenerationOptions
        {
            Tone = SelectedTone,
            ReplyLength = SelectedLength,
            Language = SelectedLanguage,
            CustomInstruction = CustomInstruction,
            PersonaId = SelectedPersona?.Id,
            MaskPii = MaskPii
        };
    }
}
