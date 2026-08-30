using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.Interfaces;

namespace AutoCadAiPlugin.Infrastructure.Security;

public class DpapiSecureStorage : ISecureStorage
{
    private readonly string _storageFilePath;
    private readonly byte[] _entropy = Encoding.UTF8.GetBytes("Ark4Studio_Autocat_Vault_Entropy_2026");
    private readonly object _lock = new();

    public DpapiSecureStorage(string? customPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            _storageFilePath = customPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "Ark4Studio", "Autocat");
            Directory.CreateDirectory(dir);
            _storageFilePath = Path.Combine(dir, "secrets.dat");
        }
    }

    public Task SaveSecretAsync(string key, string secret)
    {
        lock (_lock)
        {
            var secrets = LoadEncryptedDict();
            secrets[key] = secret;
            SaveEncryptedDict(secrets);
        }
        return Task.CompletedTask;
    }

    public Task<string?> GetSecretAsync(string key)
    {
        lock (_lock)
        {
            var secrets = LoadEncryptedDict();
            secrets.TryGetValue(key, out var secret);
            return Task.FromResult(secret);
        }
    }

    public Task DeleteSecretAsync(string key)
    {
        lock (_lock)
        {
            var secrets = LoadEncryptedDict();
            if (secrets.Remove(key))
            {
                SaveEncryptedDict(secrets);
            }
        }
        return Task.CompletedTask;
    }

    private Dictionary<string, string> LoadEncryptedDict()
    {
        if (!File.Exists(_storageFilePath))
            return new Dictionary<string, string>();

        try
        {
            byte[] encryptedBytes = File.ReadAllBytes(_storageFilePath);
            if (encryptedBytes.Length == 0) return new Dictionary<string, string>();

            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);
            string json = Encoding.UTF8.GetString(decryptedBytes);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private void SaveEncryptedDict(Dictionary<string, string> secrets)
    {
        try
        {
            string json = JsonSerializer.Serialize(secrets);
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(json);
            byte[] encryptedBytes = ProtectedData.Protect(plaintextBytes, _entropy, DataProtectionScope.CurrentUser);

            var dir = Path.GetDirectoryName(_storageFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            File.WriteAllBytes(_storageFilePath, encryptedBytes);
        }
        catch
        {
            // Logging can capture failure if needed
        }
    }
}
