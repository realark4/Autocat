using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AutoCadAiPlugin.Cad.Converters;
using AutoCadAiPlugin.Cad.Execution;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.Models;

namespace AutoCadAiPlugin.Cad.Services;

public class CadService : ICadService
{
    public Task<CadDrawingInfo> GetDrawingInfoAsync()
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var doc = scope.Document;
            var db = scope.Database;

            var docName = doc.Name ?? "Drawing";
            var dbPath = db.Filename ?? string.Empty;
            var currentSpace = db.TileMode ? "ModelSpace" : "PaperSpace";
            
            var layerTable = (LayerTable)scope.Transaction.GetObject(db.LayerTableId, OpenMode.ForRead);
            var activeLayerRec = (LayerTableRecord)scope.Transaction.GetObject(db.Clayer, OpenMode.ForRead);
            var activeLayer = activeLayerRec.Name;

            var layerNames = new List<string>();
            foreach (ObjectId id in layerTable)
            {
                var ltr = (LayerTableRecord)scope.Transaction.GetObject(id, OpenMode.ForRead);
                layerNames.Add(ltr.Name);
            }

            var linearUnits = (CadUnitType)(int)db.Insunits;

            var btr = scope.GetCurrentSpace(OpenMode.ForRead);
            int count = 0;
            foreach (ObjectId id in btr)
            {
                if (!id.IsErased) count++;
            }

            return new CadDrawingInfo(
                docName,
                dbPath,
                currentSpace,
                activeLayer,
                "World",
                linearUnits,
                count,
                layerNames
            );
        });
    }

    public Task<string> GetActiveLayerAsync()
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var ltr = (LayerTableRecord)scope.Transaction.GetObject(scope.Database.Clayer, OpenMode.ForRead);
            return ltr.Name;
        });
    }

    public Task<List<CadLayerInfo>> GetLayersAsync()
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);
            var result = new List<CadLayerInfo>();

            foreach (ObjectId id in layerTable)
            {
                var ltr = (LayerTableRecord)scope.Transaction.GetObject(id, OpenMode.ForRead);
                result.Add(new CadLayerInfo(
                    ltr.Name,
                    ltr.Color.ColorNameForDisplay,
                    ltr.IsLocked,
                    ltr.IsFrozen,
                    ltr.IsOff
                ));
            }

            return result;
        });
    }

    public Task<bool> CreateLayerAsync(string layerName, string? colorName = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForWrite);

            if (layerTable.Has(layerName))
            {
                return false; // already exists
            }

            var newLayer = new LayerTableRecord
            {
                Name = layerName
            };

            if (!string.IsNullOrWhiteSpace(colorName))
            {
                if (short.TryParse(colorName, out short colorIndex))
                {
                    newLayer.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
                }
                else
                {
                    short index = colorName.ToLowerInvariant() switch
                    {
                        "red" => 1,
                        "yellow" => 2,
                        "green" => 3,
                        "cyan" => 4,
                        "blue" => 5,
                        "magenta" => 6,
                        "white" => 7,
                        _ => 7
                    };
                    newLayer.Color = Color.FromColorIndex(ColorMethod.ByAci, index);
                }
            }

            layerTable.Add(newLayer);
            scope.Transaction.AddNewlyCreatedDBObject(newLayer, true);
            scope.Commit();
            return true;
        });
    }

    public Task<bool> SetCurrentLayerAsync(string layerName)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);

            if (!layerTable.Has(layerName))
            {
                return false;
            }

            var layerId = layerTable[layerName];
            scope.Database.Clayer = layerId;
            scope.Commit();
            return true;
        });
    }

    public Task<CadUnitType> GetLinearUnitsAsync()
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            return (CadUnitType)(int)scope.Database.Insunits;
        });
    }

    public Task<List<CadEntityInfo>> GetSelectedEntitiesAsync()
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return new List<CadEntityInfo>();

            var ed = doc.Editor;
            var selResult = ed.SelectImplied();

            var result = new List<CadEntityInfo>();
            if (selResult.Status != PromptStatus.OK || selResult.Value == null)
            {
                return result;
            }

            using var scope = new CadTransactionScope();
            foreach (SelectedObject selObj in selResult.Value)
            {
                if (selObj != null && !selObj.ObjectId.IsNull && !selObj.ObjectId.IsErased)
                {
                    var entity = (Entity)scope.Transaction.GetObject(selObj.ObjectId, OpenMode.ForRead);
                    result.Add(CadTypeConverter.ToCadEntityInfo(entity));
                }
            }

            return result;
        });
    }

    public Task<List<string>> SelectEntitiesAsync(List<string> handles)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return new List<string>();

            using var scope = new CadTransactionScope();
            var objIds = new List<ObjectId>();
            var selectedHandles = new List<string>();

            foreach (var h in handles)
            {
                try
                {
                    var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, h);
                    if (!id.IsNull && !id.IsErased)
                    {
                        objIds.Add(id);
                        selectedHandles.Add(h);
                    }
                }
                catch
                {
                    // Ignore unparseable handles
                }
            }

            if (objIds.Count > 0)
            {
                doc.Editor.SetImpliedSelection(objIds.ToArray());
            }

            return selectedHandles;
        });
    }

    public Task<CadEntityInfo?> GetEntityInfoAsync(string handle)
    {
        return CadDispatcher.RunOnCadThreadAsync<CadEntityInfo?>(() =>
        {
            using var scope = new CadTransactionScope();
            try
            {
                var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, handle);
                if (id.IsNull || id.IsErased) return null;

                var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForRead);
                return CadTypeConverter.ToCadEntityInfo(entity);
            }
            catch
            {
                return null;
            }
        });
    }

    public Task<CadBoundingBox?> GetBoundingBoxAsync(string handle)
    {
        return CadDispatcher.RunOnCadThreadAsync<CadBoundingBox?>(() =>
        {
            using var scope = new CadTransactionScope();
            try
            {
                var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, handle);
                if (id.IsNull || id.IsErased) return null;

                var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForRead);
                return entity.Bounds.ToCadBoundingBox();
            }
            catch
            {
                return null;
            }
        });
    }

    public Task<List<CadEntityInfo>> GetEntitiesInWindowAsync(CadPoint3D pt1, CadPoint3D pt2)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return new List<CadEntityInfo>();

            var selResult = doc.Editor.SelectWindow(pt1.ToPoint3d(), pt2.ToPoint3d());
            var result = new List<CadEntityInfo>();
            if (selResult.Status != PromptStatus.OK || selResult.Value == null)
            {
                return result;
            }

            using var scope = new CadTransactionScope();
            foreach (SelectedObject selObj in selResult.Value)
            {
                if (selObj != null && !selObj.ObjectId.IsNull && !selObj.ObjectId.IsErased)
                {
                    var entity = (Entity)scope.Transaction.GetObject(selObj.ObjectId, OpenMode.ForRead);
                    result.Add(CadTypeConverter.ToCadEntityInfo(entity));
                }
            }

            return result;
        });
    }

    public Task<List<CadEntityInfo>> GetEntitiesByLayerAsync(string layerName)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForRead);
            var result = new List<CadEntityInfo>();

            foreach (ObjectId id in btr)
            {
                if (!id.IsErased)
                {
                    var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForRead);
                    if (string.Equals(entity.Layer, layerName, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(CadTypeConverter.ToCadEntityInfo(entity));
                    }
                }
            }

            return result;
        });
    }

    public Task<List<CadEntityInfo>> GetEntitiesByTypeAsync(string entityType)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForRead);
            var result = new List<CadEntityInfo>();

            foreach (ObjectId id in btr)
            {
                if (!id.IsErased)
                {
                    var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForRead);
                    if (entity.GetType().Name.Equals(entityType, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(CadTypeConverter.ToCadEntityInfo(entity));
                    }
                }
            }

            return result;
        });
    }

    public Task<string> CreateLineAsync(CadPoint3D startPoint, CadPoint3D endPoint, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);

            var line = new Line(startPoint.ToPoint3d(), endPoint.ToPoint3d());
            if (!string.IsNullOrWhiteSpace(layer)) line.Layer = layer;

            btr.AppendEntity(line);
            scope.Transaction.AddNewlyCreatedDBObject(line, true);
            scope.Commit();

            return line.Handle.ToString();
        });
    }

    public Task<string> CreateCircleAsync(CadPoint3D center, double radius, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);

            var circle = new Circle(center.ToPoint3d(), Vector3d.ZAxis, radius);
            if (!string.IsNullOrWhiteSpace(layer)) circle.Layer = layer;

            btr.AppendEntity(circle);
            scope.Transaction.AddNewlyCreatedDBObject(circle, true);
            scope.Commit();

            return circle.Handle.ToString();
        });
    }

    public Task<string> CreateArcAsync(CadPoint3D center, double radius, double startAngleDegrees, double endAngleDegrees, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);

            double startRad = startAngleDegrees * (Math.PI / 180.0);
            double endRad = endAngleDegrees * (Math.PI / 180.0);

            var arc = new Arc(center.ToPoint3d(), radius, startRad, endRad);
            if (!string.IsNullOrWhiteSpace(layer)) arc.Layer = layer;

            btr.AppendEntity(arc);
            scope.Transaction.AddNewlyCreatedDBObject(arc, true);
            scope.Commit();

            return arc.Handle.ToString();
        });
    }

    public Task<string> CreatePolylineAsync(List<CadPoint2D> vertices, bool closed = false, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            if (vertices == null || vertices.Count < 2)
                throw new ArgumentException("Polyline requires at least 2 vertices.");

            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);

            var pline = new Polyline();
            for (int i = 0; i < vertices.Count; i++)
            {
                pline.AddVertexAt(i, vertices[i].ToPoint2d(), 0, 0, 0);
            }

            pline.Closed = closed;
            if (!string.IsNullOrWhiteSpace(layer)) pline.Layer = layer;

            btr.AppendEntity(pline);
            scope.Transaction.AddNewlyCreatedDBObject(pline, true);
            scope.Commit();

            return pline.Handle.ToString();
        });
    }

    public Task<string> CreateRectangleAsync(CadPoint2D corner1, CadPoint2D corner2, double cornerRadius = 0.0, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);

            double minX = Math.Min(corner1.X, corner2.X);
            double maxX = Math.Max(corner1.X, corner2.X);
            double minY = Math.Min(corner1.Y, corner2.Y);
            double maxY = Math.Max(corner1.Y, corner2.Y);

            var pline = new Polyline();

            if (cornerRadius <= 0.0001)
            {
                pline.AddVertexAt(0, new Point2d(minX, minY), 0, 0, 0);
                pline.AddVertexAt(1, new Point2d(maxX, minY), 0, 0, 0);
                pline.AddVertexAt(2, new Point2d(maxX, maxY), 0, 0, 0);
                pline.AddVertexAt(3, new Point2d(minX, maxY), 0, 0, 0);
                pline.Closed = true;
            }
            else
            {
                double bulge = Math.Tan(Math.PI / 8.0); // 45 degree half angle for 90 deg corner
                double r = Math.Min(cornerRadius, Math.Min((maxX - minX) / 2.0, (maxY - minY) / 2.0));

                pline.AddVertexAt(0, new Point2d(minX + r, minY), 0, 0, 0);
                pline.AddVertexAt(1, new Point2d(maxX - r, minY), bulge, 0, 0);
                pline.AddVertexAt(2, new Point2d(maxX, minY + r), 0, 0, 0);
                pline.AddVertexAt(3, new Point2d(maxX, maxY - r), bulge, 0, 0);
                pline.AddVertexAt(4, new Point2d(maxX - r, maxY), 0, 0, 0);
                pline.AddVertexAt(5, new Point2d(minX + r, maxY), bulge, 0, 0);
                pline.AddVertexAt(6, new Point2d(minX, maxY - r), 0, 0, 0);
                pline.AddVertexAt(7, new Point2d(minX, minY + r), bulge, 0, 0);
                pline.Closed = true;
            }

            if (!string.IsNullOrWhiteSpace(layer)) pline.Layer = layer;

            btr.AppendEntity(pline);
            scope.Transaction.AddNewlyCreatedDBObject(pline, true);
            scope.Commit();

            return pline.Handle.ToString();
        });
    }

    public Task<string> CreateEllipseAsync(CadPoint3D center, CadPoint3D majorAxisVector, double radiusRatio, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);

            var ellipse = new Ellipse(
                center.ToPoint3d(),
                Vector3d.ZAxis,
                new Vector3d(majorAxisVector.X, majorAxisVector.Y, majorAxisVector.Z),
                radiusRatio,
                0,
                2 * Math.PI
            );

            if (!string.IsNullOrWhiteSpace(layer)) ellipse.Layer = layer;

            btr.AppendEntity(ellipse);
            scope.Transaction.AddNewlyCreatedDBObject(ellipse, true);
            scope.Commit();

            return ellipse.Handle.ToString();
        });
    }

    public Task<bool> MoveEntityAsync(string handle, CadPoint3D fromPoint, CadPoint3D toPoint)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, handle);
            var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForWrite);

            var displacement = toPoint.ToPoint3d() - fromPoint.ToPoint3d();
            var matrix = Matrix3d.Displacement(displacement);

            entity.TransformBy(matrix);
            scope.Commit();
            return true;
        });
    }

    public Task<string> CopyEntityAsync(string handle, CadPoint3D fromPoint, CadPoint3D toPoint)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, handle);
            var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForRead);

            var clone = (Entity)entity.Clone();
            var displacement = toPoint.ToPoint3d() - fromPoint.ToPoint3d();
            var matrix = Matrix3d.Displacement(displacement);
            clone.TransformBy(matrix);

            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);
            btr.AppendEntity(clone);
            scope.Transaction.AddNewlyCreatedDBObject(clone, true);
            scope.Commit();

            return clone.Handle.ToString();
        });
    }

    public Task<bool> RotateEntityAsync(string handle, CadPoint3D basePoint, double rotationAngleDegrees)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, handle);
            var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForWrite);

            double angleRad = rotationAngleDegrees * (Math.PI / 180.0);
            var matrix = Matrix3d.Rotation(angleRad, Vector3d.ZAxis, basePoint.ToPoint3d());

            entity.TransformBy(matrix);
            scope.Commit();
            return true;
        });
    }

    public Task<bool> ScaleEntityAsync(string handle, CadPoint3D basePoint, double scaleFactor)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, handle);
            var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForWrite);

            var matrix = Matrix3d.Scaling(scaleFactor, basePoint.ToPoint3d());
            entity.TransformBy(matrix);
            scope.Commit();
            return true;
        });
    }

    public Task<string> MirrorEntityAsync(string handle, CadPoint3D axisPt1, CadPoint3D axisPt2, bool eraseSource = false)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, handle);
            var entity = (Entity)scope.Transaction.GetObject(id, eraseSource ? OpenMode.ForWrite : OpenMode.ForRead);

            var mirrorPlane = new Line3d(axisPt1.ToPoint3d(), axisPt2.ToPoint3d());
            var matrix = Matrix3d.Mirroring(mirrorPlane);

            if (eraseSource)
            {
                entity.TransformBy(matrix);
                scope.Commit();
                return entity.Handle.ToString();
            }
            else
            {
                var clone = (Entity)entity.Clone();
                clone.TransformBy(matrix);
                var btr = scope.GetCurrentSpace(OpenMode.ForWrite);
                btr.AppendEntity(clone);
                scope.Transaction.AddNewlyCreatedDBObject(clone, true);
                scope.Commit();
                return clone.Handle.ToString();
            }
        });
    }

    public Task<bool> FilletEntityAsync(string handle1, string handle2, double radius)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var doc = scope.Document;
            doc.SendStringToExecute($"._FILLET R {radius:F4} ._FILLET (handent \"{handle1}\") (handent \"{handle2}\") ", true, false, false);
            return true;
        });
    }

    public Task<string> OffsetEntityAsync(string handle, double distance, CadPoint3D sidePoint)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, handle);
            var curve = (Curve)scope.Transaction.GetObject(id, OpenMode.ForRead);

            var curves = curve.GetOffsetCurves(distance);
            if (curves.Count == 0)
            {
                curves = curve.GetOffsetCurves(-distance);
            }

            if (curves.Count > 0)
            {
                var btr = scope.GetCurrentSpace(OpenMode.ForWrite);
                var offsetEntity = (Entity)curves[0];
                btr.AppendEntity(offsetEntity);
                scope.Transaction.AddNewlyCreatedDBObject(offsetEntity, true);
                scope.Commit();
                return offsetEntity.Handle.ToString();
            }

            throw new InvalidOperationException("Failed to calculate offset curve.");
        });
    }

    public Task<bool> TrimEntityAsync(string cuttingEdgeHandle, string entityToTrimHandle, CadPoint3D pickPoint)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var doc = scope.Document;
            doc.SendStringToExecute($"._TRIM (handent \"{cuttingEdgeHandle}\")  (handent \"{entityToTrimHandle}\") ", true, false, false);
            return true;
        });
    }

    public Task<bool> ExtendEntityAsync(string boundaryHandle, string entityToExtendHandle, CadPoint3D pickPoint)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var doc = scope.Document;
            doc.SendStringToExecute($"._EXTEND (handent \"{boundaryHandle}\")  (handent \"{entityToExtendHandle}\") ", true, false, false);
            return true;
        });
    }

    public Task<bool> EraseEntityAsync(string handle)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, handle);
            var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForWrite);
            entity.Erase(true);
            scope.Commit();
            return true;
        });
    }

    public Task<int> EraseEntitiesAsync(List<string> handles)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            int count = 0;
            foreach (var handle in handles)
            {
                try
                {
                    var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, handle);
                    if (!id.IsNull && !id.IsErased)
                    {
                        var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForWrite);
                        entity.Erase(true);
                        count++;
                    }
                }
                catch
                {
                    // Continue with remaining handles
                }
            }

            scope.Commit();
            return count;
        });
    }

    public Task<string> CreateLinearDimensionAsync(CadPoint3D pt1, CadPoint3D pt2, CadPoint3D textLocation, bool isHorizontal, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);

            double rotationAngle = isHorizontal ? 0.0 : Math.PI / 2.0;
            var dim = new RotatedDimension(
                rotationAngle,
                pt1.ToPoint3d(),
                pt2.ToPoint3d(),
                textLocation.ToPoint3d(),
                string.Empty,
                scope.Database.Dimstyle
            );

            if (!string.IsNullOrWhiteSpace(layer)) dim.Layer = layer;

            btr.AppendEntity(dim);
            scope.Transaction.AddNewlyCreatedDBObject(dim, true);
            scope.Commit();

            return dim.Handle.ToString();
        });
    }

    public Task<string> CreateAlignedDimensionAsync(CadPoint3D pt1, CadPoint3D pt2, CadPoint3D textLocation, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);

            var dim = new AlignedDimension(
                pt1.ToPoint3d(),
                pt2.ToPoint3d(),
                textLocation.ToPoint3d(),
                string.Empty,
                scope.Database.Dimstyle
            );

            if (!string.IsNullOrWhiteSpace(layer)) dim.Layer = layer;

            btr.AppendEntity(dim);
            scope.Transaction.AddNewlyCreatedDBObject(dim, true);
            scope.Commit();

            return dim.Handle.ToString();
        });
    }

    public Task<string> CreateRadiusDimensionAsync(string circleOrArcHandle, CadPoint3D textLocation, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, circleOrArcHandle);
            var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForRead);

            Point3d center;
            double radius;

            if (entity is Circle c)
            {
                center = c.Center;
                radius = c.Radius;
            }
            else if (entity is Arc a)
            {
                center = a.Center;
                radius = a.Radius;
            }
            else
            {
                throw new ArgumentException("Entity must be a Circle or Arc to create a radial dimension.");
            }

            var dir = (textLocation.ToPoint3d() - center).GetNormal();
            var chordPoint = center + dir * radius;

            var dim = new RadialDimension(
                center,
                chordPoint,
                0,
                string.Empty,
                scope.Database.Dimstyle
            );

            if (!string.IsNullOrWhiteSpace(layer)) dim.Layer = layer;

            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);
            btr.AppendEntity(dim);
            scope.Transaction.AddNewlyCreatedDBObject(dim, true);
            scope.Commit();

            return dim.Handle.ToString();
        });
    }

    public Task<string> CreateDiameterDimensionAsync(string circleOrArcHandle, CadPoint3D textLocation, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var id = CadTypeConverter.ParseHandleToObjectId(scope.Database, circleOrArcHandle);
            var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForRead);

            Point3d center;
            double radius;

            if (entity is Circle c)
            {
                center = c.Center;
                radius = c.Radius;
            }
            else if (entity is Arc a)
            {
                center = a.Center;
                radius = a.Radius;
            }
            else
            {
                throw new ArgumentException("Entity must be a Circle or Arc to create a diametric dimension.");
            }

            var dir = (textLocation.ToPoint3d() - center).GetNormal();
            var chordPoint = center + dir * radius;
            var farChordPoint = center - dir * radius;

            var dim = new DiametricDimension(
                chordPoint,
                farChordPoint,
                0,
                string.Empty,
                scope.Database.Dimstyle
            );

            if (!string.IsNullOrWhiteSpace(layer)) dim.Layer = layer;

            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);
            btr.AppendEntity(dim);
            scope.Transaction.AddNewlyCreatedDBObject(dim, true);
            scope.Commit();

            return dim.Handle.ToString();
        });
    }

    public Task<string> CreateAngularDimensionAsync(CadPoint3D centerPoint, CadPoint3D pt1, CadPoint3D pt2, CadPoint3D arcPoint, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);

            var dim = new Point3AngularDimension(
                centerPoint.ToPoint3d(),
                pt1.ToPoint3d(),
                pt2.ToPoint3d(),
                arcPoint.ToPoint3d(),
                string.Empty,
                scope.Database.Dimstyle
            );

            if (!string.IsNullOrWhiteSpace(layer)) dim.Layer = layer;

            btr.AppendEntity(dim);
            scope.Transaction.AddNewlyCreatedDBObject(dim, true);
            scope.Commit();

            return dim.Handle.ToString();
        });
    }

    public Task<string> CreateTextAsync(string text, CadPoint3D insertionPoint, double height, double rotationDegrees = 0.0, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);

            var dbText = new DBText
            {
                Position = insertionPoint.ToPoint3d(),
                Height = height,
                TextString = text,
                Rotation = rotationDegrees * (Math.PI / 180.0)
            };

            if (!string.IsNullOrWhiteSpace(layer)) dbText.Layer = layer;

            btr.AppendEntity(dbText);
            scope.Transaction.AddNewlyCreatedDBObject(dbText, true);
            scope.Commit();

            return dbText.Handle.ToString();
        });
    }

    public Task<string> CreateMTextAsync(string text, CadPoint3D insertionPoint, double width, double height, string? layer = null)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            using var scope = new CadTransactionScope();
            var btr = scope.GetCurrentSpace(OpenMode.ForWrite);

            var mText = new MText
            {
                Location = insertionPoint.ToPoint3d(),
                Width = width,
                TextHeight = height,
                Contents = text
            };

            if (!string.IsNullOrWhiteSpace(layer)) mText.Layer = layer;

            btr.AppendEntity(mText);
            scope.Transaction.AddNewlyCreatedDBObject(mText, true);
            scope.Commit();

            return mText.Handle.ToString();
        });
    }

    public Task<bool> ZoomExtentsAsync()
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.SendStringToExecute("._ZOOM _E ", true, false, false);
            return true;
        });
    }

    public Task<bool> ZoomEntityAsync(string handle)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.SendStringToExecute($"._ZOOM _O (handent \"{handle}\")  ", true, false, false);
            return true;
        });
    }

    public Task<bool> ZoomWindowAsync(CadPoint3D pt1, CadPoint3D pt2)
    {
        return CadDispatcher.RunOnCadThreadAsync(() =>
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.SendStringToExecute($"._ZOOM _W {pt1.X},{pt1.Y} {pt2.X},{pt2.Y} ", true, false, false);
            return true;
        });
    }
}
