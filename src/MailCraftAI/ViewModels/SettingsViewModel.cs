using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MailCraftAI.Models;
using MailCraftAI.Services.AI;
using MailCraftAI.Services.Security;
using MailCraftAI.Services.Settings;

namespace MailCraftAI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ISecureStorageService _secureStorage;
    private readonly IAiProviderFactory _aiProviderFactory;

    [ObservableProperty]
    private AiProviderType _activeProvider = AiProviderType.AnthropicClaude;

    // Keys
    [ObservableProperty]
    private string _anthropicApiKey = string.Empty;

    [ObservableProperty]
    private string _openAiApiKey = string.Empty;

    [ObservableProperty]
    private string _geminiApiKey = string.Empty;

    // Models
    [ObservableProperty]
    private string _selectedModelClaude = "claude-3-7-sonnet-20250219";

    [ObservableProperty]
    private string _selectedModelOpenAi = "gpt-4o";

    [ObservableProperty]
    private string _selectedModelGemini = "gemini-2.0-flash";

    [ObservableProperty]
    private string _customOpenAiEndpoint = string.Empty;

    // Validation Status
    [ObservableProperty]
    private string? _validationMessage;

    [ObservableProperty]
    private bool _isValidating;

    [ObservableProperty]
    private bool? _lastValidationSuccess;

    // Profile
    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _jobTitle = string.Empty;

    [ObservableProperty]
    private string _companyName = string.Empty;

    [ObservableProperty]
    private string _companyDescription = string.Empty;

    [ObservableProperty]
    private string _defaultSignOff = "Best regards,";

    [ObservableProperty]
    private string _standardPhrases = string.Empty;

    [ObservableProperty]
    private string _additionalInstructions = string.Empty;

    // Preferences
    [ObservableProperty]
    private bool _maskPiiByDefault = true;

    [ObservableProperty]
    private int _maxContextLengthWarningChars = 12000;

    [ObservableProperty]
    private bool _autoTrimLongContext = true;

    [ObservableProperty]
    private ThemeModeOption _themeMode = ThemeModeOption.Dark;

    [ObservableProperty]
    private bool _enableGlobalHotkey = true;

    public ObservableCollection<AiModelInfo> ClaudeModels { get; } = [];
    public ObservableCollection<AiModelInfo> OpenAiModels { get; } = [];
    public ObservableCollection<AiModelInfo> GeminiModels { get; } = [];
    public ObservableCollection<PersonaTemplate> Personas { get; } = [];

    public SettingsViewModel(
        ISettingsService settingsService,
        ISecureStorageService secureStorage,
        IAiProviderFactory aiProviderFactory)
    {
        _settingsService = settingsService;
        _secureStorage = secureStorage;
        _aiProviderFactory = aiProviderFactory;

        PopulateModelLists();
    }

    private void PopulateModelLists()
    {
        foreach (var m in _aiProviderFactory.GetProvider(AiProviderType.AnthropicClaude).AvailableModels)
            ClaudeModels.Add(m);

        foreach (var m in _aiProviderFactory.GetProvider(AiProviderType.OpenAi).AvailableModels)
            OpenAiModels.Add(m);

        foreach (var m in _aiProviderFactory.GetProvider(AiProviderType.GoogleGemini).AvailableModels)
            GeminiModels.Add(m);
    }

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();

        ActiveProvider = settings.ActiveProvider;
        SelectedModelClaude = settings.SelectedModelClaude;
        SelectedModelOpenAi = settings.SelectedModelOpenAi;
        SelectedModelGemini = settings.SelectedModelGemini;
        CustomOpenAiEndpoint = settings.CustomOpenAiEndpoint;

        FullName = settings.Profile.FullName;
        JobTitle = settings.Profile.JobTitle;
        CompanyName = settings.Profile.CompanyName;
        CompanyDescription = settings.Profile.CompanyDescription;
        DefaultSignOff = settings.Profile.DefaultSignOff;
        StandardPhrases = settings.Profile.StandardPhrases;
        AdditionalInstructions = settings.Profile.AdditionalInstructions;

        MaskPiiByDefault = settings.MaskPiiByDefault;
        MaxContextLengthWarningChars = settings.MaxContextLengthWarningChars;
        AutoTrimLongContext = settings.AutoTrimLongContext;
        ThemeMode = settings.ThemeMode;
        EnableGlobalHotkey = settings.EnableGlobalHotkey;

        Personas.Clear();
        foreach (var p in settings.CustomPersonas)
        {
            Personas.Add(p);
        }

        // Load securely encrypted keys from DPAPI
        AnthropicApiKey = await _secureStorage.GetApiKeyAsync(AiProviderType.AnthropicClaude) ?? "";
        OpenAiApiKey = await _secureStorage.GetApiKeyAsync(AiProviderType.OpenAi) ?? "";
        GeminiApiKey = await _secureStorage.GetApiKeyAsync(AiProviderType.GoogleGemini) ?? "";
    }

    [RelayCommand]
    public async Task ValidateCurrentKeyAsync()
    {
        IsValidating = true;
        ValidationMessage = "Validating key with provider...";
        LastValidationSuccess = null;

        try
        {
            var provider = _aiProviderFactory.GetProvider(ActiveProvider);
            string key = ActiveProvider switch
            {
                AiProviderType.AnthropicClaude => AnthropicApiKey,
                AiProviderType.OpenAi => OpenAiApiKey,
                AiProviderType.GoogleGemini => GeminiApiKey,
                _ => ""
            };

            string model = ActiveProvider switch
            {
                AiProviderType.AnthropicClaude => SelectedModelClaude,
                AiProviderType.OpenAi => SelectedModelOpenAi,
                AiProviderType.GoogleGemini => SelectedModelGemini,
                _ => ""
            };

            var result = await provider.GenerateReplyAsync(new AiGenerationRequest
            {
                ApiKey = key,
                ModelId = model,
                SystemPrompt = "Respond with one word: OK",
                UserPrompt = "Ping",
                MaxTokens = 5,
                CustomEndpoint = ActiveProvider == AiProviderType.OpenAi ? CustomOpenAiEndpoint : null
            });

            LastValidationSuccess = result.Success;
            ValidationMessage = result.Success
                ? $"Key is valid! Successfully connected to {provider.DisplayName}."
                : $"Validation failed: {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            LastValidationSuccess = false;
            ValidationMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsValidating = false;
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();

        settings.ActiveProvider = ActiveProvider;
        settings.SelectedModelClaude = SelectedModelClaude;
        settings.SelectedModelOpenAi = SelectedModelOpenAi;
        settings.SelectedModelGemini = SelectedModelGemini;
        settings.CustomOpenAiEndpoint = CustomOpenAiEndpoint;

        settings.Profile = new UserBusinessProfile
        {
            FullName = FullName,
            JobTitle = JobTitle,
            CompanyName = CompanyName,
            CompanyDescription = CompanyDescription,
            DefaultSignOff = DefaultSignOff,
            StandardPhrases = StandardPhrases,
            AdditionalInstructions = AdditionalInstructions
        };

        settings.MaskPiiByDefault = MaskPiiByDefault;
        settings.MaxContextLengthWarningChars = MaxContextLengthWarningChars;
        settings.AutoTrimLongContext = AutoTrimLongContext;
        settings.ThemeMode = ThemeMode;
        settings.EnableGlobalHotkey = EnableGlobalHotkey;

        settings.CustomPersonas = [.. Personas];

        await _settingsService.SaveSettingsAsync(settings);

        // Store keys securely with DPAPI
        await _secureStorage.SaveApiKeyAsync(AiProviderType.AnthropicClaude, AnthropicApiKey);
        await _secureStorage.SaveApiKeyAsync(AiProviderType.OpenAi, OpenAiApiKey);
        await _secureStorage.SaveApiKeyAsync(AiProviderType.GoogleGemini, GeminiApiKey);
    }

    [RelayCommand]
    public void AddPersona()
    {
        var newPersona = new PersonaTemplate
        {
            Name = "New Custom Persona",
            Description = "Custom persona instructions",
            SystemPromptAddition = "Provide clear and direct answers.",
            DefaultTone = ToneOption.FriendlyProfessional,
            DefaultLength = ReplyLengthOption.Medium,
            IsBuiltIn = false
        };
        Personas.Add(newPersona);
    }

    [RelayCommand]
    public void DeletePersona(PersonaTemplate persona)
    {
        if (persona != null && !persona.IsBuiltIn)
        {
            Personas.Remove(persona);
        }
    }
}
