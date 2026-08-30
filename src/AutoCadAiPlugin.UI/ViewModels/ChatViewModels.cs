using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.ToolContracts;

namespace AutoCadAiPlugin.UI.ViewModels;

public partial class ToolExecutionItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _callId = string.Empty;

    [ObservableProperty]
    private string _toolName = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _argumentsSummary = string.Empty;

    [ObservableProperty]
    private ToolExecutionStatus _status = ToolExecutionStatus.Pending;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private bool _requiresApproval;

    [ObservableProperty]
    private bool _isActionPending;

    public ToolCallRequest ToolCall { get; }
    public TaskCompletionSource<bool>? ApprovalTcs { get; set; }
    public bool HasArguments => !string.IsNullOrWhiteSpace(ArgumentsSummary);

    public ToolExecutionItemViewModel(ToolCallRequest toolCall)
    {
        ToolCall = toolCall;
        _callId = toolCall.CallId;
        _toolName = toolCall.ToolName;
        _displayName = FormatToolDisplayName(toolCall.ToolName);
        _argumentsSummary = FormatArguments(toolCall.Arguments);
    }

    [RelayCommand]
    private void Approve()
    {
        RequiresApproval = false;
        IsActionPending = false;
        ApprovalTcs?.TrySetResult(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequiresApproval = false;
        IsActionPending = false;
        Status = ToolExecutionStatus.Cancelled;
        Message = "Cancelled by user";
        ApprovalTcs?.TrySetResult(false);
    }

    private static string FormatToolDisplayName(string name)
    {
        return name switch
        {
            "create_circle" => "Creating Circle (دایره)",
            "create_rectangle" => "Creating Rectangle (مستطیل)",
            "create_line" => "Creating Line (خط)",
            "create_arc" => "Creating Arc (کمان)",
            "create_polyline" => "Creating Polyline (پلی‌لاین)",
            "create_linear_dimension" => "Adding Dimension (ابعاد خطی)",
            "create_aligned_dimension" => "Adding Aligned Dimension (ابعاد تراز)",
            "create_radius_dimension" => "Adding Radius Dimension (اندازه‌گذاری شعاع)",
            "create_diameter_dimension" => "Adding Diameter Dimension (اندازه‌گذاری قطر)",
            "move_entity" => "Moving Entity (جابجایی آبجکت)",
            "copy_entity" => "Copying Entity (کپی آبجکت)",
            "rotate_entity" => "Rotating Entity (چرخش آبجکت)",
            "scale_entity" => "Scaling Entity (مقیاس آبجکت)",
            "erase_entity" => "Erasing Entity (حذف آبجکت)",
            "fillet_entity" => "Applying Fillet (فیلت گوشه)",
            "offset_entity" => "Offsetting Entity (آفست)",
            "trim_entity" => "Trimming Entity (برش)",
            "extend_entity" => "Extending Entity (امتداد)",
            "get_selected_entities" => "Reading Selection (خواندن آبجکت‌های انتخابی)",
            "get_drawing_info" => "Reading Drawing Info (اطلاعات ترسیم)",
            "zoom_extents" => "Zooming Extents (زوم کلی)",
            _ => name
        };
    }

    private static string FormatArguments(Dictionary<string, object?> args)
    {
        var items = new List<string>();
        foreach (var kvp in args)
        {
            if (kvp.Value != null)
            {
                string valStr = kvp.Value.ToString() ?? string.Empty;
                if (valStr.Length > 25) valStr = valStr.Substring(0, 22) + "...";
                items.Add($"{kvp.Key}: {valStr}");
            }
        }
        return string.Join(" | ", items);
    }
}

public partial class ChatMessageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _role = "assistant";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasContent))]
    private string _content = string.Empty;

    [ObservableProperty]
    private DateTime _timestamp = DateTime.Now;

    [ObservableProperty]
    private ObservableCollection<ToolExecutionItemViewModel> _toolExecutions = new();

    public bool IsUser => Role.Equals("user", StringComparison.OrdinalIgnoreCase);
    public bool IsAssistant => Role.Equals("assistant", StringComparison.OrdinalIgnoreCase);
    public bool HasToolExecutions => ToolExecutions.Count > 0;
    public bool HasContent => !string.IsNullOrWhiteSpace(Content);

    public ChatMessageViewModel(string role, string content)
    {
        _role = role;
        _content = content;
        _timestamp = DateTime.Now;
    }
}
