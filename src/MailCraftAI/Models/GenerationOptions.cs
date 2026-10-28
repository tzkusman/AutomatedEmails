namespace MailCraftAI.Models;

public class GenerationOptions
{
    public ToneOption Tone { get; set; } = ToneOption.FriendlyProfessional;
    public ReplyLengthOption ReplyLength { get; set; } = ReplyLengthOption.Medium;
    public string Language { get; set; } = "Auto-detect";
    public string CustomInstruction { get; set; } = string.Empty;
    public string? PersonaId { get; set; }
    public bool MaskPii { get; set; } = true;
    public QuickActionType? QuickAction { get; set; }
    public string? ExistingDraftToRefine { get; set; }
}
