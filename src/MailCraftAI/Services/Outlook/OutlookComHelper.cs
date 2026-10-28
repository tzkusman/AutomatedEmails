using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MailCraftAI.Services.Outlook;

public static class OutlookComHelper
{
    private const string OutlookProgId = "Outlook.Application";

    [DllImport("oleaut32.dll", PreserveSig = false)]
    private static extern void GetActiveObject(
        ref Guid rclsid,
        IntPtr pvReserved,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

    [DllImport("ole32.dll")]
    private static extern int CLSIDFromProgID(
        [MarshalAs(UnmanagedType.LPWStr)] string lpszProgID,
        out Guid lpclsid);

    /// <summary>
    /// Attempts to attach to a currently running classic Outlook instance via COM IDispatch.
    /// </summary>
    public static object? GetRunningOutlookInstance()
    {
        try
        {
            int hr = CLSIDFromProgID(OutlookProgId, out var clsid);
            if (hr < 0) return null;

            GetActiveObject(ref clsid, IntPtr.Zero, out var activeObject);
            return activeObject;
        }
        catch
        {
            // Outlook is either not running or running under different privilege level
            return null;
        }
    }

    /// <summary>
    /// Connects to a running Outlook instance or launches classic Outlook if not already running.
    /// </summary>
    public static object? GetOrStartOutlookInstance()
    {
        var existing = GetRunningOutlookInstance();
        if (existing != null)
        {
            return existing;
        }

        try
        {
            var type = Type.GetTypeFromProgID(OutlookProgId);
            if (type != null)
            {
                var instance = Activator.CreateInstance(type);
                return instance;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to instantiate Outlook COM: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Checks whether classic Outlook COM registration exists in the Windows Registry.
    /// </summary>
    public static bool IsClassicOutlookInstalled()
    {
        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey(OutlookProgId);
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks whether the OUTLOOK process is currently running.
    /// </summary>
    public static bool IsOutlookProcessRunning()
    {
        var processes = Process.GetProcessesByName("OUTLOOK");
        return processes.Length > 0;
    }

    /// <summary>
    /// Safely releases a COM object to avoid leaving ghost processes in memory.
    /// </summary>
    public static void ReleaseCom(object? comObject)
    {
        if (comObject != null && Marshal.IsComObject(comObject))
        {
            try
            {
                Marshal.ReleaseComObject(comObject);
            }
            catch
            {
                // Ignored
            }
        }
    }

    /// <summary>
    /// Converts plain text into clean, professional email HTML suitable for Outlook.
    /// </summary>
    public static string ConvertToOutlookHtml(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        // If content already contains rich HTML body/paragraphs, return formatted
        if (content.Trim().StartsWith("<p", StringComparison.OrdinalIgnoreCase) ||
            content.Trim().StartsWith("<div", StringComparison.OrdinalIgnoreCase) ||
            content.Trim().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            return content;
        }

        var sb = new StringBuilder();
        var paragraphs = Regex.Split(content.Trim(), @"(?:\r?\n){2,}");

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph)) continue;

            // Handle bullet points
            var lines = paragraph.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool isList = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("• "))
                {
                    if (!isList)
                    {
                        sb.Append("<ul style=\"margin-top: 4pt; margin-bottom: 8pt; padding-left: 20pt; font-family: Calibri, Arial, sans-serif; font-size: 11pt; color: #222222;\">");
                        isList = true;
                    }
                    var itemText = WebUtility.HtmlEncode(line[2..].Trim());
                    sb.Append($"<li style=\"margin-bottom: 3pt;\">{itemText}</li>");
                }
                else
                {
                    if (isList)
                    {
                        sb.Append("</ul>");
                        isList = false;
                    }
                    var encoded = WebUtility.HtmlEncode(line);
                    sb.Append($"<p style=\"font-family: Calibri, Arial, sans-serif; font-size: 11pt; color: #222222; margin: 0 0 8pt 0; line-height: 1.4;\">{encoded}</p>");
                }
            }

            if (isList)
            {
                sb.Append("</ul>");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Inserts the AI-generated HTML snippet at the beginning of an existing Outlook HTML body,
    /// safely preserving the user's signature and the quoted email thread.
    /// </summary>
    public static string InsertAiReplyAboveSignatureAndThread(string existingHtml, string aiReplyHtml)
    {
        if (string.IsNullOrWhiteSpace(existingHtml))
        {
            return $"<html><body>{aiReplyHtml}</body></html>";
        }

        // Look for body tag
        int bodyIndex = existingHtml.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
        if (bodyIndex >= 0)
        {
            int closeTag = existingHtml.IndexOf('>', bodyIndex);
            if (closeTag >= 0)
            {
                // Check if there is an outer div class="WordSection1"
                int wordSectionIndex = existingHtml.IndexOf("<div class=\"WordSection1\"", closeTag, StringComparison.OrdinalIgnoreCase);
                if (wordSectionIndex >= 0 && wordSectionIndex < closeTag + 50)
                {
                    int wordSectionClose = existingHtml.IndexOf('>', wordSectionIndex);
                    if (wordSectionClose >= 0)
                    {
                        return existingHtml.Insert(wordSectionClose + 1, aiReplyHtml + "<br/>");
                    }
                }

                return existingHtml.Insert(closeTag + 1, aiReplyHtml + "<br/>");
            }
        }

        // Fallback: prepend
        return aiReplyHtml + "<br/><br/>" + existingHtml;
    }
}
