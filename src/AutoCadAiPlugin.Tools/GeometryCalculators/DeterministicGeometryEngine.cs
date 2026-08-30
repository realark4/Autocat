using System;
using System.Collections.Generic;
using AutoCadAiPlugin.Core.Models;

namespace AutoCadAiPlugin.Tools.GeometryCalculators;

public static class DeterministicGeometryEngine
{
    public static CadPoint2D CalculateRectangleCenter(CadPoint2D corner1, CadPoint2D corner2)
    {
        return new CadPoint2D((corner1.X + corner2.X) / 2.0, (corner1.Y + corner2.Y) / 2.0);
    }

    public static CadPoint3D CalculateBoundingBoxCenter(CadBoundingBox bbox)
    {
        return bbox.Center;
    }

    public static (CadPoint2D Corner1, CadPoint2D Corner2) CalculateRectangleFromOriginSize(CadPoint2D origin, double width, double height)
    {
        return (origin, new CadPoint2D(origin.X + width, origin.Y + height));
    }

    public static CadPoint2D CalculateRectangleCenterFromOriginSize(CadPoint2D origin, double width, double height)
    {
        return new CadPoint2D(origin.X + width / 2.0, origin.Y + height / 2.0);
    }

    public static CadPoint2D CalculateRelativePoint(CadPoint2D basePoint, double offsetX, double offsetY)
    {
        return new CadPoint2D(basePoint.X + offsetX, basePoint.Y + offsetY);
    }

    public static CadPoint3D CalculateRelativePoint3D(CadPoint3D basePoint, double offsetX, double offsetY, double offsetZ = 0.0)
    {
        return new CadPoint3D(basePoint.X + offsetX, basePoint.Y + offsetY, basePoint.Z + offsetZ);
    }

    public static CadPoint3D CalculateMidPoint(CadPoint3D pt1, CadPoint3D pt2)
    {
        return new CadPoint3D((pt1.X + pt2.X) / 2.0, (pt1.Y + pt2.Y) / 2.0, (pt1.Z + pt2.Z) / 2.0);
    }

    public static List<CadPoint2D> GenerateRegularPolygonVertices(CadPoint2D center, double radius, int sides)
    {
        var vertices = new List<CadPoint2D>();
        if (sides < 3) return vertices;

        double angleStep = 2.0 * Math.PI / sides;
        for (int i = 0; i < sides; i++)
        {
            double angle = i * angleStep;
            double x = center.X + radius * Math.Cos(angle);
            double y = center.Y + radius * Math.Sin(angle);
            vertices.Add(new CadPoint2D(x, y));
        }

        return vertices;
    }

    public static (CadPoint3D TopDimLoc, CadPoint3D RightDimLoc) CalculateDimensionOffsets(CadPoint2D corner1, CadPoint2D corner2, double offsetDistance = 15.0)
    {
        double minX = Math.Min(corner1.X, corner2.X);
        double maxX = Math.Max(corner1.X, corner2.X);
        double minY = Math.Min(corner1.Y, corner2.Y);
        double maxY = Math.Max(corner1.Y, corner2.Y);

        var topDimLoc = new CadPoint3D((minX + maxX) / 2.0, maxY + offsetDistance, 0);
        var rightDimLoc = new CadPoint3D(maxX + offsetDistance, (minY + maxY) / 2.0, 0);

        return (topDimLoc, rightDimLoc);
    }
}
