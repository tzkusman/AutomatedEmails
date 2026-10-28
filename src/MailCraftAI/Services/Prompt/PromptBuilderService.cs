using System;
using System.Text;
using MailCraftAI.Models;

namespace MailCraftAI.Services.Prompt;

public class PromptBuilderService : IPromptBuilderService
{
    public BuiltPrompts BuildGenerationPrompts(
        EmailContext emailContext,
        GenerationOptions options,
        UserBusinessProfile profile,
        PersonaTemplate? persona)
    {
        var sys = new StringBuilder();

        // 1. Core Persona & Grounding
        sys.AppendLine("You are MailCraft AI, an executive-grade email drafting assistant. Your job is to draft a polished, professional, and clear reply to the incoming email.");
        sys.AppendLine("CRITICAL RULES:");
        sys.AppendLine("1. Return ONLY the final email text ready to send. Do NOT include markdown code fences, meta-commentary, or introductory remarks like 'Here is your draft:'.");
        sys.AppendLine("2. Answer every specific question, inquiry, or actionable point raised in the incoming email context.");
        sys.AppendLine("3. NEVER invent, fabricate, or hallucinate facts, dates, prices, delivery guarantees, or commitments not present in the context. If key information is missing, insert bracketed placeholders like [proposed date], [quoted price], [project manager name].");
        sys.AppendLine("4. Match the formality, nuance, and professional relationship established with the sender, unless specifically overridden by the requested tone.");
        sys.AppendLine("5. Maintain standard professional email formatting with an appropriate greeting, well-spaced paragraphs, and an appropriate closing sign-off.");

        // 2. Language Directive
        if (!string.IsNullOrWhiteSpace(options.Language) && !options.Language.Equals("Auto-detect", StringComparison.OrdinalIgnoreCase))
        {
            sys.AppendLine($"6. Reply strictly in the following language: {options.Language}.");
        }
        else
        {
            sys.AppendLine("6. Reply in the same language as the incoming email (auto-detected).");
        }

        // 3. Tone Directive
        sys.AppendLine();
        sys.AppendLine($"REQUESTED TONE: {GetToneDescription(options.Tone)}");

        // 4. Length Directive
        sys.AppendLine($"REQUESTED LENGTH: {GetLengthDescription(options.ReplyLength)}");

        // 5. Persona Addition
        if (persona != null && !string.IsNullOrWhiteSpace(persona.SystemPromptAddition))
        {
            sys.AppendLine();
            sys.AppendLine($"ROLE / PERSONA GUIDANCE ({persona.Name}):");
            sys.AppendLine(persona.SystemPromptAddition.Trim());
        }

        // 6. User Profile / Business Context
        if (profile.HasContent)
        {
            sys.AppendLine();
            sys.AppendLine("SENDER BUSINESS PROFILE & IDENTITY:");
            if (!string.IsNullOrWhiteSpace(profile.FullName)) sys.AppendLine($"- Name: {profile.FullName}");
            if (!string.IsNullOrWhiteSpace(profile.JobTitle)) sys.AppendLine($"- Title: {profile.JobTitle}");
            if (!string.IsNullOrWhiteSpace(profile.CompanyName)) sys.AppendLine($"- Company: {profile.CompanyName}");
            if (!string.IsNullOrWhiteSpace(profile.CompanyDescription)) sys.AppendLine($"- Company Context: {profile.CompanyDescription}");
            if (!string.IsNullOrWhiteSpace(profile.DefaultSignOff)) sys.AppendLine($"- Default Sign-Off: {profile.DefaultSignOff}");
            if (!string.IsNullOrWhiteSpace(profile.StandardPhrases)) sys.AppendLine($"- Standard Phrases to incorporate when suitable: {profile.StandardPhrases}");
            if (!string.IsNullOrWhiteSpace(profile.AdditionalInstructions)) sys.AppendLine($"- Style Instructions: {profile.AdditionalInstructions}");
        }

        // Build User Prompt
        var user = new StringBuilder();
        user.AppendLine("=== INCOMING EMAIL CONTEXT ===");
        if (!string.IsNullOrWhiteSpace(emailContext.Subject)) user.AppendLine($"Subject: {emailContext.Subject}");
        if (!string.IsNullOrWhiteSpace(emailContext.SenderName) || !string.IsNullOrWhiteSpace(emailContext.SenderEmail))
            user.AppendLine($"From: {emailContext.FormattedSender}");
        if (emailContext.ReceivedTime.HasValue) user.AppendLine($"Date: {emailContext.ReceivedTime.Value:f}");
        if (emailContext.RecipientsTo.Count > 0) user.AppendLine($"To: {string.Join(", ", emailContext.RecipientsTo)}");
        if (emailContext.RecipientsCc.Count > 0) user.AppendLine($"CC: {string.Join(", ", emailContext.RecipientsCc)}");
        if (emailContext.AttachmentNames.Count > 0) user.AppendLine($"Attachments Present: {string.Join(", ", emailContext.AttachmentNames)}");

        user.AppendLine();
        user.AppendLine("EMAIL BODY / CONTENT:");
        user.AppendLine(string.IsNullOrWhiteSpace(emailContext.BodyText) ? "(No text content)" : emailContext.BodyText.Trim());

        if (!string.IsNullOrWhiteSpace(options.CustomInstruction))
        {
            user.AppendLine();
            user.AppendLine("=== SPECIFIC USER INSTRUCTION FOR THIS REPLY ===");
            user.AppendLine(options.CustomInstruction.Trim());
        }

        user.AppendLine();
        user.AppendLine("Draft the professional reply now:");

        return new BuiltPrompts(sys.ToString(), user.ToString());
    }

    public BuiltPrompts BuildQuickActionPrompts(
        string currentReplyDraft,
        QuickActionType action,
        string? targetLanguage,
        UserBusinessProfile profile)
    {
        var sys = new StringBuilder();
        sys.AppendLine("You are MailCraft AI, an email editing assistant.");
        sys.AppendLine("Rewrite the provided email draft according to the requested quick action.");
        sys.AppendLine("Maintain all critical dates, names, commitments, and bracketed placeholders.");
        sys.AppendLine("Output ONLY the revised email text with no commentary or formatting fences.");

        string actionDescription = action switch
        {
            QuickActionType.Shorten => "Condense the email to be brief, direct, and high-impact while retaining all essential decisions and information.",
            QuickActionType.MakeMoreFormal => "Refine the email to adopt a more formal, polished, executive corporate tone with sophisticated vocabulary.",
            QuickActionType.MakeFriendlier => "Make the email warmer, more approachable, and conversational while remaining completely professional.",
            QuickActionType.FixGrammar => "Correct any grammatical mistakes, awkward phrasing, punctuation errors, or typos, making the text flow naturally.",
            QuickActionType.Translate => $"Translate the entire email accurately into {targetLanguage ?? "English"}, ensuring natural business idioms.",
            _ => "Refine and polish the email draft."
        };

        sys.AppendLine($"ACTION: {actionDescription}");

        if (profile.HasContent && !string.IsNullOrWhiteSpace(profile.DefaultSignOff))
        {
            sys.AppendLine($"Ensure sign-off ends with: {profile.DefaultSignOff} {profile.FullName}".Trim());
        }

        var user = new StringBuilder();
        user.AppendLine("=== CURRENT EMAIL DRAFT ===");
        user.AppendLine(currentReplyDraft);
        user.AppendLine();
        user.AppendLine("Please provide the revised draft now:");

        return new BuiltPrompts(sys.ToString(), user.ToString());
    }

    private static string GetToneDescription(ToneOption tone) => tone switch
    {
        ToneOption.Formal => "Formal, elegant, and strictly professional. Suitable for legal, executive, or institutional communications.",
        ToneOption.FriendlyProfessional => "Warm, courteous, and approachable while maintaining professional credibility and competence.",
        ToneOption.Firm => "Decisive, assertive, clear, and unambiguous. Focused on boundaries, deadlines, and agreed terms.",
        ToneOption.Apologetic => "Sincere, empathetic, acknowledging the issue directly, and proposing an immediate resolution or reassurance.",
        ToneOption.ShortAndDirect => "Ultra-concise, decision-focused, and bulleted where helpful. Zero pleasantries or filler.",
        _ => "Professional and clear."
    };

    private static string GetLengthDescription(ReplyLengthOption length) => length switch
    {
        ReplyLengthOption.Short => "Short (1-2 crisp paragraphs or focused bullet points).",
        ReplyLengthOption.Medium => "Medium (2-4 well-structured paragraphs providing complete context).",
        ReplyLengthOption.Detailed => "Detailed (thorough, comprehensive explanation addressing every element with organized sub-points).",
        _ => "Balanced standard length."
    };
}
