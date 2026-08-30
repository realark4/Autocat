using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.Models;
using AutoCadAiPlugin.Core.ToolContracts;
using AutoCadAiPlugin.Tools.Base;

namespace AutoCadAiPlugin.Tools.Implementations;

public class CreateLinearDimensionTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_linear_dimension",
        "Creates a linear horizontal or vertical dimension between two measurement points."
    )
    .AddProperty("pt1X", "number", "X coordinate of first dimension definition point", required: true)
    .AddProperty("pt1Y", "number", "Y coordinate of first dimension definition point", required: true)
    .AddProperty("pt2X", "number", "X coordinate of second dimension definition point", required: true)
    .AddProperty("pt2Y", "number", "Y coordinate of second dimension definition point", required: true)
    .AddProperty("textLocationX", "number", "X coordinate of dimension line / text placement", required: true)
    .AddProperty("textLocationY", "number", "Y coordinate of dimension line / text placement", required: true)
    .AddProperty("isHorizontal", "boolean", "True for horizontal dimension, false for vertical dimension", required: false, defaultValue: true)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var p1 = new CadPoint3D(GetDouble(arguments, "pt1X"), GetDouble(arguments, "pt1Y"), 0);
        var p2 = new CadPoint3D(GetDouble(arguments, "pt2X"), GetDouble(arguments, "pt2Y"), 0);
        var textLoc = new CadPoint3D(GetDouble(arguments, "textLocationX"), GetDouble(arguments, "textLocationY"), 0);
        bool isHoriz = GetBool(arguments, "isHorizontal", true);
        string? layer = GetString(arguments, "layer");

        string handle = await cadService.CreateLinearDimensionAsync(p1, p2, textLoc, isHoriz, layer);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle, IsHorizontal = isHoriz }, $"Linear dimension created (Handle: {handle}).");
    }
}

public class CreateAlignedDimensionTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_aligned_dimension",
        "Creates an aligned dimension parallel to the baseline between two points."
    )
    .AddProperty("pt1X", "number", "X coordinate of first point", required: true)
    .AddProperty("pt1Y", "number", "Y coordinate of first point", required: true)
    .AddProperty("pt2X", "number", "X coordinate of second point", required: true)
    .AddProperty("pt2Y", "number", "Y coordinate of second point", required: true)
    .AddProperty("textLocationX", "number", "X coordinate of dimension line placement", required: true)
    .AddProperty("textLocationY", "number", "Y coordinate of dimension line placement", required: true)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var p1 = new CadPoint3D(GetDouble(arguments, "pt1X"), GetDouble(arguments, "pt1Y"), 0);
        var p2 = new CadPoint3D(GetDouble(arguments, "pt2X"), GetDouble(arguments, "pt2Y"), 0);
        var textLoc = new CadPoint3D(GetDouble(arguments, "textLocationX"), GetDouble(arguments, "textLocationY"), 0);
        string? layer = GetString(arguments, "layer");

        string handle = await cadService.CreateAlignedDimensionAsync(p1, p2, textLoc, layer);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle }, $"Aligned dimension created (Handle: {handle}).");
    }
}

public class CreateRadiusDimensionTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_radius_dimension",
        "Creates a radial dimension (R) for a circle or arc."
    )
    .AddProperty("handle", "string", "Handle of the circle or arc entity", required: true)
    .AddProperty("textLocationX", "number", "X coordinate for the dimension text", required: true)
    .AddProperty("textLocationY", "number", "Y coordinate for the dimension text", required: true)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? handle = GetString(arguments, "handle");
        if (string.IsNullOrWhiteSpace(handle))
            return ToolCallResult.Fail(callId, Definition.Name, "handle is required.");

        var textLoc = new CadPoint3D(GetDouble(arguments, "textLocationX"), GetDouble(arguments, "textLocationY"), 0);
        string? layer = GetString(arguments, "layer");

        string dimHandle = await cadService.CreateRadiusDimensionAsync(handle, textLoc, layer);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = dimHandle, EntityHandle = handle }, $"Radius dimension created (Handle: {dimHandle}).");
    }
}

public class CreateDiameterDimensionTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_diameter_dimension",
        "Creates a diametric dimension (Ø) for a circle or arc."
    )
    .AddProperty("handle", "string", "Handle of the circle or arc entity", required: true)
    .AddProperty("textLocationX", "number", "X coordinate for the dimension text", required: true)
    .AddProperty("textLocationY", "number", "Y coordinate for the dimension text", required: true)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? handle = GetString(arguments, "handle");
        if (string.IsNullOrWhiteSpace(handle))
            return ToolCallResult.Fail(callId, Definition.Name, "handle is required.");

        var textLoc = new CadPoint3D(GetDouble(arguments, "textLocationX"), GetDouble(arguments, "textLocationY"), 0);
        string? layer = GetString(arguments, "layer");

        string dimHandle = await cadService.CreateDiameterDimensionAsync(handle, textLoc, layer);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = dimHandle, EntityHandle = handle }, $"Diameter dimension created (Handle: {dimHandle}).");
    }
}

public class CreateAngularDimensionTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_angular_dimension",
        "Creates an angular dimension from vertex and two ray end points."
    )
    .AddProperty("centerX", "number", "X coordinate of angle vertex", required: true)
    .AddProperty("centerY", "number", "Y coordinate of angle vertex", required: true)
    .AddProperty("pt1X", "number", "X coordinate of first ray point", required: true)
    .AddProperty("pt1Y", "number", "Y coordinate of first ray point", required: true)
    .AddProperty("pt2X", "number", "X coordinate of second ray point", required: true)
    .AddProperty("pt2Y", "number", "Y coordinate of second ray point", required: true)
    .AddProperty("arcPtX", "number", "X coordinate of dimension arc placement", required: true)
    .AddProperty("arcPtY", "number", "Y coordinate of dimension arc placement", required: true)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var center = new CadPoint3D(GetDouble(arguments, "centerX"), GetDouble(arguments, "centerY"), 0);
        var p1 = new CadPoint3D(GetDouble(arguments, "pt1X"), GetDouble(arguments, "pt1Y"), 0);
        var p2 = new CadPoint3D(GetDouble(arguments, "pt2X"), GetDouble(arguments, "pt2Y"), 0);
        var arcPt = new CadPoint3D(GetDouble(arguments, "arcPtX"), GetDouble(arguments, "arcPtY"), 0);
        string? layer = GetString(arguments, "layer");

        string dimHandle = await cadService.CreateAngularDimensionAsync(center, p1, p2, arcPt, layer);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = dimHandle }, $"Angular dimension created (Handle: {dimHandle}).");
    }
}
