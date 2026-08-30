using System;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;

namespace AutoCadAiPlugin.Cad.Execution;

public static class CadDispatcher
{
    private static SynchronizationContext? _uiContext;

    public static void Initialize()
    {
        _uiContext = SynchronizationContext.Current;
    }

    public static Task RunOnCadThreadAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();

        if (_uiContext != null && SynchronizationContext.Current != _uiContext)
        {
            _uiContext.Post(_ =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);
        }
        else
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }

        return tcs.Task;
    }

    public static Task<T> RunOnCadThreadAsync<T>(Func<T> function)
    {
        var tcs = new TaskCompletionSource<T>();

        if (_uiContext != null && SynchronizationContext.Current != _uiContext)
        {
            _uiContext.Post(_ =>
            {
                try
                {
                    var result = function();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);
        }
        else
        {
            try
            {
                var result = function();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }

        return tcs.Task;
    }
}
