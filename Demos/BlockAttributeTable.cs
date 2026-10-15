using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using CadCoreFramework.Core;
using CadCoreFramework.Models;
using CadCoreFramework.Utils;

namespace CadCoreFramework.Demos
{
    /// <summary>Kết quả của <see cref="BlockAttributeTable.Import"/>.</summary>
    public sealed class AttributeImportResult
    {
        /// <summary>Số attribute đã đổi giá trị.</summary>
        public int AttributesChanged { get; set; }
        /// <summary>Số block reference có ít nhất một attribute đổi.</summary>
        public int BlocksChanged { get; set; }
        /// <summary>Handle trong file không còn là block reference trong bản vẽ (đã xóa, hoặc file của bản vẽ khác).</summary>
        public List<string> MissingHandles { get; } = new List<string>();
        /// <summary>Handle của block nằm trên layer khóa (hoặc có attribute trên layer khóa): bỏ qua, không sửa.</summary>
        public List<string> LockedHandles { get; } = new List<string>();
        /// <summary>Cột không khớp tag attribute nào của các block được tìm thấy.</summary>
        public List<string> UnknownColumns { get; } = new List<string>();
    }

    /// <summary>
    /// Bảng attribute của block: xuất mọi block reference có attribute ra <see cref="DataSheet"/>, và nhập
    /// ngược giá trị attribute từ bảng (khớp theo cột HANDLE). Chạy BÊN TRONG <c>ICadContext.Execute*</c>.
    ///
    /// Cột cố định: HANDLE, BLOCK, LAYOUT, X, Y (chỉ để đọc; nhập lại không đổi vị trí). Sau đó là một cột cho
    /// mỗi tag attribute. Tag trùng tên cột cố định thì cột của nó có tiền tố "ATTR:".
    /// </summary>
    public static class BlockAttributeTable
    {
        public const string HandleColumn = "HANDLE";
        public const string AttributePrefix = "ATTR:";
        public static readonly string[] FixedColumns = { HandleColumn, "BLOCK", "LAYOUT", "X", "Y" };

        /// <summary>
        /// Xuất block reference có attribute ở model space và mọi layout. <paramref name="blockName"/> = null:
        /// mọi block; ngược lại so với tên hiệu lực (block động vẫn khớp), không phân biệt hoa thường.
        /// </summary>
        public static DataSheet Export(ICadScope scope, string blockName = null)
        {
            Transaction tr = scope.Transaction;
            var rows = new List<(string Handle, string Block, string Layout, double X, double Y, Dictionary<string, string> Attributes)>();
            var tags = new List<string>();
            var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var blockTable = (BlockTable)tr.GetObject(scope.Database.BlockTableId, OpenMode.ForRead);
            foreach (ObjectId btrId in blockTable)
            {
                var space = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                if (!space.IsLayout) continue;
                string layout = ((Layout)tr.GetObject(space.LayoutId, OpenMode.ForRead)).LayoutName;

                foreach (ObjectId id in space)
                {
                    if (id.ObjectClass.DxfName != "INSERT") continue;
                    var reference = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
                    if (reference.AttributeCollection.Count == 0) continue;
                    string name = DynamicBlockHelper.GetEffectiveName(tr, reference);
                    if (blockName != null && !string.Equals(name, blockName, StringComparison.OrdinalIgnoreCase)) continue;

                    Dictionary<string, string> attributes = BlockHelper.GetBlockAttributes(tr, reference);
                    foreach (string tag in attributes.Keys)
                        if (seenTags.Add(tag)) tags.Add(tag);
                    rows.Add((reference.Handle.ToString(), name, layout, reference.Position.X, reference.Position.Y, attributes));
                }
            }

            var sheet = new DataSheet(FixedColumns.Concat(tags.Select(ColumnForTag)));
            foreach (var row in rows)
            {
                var values = new List<object?> { row.Handle, row.Block, row.Layout, row.X, row.Y };
                values.AddRange(tags.Select(t => row.Attributes.TryGetValue(t, out string v) ? (object)v : null));
                sheet.AddRow(values.ToArray());
            }
            return sheet;
        }

        /// <summary>
        /// Ghi giá trị attribute từ bảng vào bản vẽ. Chỉ đổi attribute có giá trị khác; ô rỗng nghĩa là attribute rỗng.
        /// Block reference không có tag của một cột thì bỏ qua ô đó. Ném <see cref="ArgumentException"/> nếu bảng
        /// không có cột HANDLE.
        /// </summary>
        public static AttributeImportResult Import(ICadScope scope, DataSheet sheet)
        {
            if (!sheet.HasColumn(HandleColumn))
                throw new ArgumentException($"Bảng không có cột {HandleColumn}: hãy dùng file đã xuất bằng DEMO_ATTR_EXPORT.");

            Transaction tr = scope.Transaction;
            var result = new AttributeImportResult();
            List<string> attributeColumns = sheet.Columns.Where(c => !FixedColumns.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
            var matchedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int r = 0; r < sheet.RowCount; r++)
            {
                string handleText = sheet.GetText(r, HandleColumn).Trim();
                if (handleText.Length == 0) continue;
                if (!TryGetReference(scope, handleText, out BlockReference reference))
                {
                    result.MissingHandles.Add(handleText);
                    continue;
                }

                Dictionary<string, AttributeReference> byTag = reference.AttributeCollection.Cast<ObjectId>()
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as AttributeReference)
                    .Where(a => a != null)
                    .GroupBy(a => a.Tag, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var changes = new List<(AttributeReference Attribute, string Value)>();
                foreach (string column in attributeColumns)
                {
                    string tag = column.StartsWith(AttributePrefix, StringComparison.OrdinalIgnoreCase) ? column.Substring(AttributePrefix.Length) : column;
                    if (!byTag.TryGetValue(tag, out AttributeReference attribute)) continue;
                    matchedColumns.Add(column);
                    string value = sheet.GetText(r, column);
                    if (attribute.TextString != value) changes.Add((attribute, value));
                }
                if (changes.Count == 0) continue;

                if (IsOnLockedLayer(tr, reference) || changes.Any(c => IsOnLockedLayer(tr, c.Attribute)))
                {
                    result.LockedHandles.Add(handleText);
                    continue;
                }

                foreach (var (attribute, value) in changes)
                    BlockHelper.SetBlockAttribute(tr, reference, attribute.Tag, value);
                result.AttributesChanged += changes.Count;
                result.BlocksChanged++;
            }

            result.UnknownColumns.AddRange(attributeColumns.Where(c => !matchedColumns.Contains(c)));
            return result;
        }

        private static string ColumnForTag(string tag)
            => FixedColumns.Contains(tag, StringComparer.OrdinalIgnoreCase) ? AttributePrefix + tag : tag;

        private static bool TryGetReference(ICadScope scope, string handleText, out BlockReference reference)
        {
            reference = null;
            if (!long.TryParse(handleText, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out long value)) return false;
            if (!scope.Database.TryGetObjectId(new Handle(value), out ObjectId id) || id.IsErased) return false;
            reference = scope.Transaction.GetObject(id, OpenMode.ForRead) as BlockReference;
            return reference != null;
        }

        private static bool IsOnLockedLayer(Transaction tr, Entity entity)
            => ((LayerTableRecord)tr.GetObject(entity.LayerId, OpenMode.ForRead)).IsLocked;
    }
}
