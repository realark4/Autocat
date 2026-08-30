using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.ToolContracts;
using AutoCadAiPlugin.Infrastructure.Configuration;
using AutoCadAiPlugin.Infrastructure.Persistence;

namespace AutoCadAiPlugin.UI.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly PluginConfigurationManager _configManager;
    private readonly ConversationHistoryStore _historyStore;
    private readonly Func<SettingsViewModel> _settingsVmFactory;
    private CancellationTokenSource? _cts;
    private string? _lastUserPrompt;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private FlowDirection _chatFlowDirection = FlowDirection.RightToLeft;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private SettingsViewModel? _currentSettingsViewModel;

    [ObservableProperty]
    private string _activeProviderName = "Mock";

    [ObservableProperty]
    private string _activeModelName = "mock-agent";

    [ObservableProperty]
    private bool _canRetryLastMessage;

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();
    public AiConversation CurrentConversation { get; private set; } = new();

    public ChatViewModel(
        IAgentOrchestrator orchestrator,
        PluginConfigurationManager configManager,
        ConversationHistoryStore historyStore,
        Func<SettingsViewModel> settingsVmFactory)
    {
        _orchestrator = orchestrator;
        _configManager = configManager;
        _historyStore = historyStore;
        _settingsVmFactory = settingsVmFactory;

        _orchestrator.OnToolStatusChanged += HandleToolStatusChanged;
        _orchestrator.OnStatusMessage += HandleStatusMessage;

        UpdateProviderHeaders();
        InitializeWelcomeMessage();
    }

    private void UpdateProviderHeaders()
    {
        var cfg = _configManager.Config;
        ActiveProviderName = cfg.ProviderType.ToString();
        ActiveModelName = cfg.Model;
        ChatFlowDirection = cfg.Language == "fa" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    private void InitializeWelcomeMessage()
    {
        bool isFa = _configManager.Config.Language == "fa";
        string welcome = isFa
            ? "سلام! من Autocat، دستیار هوش مصنوعی شما در AutoCAD هستم. می‌توانید درخواست‌های رسم، ویرایش، اندازه‌گذاری یا جابجایی خود را بنویسید."
            : "Hello! I am Autocat, your AI CAD assistant. What would you like to draw, modify, or inspect in AutoCAD today?";

        Messages.Add(new ChatMessageViewModel("assistant", welcome));
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || IsBusy) return;

        string prompt = InputText.Trim();
        InputText = string.Empty;

        _lastUserPrompt = prompt;
        CanRetryLastMessage = true;
        Messages.Add(new ChatMessageViewModel("user", prompt));

        IsBusy = true;
        StatusMessage = _configManager.Config.Language == "fa"
            ? "در حال آماده‌سازی درخواست..."
            : "Preparing request...";
        _cts = new CancellationTokenSource();
        ChatMessageViewModel? assistantMsg = null;

        try
        {
            var config = await _configManager.LoadConfigWithSecretsAsync();
            UpdateProviderHeaders();

            assistantMsg = new ChatMessageViewModel("assistant", string.Empty, isLoading: true);
            Messages.Add(assistantMsg);

            string reply = await _orchestrator.RunConversationTurnAsync(
                CurrentConversation,
                prompt,
                config,
                RequestUserApprovalAsync,
                _cts.Token);

            assistantMsg.IsLoading = false;
            assistantMsg.Content = reply;
            await _historyStore.SaveConversationAsync(CurrentConversation);
        }
        catch (OperationCanceledException)
        {
            if (assistantMsg != null)
            {
                assistantMsg.IsLoading = false;
                assistantMsg.Content = _configManager.Config.Language == "fa"
                    ? "عملیات به درخواست شما متوقف شد."
                    : "The operation was stopped.";
            }
            StatusMessage = _configManager.Config.Language == "fa" ? "متوقف شد" : "Stopped";
        }
        catch (Exception ex)
        {
            if (assistantMsg == null)
            {
                assistantMsg = new ChatMessageViewModel("assistant", string.Empty);
                Messages.Add(assistantMsg);
            }
            assistantMsg.IsLoading = false;
            assistantMsg.Content = $"Error: {ex.Message}";
        }
        finally
        {
            if (assistantMsg != null)
            {
                assistantMsg.IsLoading = false;
            }
            IsBusy = false;
            StatusMessage = _configManager.Config.Language == "fa" ? "آماده" : "Ready";
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _cts?.Cancel();
        StatusMessage = _configManager.Config.Language == "fa"
            ? "در حال توقف..."
            : "Stopping...";
    }

    [RelayCommand]
    private async Task RetryLastMessageAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(_lastUserPrompt)) return;

        InputText = _lastUserPrompt;
        await SendMessageAsync();
    }

    [RelayCommand]
    private void QuickPrompt(string promptText)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(promptText)) return;

        InputText = promptText;
        SendMessageCommand.Execute(null);
    }

    [RelayCommand]
    private void NewChat()
    {
        if (IsBusy) return;

        CurrentConversation = new AiConversation();
        _lastUserPrompt = null;
        CanRetryLastMessage = false;
        Messages.Clear();
        InitializeWelcomeMessage();
    }

    [RelayCommand]
    private void ClearChat()
    {
        if (IsBusy) return;

        CurrentConversation = new AiConversation();
        _lastUserPrompt = null;
        CanRetryLastMessage = false;
        Messages.Clear();
        InitializeWelcomeMessage();
        StatusMessage = _configManager.Config.Language == "fa" ? "گفت‌وگو پاک شد" : "Chat cleared";
    }

    [RelayCommand]
    private void ExportChat()
    {
        string md = _historyStore.ExportConversationToMarkdown(CurrentConversation);
        Clipboard.SetText(md);
        StatusMessage = "Chat copied to clipboard as Markdown!";
    }

    [RelayCommand]
    private void OpenSettings()
    {
        CurrentSettingsViewModel = _settingsVmFactory();
        IsSettingsOpen = true;
    }

    public void CloseSettings()
    {
        IsSettingsOpen = false;
        CurrentSettingsViewModel = null;
        UpdateProviderHeaders();
    }

    private Task<bool> RequestUserApprovalAsync(ToolCallRequest toolCall)
    {
        var tcs = new TaskCompletionSource<bool>();

        Application.Current?.Dispatcher?.Invoke(() =>
        {
            var activeMsg = Messages.Count > 0 ? Messages[Messages.Count - 1] : null;
            if (activeMsg != null)
            {
                var execItem = new ToolExecutionItemViewModel(toolCall)
                {
                    Status = ToolExecutionStatus.RequiresConfirmation,
                    RequiresApproval = true,
                    IsActionPending = true,
                    ApprovalTcs = tcs
                };
                activeMsg.ToolExecutions.Add(execItem);
            }
        });

        return tcs.Task;
    }

    private void HandleToolStatusChanged(ToolCallRequest toolCall, ToolExecutionStatus status, string? message)
    {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            if (Messages.Count == 0) return;
            var currentAssistantMsg = Messages[Messages.Count - 1];

            ToolExecutionItemViewModel? existing = null;
            foreach (var item in currentAssistantMsg.ToolExecutions)
            {
                if (item.CallId == toolCall.CallId)
                {
                    existing = item;
                    break;
                }
            }

            if (existing == null)
            {
                existing = new ToolExecutionItemViewModel(toolCall);
                currentAssistantMsg.ToolExecutions.Add(existing);
            }

            existing.Status = status;
            existing.Message = message;
        });
    }

    private void HandleStatusMessage(string message)
    {
        if (Application.Current?.Dispatcher == null || Application.Current.Dispatcher.CheckAccess())
        {
            StatusMessage = message;
            return;
        }

        Application.Current.Dispatcher.Invoke(() => StatusMessage = message);
    }
}
