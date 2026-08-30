using System;
using System.Drawing;
using Autodesk.AutoCAD.Windows;
using AutoCadAiPlugin.UI.Views;

namespace AutoCadAiPlugin.Palette;

public class AiCadPaletteSet
{
    private static PaletteSet? _paletteSet;
    private static AiChatView? _chatView;

    public static void ShowPalette(AiChatView chatView)
    {
        _chatView = chatView;

        if (_paletteSet == null)
        {
            _paletteSet = new PaletteSet("Autocat AI Assistant", new Guid("4A7E24D9-6A4B-4E38-9B21-8E61C39BFB20"))
            {
                Style = PaletteSetStyles.ShowAutoHideButton |
                        PaletteSetStyles.ShowCloseButton |
                        PaletteSetStyles.ShowPropertiesMenu,
                Dock = DockSides.Right,
                MinimumSize = new Size(360, 500),
                Size = new Size(420, 720)
            };

            _paletteSet.AddVisual("Autocat Chat", _chatView);
        }

        _paletteSet.Visible = true;
    }

    public static void TogglePalette(AiChatView chatView)
    {
        if (_paletteSet == null || !_paletteSet.Visible)
        {
            ShowPalette(chatView);
        }
        else
        {
            _paletteSet.Visible = false;
        }
    }

    public static void ClosePalette()
    {
        if (_paletteSet != null)
        {
            _paletteSet.Visible = false;
            _paletteSet.Dispose();
            _paletteSet = null;
        }
    }
}
