using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MailCraftAI.Models;

namespace MailCraftAI.Services.Security;

public class DpapiSecureStorageService : ISecureStorageService
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("MailCraftAI_Secure_Credentials_Salt_v1");
    private readonly string _storageFilePath;
    private readonly object _lock = new();

    public DpapiSecureStorageService(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _storageFilePath = customPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "MailCraftAI");
            Directory.CreateDirectory(folder);
            _storageFilePath = Path.Combine(folder, "credentials.dat");
        }
    }

    public Task SaveApiKeyAsync(AiProviderType provider, string apiKey)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                var dict = LoadEncryptedStore();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    dict.Remove(provider.ToString());
                }
                else
                {
                    dict[provider.ToString()] = apiKey.Trim();
                }
                SaveEncryptedStore(dict);
            }
        });
    }

    public Task<string?> GetApiKeyAsync(AiProviderType provider)
    {
        return Task.Run<string?>(() =>
        {
            lock (_lock)
            {
                var dict = LoadEncryptedStore();
                return dict.TryGetValue(provider.ToString(), out var key) && !string.IsNullOrWhiteSpace(key)
                    ? key
                    : null;
            }
        });
    }

    public Task DeleteApiKeyAsync(AiProviderType provider)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                var dict = LoadEncryptedStore();
                if (dict.Remove(provider.ToString()))
                {
                    SaveEncryptedStore(dict);
                }
            }
        });
    }

    public Task<bool> HasApiKeyAsync(AiProviderType provider)
    {
        return Task.Run(async () =>
        {
            var key = await GetApiKeyAsync(provider);
            return !string.IsNullOrWhiteSpace(key);
        });
    }

    private Dictionary<string, string> LoadEncryptedStore()
    {
        if (!File.Exists(_storageFilePath))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            byte[] encryptedBytes = File.ReadAllBytes(_storageFilePath);
            if (encryptedBytes.Length == 0) return new Dictionary<string, string>();

            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
            string json = Encoding.UTF8.GetString(decryptedBytes);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch (Exception)
        {
            // Corrupt or encrypted by another user profile
            return new Dictionary<string, string>();
        }
    }

    private void SaveEncryptedStore(Dictionary<string, string> dict)
    {
        try
        {
            string json = JsonSerializer.Serialize(dict);
            byte[] plainBytes = Encoding.UTF8.GetBytes(json);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_storageFilePath, encryptedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to write DPAPI encrypted credentials: {ex.Message}", ex);
        }
    }
}
