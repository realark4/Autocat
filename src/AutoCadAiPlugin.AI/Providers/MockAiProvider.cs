using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.ToolContracts;

namespace AutoCadAiPlugin.AI.Providers;

public class MockAiProvider : IAiProvider
{
    public AiProviderType ProviderType => AiProviderType.Mock;

    public Task<AiResponse> SendMessageAsync(
        List<AiMessage> history,
        List<ToolDefinition> availableTools,
        AiProviderConfig config,
        CancellationToken cancellationToken = default)
    {
        // Find last user or tool message
        if (history.Count == 0)
        {
            return Task.FromResult(AiResponse.Success("سلام! من دستیار هوش مصنوعی Autocat هستم. چه چیزی برایتان در اتوکد رسم یا ویرایش کنم؟"));
        }

        var lastMessage = history[history.Count - 1];

        // If the last message was a tool result, produce final completion response
        if (lastMessage.Role == "tool")
        {
            string resp = config.Language == "fa"
                ? "عملیات در اتوکد با موفقیت و بر اساس پارامترهای مشخص‌شده انجام شد."
                : "The CAD operation has been successfully completed in AutoCAD.";
            return Task.FromResult(AiResponse.Success(resp));
        }

        string userText = lastMessage.Content.Trim();
        var toolCalls = new List<ToolCallRequest>();

        // Scenario 1: Circle with radius / diameter and coordinates
        // e.g. "یک دایره با شعاع 25 در 100,100 بکش" or "Draw circle radius 25 at 100,100"
        var circleMatch = Regex.Match(userText, @"(دایره|circle).*?(شعاع|radius|قطر|diameter)\s*([\d\.]+).*?(در|at)?\s*([\d\.]+)[,\s]+([\d\.]+)", RegexOptions.IgnoreCase);
        if (circleMatch.Success)
        {
            bool isRadius = circleMatch.Groups[2].Value.Contains("شعاع") || circleMatch.Groups[2].Value.ToLower().Contains("radius");
            double val = double.Parse(circleMatch.Groups[3].Value);
            double x = double.Parse(circleMatch.Groups[5].Value);
            double y = double.Parse(circleMatch.Groups[6].Value);

            var args = new Dictionary<string, object?>
            {
                ["centerX"] = x,
                ["centerY"] = y,
                ["centerZ"] = 0.0
            };
            if (isRadius) args["radius"] = val; else args["diameter"] = val;

            toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "create_circle", args));
            return Task.FromResult(AiResponse.Success(null, toolCalls));
        }

        // Scenario 6: Compound Rectangle 500x300 with R20 fillet and center hole Ø80
        // e.g. "مقطع مستطیلی 500 در 300 ایجاد کن، چهار گوشه را R20 کن و وسط آن یک سوراخ Ø80 ایجاد کن"
        if ((userText.Contains("500") && userText.Contains("300")) || (userText.Contains("مقطع") && userText.Contains("سوراخ")))
        {
            // 1. Create rectangle 500x300 with cornerRadius 20
            var rectArgs = new Dictionary<string, object?>
            {
                ["corner1X"] = 0.0,
                ["corner1Y"] = 0.0,
                ["width"] = 500.0,
                ["height"] = 300.0,
                ["cornerRadius"] = 20.0
            };
            toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "create_rectangle", rectArgs));

            // 2. Create hole Ø80 (radius 40) at center (250, 150)
            var circleArgs = new Dictionary<string, object?>
            {
                ["centerX"] = 250.0,
                ["centerY"] = 150.0,
                ["radius"] = 40.0
            };
            toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "create_circle", circleArgs));

            // 3. Add dimensions
            var dimHArgs = new Dictionary<string, object?>
            {
                ["pt1X"] = 0.0,
                ["pt1Y"] = 0.0,
                ["pt2X"] = 500.0,
                ["pt2Y"] = 0.0,
                ["textLocationX"] = 250.0,
                ["textLocationY"] = -30.0,
                ["isHorizontal"] = true
            };
            toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "create_linear_dimension", dimHArgs));

            var dimVArgs = new Dictionary<string, object?>
            {
                ["pt1X"] = 0.0,
                ["pt1Y"] = 0.0,
                ["pt2X"] = 0.0,
                ["pt2Y"] = 300.0,
                ["textLocationX"] = -30.0,
                ["textLocationY"] = 150.0,
                ["isHorizontal"] = false
            };
            toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "create_linear_dimension", dimVArgs));

            return Task.FromResult(AiResponse.Success(null, toolCalls));
        }

        // Scenario 2 & 3: Rectangle 200x100 with centered hole Ø40 and dimensions
        // e.g. "یک مستطیل 200 در 100 بکش، وسط آن یک سوراخ با قطر 40 ایجاد کن"
        var rectHoleMatch = Regex.Match(userText, @"(مستطیل|rectangle)\s*([\d\.]+)\s*(در|x|by|×)\s*([\d\.]+)", RegexOptions.IgnoreCase);
        if (rectHoleMatch.Success)
        {
            double w = double.Parse(rectHoleMatch.Groups[2].Value);
            double h = double.Parse(rectHoleMatch.Groups[4].Value);

            var rectArgs = new Dictionary<string, object?>
            {
                ["corner1X"] = 0.0,
                ["corner1Y"] = 0.0,
                ["width"] = w,
                ["height"] = h,
                ["cornerRadius"] = 0.0
            };
            toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "create_rectangle", rectArgs));

            if (userText.Contains("سوراخ") || userText.Contains("دایره") || userText.Contains("hole") || userText.Contains("circle"))
            {
                var diaMatch = Regex.Match(userText, @"(قطر|diameter|Ø|شعاع|radius)\s*([\d\.]+)", RegexOptions.IgnoreCase);
                double holeRadius = diaMatch.Success ? double.Parse(diaMatch.Groups[2].Value) / (diaMatch.Groups[1].Value.Contains("شعاع") ? 1.0 : 2.0) : 20.0;

                var circleArgs = new Dictionary<string, object?>
                {
                    ["centerX"] = w / 2.0,
                    ["centerY"] = h / 2.0,
                    ["radius"] = holeRadius
                };
                toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "create_circle", circleArgs));
            }

            if (userText.Contains("ابعاد") || userText.Contains("dimension"))
            {
                toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "create_linear_dimension", new Dictionary<string, object?>
                {
                    ["pt1X"] = 0.0,
                    ["pt1Y"] = 0.0,
                    ["pt2X"] = w,
                    ["pt2Y"] = 0.0,
                    ["textLocationX"] = w / 2.0,
                    ["textLocationY"] = -15.0,
                    ["isHorizontal"] = true
                }));

                toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "create_linear_dimension", new Dictionary<string, object?>
                {
                    ["pt1X"] = 0.0,
                    ["pt1Y"] = 0.0,
                    ["pt2X"] = 0.0,
                    ["pt2Y"] = h,
                    ["textLocationX"] = -15.0,
                    ["textLocationY"] = h / 2.0,
                    ["isHorizontal"] = false
                }));
            }

            return Task.FromResult(AiResponse.Success(null, toolCalls));
        }

        // Scenario 4: Move selected entity (e.g. "دایره انتخاب‌شده را 50 واحد به راست ببر" or "Move selected 50 right")
        if (userText.Contains("حرکت") || userText.Contains("ببر") || userText.Contains("انتقال") || userText.Contains("move"))
        {
            var moveDistMatch = Regex.Match(userText, @"([\d\.\-]+)", RegexOptions.IgnoreCase);
            double dist = moveDistMatch.Success ? double.Parse(moveDistMatch.Groups[1].Value) : 50.0;
            double dx = 0, dy = 0;

            if (userText.Contains("راست") || userText.Contains("right")) dx = dist;
            else if (userText.Contains("چپ") || userText.Contains("left")) dx = -dist;
            else if (userText.Contains("بالا") || userText.Contains("up")) dy = dist;
            else if (userText.Contains("پایین") || userText.Contains("down")) dy = -dist;
            else dx = dist;

            var moveArgs = new Dictionary<string, object?>
            {
                ["handle"] = "selected",
                ["fromX"] = 0.0,
                ["fromY"] = 0.0,
                ["toX"] = dx,
                ["toY"] = dy
            };
            toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "move_entity", moveArgs));
            return Task.FromResult(AiResponse.Success(null, toolCalls));
        }

        // Scenario 5: Simple Line (e.g. "یک خط از 0,0 به 100,100 بکش" or "Draw line from 0,0 to 100,100")
        if (userText.Contains("خط") || userText.Contains("line"))
        {
            var ptsMatch = Regex.Matches(userText, @"([\d\.\-]+)[,\s]+([\d\.\-]+)");
            double x1 = 0, y1 = 0, x2 = 100, y2 = 100;
            if (ptsMatch.Count >= 2)
            {
                x1 = double.Parse(ptsMatch[0].Groups[1].Value);
                y1 = double.Parse(ptsMatch[0].Groups[2].Value);
                x2 = double.Parse(ptsMatch[1].Groups[1].Value);
                y2 = double.Parse(ptsMatch[1].Groups[2].Value);
            }

            var lineArgs = new Dictionary<string, object?>
            {
                ["startX"] = x1,
                ["startY"] = y1,
                ["endX"] = x2,
                ["endY"] = y2
            };
            toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "create_line", lineArgs));
            return Task.FromResult(AiResponse.Success(null, toolCalls));
        }

        // Default / Zoom
        if (userText.Contains("zoom") || userText.Contains("زوم"))
        {
            toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "zoom_extents", new Dictionary<string, object?>()));
            return Task.FromResult(AiResponse.Success(null, toolCalls));
        }

        // Fallback default rectangle
        toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), "create_rectangle", new Dictionary<string, object?>
        {
            ["corner1X"] = 0.0,
            ["corner1Y"] = 0.0,
            ["width"] = 100.0,
            ["height"] = 50.0
        }));

        return Task.FromResult(AiResponse.Success(null, toolCalls));
    }

    public Task<bool> ValidateConnectionAsync(string apiKey, string? model, string? baseUrl = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<List<string>> GetSupportedModelsAsync(string apiKey, string? baseUrl = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<string> { "mock-agent", "mock-fast", "mock-precision" });
    }
}
