using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.Models;
using AutoCadAiPlugin.Core.ToolContracts;
using AutoCadAiPlugin.Tools.Base;

namespace AutoCadAiPlugin.Tools.Implementations;

public class CreateTextTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_text",
        "Creates single-line text (DBText) at a specific coordinate."
    )
    .AddProperty("text", "string", "Text string to display", required: true)
    .AddProperty("insertionX", "number", "X coordinate of insertion point", required: true)
    .AddProperty("insertionY", "number", "Y coordinate of insertion point", required: true)
    .AddProperty("height", "number", "Text height in drawing units", required: true, minimum: 0.0001)
    .AddProperty("rotationDegrees", "number", "Rotation angle in degrees (default 0)", required: false, defaultValue: 0.0)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? text = GetString(arguments, "text");
        if (string.IsNullOrWhiteSpace(text))
            return ToolCallResult.Fail(callId, Definition.Name, "text is required.");

        var insertPt = new CadPoint3D(GetDouble(arguments, "insertionX"), GetDouble(arguments, "insertionY"), 0);
        double height = GetDouble(arguments, "height", 2.5);
        double rot = GetDouble(arguments, "rotationDegrees", 0.0);
        string? layer = GetString(arguments, "layer");

        string handle = await cadService.CreateTextAsync(text, insertPt, height, rot, layer);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle, Text = text }, $"Text '{text}' created (Handle: {handle}).");
    }
}

public class CreateMTextTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_mtext",
        "Creates multiline formatted text (MText) in AutoCAD."
    )
    .AddProperty("text", "string", "Multiline text contents", required: true)
    .AddProperty("insertionX", "number", "X coordinate of insertion point", required: true)
    .AddProperty("insertionY", "number", "Y coordinate of insertion point", required: true)
    .AddProperty("width", "number", "Bounding box width of MText", required: true, minimum: 0.0001)
    .AddProperty("height", "number", "Character text height", required: true, minimum: 0.0001)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? text = GetString(arguments, "text");
        if (string.IsNullOrWhiteSpace(text))
            return ToolCallResult.Fail(callId, Definition.Name, "text is required.");

        var insertPt = new CadPoint3D(GetDouble(arguments, "insertionX"), GetDouble(arguments, "insertionY"), 0);
        double width = GetDouble(arguments, "width", 50.0);
        double height = GetDouble(arguments, "height", 2.5);
        string? layer = GetString(arguments, "layer");

        string handle = await cadService.CreateMTextAsync(text, insertPt, width, height, layer);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle, Text = text }, $"MText created (Handle: {handle}).");
    }
}

public class ZoomExtentsTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "zoom_extents",
        "Zooms AutoCAD viewport to display the drawing extents."
    );

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        bool success = await cadService.ZoomExtentsAsync();
        return ToolCallResult.Ok(callId, Definition.Name, new { Success = success }, "Viewport zoomed to extents.");
    }
}

public class ZoomEntityTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "zoom_entity",
        "Zooms AutoCAD viewport to focus on a specific entity."
    )
    .AddProperty("handle", "string", "Hexadecimal handle of entity to zoom to", required: true);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? handle = GetString(arguments, "handle");
        if (string.IsNullOrWhiteSpace(handle))
            return ToolCallResult.Fail(callId, Definition.Name, "handle is required.");

        bool success = await cadService.ZoomEntityAsync(handle);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle }, $"Viewport zoomed to entity {handle}.");
    }
}

public class ZoomWindowTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "zoom_window",
        "Zooms AutoCAD viewport to a specified rectangular coordinate area."
    )
    .AddProperty("pt1X", "number", "X coordinate of first window corner", required: true)
    .AddProperty("pt1Y", "number", "Y coordinate of first window corner", required: true)
    .AddProperty("pt2X", "number", "X coordinate of second window corner", required: true)
    .AddProperty("pt2Y", "number", "Y coordinate of second window corner", required: true);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var p1 = new CadPoint3D(GetDouble(arguments, "pt1X"), GetDouble(arguments, "pt1Y"), 0);
        var p2 = new CadPoint3D(GetDouble(arguments, "pt2X"), GetDouble(arguments, "pt2Y"), 0);

        bool success = await cadService.ZoomWindowAsync(p1, p2);
        return ToolCallResult.Ok(callId, Definition.Name, new { Corner1 = p1, Corner2 = p2 }, "Viewport zoomed to specified window.");
    }
}
