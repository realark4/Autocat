using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AutoCadAiPlugin.Core.Models;

namespace AutoCadAiPlugin.Cad.Converters;

public static class CadTypeConverter
{
    public static Point3d ToPoint3d(this CadPoint3D pt) => new(pt.X, pt.Y, pt.Z);

    public static Point3d ToPoint3d(this CadPoint2D pt, double z = 0.0) => new(pt.X, pt.Y, z);

    public static Point2d ToPoint2d(this CadPoint2D pt) => new(pt.X, pt.Y);

    public static CadPoint3D ToCadPoint3D(this Point3d pt) => new(pt.X, pt.Y, pt.Z);

    public static CadPoint2D ToCadPoint2D(this Point2d pt) => new(pt.X, pt.Y);

    public static CadPoint2D ToCadPoint2D(this Point3d pt) => new(pt.X, pt.Y);

    public static CadBoundingBox? ToCadBoundingBox(this Extents3d? extents)
    {
        if (!extents.HasValue) return null;
        return new CadBoundingBox(extents.Value.MinPoint.ToCadPoint3D(), extents.Value.MaxPoint.ToCadPoint3D());
    }

    public static ObjectId ParseHandleToObjectId(Database db, string handleStr)
    {
        if (long.TryParse(handleStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long handleVal))
        {
            var handle = new Handle(handleVal);
            return db.GetObjectId(false, handle, 0);
        }

        throw new ArgumentException($"Invalid AutoCAD handle string: {handleStr}");
    }

    public static CadEntityInfo ToCadEntityInfo(Entity entity)
    {
        var handle = entity.Handle.ToString();
        var objectId = entity.ObjectId.ToString();
        var entityType = entity.GetType().Name;
        var layer = entity.Layer;
        var color = entity.Color.ToString();

        CadBoundingBox? bbox = null;
        try
        {
            if (entity.Bounds.HasValue)
            {
                bbox = entity.Bounds.ToCadBoundingBox();
            }
        }
        catch
        {
            // Ignored if bounds cannot be calculated
        }

        var props = new Dictionary<string, object>();

        if (entity is Line line)
        {
            props["StartPoint"] = line.StartPoint.ToCadPoint3D();
            props["EndPoint"] = line.EndPoint.ToCadPoint3D();
            props["Length"] = line.Length;
        }
        else if (entity is Circle circle)
        {
            props["Center"] = circle.Center.ToCadPoint3D();
            props["Radius"] = circle.Radius;
            props["Diameter"] = circle.Radius * 2.0;
            props["Area"] = circle.Area;
        }
        else if (entity is Arc arc)
        {
            props["Center"] = arc.Center.ToCadPoint3D();
            props["Radius"] = arc.Radius;
            props["StartAngle"] = arc.StartAngle * (180.0 / Math.PI);
            props["EndAngle"] = arc.EndAngle * (180.0 / Math.PI);
            props["Length"] = arc.Length;
        }
        else if (entity is Polyline pline)
        {
            props["NumberOfVertices"] = pline.NumberOfVertices;
            props["Closed"] = pline.Closed;
            props["Length"] = pline.Length;
            props["Area"] = pline.Area;
        }

        return new CadEntityInfo(handle, objectId, entityType, layer, color, bbox, props);
    }
}
