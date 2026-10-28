using MailCraftAI.Models;

namespace MailCraftAI.Services.Prompt;

public record BuiltPrompts(
    string SystemPrompt,
    string UserPrompt
);

public interface IPromptBuilderService
{
    BuiltPrompts BuildGenerationPrompts(
        EmailContext emailContext,
        GenerationOptions options,
        UserBusinessProfile profile,
        PersonaTemplate? persona);

    BuiltPrompts BuildQuickActionPrompts(
        string currentReplyDraft,
        QuickActionType action,
        string? targetLanguage,
        UserBusinessProfile profile);
}
