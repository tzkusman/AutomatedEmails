using System;
using System.Windows;
using MailCraftAI.Models;
using MailCraftAI.Services.AI;
using MailCraftAI.Services.Context;
using MailCraftAI.Services.History;
using MailCraftAI.Services.Outlook;
using MailCraftAI.Services.Prompt;
using MailCraftAI.Services.Security;
using MailCraftAI.Services.Settings;
using MailCraftAI.Services.SystemIntegration;
using MailCraftAI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MailCraftAI;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public App()
    {
        // Global safety handlers so the app never terminates abruptly
        DispatcherUnhandledException += (s, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"Dispatcher error: {args.Exception}");
            MessageBox.Show(
                $"MailCraft AI encountered a minor issue:\n\n{args.Exception.Message}\n\nThe application will continue running.",
                "MailCraft AI",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"Domain unhandled error: {args.ExceptionObject}");
        };

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IOutlookService, OutlookService>();
        services.AddSingleton<ISecureStorageService, DpapiSecureStorageService>();
        services.AddSingleton<IPiiMaskingService, PiiMaskingService>();
        services.AddSingleton<IDocumentExtractorService, DocumentExtractorService>();
        services.AddSingleton<IContextTrimmerService, ContextTrimmerService>();
        services.AddSingleton<IPromptBuilderService, PromptBuilderService>();
        services.AddSingleton<IHistoryService, SqliteHistoryService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();

        // AI Providers
        services.AddSingleton<IAiProvider, AnthropicProvider>();
        services.AddSingleton<IAiProvider, OpenAiProvider>();
        services.AddSingleton<IAiProvider, GeminiProvider>();
        services.AddSingleton<IAiProviderFactory, AiProviderFactory>();

        // ViewModels
        services.AddSingleton<InputContextViewModel>();
        services.AddSingleton<ReplyEditorViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<MainViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        // Initialize MainViewModel with defensive try-catch
        var mainVm = ServiceProvider.GetRequiredService<MainViewModel>();
        try
        {
            await mainVm.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Initialization notice: {ex.Message}");
        }

        // Register Global Hotkey
        try
        {
            var hotkey = ServiceProvider.GetRequiredService<IGlobalHotkeyService>();
            hotkey.Register(mainWindow);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to register hotkey: {ex.Message}");
        }
    }

    public static void SetTheme(ThemeModeOption mode)
    {
        var dicts = Current.Resources.MergedDictionaries;
        var newTheme = new ResourceDictionary();

        if (mode == ThemeModeOption.Dark)
        {
            newTheme.Source = new Uri("Styles/DarkTheme.xaml", UriKind.Relative);
        }
        else
        {
            newTheme.Source = new Uri("Styles/LightTheme.xaml", UriKind.Relative);
        }

        if (dicts.Count > 1)
        {
            dicts[1] = newTheme;
        }
    }
}
