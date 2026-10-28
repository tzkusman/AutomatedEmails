using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MailCraftAI.Models;

namespace MailCraftAI.Services.Settings;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly object _lock = new();
    private AppSettings? _cachedSettings;

    public SettingsService(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _settingsFilePath = customPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "MailCraftAI");
            Directory.CreateDirectory(folder);
            _settingsFilePath = Path.Combine(folder, "settings.json");
        }
    }

    public Task<AppSettings> LoadSettingsAsync()
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                if (_cachedSettings != null) return _cachedSettings;

                if (!File.Exists(_settingsFilePath))
                {
                    _cachedSettings = CreateDefaultSettings();
                    SaveSettingsInternal(_cachedSettings);
                    return _cachedSettings;
                }

                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    _cachedSettings = loaded ?? CreateDefaultSettings();

                    // Ensure built-in personas exist
                    EnsureBuiltInPersonas(_cachedSettings);
                    return _cachedSettings;
                }
                catch
                {
                    _cachedSettings = CreateDefaultSettings();
                    return _cachedSettings;
                }
            }
        });
    }

    public Task SaveSettingsAsync(AppSettings settings)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                _cachedSettings = settings;
                SaveSettingsInternal(settings);
            }
        });
    }

    private void SaveSettingsInternal(AppSettings settings)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to write settings: {ex.Message}");
        }
    }

    private static AppSettings CreateDefaultSettings()
    {
        var settings = new AppSettings();
        EnsureBuiltInPersonas(settings);
        return settings;
    }

    private static void EnsureBuiltInPersonas(AppSettings settings)
    {
        settings.CustomPersonas ??= [];

        var defaults = new List<PersonaTemplate>
        {
            PersonaTemplate.CreateGeneral(),
            PersonaTemplate.CreateCustomerSupport(),
            PersonaTemplate.CreateSalesQuotation(),
            PersonaTemplate.CreateVendorFollowup(),
            PersonaTemplate.CreateExecutive()
        };

        foreach (var def in defaults)
        {
            if (!settings.CustomPersonas.Exists(p => p.Id == def.Id))
            {
                settings.CustomPersonas.Add(def);
            }
        }
    }
}
