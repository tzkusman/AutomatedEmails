using System.Threading.Tasks;
using MailCraftAI.Models;

namespace MailCraftAI.Services.Context;

public interface IDocumentExtractorService
{
    Task<EmailContext> ExtractFromFileAsync(string filePath);
    bool IsSupportedExtension(string extension);
}
