using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;

namespace AutoCadAiPlugin.Infrastructure.Configuration;

public class PluginConfigurationManager
{
    private readonly string _settingsFilePath;
    private readonly ISecureStorage _secureStorage;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public AiProviderConfig Config { get; private set; }

    public PluginConfigurationManager(ISecureStorage secureStorage, string? customSettingsPath = null)
    {
        _secureStorage = secureStorage;

        if (!string.IsNullOrWhiteSpace(customSettingsPath))
        {
            _settingsFilePath = customSettingsPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "Ark4Studio", "Autocat");
            Directory.CreateDirectory(dir);
            _settingsFilePath = Path.Combine(dir, "settings.json");
        }

        Config = LoadConfig();
    }

    public async Task<AiProviderConfig> LoadConfigWithSecretsAsync()
    {
        var config = LoadConfig();
        string secretKey = $"API_KEY_{config.ProviderType}";
        config.ApiKey = await _secureStorage.GetSecretAsync(secretKey);
        Config = config;
        return config;
    }

    public async Task SaveConfigWithSecretsAsync(AiProviderConfig config)
    {
        Config = config;

        // Save API key to secure vault
        if (!string.IsNullOrWhiteSpace(config.ApiKey))
        {
            string secretKey = $"API_KEY_{config.ProviderType}";
            await _secureStorage.SaveSecretAsync(secretKey, config.ApiKey);
        }

        // Save non-secret settings to JSON file
        var safeConfig = new AiProviderConfig
        {
            ProviderType = config.ProviderType,
            Model = config.Model,
            BaseUrl = config.BaseUrl,
            Temperature = config.Temperature,
            MaxTokens = config.MaxTokens,
            SendDrawingContext = config.SendDrawingContext,
            RequireConfirmationForDestructiveOps = config.RequireConfirmationForDestructiveOps,
            Language = config.Language,
            Theme = config.Theme,
            ApiKey = null // Never serialize secret to plaintext JSON
        };

        try
        {
            var dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(safeConfig, _jsonOptions);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Ignored or logged
        }
    }

    private AiProviderConfig LoadConfig()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AiProviderConfig>(json);
                if (loaded != null)
                {
                    if (loaded.ProviderType == AiProviderType.Gemini && 
                        (string.IsNullOrWhiteSpace(loaded.Model) || 
                         loaded.Model.StartsWith("gemini-2.0", StringComparison.OrdinalIgnoreCase) || 
                         loaded.Model.StartsWith("gemini-1.5", StringComparison.OrdinalIgnoreCase) || 
                         loaded.Model.Equals("gemini-flash", StringComparison.OrdinalIgnoreCase)))
                    {
                        loaded.Model = "gemini-flash-latest";
                    }
                    return loaded;
                }
            }
            catch
            {
                // Fallback to default
            }
        }

        return new AiProviderConfig
        {
            ProviderType = AiProviderType.Mock,
            Model = "mock-agent",
            Temperature = 0.2,
            MaxTokens = 2048,
            SendDrawingContext = true,
            RequireConfirmationForDestructiveOps = true,
            Language = "fa",
            Theme = "Dark"
        };
    }
}
