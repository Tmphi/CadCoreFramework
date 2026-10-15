// Lời giải mẫu L02. Làm bài trong ../L02/TrainDump.cs trước, rồi mới đọc file này.
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using CadCoreFramework.Core;
using CadCoreFramework.Utils;

namespace CadCoreFramework.Labs.L02
{
    public sealed class LayerInfo
    {
        public LayerInfo(string name, bool isOff, short colorIndex)
        {
            Name = name;
            IsOff = isOff;
            ColorIndex = colorIndex;
        }

        public string Name { get; }
        public bool IsOff { get; }
        public short ColorIndex { get; }
    }

    public static class TrainDump
    {
        public static List<LayerInfo> ListLayers(ICadScope scope)
        {
            var result = new List<LayerInfo>();
            var table = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);
            foreach (ObjectId id in table)
            {
                var layer = (LayerTableRecord)scope.Transaction.GetObject(id, OpenMode.ForRead);
                result.Add(new LayerInfo(layer.Name, layer.IsOff, layer.Color.ColorIndex));
            }
            return result;
        }

        // ObjectClass đọc từ ObjectId, không mở đối tượng: duyệt 100 000 entity vẫn rẻ (T07 §2).
        public static Dictionary<string, int> CountByDxfName(ICadScope scope)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (ObjectId id in scope.CurrentSpace)
            {
                string dxf = id.ObjectClass.DxfName;
                counts[dxf] = counts.TryGetValue(dxf, out int n) ? n + 1 : 1;
            }
            return counts;
        }

        // Lọc INSERT bằng ObjectClass trước, rồi mới mở ForRead đúng những block reference cần đọc.
        public static Dictionary<string, int> CountByEffectiveName(ICadScope scope)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (ObjectId id in scope.CurrentSpace)
            {
                if (id.ObjectClass.DxfName != "INSERT") continue;
                var br = (BlockReference)scope.Transaction.GetObject(id, OpenMode.ForRead);
                string name = DynamicBlockHelper.GetEffectiveName(scope.Transaction, br);
                counts[name] = counts.TryGetValue(name, out int n) ? n + 1 : 1;
            }
            return counts;
        }
    }
}
