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

        var userMsg = new ChatMessageViewModel("user", prompt);
        Messages.Add(userMsg);

        IsBusy = true;
        _cts = new CancellationTokenSource();

        try
        {
            var config = await _configManager.LoadConfigWithSecretsAsync();
            UpdateProviderHeaders();

            var assistantMsg = new ChatMessageViewModel("assistant", string.Empty);
            Messages.Add(assistantMsg);

            string reply = await _orchestrator.RunConversationTurnAsync(
                CurrentConversation,
                prompt,
                config,
                RequestUserApprovalAsync,
                _cts.Token);

            assistantMsg.Content = reply;
            await _historyStore.SaveConversationAsync(CurrentConversation);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation stopped by user.";
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessageViewModel("assistant", $"Error: {ex.Message}"));
        }
        finally
        {
            IsBusy = false;
            StatusMessage = "Ready";
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _cts?.Cancel();
        IsBusy = false;
        StatusMessage = "Cancelled";
    }

    [RelayCommand]
    private void QuickPrompt(string promptText)
    {
        InputText = promptText;
        SendMessageCommand.Execute(null);
    }

    [RelayCommand]
    private void NewChat()
    {
        CurrentConversation = new AiConversation();
        Messages.Clear();
        InitializeWelcomeMessage();
    }

    [RelayCommand]
    private void ClearChat()
    {
        Messages.Clear();
        InitializeWelcomeMessage();
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
        StatusMessage = message;
    }
}
