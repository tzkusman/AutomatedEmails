using System;

namespace MailCraftAI.Models;

public class PersonaTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SystemPromptAddition { get; set; } = string.Empty;
    public ToneOption DefaultTone { get; set; } = ToneOption.FriendlyProfessional;
    public ReplyLengthOption DefaultLength { get; set; } = ReplyLengthOption.Medium;
    public bool IsBuiltIn { get; set; }

    public static PersonaTemplate CreateGeneral() => new()
    {
        Id = "general",
        Name = "General Professional",
        Description = "Balanced, professional and clear everyday communication",
        SystemPromptAddition = "Maintain a courteous, clear, and proactive professional attitude.",
        DefaultTone = ToneOption.FriendlyProfessional,
        DefaultLength = ReplyLengthOption.Medium,
        IsBuiltIn = true
    };

    public static PersonaTemplate CreateCustomerSupport() => new()
    {
        Id = "support",
        Name = "Customer Support",
        Description = "Helpful, empathetic, solution-oriented resolution of issues",
        SystemPromptAddition = "Act as an empathetic and highly competent customer support specialist. Acknowledge customer frustration warmly, explain next steps clearly, and provide actionable solutions without unnecessary jargon.",
        DefaultTone = ToneOption.FriendlyProfessional,
        DefaultLength = ReplyLengthOption.Medium,
        IsBuiltIn = true
    };

    public static PersonaTemplate CreateSalesQuotation() => new()
    {
        Id = "sales",
        Name = "Sales & Quotation",
        Description = "Persuasive, value-focused, and consultative responses",
        SystemPromptAddition = "Act as a consultative sales executive. Highlight value, clarity, and return on investment. Always include a clear call-to-action (e.g. scheduling a discovery call or confirming proposal details) without being overly pushy.",
        DefaultTone = ToneOption.Formal,
        DefaultLength = ReplyLengthOption.Detailed,
        IsBuiltIn = true
    };

    public static PersonaTemplate CreateVendorFollowup() => new()
    {
        Id = "vendor",
        Name = "Vendor Follow-up",
        Description = "Concise, deadline-driven, and firm coordination with suppliers",
        SystemPromptAddition = "Act as a procurement and operations manager coordinating with external vendors. Focus on deliverables, milestone timelines, itemized questions, and delivery confirmation.",
        DefaultTone = ToneOption.Firm,
        DefaultLength = ReplyLengthOption.Short,
        IsBuiltIn = true
    };

    public static PersonaTemplate CreateExecutive() => new()
    {
        Id = "executive",
        Name = "Executive / Brief",
        Description = "Direct, high-impact, decision-ready brevity",
        SystemPromptAddition = "Write in the voice of a senior executive: direct, concise, zero fluff, clear decisions and action items bulleted for rapid reading.",
        DefaultTone = ToneOption.ShortAndDirect,
        DefaultLength = ReplyLengthOption.Short,
        IsBuiltIn = true
    };
}
