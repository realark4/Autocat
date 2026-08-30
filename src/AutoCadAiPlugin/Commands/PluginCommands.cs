using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using AutoCadAiPlugin.Palette;

namespace AutoCadAiPlugin.Commands;

public class PluginCommands
{
    [CommandMethod("AICAD")]
    public void OpenAiCad()
    {
        var chatView = PluginApplication.ChatView;
        if (chatView != null)
        {
            AiCadPaletteSet.TogglePalette(chatView);
        }
        else
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\n[Autocat] Error: AI Chat View is not initialized.\n");
        }
    }

    [CommandMethod("AICADSETTINGS")]
    public void OpenSettings()
    {
        var chatView = PluginApplication.ChatView;
        var chatVm = PluginApplication.ChatViewModel;

        if (chatView != null && chatVm != null)
        {
            AiCadPaletteSet.ShowPalette(chatView);
            chatVm.OpenSettingsCommand.Execute(null);
        }
    }

    [CommandMethod("AICADMOCK")]
    public void RunMockDemo()
    {
        var chatView = PluginApplication.ChatView;
        var chatVm = PluginApplication.ChatViewModel;

        if (chatView != null && chatVm != null)
        {
            AiCadPaletteSet.ShowPalette(chatView);
            chatVm.QuickPromptCommand.Execute("یک مستطیل 200 در 100 بکش، وسط آن یک سوراخ با قطر 40 ایجاد کن و ابعاد را درج کن");
        }
    }
}
