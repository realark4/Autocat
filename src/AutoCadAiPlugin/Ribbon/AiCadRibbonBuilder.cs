using System;
using Autodesk.Windows;

namespace AutoCadAiPlugin.Ribbon;

public static class AiCadRibbonBuilder
{
    public static void InitializeRibbon()
    {
        try
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon == null) return;

            string tabId = "Ark4Studio_Autocat_Tab";
            RibbonTab? tab = null;

            foreach (var t in ribbon.Tabs)
            {
                if (t.Id == tabId)
                {
                    tab = t;
                    break;
                }
            }

            if (tab == null)
            {
                tab = new RibbonTab
                {
                    Title = "AI CAD",
                    Id = tabId
                };
                ribbon.Tabs.Add(tab);
            }

            var panelSource = new RibbonPanelSource
            {
                Title = "AI Assistant"
            };

            var btnOpen = new RibbonButton
            {
                Text = "Open AI\nAssistant",
                ShowText = true,
                ShowImage = true,
                Size = RibbonItemSize.Large,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                CommandParameter = "._AICAD ",
                CommandHandler = new RibbonCommandHandler()
            };

            var btnSettings = new RibbonButton
            {
                Text = "AI\nSettings",
                ShowText = true,
                ShowImage = true,
                Size = RibbonItemSize.Standard,
                CommandParameter = "._AICADSETTINGS ",
                CommandHandler = new RibbonCommandHandler()
            };

            var btnMock = new RibbonButton
            {
                Text = "Demo / Mock\nTest",
                ShowText = true,
                ShowImage = true,
                Size = RibbonItemSize.Standard,
                CommandParameter = "._AICADMOCK ",
                CommandHandler = new RibbonCommandHandler()
            };

            panelSource.Items.Add(btnOpen);
            panelSource.Items.Add(btnSettings);
            panelSource.Items.Add(btnMock);

            var panel = new RibbonPanel
            {
                Source = panelSource
            };

            tab.Panels.Add(panel);
        }
        catch
        {
            // Ribbon builder fallback if CUI is disabled
        }
    }

    private class RibbonCommandHandler : System.Windows.Input.ICommand
    {
        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            if (parameter is RibbonButton button && button.CommandParameter is string cmd)
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                doc?.SendStringToExecute(cmd, true, false, false);
            }
        }

        public event EventHandler? CanExecuteChanged;
    }
}
