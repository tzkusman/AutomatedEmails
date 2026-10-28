using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MailCraftAI.Models;

namespace MailCraftAI.Services.Outlook;

public class OutlookService : IOutlookService
{
    private const int OlMailItem = 0;
    private const int OlMailClass = 43;
    private const int OlTo = 1;
    private const int OlCc = 2;

    public Task<OutlookStatusResult> CheckStatusAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                if (!OutlookComHelper.IsClassicOutlookInstalled())
                {
                    return new OutlookStatusResult(
                        OutlookStatusType.NotInstalled,
                        "Classic Outlook is not registered on this PC. (New Outlook web version does not support COM integration).",
                        false);
                }

                bool isProcessRunning = OutlookComHelper.IsOutlookProcessRunning();

                var app = OutlookComHelper.GetRunningOutlookInstance();
                if (app != null)
                {
                    OutlookComHelper.ReleaseCom(app);
                    return new OutlookStatusResult(
                        OutlookStatusType.Connected,
                        "Connected to running Outlook instance.",
                        true);
                }

                if (isProcessRunning)
                {
                    return new OutlookStatusResult(
                        OutlookStatusType.ElevationMismatch,
                        "Outlook is running, but COM access was denied. Ensure MailCraft AI and Outlook run with the same privilege level (do not run as Administrator).",
                        false);
                }

                return new OutlookStatusResult(
                    OutlookStatusType.NotRunning,
                    "Classic Outlook is not currently running. Click here to launch it.",
                    false);
            }
            catch (Exception ex)
            {
                return new OutlookStatusResult(
                    OutlookStatusType.Error,
                    $"Error connecting to Outlook: {ex.Message}",
                    false);
            }
        });
    }

    public Task<bool> EnsureOutlookRunningAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var app = OutlookComHelper.GetOrStartOutlookInstance();
                if (app != null)
                {
                    OutlookComHelper.ReleaseCom(app);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EnsureOutlookRunningAsync failed: {ex.Message}");
            }
            return false;
        });
    }

    public Task<EmailContext?> GetCurrentSelectedEmailAsync()
    {
        return Task.Run(new Func<EmailContext?>(() =>
        {
            dynamic? app = null;
            dynamic? explorer = null;
            dynamic? selection = null;
            dynamic? inspector = null;
            dynamic? currentItem = null;

            try
            {
                app = OutlookComHelper.GetOrStartOutlookInstance();
                if (app == null) return null;

                // Priority 1: Check active inspector (an email opened in its own window)
                try
                {
                    inspector = app.ActiveInspector;
                    if (inspector != null)
                    {
                        currentItem = inspector.CurrentItem;
                    }
                }
                catch
                {
                    // Ignore inspector errors
                }

                // Priority 2: Check explorer selection (email selected in the list)
                if (currentItem == null)
                {
                    try
                    {
                        explorer = app.ActiveExplorer;
                        if (explorer != null)
                        {
                            dynamic? sel = explorer.Selection;
                            if (sel != null)
                            {
                                int count = (int)sel.Count;
                                if (count > 0)
                                {
                                    currentItem = sel[1];
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore explorer errors
                    }
                }

                if (currentItem != null)
                {
                    int itemClass = 0;
                    try { itemClass = (int)currentItem.Class; } catch { }

                    if (itemClass == OlMailClass)
                    {
                        return ExtractEmailContext(currentItem);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get selected email: {ex.Message}");
                return null;
            }
            finally
            {
                OutlookComHelper.ReleaseCom(currentItem);
                OutlookComHelper.ReleaseCom(selection);
                OutlookComHelper.ReleaseCom(explorer);
                OutlookComHelper.ReleaseCom(inspector);
                OutlookComHelper.ReleaseCom(app);
            }
        }));
    }

    public Task<bool> CreateReplyDraftAsync(EmailContext originalContext, string replyHtml, bool replyAll)
    {
        return Task.Run(() =>
        {
            dynamic? app = null;
            dynamic? ns = null;
            dynamic? originalMail = null;
            dynamic? replyItem = null;

            try
            {
                app = OutlookComHelper.GetOrStartOutlookInstance();
                if (app == null) return false;

                ns = app.GetNamespace("MAPI");

                // Retrieve original mail by EntryID if available
                if (!string.IsNullOrEmpty(originalContext.OutlookEntryId))
                {
                    try
                    {
                        originalMail = string.IsNullOrEmpty(originalContext.OutlookStoreId)
                            ? ns.GetItemFromID(originalContext.OutlookEntryId)
                            : ns.GetItemFromID(originalContext.OutlookEntryId, originalContext.OutlookStoreId);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Could not re-fetch mail by ID: {ex.Message}");
                    }
                }

                // Fallback: check current selection
                if (originalMail == null)
                {
                    dynamic? exp = app.ActiveExplorer;
                    if (exp != null)
                    {
                        dynamic? sel = exp.Selection;
                        if (sel != null)
                        {
                            int count = (int)sel.Count;
                            if (count > 0)
                            {
                                originalMail = sel[1];
                            }
                        }
                    }
                }

                if (originalMail == null)
                {
                    return false;
                }

                // Create the reply draft
                replyItem = replyAll ? originalMail.ReplyAll() : originalMail.Reply();

                // Display draft in Outlook - triggers Outlook to insert user's default signature
                replyItem.Display(false);

                // Format AI reply HTML
                string formattedAiHtml = OutlookComHelper.ConvertToOutlookHtml(replyHtml);

                // Insert above signature and quoted thread
                string currentBodyHtml = (string)(replyItem.HTMLBody ?? string.Empty);
                replyItem.HTMLBody = OutlookComHelper.InsertAiReplyAboveSignatureAndThread(currentBodyHtml, formattedAiHtml);

                // IMPORTANT: We NEVER call replyItem.Send().
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating reply draft: {ex.Message}");
                return false;
            }
            finally
            {
                OutlookComHelper.ReleaseCom(replyItem);
                OutlookComHelper.ReleaseCom(originalMail);
                OutlookComHelper.ReleaseCom(ns);
                OutlookComHelper.ReleaseCom(app);
            }
        });
    }

    public Task<bool> CreateNewEmailDraftAsync(string subject, string replyHtml, string? toRecipient = null)
    {
        return Task.Run(() =>
        {
            dynamic? app = null;
            dynamic? newMail = null;

            try
            {
                app = OutlookComHelper.GetOrStartOutlookInstance();
                if (app == null) return false;

                newMail = app.CreateItem(OlMailItem);
                if (newMail == null) return false;

                if (!string.IsNullOrWhiteSpace(subject))
                {
                    newMail.Subject = subject;
                }

                if (!string.IsNullOrWhiteSpace(toRecipient))
                {
                    newMail.To = toRecipient;
                }

                // Display first so Outlook populates default signature
                newMail.Display(false);

                string formattedHtml = OutlookComHelper.ConvertToOutlookHtml(replyHtml);
                string existingHtml = (string)(newMail.HTMLBody ?? string.Empty);

                newMail.HTMLBody = OutlookComHelper.InsertAiReplyAboveSignatureAndThread(existingHtml, formattedHtml);

                // Never call .Send()
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating new email draft: {ex.Message}");
                return false;
            }
            finally
            {
                OutlookComHelper.ReleaseCom(newMail);
                OutlookComHelper.ReleaseCom(app);
            }
        });
    }

    private static EmailContext ExtractEmailContext(dynamic mail)
    {
        var context = new EmailContext
        {
            SourceType = InputSourceType.Outlook,
            Subject = (string)(mail.Subject ?? "(No Subject)"),
            BodyText = (string)(mail.Body ?? string.Empty),
            BodyHtml = (string)(mail.HTMLBody ?? string.Empty),
            OutlookEntryId = (string)(mail.EntryID ?? string.Empty)
        };

        try
        {
            context.ReceivedTime = (DateTime)mail.ReceivedTime;
        }
        catch
        {
            context.ReceivedTime = DateTime.Now;
        }

        try
        {
            dynamic? parent = mail.Parent;
            if (parent != null)
            {
                context.OutlookStoreId = (string)(parent.StoreID ?? string.Empty);
            }
        }
        catch { }

        // Extract sender
        try
        {
            context.SenderName = (string)(mail.SenderName ?? string.Empty);
            context.SenderEmail = ResolveSmtpAddress(mail);
        }
        catch
        {
            try { context.SenderEmail = (string)(mail.SenderEmailAddress ?? string.Empty); } catch { }
        }

        // Extract recipients
        try
        {
            dynamic? recipients = mail.Recipients;
            if (recipients != null)
            {
                int count = (int)recipients.Count;
                for (int i = 1; i <= count; i++)
                {
                    try
                    {
                        dynamic r = recipients[i];
                        string name = (string)(r.Name ?? "");
                        string email = "";
                        try
                        {
                            dynamic? addrEntry = r.AddressEntry;
                            if (addrEntry != null)
                            {
                                dynamic? exUser = addrEntry.GetExchangeUser();
                                if (exUser != null) email = (string)(exUser.PrimarySmtpAddress ?? "");
                            }
                        }
                        catch { }

                        if (string.IsNullOrEmpty(email))
                        {
                            try { email = (string)(r.Address ?? ""); } catch { }
                        }

                        string display = !string.IsNullOrEmpty(email) ? $"{name} <{email}>" : name;
                        int rType = (int)r.Type;

                        if (rType == OlTo) context.RecipientsTo.Add(display);
                        else if (rType == OlCc) context.RecipientsCc.Add(display);

                        OutlookComHelper.ReleaseCom(r);
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to parse recipients: {ex.Message}");
        }

        // Extract attachment names
        try
        {
            dynamic? attachments = mail.Attachments;
            if (attachments != null)
            {
                int count = (int)attachments.Count;
                for (int i = 1; i <= count; i++)
                {
                    try
                    {
                        dynamic att = attachments[i];
                        context.AttachmentNames.Add((string)(att.FileName ?? $"Attachment_{i}"));
                        OutlookComHelper.ReleaseCom(att);
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to parse attachments: {ex.Message}");
        }

        return context;
    }

    private static string ResolveSmtpAddress(dynamic mail)
    {
        string address = "";
        try { address = (string)(mail.SenderEmailAddress ?? string.Empty); } catch { }

        string senderType = "";
        try { senderType = (string)(mail.SenderEmailType ?? string.Empty); } catch { }

        if (senderType == "EX")
        {
            dynamic? senderEntry = null;
            dynamic? exUser = null;
            try
            {
                senderEntry = mail.Sender;
                if (senderEntry != null)
                {
                    exUser = senderEntry.GetExchangeUser();
                    if (exUser != null)
                    {
                        string smtp = (string)(exUser.PrimarySmtpAddress ?? string.Empty);
                        if (!string.IsNullOrWhiteSpace(smtp)) return smtp;
                    }
                }
            }
            catch { }
            finally
            {
                OutlookComHelper.ReleaseCom(exUser);
                OutlookComHelper.ReleaseCom(senderEntry);
            }
        }

        return address;
    }
}
