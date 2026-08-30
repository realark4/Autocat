using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.Models;
using AutoCadAiPlugin.Core.ToolContracts;
using AutoCadAiPlugin.Tools.Base;
using AutoCadAiPlugin.Tools.GeometryCalculators;

namespace AutoCadAiPlugin.Tools.Implementations;

public class CreateLineTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_line",
        "Creates a straight line segment in AutoCAD between two 3D/2D points."
    )
    .AddProperty("startX", "number", "X coordinate of start point", required: true)
    .AddProperty("startY", "number", "Y coordinate of start point", required: true)
    .AddProperty("startZ", "number", "Z coordinate of start point (default 0)", required: false, defaultValue: 0.0)
    .AddProperty("endX", "number", "X coordinate of end point", required: true)
    .AddProperty("endY", "number", "Y coordinate of end point", required: true)
    .AddProperty("endZ", "number", "Z coordinate of end point (default 0)", required: false, defaultValue: 0.0)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var start = new CadPoint3D(GetDouble(arguments, "startX"), GetDouble(arguments, "startY"), GetDouble(arguments, "startZ"));
        var end = new CadPoint3D(GetDouble(arguments, "endX"), GetDouble(arguments, "endY"), GetDouble(arguments, "endZ"));
        string? layer = GetString(arguments, "layer");

        string handle = await cadService.CreateLineAsync(start, end, layer);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle, Start = start, End = end }, $"Line created successfully (Handle: {handle}).");
    }
}

public class CreateCircleTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_circle",
        "Creates a circle in AutoCAD given center coordinates and radius (or diameter)."
    )
    .AddProperty("centerX", "number", "X coordinate of circle center", required: true)
    .AddProperty("centerY", "number", "Y coordinate of circle center", required: true)
    .AddProperty("centerZ", "number", "Z coordinate of circle center (default 0)", required: false, defaultValue: 0.0)
    .AddProperty("radius", "number", "Radius of the circle", required: false)
    .AddProperty("diameter", "number", "Diameter of the circle (used if radius is omitted)", required: false)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var center = new CadPoint3D(GetDouble(arguments, "centerX"), GetDouble(arguments, "centerY"), GetDouble(arguments, "centerZ"));
        double radius = GetDouble(arguments, "radius");

        if (radius <= 0.00001)
        {
            double diameter = GetDouble(arguments, "diameter");
            if (diameter > 0.00001)
            {
                radius = diameter / 2.0;
            }
            else
            {
                return ToolCallResult.Fail(callId, Definition.Name, "Either 'radius' (> 0) or 'diameter' (> 0) must be specified.");
            }
        }

        string? layer = GetString(arguments, "layer");
        string handle = await cadService.CreateCircleAsync(center, radius, layer);

        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle, Center = center, Radius = radius, Diameter = radius * 2.0 }, $"Circle (R={radius:F2}, Ø={radius * 2.0:F2}) created at ({center.X:F2}, {center.Y:F2}) with Handle: {handle}.");
    }
}

public class CreateArcTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_arc",
        "Creates a circular arc in AutoCAD given center, radius, start angle, and end angle (in degrees)."
    )
    .AddProperty("centerX", "number", "X coordinate of arc center", required: true)
    .AddProperty("centerY", "number", "Y coordinate of arc center", required: true)
    .AddProperty("centerZ", "number", "Z coordinate of arc center (default 0)", required: false, defaultValue: 0.0)
    .AddProperty("radius", "number", "Radius of the arc", required: true, minimum: 0.0001)
    .AddProperty("startAngle", "number", "Start angle in degrees (0 = East/X+)", required: true)
    .AddProperty("endAngle", "number", "End angle in degrees (counter-clockwise)", required: true)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var center = new CadPoint3D(GetDouble(arguments, "centerX"), GetDouble(arguments, "centerY"), GetDouble(arguments, "centerZ"));
        double radius = GetDouble(arguments, "radius");
        double startAngle = GetDouble(arguments, "startAngle");
        double endAngle = GetDouble(arguments, "endAngle");
        string? layer = GetString(arguments, "layer");

        if (radius <= 0.0001)
            return ToolCallResult.Fail(callId, Definition.Name, "Radius must be greater than 0.");

        string handle = await cadService.CreateArcAsync(center, radius, startAngle, endAngle, layer);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle, Center = center, Radius = radius, StartAngle = startAngle, EndAngle = endAngle }, $"Arc created successfully (Handle: {handle}).");
    }
}

public class CreatePolylineTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_polyline",
        "Creates a 2D lightweight polyline connecting multiple vertex coordinates."
    )
    .AddProperty("vertices", "array", "Array of 2D vertex points [[x1, y1], [x2, y2], ...]", required: true)
    .AddProperty("closed", "boolean", "Whether the polyline is closed (default false)", required: false, defaultValue: false)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var vertices = GetPoint2DList(arguments, "vertices");
        if (vertices.Count < 2)
            return ToolCallResult.Fail(callId, Definition.Name, "Polyline requires at least 2 vertices.");

        bool closed = GetBool(arguments, "closed");
        string? layer = GetString(arguments, "layer");

        string handle = await cadService.CreatePolylineAsync(vertices, closed, layer);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle, VertexCount = vertices.Count, Closed = closed }, $"Polyline with {vertices.Count} vertices created (Handle: {handle}).");
    }
}

public class CreateRectangleTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_rectangle",
        "Creates a rectangular polyline from either corner points or origin + width and height, with optional corner fillets."
    )
    .AddProperty("corner1X", "number", "X coordinate of first corner / origin", required: false)
    .AddProperty("corner1Y", "number", "Y coordinate of first corner / origin", required: false)
    .AddProperty("corner2X", "number", "X coordinate of opposite corner (used if width is omitted)", required: false)
    .AddProperty("corner2Y", "number", "Y coordinate of opposite corner (used if height is omitted)", required: false)
    .AddProperty("width", "number", "Width of rectangle (along X axis)", required: false)
    .AddProperty("height", "number", "Height of rectangle (along Y axis)", required: false)
    .AddProperty("cornerRadius", "number", "Optional fillet radius for 4 corners (e.g. 20 for R20)", required: false, defaultValue: 0.0)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        double x1 = GetDouble(arguments, "corner1X", GetDouble(arguments, "x", 0.0));
        double y1 = GetDouble(arguments, "corner1Y", GetDouble(arguments, "y", 0.0));

        double width = GetDouble(arguments, "width");
        double height = GetDouble(arguments, "height");

        double x2, y2;
        if (width > 0.0001 && height > 0.0001)
        {
            x2 = x1 + width;
            y2 = y1 + height;
        }
        else
        {
            x2 = GetDouble(arguments, "corner2X");
            y2 = GetDouble(arguments, "corner2Y");
            width = Math.Abs(x2 - x1);
            height = Math.Abs(y2 - y1);
        }

        if (width <= 0.0001 || height <= 0.0001)
            return ToolCallResult.Fail(callId, Definition.Name, "Rectangle width and height must be greater than 0.");

        double cornerRadius = GetDouble(arguments, "cornerRadius");
        string? layer = GetString(arguments, "layer");

        var c1 = new CadPoint2D(Math.Min(x1, x2), Math.Min(y1, y2));
        var c2 = new CadPoint2D(Math.Max(x1, x2), Math.Max(y1, y2));
        var center = DeterministicGeometryEngine.CalculateRectangleCenter(c1, c2);

        string handle = await cadService.CreateRectangleAsync(c1, c2, cornerRadius, layer);

        return ToolCallResult.Ok(callId, Definition.Name, new
        {
            Handle = handle,
            Corner1 = c1,
            Corner2 = c2,
            Width = width,
            Height = height,
            Center = center,
            CornerRadius = cornerRadius
        }, $"Rectangle ({width:F2} × {height:F2}) created with center ({center.X:F2}, {center.Y:F2}), Handle: {handle}.");
    }
}

public class CreateEllipseTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_ellipse",
        "Creates an ellipse in AutoCAD given center, major axis vector, and radius ratio."
    )
    .AddProperty("centerX", "number", "X coordinate of ellipse center", required: true)
    .AddProperty("centerY", "number", "Y coordinate of ellipse center", required: true)
    .AddProperty("centerZ", "number", "Z coordinate of ellipse center (default 0)", required: false, defaultValue: 0.0)
    .AddProperty("majorAxisX", "number", "X component of major axis vector (semi-major length)", required: true)
    .AddProperty("majorAxisY", "number", "Y component of major axis vector", required: false, defaultValue: 0.0)
    .AddProperty("radiusRatio", "number", "Ratio of minor axis to major axis length (0.0 < ratio <= 1.0)", required: true, minimum: 0.0001, maximum: 1.0)
    .AddProperty("layer", "string", "Optional target layer", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var center = new CadPoint3D(GetDouble(arguments, "centerX"), GetDouble(arguments, "centerY"), GetDouble(arguments, "centerZ"));
        var majorVec = new CadPoint3D(GetDouble(arguments, "majorAxisX"), GetDouble(arguments, "majorAxisY"), 0);
        double radiusRatio = GetDouble(arguments, "radiusRatio", 0.5);
        string? layer = GetString(arguments, "layer");

        string handle = await cadService.CreateEllipseAsync(center, majorVec, radiusRatio, layer);
        return ToolCallResult.Ok(callId, Definition.Name, new { Handle = handle, Center = center }, $"Ellipse created successfully (Handle: {handle}).");
    }
}
