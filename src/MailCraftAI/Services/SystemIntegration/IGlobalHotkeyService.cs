using System;
using System.Windows;

namespace MailCraftAI.Services.SystemIntegration;

public interface IGlobalHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;
    bool Register(Window window);
    void Unregister();
    bool IsRegistered { get; }
}
