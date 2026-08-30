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

        UpdateModelsForProvider();
    }

    partial void OnSelectedProviderChanged(AiProviderType value)
    {
        UpdateModelsForProvider();
    }

    private void UpdateModelsForProvider()
    {
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

        if (AvailableModels.Count > 0 && !AvailableModels.Contains(SelectedModel))
        {
            SelectedModel = AvailableModels[0];
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTestingConnection = true;
        ConnectionStatusMessage = "Testing connection...";
        IsConnectionSuccess = false;

        IAiProvider? targetProvider = null;
        foreach (var p in _providers)
        {
            if (p.ProviderType == SelectedProvider)
            {
                targetProvider = p;
                break;
            }
        }

        if (targetProvider == null)
        {
            IsTestingConnection = false;
            ConnectionStatusMessage = "Selected provider not found.";
            return;
        }

        try
        {
            bool ok = await targetProvider.ValidateConnectionAsync(ApiKey, SelectedModel, BaseUrl);
            IsConnectionSuccess = ok;
            ConnectionStatusMessage = ok ? "✓ Connection successful!" : "✕ Connection failed. Check API key/URL.";
        }
        catch (Exception ex)
        {
            IsConnectionSuccess = false;
            ConnectionStatusMessage = $"✕ Error: {ex.Message}";
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var newConfig = new AiProviderConfig
        {
            ProviderType = SelectedProvider,
            Model = SelectedModel,
            ApiKey = ApiKey,
            BaseUrl = string.IsNullOrWhiteSpace(BaseUrl) ? null : BaseUrl,
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

    [RelayCommand]
    private void Close()
    {
        _onClose?.Invoke();
    }
}
