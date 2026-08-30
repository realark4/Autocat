using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.Models;
using AutoCadAiPlugin.Core.ToolContracts;

namespace AutoCadAiPlugin.Tools.Base;

public abstract class BaseCadTool : ITool
{
    public abstract ToolDefinition Definition { get; }

    public abstract Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default);

    protected static double GetDouble(Dictionary<string, object?> args, string key, double defaultValue = 0.0)
    {
        if (!args.TryGetValue(key, out var val) || val == null) return defaultValue;

        if (val is JsonElement elem)
        {
            if (elem.ValueKind == JsonValueKind.Number) return elem.GetDouble();
            if (elem.ValueKind == JsonValueKind.String && double.TryParse(elem.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            return defaultValue;
        }

        if (val is double dVal) return dVal;
        if (val is float fVal) return fVal;
        if (val is int iVal) return iVal;
        if (val is long lVal) return lVal;
        if (double.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) return parsed;

        return defaultValue;
    }

    protected static string? GetString(Dictionary<string, object?> args, string key, string? defaultValue = null)
    {
        if (!args.TryGetValue(key, out var val) || val == null) return defaultValue;

        if (val is JsonElement elem)
        {
            return elem.ValueKind == JsonValueKind.String ? elem.GetString() : elem.ToString();
        }

        return val.ToString();
    }

    protected static bool GetBool(Dictionary<string, object?> args, string key, bool defaultValue = false)
    {
        if (!args.TryGetValue(key, out var val) || val == null) return defaultValue;

        if (val is JsonElement elem)
        {
            if (elem.ValueKind == JsonValueKind.True) return true;
            if (elem.ValueKind == JsonValueKind.False) return false;
            if (elem.ValueKind == JsonValueKind.String && bool.TryParse(elem.GetString(), out var b)) return b;
            return defaultValue;
        }

        if (val is bool bVal) return bVal;
        if (bool.TryParse(val.ToString(), out var parsed)) return parsed;

        return defaultValue;
    }

    protected static CadPoint3D GetPoint3D(Dictionary<string, object?> args, string key, CadPoint3D? defaultPt = null)
    {
        defaultPt ??= CadPoint3D.Origin;

        if (args.TryGetValue(key + "X", out _) || args.TryGetValue(key + "_x", out _))
        {
            double x = GetDouble(args, key + "X", GetDouble(args, key + "_x", defaultPt.X));
            double y = GetDouble(args, key + "Y", GetDouble(args, key + "_y", defaultPt.Y));
            double z = GetDouble(args, key + "Z", GetDouble(args, key + "_z", defaultPt.Z));
            return new CadPoint3D(x, y, z);
        }

        if (!args.TryGetValue(key, out var val) || val == null)
        {
            // Check direct properties e.g. "x", "y", "z" or "centerX", "centerY"
            if (args.ContainsKey("x") || args.ContainsKey("y"))
            {
                return new CadPoint3D(GetDouble(args, "x"), GetDouble(args, "y"), GetDouble(args, "z"));
            }
            if (args.ContainsKey("centerX") || args.ContainsKey("centerY"))
            {
                return new CadPoint3D(GetDouble(args, "centerX"), GetDouble(args, "centerY"), GetDouble(args, "centerZ"));
            }
            return defaultPt;
        }

        if (val is JsonElement elem)
        {
            if (elem.ValueKind == JsonValueKind.Array)
            {
                var arr = elem.EnumerateArray();
                double x = arr.MoveNext() ? arr.Current.GetDouble() : 0.0;
                double y = arr.MoveNext() ? arr.Current.GetDouble() : 0.0;
                double z = arr.MoveNext() ? arr.Current.GetDouble() : 0.0;
                return new CadPoint3D(x, y, z);
            }
            if (elem.ValueKind == JsonValueKind.Object)
            {
                double x = elem.TryGetProperty("x", out var px) ? px.GetDouble() : 0.0;
                double y = elem.TryGetProperty("y", out var py) ? py.GetDouble() : 0.0;
                double z = elem.TryGetProperty("z", out var pz) ? pz.GetDouble() : 0.0;
                return new CadPoint3D(x, y, z);
            }
        }

        return defaultPt;
    }

    protected static CadPoint2D GetPoint2D(Dictionary<string, object?> args, string key, CadPoint2D? defaultPt = null)
    {
        defaultPt ??= CadPoint2D.Origin;

        if (args.TryGetValue(key + "X", out _) || args.TryGetValue(key + "_x", out _))
        {
            double x = GetDouble(args, key + "X", GetDouble(args, key + "_x", defaultPt.X));
            double y = GetDouble(args, key + "Y", GetDouble(args, key + "_y", defaultPt.Y));
            return new CadPoint2D(x, y);
        }

        if (!args.TryGetValue(key, out var val) || val == null)
        {
            if (args.ContainsKey("x") || args.ContainsKey("y"))
            {
                return new CadPoint2D(GetDouble(args, "x"), GetDouble(args, "y"));
            }
            return defaultPt;
        }

        if (val is JsonElement elem)
        {
            if (elem.ValueKind == JsonValueKind.Array)
            {
                var arr = elem.EnumerateArray();
                double x = arr.MoveNext() ? arr.Current.GetDouble() : 0.0;
                double y = arr.MoveNext() ? arr.Current.GetDouble() : 0.0;
                return new CadPoint2D(x, y);
            }
            if (elem.ValueKind == JsonValueKind.Object)
            {
                double x = elem.TryGetProperty("x", out var px) ? px.GetDouble() : 0.0;
                double y = elem.TryGetProperty("y", out var py) ? py.GetDouble() : 0.0;
                return new CadPoint2D(x, y);
            }
        }

        return defaultPt;
    }

    protected static List<CadPoint2D> GetPoint2DList(Dictionary<string, object?> args, string key)
    {
        var list = new List<CadPoint2D>();
        if (!args.TryGetValue(key, out var val) || val == null) return list;

        if (val is JsonElement elem && elem.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in elem.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Array)
                {
                    var arr = item.EnumerateArray();
                    double x = arr.MoveNext() ? arr.Current.GetDouble() : 0.0;
                    double y = arr.MoveNext() ? arr.Current.GetDouble() : 0.0;
                    list.Add(new CadPoint2D(x, y));
                }
                else if (item.ValueKind == JsonValueKind.Object)
                {
                    double x = item.TryGetProperty("x", out var px) ? px.GetDouble() : 0.0;
                    double y = item.TryGetProperty("y", out var py) ? py.GetDouble() : 0.0;
                    list.Add(new CadPoint2D(x, y));
                }
            }
        }

        return list;
    }

    protected static List<string> GetStringList(Dictionary<string, object?> args, string key)
    {
        var list = new List<string>();
        if (!args.TryGetValue(key, out var val) || val == null) return list;

        if (val is JsonElement elem && elem.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in elem.EnumerateArray())
            {
                var s = item.GetString();
                if (!string.IsNullOrEmpty(s)) list.Add(s);
            }
        }
        else if (val is IEnumerable<string> strEnum)
        {
            list.AddRange(strEnum);
        }
        else if (val is string singleStr)
        {
            list.Add(singleStr);
        }

        return list;
    }
}
