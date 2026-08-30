using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AutoCadAiPlugin.Core.Enums;

namespace AutoCadAiPlugin.UI.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ToolExecutionStatus status)
        {
            return status switch
            {
                ToolExecutionStatus.Pending => new SolidColorBrush(Color.FromRgb(130, 148, 166)),
                ToolExecutionStatus.Executing => new SolidColorBrush(Color.FromRgb(73, 196, 229)),
                ToolExecutionStatus.RequiresConfirmation => new SolidColorBrush(Color.FromRgb(255, 180, 84)),
                ToolExecutionStatus.Completed => new SolidColorBrush(Color.FromRgb(57, 211, 154)),
                ToolExecutionStatus.Failed => new SolidColorBrush(Color.FromRgb(255, 109, 122)),
                ToolExecutionStatus.Cancelled => new SolidColorBrush(Color.FromRgb(114, 130, 147)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ToolExecutionStatus status)
        {
            return status switch
            {
                ToolExecutionStatus.Pending => "⏳",
                ToolExecutionStatus.Executing => "⚡",
                ToolExecutionStatus.RequiresConfirmation => "⚠",
                ToolExecutionStatus.Completed => "✓",
                ToolExecutionStatus.Failed => "✕",
                ToolExecutionStatus.Cancelled => "⊘",
                _ => "•"
            };
        }
        return "•";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StatusToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ToolExecutionStatus status)
        {
            return status switch
            {
                ToolExecutionStatus.Pending => "در صف اجرا",
                ToolExecutionStatus.Executing => "در حال اجرا",
                ToolExecutionStatus.RequiresConfirmation => "نیازمند تأیید",
                ToolExecutionStatus.Completed => "انجام شد",
                ToolExecutionStatus.Failed => "ناموفق",
                ToolExecutionStatus.Cancelled => "لغو شد",
                _ => "وضعیت نامشخص"
            };
        }

        return "وضعیت نامشخص";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            return v == Visibility.Visible;
        }
        return false;
    }
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}

public class RoleToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string role = value?.ToString() ?? "assistant";
        return role.Equals("user", StringComparison.OrdinalIgnoreCase) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
