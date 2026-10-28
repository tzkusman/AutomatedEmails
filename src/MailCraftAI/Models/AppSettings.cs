namespace MailCraftAI.Models;

public class AiModelInfo
{
    public string ModelId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRecommended { get; set; }
}

public class UserBusinessProfile
{
    public string FullName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyDescription { get; set; } = string.Empty;
    public string DefaultSignOff { get; set; } = "Best regards,";
    public string StandardPhrases { get; set; } = string.Empty;
    public string AdditionalInstructions { get; set; } = string.Empty;

    public bool HasContent => !string.IsNullOrWhiteSpace(FullName) ||
                              !string.IsNullOrWhiteSpace(CompanyName) ||
                              !string.IsNullOrWhiteSpace(JobTitle);
}

public class AppSettings
{
    public AiProviderType ActiveProvider { get; set; } = AiProviderType.AnthropicClaude;
    public string SelectedModelClaude { get; set; } = "claude-3-7-sonnet-20250219";
    public string SelectedModelOpenAi { get; set; } = "gpt-4o";
    public string SelectedModelGemini { get; set; } = "gemini-2.5-flash";

    public string CustomOpenAiEndpoint { get; set; } = string.Empty;

    public UserBusinessProfile Profile { get; set; } = new();
    public List<PersonaTemplate> CustomPersonas { get; set; } = [];

    public bool MaskPiiByDefault { get; set; } = true;
    public int MaxContextLengthWarningChars { get; set; } = 12000;
    public bool AutoTrimLongContext { get; set; } = true;

    public ThemeModeOption ThemeMode { get; set; } = ThemeModeOption.Light;
    public bool EnableGlobalHotkey { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
}
