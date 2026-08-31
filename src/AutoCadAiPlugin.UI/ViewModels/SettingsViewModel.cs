using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Infrastructure.Configuration;

namespace AutoCadAiPlugin.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly PluginConfigurationManager _configManager;
    private readonly IEnumerable<IAiProvider> _providers;
    private readonly Action? _onClose;

    [ObservableProperty]
    private AiProviderType _selectedProvider = AiProviderType.Mock;

    [ObservableProperty]
    private string _selectedModel = "mock-agent";

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _baseUrl = string.Empty;

    [ObservableProperty]
    private double _temperature = 0.2;

    [ObservableProperty]
    private int _maxTokens = 2048;

    [ObservableProperty]
    private bool _sendDrawingContext = true;

    [ObservableProperty]
    private bool _requireConfirmationForDestructiveOps = true;

    [ObservableProperty]
    private string _selectedLanguage = "fa";

    [ObservableProperty]
    private string _selectedTheme = "Dark";

    [ObservableProperty]
    private bool _isTestingConnection;

    [ObservableProperty]
    private bool _isLoadingModels;

    [ObservableProperty]
    private string? _connectionStatusMessage;

    [ObservableProperty]
    private bool _isConnectionSuccess;

    public ObservableCollection<AiProviderType> Providers { get; } = new()
    {
        AiProviderType.Mock,
        AiProviderType.OpenAI,
        AiProviderType.Gemini,
        AiProviderType.Anthropic
    };

    public ObservableCollection<string> AvailableModels { get; } = new();
    public ObservableCollection<string> Languages { get; } = new() { "fa", "en" };
    public ObservableCollection<string> Themes { get; } = new() { "Dark", "Light" };

    public SettingsViewModel(
        PluginConfigurationManager configManager,
        IEnumerable<IAiProvider> providers,
        Action? onClose = null)
    {
        _configManager = configManager;
        _providers = providers;
        _onClose = onClose;

        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        var cfg = _configManager.Config;
        _selectedProvider = cfg.ProviderType;
        _selectedModel = cfg.Model;
        _apiKey = cfg.ApiKey ?? string.Empty;
        _baseUrl = cfg.BaseUrl ?? string.Empty;
        _temperature = cfg.Temperature;
        _maxTokens = cfg.MaxTokens;
        _sendDrawingContext = cfg.SendDrawingContext;
        _requireConfirmationForDestructiveOps = cfg.RequireConfirmationForDestructiveOps;
        _selectedLanguage = cfg.Language;
        _selectedTheme = cfg.Theme;

        // Keep a model saved from a proxy or local gateway even when it is not
        // part of the built-in suggestions.
        UpdateModelsForProvider(preserveCurrentModel: true);
    }

    partial void OnSelectedProviderChanged(AiProviderType value)
    {
        UpdateModelsForProvider();
    }

    private void UpdateModelsForProvider(bool preserveCurrentModel = false)
    {
        string currentModel = SelectedModel?.Trim() ?? string.Empty;
        AvailableModels.Clear();
        switch (SelectedProvider)
        {
            case AiProviderType.Mock:
                AvailableModels.Add("mock-agent");
                AvailableModels.Add("mock-precision");
                break;
            case AiProviderType.OpenAI:
                AvailableModels.Add("gpt-4o");
                AvailableModels.Add("gpt-4o-mini");
                AvailableModels.Add("o3-mini");
                AvailableModels.Add("gpt-4-turbo");
                break;
            case AiProviderType.Gemini:
                AvailableModels.Add("gemini-3.1-flash-lite");
                AvailableModels.Add("gemini-3.7-flash");
                AvailableModels.Add("gemini-3.5-flash-lite");
                AvailableModels.Add("gemini-3.1-pro-preview");
                AvailableModels.Add("gemini-3.6-flash");
                AvailableModels.Add("gemini-3-flash-preview");
                AvailableModels.Add("gemini-flash-latest");
                break;
            case AiProviderType.Anthropic:
                AvailableModels.Add("claude-3-7-sonnet-20250219");
                AvailableModels.Add("claude-3-5-sonnet-20241022");
                AvailableModels.Add("claude-3-5-haiku-20241022");
                break;
        }

        if (preserveCurrentModel && !string.IsNullOrWhiteSpace(currentModel))
        {
            if (!AvailableModels.Contains(currentModel))
            {
                AvailableModels.Insert(0, currentModel);
            }

            SelectedModel = currentModel;
        }
        else if (AvailableModels.Count > 0)
        {
            SelectedModel = AvailableModels[0];
        }
    }

    [RelayCommand]
    private async Task LoadModelsAsync()
    {
        if (IsLoadingModels || IsTestingConnection) return;

        IAiProvider? targetProvider = FindSelectedProvider();
        bool isFa = SelectedLanguage == "fa";
        if (targetProvider == null)
        {
            ConnectionStatusMessage = isFa ? "سرویس‌دهنده پیدا نشد." : "Selected provider not found.";
            return;
        }

        if (!IsValidBaseUrl(BaseUrl))
        {
            ConnectionStatusMessage = isFa
                ? "Base URL باید یک آدرس کامل با http:// یا https:// باشد."
                : "Base URL must be an absolute http:// or https:// URL.";
            return;
        }

        IsLoadingModels = true;
        ConnectionStatusMessage = isFa ? "در حال دریافت مدل‌ها..." : "Loading models...";

        try
        {
            string currentModel = SelectedModel?.Trim() ?? string.Empty;
            var models = await targetProvider.GetSupportedModelsAsync(ApiKey, BaseUrl);

            AvailableModels.Clear();
            foreach (string model in models)
            {
                string cleanModel = model?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(cleanModel) && !AvailableModels.Contains(cleanModel))
                {
                    AvailableModels.Add(cleanModel);
                }
            }

            if (!string.IsNullOrWhiteSpace(currentModel) && !AvailableModels.Contains(currentModel))
            {
                AvailableModels.Insert(0, currentModel);
            }

            if (string.IsNullOrWhiteSpace(SelectedModel) && AvailableModels.Count > 0)
            {
                SelectedModel = AvailableModels[0];
            }

            ConnectionStatusMessage = AvailableModels.Count > 0
                ? (isFa ? $"{AvailableModels.Count} مدل دریافت شد" : $"{AvailableModels.Count} models loaded")
                : (isFa ? "مدلی از سرویس دریافت نشد؛ نام مدل را دستی وارد کنید." : "No models returned; enter the model name manually.");
        }
        catch (Exception ex)
        {
            ConnectionStatusMessage = isFa
                ? $"دریافت مدل‌ها ناموفق بود: {ex.Message}"
                : $"Could not load models: {ex.Message}";
        }
        finally
        {
            IsLoadingModels = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        bool isFa = SelectedLanguage == "fa";
        string model = SelectedModel?.Trim() ?? string.Empty;
        if (SelectedProvider != AiProviderType.Mock && string.IsNullOrWhiteSpace(model))
        {
            ConnectionStatusMessage = isFa ? "نام مدل را وارد کنید." : "Enter a model name.";
            IsConnectionSuccess = false;
            return;
        }

        if (!IsValidBaseUrl(BaseUrl))
        {
            ConnectionStatusMessage = isFa
                ? "Base URL باید یک آدرس کامل با http:// یا https:// باشد."
                : "Base URL must be an absolute http:// or https:// URL.";
            IsConnectionSuccess = false;
            return;
        }

        IsTestingConnection = true;
        ConnectionStatusMessage = isFa ? "در حال بررسی اتصال..." : "Testing connection...";
        IsConnectionSuccess = false;

        IAiProvider? targetProvider = FindSelectedProvider();

        if (targetProvider == null)
        {
            IsTestingConnection = false;
            ConnectionStatusMessage = isFa ? "سرویس‌دهنده پیدا نشد." : "Selected provider not found.";
            return;
        }

        try
        {
            bool ok = await targetProvider.ValidateConnectionAsync(ApiKey, model, BaseUrl);
            IsConnectionSuccess = ok;
            ConnectionStatusMessage = ok
                ? (isFa ? "✓ اتصال با موفقیت برقرار شد" : "✓ Connection successful")
                : (isFa ? "✕ اتصال ناموفق بود؛ کلید یا آدرس را بررسی کنید" : "✕ Connection failed; check API key or URL");
        }
        catch (Exception ex)
        {
            IsConnectionSuccess = false;
            ConnectionStatusMessage = isFa ? $"✕ خطا: {ex.Message}" : $"✕ Error: {ex.Message}";
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        bool isFa = SelectedLanguage == "fa";
        string model = SelectedModel?.Trim() ?? string.Empty;
        if (SelectedProvider != AiProviderType.Mock && string.IsNullOrWhiteSpace(model))
        {
            ConnectionStatusMessage = isFa ? "نام مدل را وارد کنید." : "Enter a model name.";
            return;
        }

        if (!IsValidBaseUrl(BaseUrl))
        {
            ConnectionStatusMessage = isFa
                ? "Base URL باید یک آدرس کامل با http:// یا https:// باشد."
                : "Base URL must be an absolute http:// or https:// URL.";
            return;
        }

        var newConfig = new AiProviderConfig
        {
            ProviderType = SelectedProvider,
            Model = model,
            ApiKey = ApiKey,
            BaseUrl = string.IsNullOrWhiteSpace(BaseUrl) ? null : BaseUrl.Trim(),
            Temperature = Temperature,
            MaxTokens = MaxTokens,
            SendDrawingContext = SendDrawingContext,
            RequireConfirmationForDestructiveOps = RequireConfirmationForDestructiveOps,
            Language = SelectedLanguage,
            Theme = SelectedTheme
        };

        await _configManager.SaveConfigWithSecretsAsync(newConfig);
        _onClose?.Invoke();
    }

    private IAiProvider? FindSelectedProvider()
    {
        foreach (var provider in _providers)
        {
            if (provider.ProviderType == SelectedProvider)
            {
                return provider;
            }
        }

        return null;
    }

    private static bool IsValidBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return true;

        return Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    [RelayCommand]
    private void Close()
    {
        _onClose?.Invoke();
    }
}
