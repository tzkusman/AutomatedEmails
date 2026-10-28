using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MailCraftAI.Models;
using MimeKit;
using MsgReader.Outlook;
using UglyToad.PdfPig;

namespace MailCraftAI.Services.Context;

public class DocumentExtractorService : IDocumentExtractorService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".msg", ".eml", ".pdf", ".docx", ".txt", ".md", ".csv"
    };

    public bool IsSupportedExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return false;
        if (!extension.StartsWith('.')) extension = "." + extension;
        return SupportedExtensions.Contains(extension);
    }

    public async Task<EmailContext> ExtractFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Context file not found.", filePath);
        }

        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".msg" => await Task.Run(() => ExtractMsg(filePath)),
            ".eml" => await Task.Run(() => ExtractEml(filePath)),
            ".pdf" => await Task.Run(() => ExtractPdf(filePath)),
            ".docx" => await Task.Run(() => ExtractDocx(filePath)),
            ".txt" or ".md" or ".csv" => await ExtractPlainTextAsync(filePath),
            _ => throw new NotSupportedException($"File format '{ext}' is not supported.")
        };
    }

    private static EmailContext ExtractMsg(string filePath)
    {
        using var msg = new Storage.Message(filePath);
        var context = new EmailContext
        {
            SourceType = InputSourceType.FileAttachment,
            SourceFilePath = filePath,
            Subject = msg.Subject ?? Path.GetFileNameWithoutExtension(filePath),
            BodyText = msg.BodyText ?? string.Empty,
            BodyHtml = msg.BodyHtml ?? string.Empty,
            ReceivedTime = msg.SentOn
        };

        if (msg.Sender != null)
        {
            context.SenderName = msg.Sender.DisplayName ?? string.Empty;
            context.SenderEmail = msg.Sender.Email ?? string.Empty;
        }

        if (msg.Recipients != null)
        {
            foreach (var r in msg.Recipients)
            {
                string display = !string.IsNullOrWhiteSpace(r.DisplayName) ? r.DisplayName : r.Email;
                if (r.Type == RecipientType.To) context.RecipientsTo.Add(display);
                else if (r.Type == RecipientType.Cc) context.RecipientsCc.Add(display);
            }
        }

        if (msg.Attachments != null)
        {
            foreach (var att in msg.Attachments)
            {
                if (att is Storage.Attachment fileAtt)
                {
                    context.AttachmentNames.Add(fileAtt.FileName ?? "Attachment");
                }
            }
        }

        return context;
    }

    private static EmailContext ExtractEml(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var message = MimeMessage.Load(stream);

        var context = new EmailContext
        {
            SourceType = InputSourceType.FileAttachment,
            SourceFilePath = filePath,
            Subject = message.Subject ?? Path.GetFileNameWithoutExtension(filePath),
            BodyText = message.TextBody ?? string.Empty,
            BodyHtml = message.HtmlBody ?? string.Empty,
            ReceivedTime = message.Date.DateTime
        };

        if (message.From.Count > 0)
        {
            var firstFrom = message.From[0];
            context.SenderName = firstFrom.Name ?? string.Empty;
            if (firstFrom is MailboxAddress mb)
            {
                context.SenderEmail = mb.Address;
            }
        }

        foreach (var mb in message.To.Mailboxes)
        {
            context.RecipientsTo.Add($"{mb.Name} <{mb.Address}>");
        }

        foreach (var mb in message.Cc.Mailboxes)
        {
            context.RecipientsCc.Add($"{mb.Name} <{mb.Address}>");
        }

        foreach (var attachment in message.Attachments)
        {
            if (attachment is MimePart part)
            {
                context.AttachmentNames.Add(part.FileName ?? "Attachment");
            }
        }

        return context;
    }

    private static EmailContext ExtractPdf(string filePath)
    {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(filePath);

        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return new EmailContext
        {
            SourceType = InputSourceType.FileAttachment,
            SourceFilePath = filePath,
            Subject = Path.GetFileNameWithoutExtension(filePath),
            BodyText = sb.ToString()
        };
    }

    private static EmailContext ExtractDocx(string filePath)
    {
        var sb = new StringBuilder();
        using var wordDoc = WordprocessingDocument.Open(filePath, false);
        var body = wordDoc.MainDocumentPart?.Document.Body;

        if (body != null)
        {
            foreach (var para in body.Elements<Paragraph>())
            {
                string text = para.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
            }
        }

        return new EmailContext
        {
            SourceType = InputSourceType.FileAttachment,
            SourceFilePath = filePath,
            Subject = Path.GetFileNameWithoutExtension(filePath),
            BodyText = sb.ToString()
        };
    }

    private static async Task<EmailContext> ExtractPlainTextAsync(string filePath)
    {
        string text = await File.ReadAllTextAsync(filePath);
        return new EmailContext
        {
            SourceType = InputSourceType.FileAttachment,
            SourceFilePath = filePath,
            Subject = Path.GetFileNameWithoutExtension(filePath),
            BodyText = text
        };
    }
}
