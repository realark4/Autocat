using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AutoCadAiPlugin.UI.ViewModels;

namespace AutoCadAiPlugin.UI.Views;

public partial class AiChatView : UserControl
{
    private ChatViewModel? _viewModel;

    public AiChatView()
    {
        InitializeComponent();
        DataContextChanged += HandleDataContextChanged;
    }

    private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachViewModel();
        _viewModel = e.NewValue as ChatViewModel;

        if (_viewModel == null) return;

        _viewModel.Messages.CollectionChanged += HandleMessagesChanged;
        _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        foreach (var message in _viewModel.Messages)
        {
            AttachMessage(message);
        }

        ScrollToLatestMessage();
    }

    private void HandleMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (ChatMessageViewModel message in e.OldItems)
            {
                DetachMessage(message);
            }
        }

        if (e.NewItems != null)
        {
            foreach (ChatMessageViewModel message in e.NewItems)
            {
                AttachMessage(message);
            }
        }

        ScrollToLatestMessage();
    }

    private void AttachMessage(ChatMessageViewModel message)
    {
        message.PropertyChanged += HandleMessagePropertyChanged;
        message.ToolExecutions.CollectionChanged += HandleToolExecutionsChanged;
    }

    private void DetachMessage(ChatMessageViewModel message)
    {
        message.PropertyChanged -= HandleMessagePropertyChanged;
        message.ToolExecutions.CollectionChanged -= HandleToolExecutionsChanged;
    }

    private void HandleMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatMessageViewModel.Content) ||
            e.PropertyName == nameof(ChatMessageViewModel.IsLoading))
        {
            ScrollToLatestMessage();
        }
    }

    private void HandleToolExecutionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScrollToLatestMessage();
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatViewModel.IsBusy) && _viewModel?.IsBusy == false)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new System.Action(() => ComposerTextBox.Focus()));
        }
    }

    private void ScrollToLatestMessage()
    {
        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new System.Action(() => MessagesScrollViewer.ScrollToEnd()));
    }

    private void DetachViewModel()
    {
        if (_viewModel == null) return;

        _viewModel.Messages.CollectionChanged -= HandleMessagesChanged;
        _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
        foreach (var message in _viewModel.Messages)
        {
            DetachMessage(message);
        }
        _viewModel = null;
    }
}
