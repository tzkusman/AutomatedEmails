using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MailCraftAI.Models;
using MailCraftAI.Services.AI;
using MailCraftAI.Services.Context;
using MailCraftAI.Services.History;
using MailCraftAI.Services.Outlook;
using MailCraftAI.Services.Prompt;
using MailCraftAI.Services.Security;
using MailCraftAI.Services.Settings;

namespace MailCraftAI.ViewModels;

public partial class ReplyEditorViewModel : ObservableObject
{
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly IPiiMaskingService _piiMasker;
    private readonly IContextTrimmerService _contextTrimmer;
    private readonly IOutlookService _outlookService;
    private readonly IHistoryService _historyService;
    private readonly ISettingsService _settingsService;
    private readonly ISecureStorageService _secureStorage;

    private CancellationTokenSource? _generationCts;

    [ObservableProperty]
    private string _replyText = string.Empty;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private string _statusMessage = "Ready to generate reply.";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _canCreateOutlookReply;

    public Func<EmailContext>? GetCurrentContextFunc { get; set; }
    public Func<GenerationOptions>? GetCurrentOptionsFunc { get; set; }
    public Func<PersonaTemplate?>? GetCurrentPersonaFunc { get; set; }

    public ReplyEditorViewModel(
        IAiProviderFactory aiProviderFactory,
        IPromptBuilderService promptBuilder,
        IPiiMaskingService piiMasker,
        IContextTrimmerService contextTrimmer,
        IOutlookService outlookService,
        IHistoryService historyService,
        ISettingsService settingsService,
        ISecureStorageService secureStorage)
    {
        _aiProviderFactory = aiProviderFactory;
        _promptBuilder = promptBuilder;
        _piiMasker = piiMasker;
        _contextTrimmer = contextTrimmer;
        _outlookService = outlookService;
        _historyService = historyService;
        _settingsService = settingsService;
        _secureStorage = secureStorage;
    }

    [RelayCommand]
    public async Task GenerateReplyAsync()
    {
        if (IsGenerating) return;

        var context = GetCurrentContextFunc?.Invoke();
        if (context == null || (string.IsNullOrWhiteSpace(context.BodyText) && string.IsNullOrWhiteSpace(context.Subject)))
        {
            ErrorMessage = "Please provide email context (Pull from Outlook, paste text, or drop a file) before generating.";
            return;
        }

        var options = GetCurrentOptionsFunc?.Invoke() ?? new GenerationOptions();
        var persona = GetCurrentPersonaFunc?.Invoke();

        await ExecuteAiWorkflowAsync(context, options, persona);
    }

    private async Task ExecuteAiWorkflowAsync(
        EmailContext context,
        GenerationOptions options,
        PersonaTemplate? persona)
    {
        ErrorMessage = null;
        IsGenerating = true;
        StatusMessage = "Connecting to AI provider...";
        ReplyText = string.Empty;

        _generationCts = new CancellationTokenSource();
        var ct = _generationCts.Token;

        var sw = Stopwatch.StartNew();

        try
        {
            var settings = await _settingsService.LoadSettingsAsync();
            var providerType = settings.ActiveProvider;
            var provider = _aiProviderFactory.GetProvider(providerType);

            string? apiKey = await _secureStorage.GetApiKeyAsync(providerType);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ErrorMessage = $"No API key found for {provider.DisplayName}. Please open Settings (gear icon) and enter your API key.";
                return;
            }

            string modelId = providerType switch
            {
                AiProviderType.AnthropicClaude => settings.SelectedModelClaude,
                AiProviderType.OpenAi => settings.SelectedModelOpenAi,
                AiProviderType.GoogleGemini => settings.SelectedModelGemini,
                _ => ""
            };

            // Optional PII masking
            EmailContext effectiveContext = context;
            PiiMaskResult? maskResult = null;

            if (options.MaskPii)
            {
                maskResult = _piiMasker.Mask(context.BodyText);
                effectiveContext = new EmailContext
                {
                    SourceType = context.SourceType,
                    Subject = context.Subject,
                    SenderName = context.SenderName,
                    SenderEmail = context.SenderEmail,
                    RecipientsTo = context.RecipientsTo,
                    RecipientsCc = context.RecipientsCc,
                    ReceivedTime = context.ReceivedTime,
                    BodyText = maskResult.MaskedText,
                    AttachmentNames = context.AttachmentNames,
                    OutlookEntryId = context.OutlookEntryId,
                    OutlookStoreId = context.OutlookStoreId
                };
            }

            // Auto-trim if configured
            var trimResult = _contextTrimmer.ProcessContext(
                effectiveContext.BodyText,
                settings.MaxContextLengthWarningChars,
                settings.AutoTrimLongContext);

            effectiveContext.BodyText = trimResult.ProcessedText;

            // Build prompts
            var built = _promptBuilder.BuildGenerationPrompts(
                effectiveContext,
                options,
                settings.Profile,
                persona);

            var request = new AiGenerationRequest
            {
                ModelId = modelId,
                ApiKey = apiKey,
                SystemPrompt = built.SystemPrompt,
                UserPrompt = built.UserPrompt,
                CustomEndpoint = settings.CustomOpenAiEndpoint
            };

            StatusMessage = $"Streaming draft from {provider.DisplayName} ({modelId})...";

            var sb = new StringBuilder();
            await foreach (var chunk in provider.StreamReplyAsync(request, ct))
            {
                sb.Append(chunk);
                ReplyText = sb.ToString();
            }

            string finalReply = sb.ToString();

            // Restore masked PII if applicable
            if (options.MaskPii && maskResult != null && maskResult.ItemsMaskedCount > 0)
            {
                finalReply = _piiMasker.Unmask(finalReply, maskResult.ReplacementMap);
                ReplyText = finalReply;
            }

            sw.Stop();
            int wordCount = finalReply.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            StatusMessage = $"Completed in {sw.Elapsed.TotalSeconds:F1}s (~{wordCount} words) via {provider.DisplayName}.";

            CanCreateOutlookReply = context.IsFromOutlook;

            // Persist in local SQLite history
            await _historyService.AddRecordAsync(new HistoryRecord
            {
                SourceType = context.SourceType.ToString(),
                OriginalSubject = context.Subject,
                OriginalSender = context.FormattedSender,
                OriginalSummary = context.SummaryText,
                UserInstruction = options.CustomInstruction,
                Tone = options.Tone.ToString(),
                ReplyLength = options.ReplyLength.ToString(),
                Language = options.Language,
                ProviderName = provider.DisplayName,
                ModelName = modelId,
                GeneratedSubject = context.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? context.Subject : $"Re: {context.Subject}",
                GeneratedReply = finalReply
            });
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Generation cancelled by user.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            StatusMessage = "Generation failed.";
        }
        finally
        {
            IsGenerating = false;
            _generationCts = null;
        }
    }

    [RelayCommand]
    public void CancelGeneration()
    {
        _generationCts?.Cancel();
    }

    [RelayCommand]
    public async Task ApplyQuickActionAsync(string actionName)
    {
        if (string.IsNullOrWhiteSpace(ReplyText) || IsGenerating) return;

        if (!Enum.TryParse<QuickActionType>(actionName, true, out var action))
        {
            return;
        }

        ErrorMessage = null;
        IsGenerating = true;
        StatusMessage = $"Applying quick action: {actionName}...";

        _generationCts = new CancellationTokenSource();
        var ct = _generationCts.Token;

        try
        {
            var settings = await _settingsService.LoadSettingsAsync();
            var provider = _aiProviderFactory.GetProvider(settings.ActiveProvider);
            string? apiKey = await _secureStorage.GetApiKeyAsync(settings.ActiveProvider);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ErrorMessage = "API key not found. Please check Settings.";
                return;
            }

            string modelId = settings.ActiveProvider switch
            {
                AiProviderType.AnthropicClaude => settings.SelectedModelClaude,
                AiProviderType.OpenAi => settings.SelectedModelOpenAi,
                AiProviderType.GoogleGemini => settings.SelectedModelGemini,
                _ => ""
            };

            var built = _promptBuilder.BuildQuickActionPrompts(
                ReplyText,
                action,
                "English",
                settings.Profile);

            var request = new AiGenerationRequest
            {
                ModelId = modelId,
                ApiKey = apiKey,
                SystemPrompt = built.SystemPrompt,
                UserPrompt = built.UserPrompt,
                CustomEndpoint = settings.CustomOpenAiEndpoint
            };

            var sb = new StringBuilder();
            await foreach (var chunk in provider.StreamReplyAsync(request, ct))
            {
                sb.Append(chunk);
                ReplyText = sb.ToString();
            }

            StatusMessage = $"Applied {actionName} successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Quick action failed: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
            _generationCts = null;
        }
    }

    [RelayCommand]
    public void CopyReply()
    {
        if (!string.IsNullOrWhiteSpace(ReplyText))
        {
            Clipboard.SetText(ReplyText);
            StatusMessage = "Reply copied to clipboard.";
        }
    }

    [RelayCommand]
    public async Task CreateOutlookReplyDraftAsync(string mode)
    {
        if (string.IsNullOrWhiteSpace(ReplyText)) return;

        bool replyAll = string.Equals(mode, "all", StringComparison.OrdinalIgnoreCase);
        var context = GetCurrentContextFunc?.Invoke() ?? new EmailContext();

        StatusMessage = "Creating Outlook reply draft...";
        bool success = await _outlookService.CreateReplyDraftAsync(context, ReplyText, replyAll);

        StatusMessage = success
            ? "Outlook draft created and displayed for your review."
            : "Could not create reply draft in Outlook. Ensure Outlook is running and email is selected.";
    }

    [RelayCommand]
    public async Task CreateNewEmailDraftAsync()
    {
        if (string.IsNullOrWhiteSpace(ReplyText)) return;

        var context = GetCurrentContextFunc?.Invoke() ?? new EmailContext();
        string subject = !string.IsNullOrWhiteSpace(context.Subject) ? context.Subject : "MailCraft Draft";

        StatusMessage = "Creating new Outlook email draft...";
        bool success = await _outlookService.CreateNewEmailDraftAsync(subject, ReplyText, context.SenderEmail);

        StatusMessage = success
            ? "New Outlook email draft created and displayed."
            : "Could not create new email in Outlook. Check Outlook connection.";
    }
}
