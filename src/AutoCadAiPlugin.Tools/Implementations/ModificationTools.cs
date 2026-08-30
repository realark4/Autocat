using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.Models;
using AutoCadAiPlugin.Core.ToolContracts;
using AutoCadAiPlugin.Tools.Base;

namespace AutoCadAiPlugin.Tools.Implementations;

public class MoveEntityTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "move_entity",
        "Moves an AutoCAD entity from a base point to a destination point or by relative displacement vector (deltaX, deltaY).",
        RiskLevel.Medium
    )
    .AddProperty("handle", "string", "Hexadecimal handle of the entity to move (or 'selected' to move current selection)", required: true)
    .AddProperty("fromX", "number", "X coordinate of base point (default 0)", required: false, defaultValue: 0.0)
    .AddProperty("fromY", "number", "Y coordinate of base point (default 0)", required: false, defaultValue: 0.0)
    .AddProperty("toX", "number", "X coordinate of destination point (or deltaX if from is 0,0)", required: true)
    .AddProperty("toY", "number", "Y coordinate of destination point (or deltaY if from is 0,0)", required: true);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? handle = GetString(arguments, "handle");
        if (string.IsNullOrWhiteSpace(handle))
            return ToolCallResult.Fail(callId, Definition.Name, "handle is required.");

        if (handle.Equals("selected", StringComparison.OrdinalIgnoreCase))
        {
            var selected = await cadService.GetSelectedEntitiesAsync();
            if (selected.Count == 0)
                return ToolCallResult.Fail(callId, Definition.Name, "No entities currently selected to move.");
            handle = selected[0].Handle;
        }

        var fromPt = new CadPoint3D(GetDouble(arguments, "fromX"), GetDouble(arguments, "fromY"), 0);
        var toPt = new CadPoint3D(GetDouble(arguments, "toX"), GetDouble(arguments, "toY"), 0);

        bool success = await cadService.MoveEntityAsync(handle, fromPt, toPt);
        if (success)
        {
            double dx = toPt.X - fromPt.X;
            double dy = toPt.Y - fromPt.Y;
            return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle, DeltaX = dx, DeltaY = dy }, $"Entity {handle} moved by ΔX={dx:F2}, ΔY={dy:F2}.");
        }

        return ToolCallResult.Fail(callId, Definition.Name, $"Failed to move entity {handle}.");
    }
}

public class CopyEntityTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "copy_entity",
        "Duplicates an AutoCAD entity and moves the copy to a target point."
    )
    .AddProperty("handle", "string", "Hexadecimal handle of the entity to copy", required: true)
    .AddProperty("fromX", "number", "X coordinate of base point (default 0)", required: false, defaultValue: 0.0)
    .AddProperty("fromY", "number", "Y coordinate of base point (default 0)", required: false, defaultValue: 0.0)
    .AddProperty("toX", "number", "X coordinate of copy destination point", required: true)
    .AddProperty("toY", "number", "Y coordinate of copy destination point", required: true);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? handle = GetString(arguments, "handle");
        if (string.IsNullOrWhiteSpace(handle))
            return ToolCallResult.Fail(callId, Definition.Name, "handle is required.");

        var fromPt = new CadPoint3D(GetDouble(arguments, "fromX"), GetDouble(arguments, "fromY"), 0);
        var toPt = new CadPoint3D(GetDouble(arguments, "toX"), GetDouble(arguments, "toY"), 0);

        string newHandle = await cadService.CopyEntityAsync(handle, fromPt, toPt);
        return ToolCallResult.Ok(callId, Definition.Name, new { OriginalHandle = handle, NewHandle = newHandle }, $"Entity copied successfully (New Handle: {newHandle}).");
    }
}

public class RotateEntityTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "rotate_entity",
        "Rotates an entity around a base point by a given angle in degrees."
    )
    .AddProperty("handle", "string", "Hexadecimal handle of the entity to rotate", required: true)
    .AddProperty("basePointX", "number", "X coordinate of rotation center point", required: true)
    .AddProperty("basePointY", "number", "Y coordinate of rotation center point", required: true)
    .AddProperty("angleDegrees", "number", "Rotation angle in degrees (counter-clockwise)", required: true);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? handle = GetString(arguments, "handle");
        if (string.IsNullOrWhiteSpace(handle))
            return ToolCallResult.Fail(callId, Definition.Name, "handle is required.");

        var basePt = new CadPoint3D(GetDouble(arguments, "basePointX"), GetDouble(arguments, "basePointY"), 0);
        double angle = GetDouble(arguments, "angleDegrees");

        bool success = await cadService.RotateEntityAsync(handle, basePt, angle);
        if (success)
            return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle, Angle = angle }, $"Entity {handle} rotated by {angle}° around ({basePt.X:F2}, {basePt.Y:F2}).");

        return ToolCallResult.Fail(callId, Definition.Name, $"Failed to rotate entity {handle}.");
    }
}

public class ScaleEntityTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "scale_entity",
        "Scales an entity around a base point by a scale factor."
    )
    .AddProperty("handle", "string", "Hexadecimal handle of the entity to scale", required: true)
    .AddProperty("basePointX", "number", "X coordinate of scale base point", required: true)
    .AddProperty("basePointY", "number", "Y coordinate of scale base point", required: true)
    .AddProperty("scaleFactor", "number", "Uniform scale factor (e.g. 2.0 = double size, 0.5 = half size)", required: true, minimum: 0.0001);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? handle = GetString(arguments, "handle");
        if (string.IsNullOrWhiteSpace(handle))
            return ToolCallResult.Fail(callId, Definition.Name, "handle is required.");

        var basePt = new CadPoint3D(GetDouble(arguments, "basePointX"), GetDouble(arguments, "basePointY"), 0);
        double factor = GetDouble(arguments, "scaleFactor", 1.0);

        if (factor <= 0.0001)
            return ToolCallResult.Fail(callId, Definition.Name, "scaleFactor must be greater than 0.");

        bool success = await cadService.ScaleEntityAsync(handle, basePt, factor);
        if (success)
            return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle, Scale = factor }, $"Entity {handle} scaled by {factor}x.");

        return ToolCallResult.Fail(callId, Definition.Name, $"Failed to scale entity {handle}.");
    }
}

public class MirrorEntityTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "mirror_entity",
        "Mirrors an entity across a reflection axis line defined by two points."
    )
    .AddProperty("handle", "string", "Hexadecimal handle of the entity to mirror", required: true)
    .AddProperty("axisPt1X", "number", "X coordinate of mirror axis start point", required: true)
    .AddProperty("axisPt1Y", "number", "Y coordinate of mirror axis start point", required: true)
    .AddProperty("axisPt2X", "number", "X coordinate of mirror axis end point", required: true)
    .AddProperty("axisPt2Y", "number", "Y coordinate of mirror axis end point", required: true)
    .AddProperty("eraseSource", "boolean", "Whether to delete the original source entity (default false)", required: false, defaultValue: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? handle = GetString(arguments, "handle");
        if (string.IsNullOrWhiteSpace(handle))
            return ToolCallResult.Fail(callId, Definition.Name, "handle is required.");

        var p1 = new CadPoint3D(GetDouble(arguments, "axisPt1X"), GetDouble(arguments, "axisPt1Y"), 0);
        var p2 = new CadPoint3D(GetDouble(arguments, "axisPt2X"), GetDouble(arguments, "axisPt2Y"), 0);
        bool erase = GetBool(arguments, "eraseSource");

        string resultHandle = await cadService.MirrorEntityAsync(handle, p1, p2, erase);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = resultHandle, ErasedSource = erase }, $"Entity mirrored successfully (Handle: {resultHandle}).");
    }
}

public class EraseEntityTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "erase_entity",
        "Deletes/erases one or more entities from the drawing. High risk operation.",
        RiskLevel.High,
        requiresConfirmation: true
    )
    .AddProperty("handle", "string", "Hexadecimal handle of the entity to delete (or comma-separated handles)", required: false)
    .AddProperty("handles", "array", "Array of entity handles to delete", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var handles = GetStringList(arguments, "handles");
        string? singleHandle = GetString(arguments, "handle");

        if (!string.IsNullOrWhiteSpace(singleHandle))
        {
            if (singleHandle.Contains(","))
            {
                foreach (var h in singleHandle.Split(','))
                {
                    var trimmed = h.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !handles.Contains(trimmed)) handles.Add(trimmed);
                }
            }
            else if (!handles.Contains(singleHandle))
            {
                handles.Add(singleHandle);
            }
        }

        if (handles.Count == 0)
            return ToolCallResult.Fail(callId, Definition.Name, "No entity handles provided for deletion.");

        int count = await cadService.EraseEntitiesAsync(handles);
        return ToolCallResult.Ok(callId, Definition.Name, new { DeletedCount = count, Handles = handles }, $"Deleted {count} entity/entities from drawing.");
    }
}

public class FilletEntityTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "fillet_entity",
        "Applies a fillet radius between two AutoCAD entities."
    )
    .AddProperty("handle1", "string", "Handle of first entity", required: true)
    .AddProperty("handle2", "string", "Handle of second entity", required: true)
    .AddProperty("radius", "number", "Fillet radius", required: true, minimum: 0.0);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? h1 = GetString(arguments, "handle1");
        string? h2 = GetString(arguments, "handle2");
        double radius = GetDouble(arguments, "radius");

        if (string.IsNullOrWhiteSpace(h1) || string.IsNullOrWhiteSpace(h2))
            return ToolCallResult.Fail(callId, Definition.Name, "Both handle1 and handle2 are required.");

        bool success = await cadService.FilletEntityAsync(h1, h2, radius);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle1 = h1, Handle2 = h2, Radius = radius }, $"Fillet (R={radius:F2}) queued between {h1} and {h2}.");
    }
}

public class OffsetEntityTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "offset_entity",
        "Offsets a curve/polyline/line/circle by a specified distance."
    )
    .AddProperty("handle", "string", "Handle of entity to offset", required: true)
    .AddProperty("distance", "number", "Offset distance (> 0)", required: true, minimum: 0.0001)
    .AddProperty("sideX", "number", "X coordinate of side point to indicate offset direction", required: false, defaultValue: 0.0)
    .AddProperty("sideY", "number", "Y coordinate of side point", required: false, defaultValue: 0.0);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? handle = GetString(arguments, "handle");
        if (string.IsNullOrWhiteSpace(handle))
            return ToolCallResult.Fail(callId, Definition.Name, "handle is required.");

        double dist = GetDouble(arguments, "distance");
        var sidePt = new CadPoint3D(GetDouble(arguments, "sideX"), GetDouble(arguments, "sideY"), 0);

        string newHandle = await cadService.OffsetEntityAsync(handle, dist, sidePt);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = newHandle, Distance = dist }, $"Entity offset by {dist:F2} (New Handle: {newHandle}).");
    }
}

public class TrimEntityTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "trim_entity",
        "Trims an entity against a cutting edge."
    )
    .AddProperty("cuttingEdgeHandle", "string", "Handle of boundary/cutting edge entity", required: true)
    .AddProperty("entityToTrimHandle", "string", "Handle of entity to trim", required: true)
    .AddProperty("pickPointX", "number", "X coordinate on the side to trim away", required: false, defaultValue: 0.0)
    .AddProperty("pickPointY", "number", "Y coordinate on the side to trim away", required: false, defaultValue: 0.0);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? cEdge = GetString(arguments, "cuttingEdgeHandle");
        string? toTrim = GetString(arguments, "entityToTrimHandle");

        if (string.IsNullOrWhiteSpace(cEdge) || string.IsNullOrWhiteSpace(toTrim))
            return ToolCallResult.Fail(callId, Definition.Name, "cuttingEdgeHandle and entityToTrimHandle are required.");

        var pickPt = new CadPoint3D(GetDouble(arguments, "pickPointX"), GetDouble(arguments, "pickPointY"), 0);
        bool success = await cadService.TrimEntityAsync(cEdge, toTrim, pickPt);
        return ToolCallResult.Ok(callId, Definition.Name, new { CuttingEdge = cEdge, Trimmed = toTrim }, $"Trim command executed for {toTrim}.");
    }
}

public class ExtendEntityTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "extend_entity",
        "Extends an entity to meet a boundary edge."
    )
    .AddProperty("boundaryHandle", "string", "Handle of boundary entity", required: true)
    .AddProperty("entityToExtendHandle", "string", "Handle of entity to extend", required: true)
    .AddProperty("pickPointX", "number", "X coordinate on the end to extend", required: false, defaultValue: 0.0)
    .AddProperty("pickPointY", "number", "Y coordinate on the end to extend", required: false, defaultValue: 0.0);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? boundary = GetString(arguments, "boundaryHandle");
        string? toExtend = GetString(arguments, "entityToExtendHandle");

        if (string.IsNullOrWhiteSpace(boundary) || string.IsNullOrWhiteSpace(toExtend))
            return ToolCallResult.Fail(callId, Definition.Name, "boundaryHandle and entityToExtendHandle are required.");

        var pickPt = new CadPoint3D(GetDouble(arguments, "pickPointX"), GetDouble(arguments, "pickPointY"), 0);
        bool success = await cadService.ExtendEntityAsync(boundary, toExtend, pickPt);
        return ToolCallResult.Ok(callId, Definition.Name, new { Boundary = boundary, Extended = toExtend }, $"Extend command executed for {toExtend}.");
    }
}
