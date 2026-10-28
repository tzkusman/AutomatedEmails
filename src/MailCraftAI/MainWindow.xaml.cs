using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MailCraftAI.Models;
using MailCraftAI.ViewModels;

namespace MailCraftAI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private ThemeModeOption _currentTheme = ThemeModeOption.Light;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Populate PasswordBoxes from ViewModel when Settings window initializes
        _viewModel.SettingsVm.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(SettingsViewModel.AnthropicApiKey))
            {
                if (PbClaude.Password != _viewModel.SettingsVm.AnthropicApiKey)
                    PbClaude.Password = _viewModel.SettingsVm.AnthropicApiKey;
            }
            else if (args.PropertyName == nameof(SettingsViewModel.OpenAiApiKey))
            {
                if (PbOpenAi.Password != _viewModel.SettingsVm.OpenAiApiKey)
                    PbOpenAi.Password = _viewModel.SettingsVm.OpenAiApiKey;
            }
            else if (args.PropertyName == nameof(SettingsViewModel.GeminiApiKey))
            {
                if (PbGemini.Password != _viewModel.SettingsVm.GeminiApiKey)
                    PbGemini.Password = _viewModel.SettingsVm.GeminiApiKey;
            }
        };
    }

    private void OnClaudePasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            _viewModel.SettingsVm.AnthropicApiKey = pb.Password;
        }
    }

    private void OnOpenAiPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            _viewModel.SettingsVm.OpenAiApiKey = pb.Password;
        }
    }

    private void OnGeminiPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            _viewModel.SettingsVm.GeminiApiKey = pb.Password;
        }
    }

    private void OnToggleThemeClicked(object sender, RoutedEventArgs e)
    {
        _currentTheme = _currentTheme == ThemeModeOption.Dark ? ThemeModeOption.Light : ThemeModeOption.Dark;
        App.SetTheme(_currentTheme);
    }

    private void OnCloseHistoryClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.IsHistoryOpen = false;
    }

    private void OnClosePrivacyNoticeClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.IsPrivacyNoticeOpen = false;
    }

    private void OnFileDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async void OnFileDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0)
            {
                string filePath = files[0];
                await _viewModel.InputVm.LoadFileContextAsync(filePath);
            }
        }
    }
}