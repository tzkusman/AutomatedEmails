namespace MailCraftAI.Models;

public enum ToneOption
{
    Formal,
    FriendlyProfessional,
    Firm,
    Apologetic,
    ShortAndDirect
}

public enum ReplyLengthOption
{
    Short,
    Medium,
    Detailed
}

public enum InputSourceType
{
    Outlook,
    PastedText,
    FileAttachment
}

public enum AiProviderType
{
    AnthropicClaude,
    OpenAi,
    GoogleGemini
}

public enum OutlookStatusType
{
    Connected,
    NotRunning,
    NotInstalled,
    ElevationMismatch,
    Error
}

public enum ThemeModeOption
{
    Dark,
    Light,
    System
}

public enum QuickActionType
{
    Shorten,
    MakeMoreFormal,
    MakeFriendlier,
    FixGrammar,
    Translate
}
