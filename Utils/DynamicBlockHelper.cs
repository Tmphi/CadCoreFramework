using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Các hàm tiện ích toàn diện để làm việc với Block động (Dynamic Block) của AutoCAD.
    /// Giúp đọc/ghi trạng thái Visibility, thay đổi kích thước Distance, Góc xoay, Flip v.v...
    /// </summary>
    public static class DynamicBlockHelper
    {
        /// <summary>
        /// Lấy ra tên gốc (tên thực sự) của Block. Rất hữu ích vì khi Block động bị thay đổi tham số, AutoCAD sẽ tạo ra tên ẩn danh (*U).
        /// </summary>
        public static string GetEffectiveName(Transaction tr, BlockReference blockRef)
        {
            if (blockRef == null) throw new ArgumentNullException(nameof(blockRef));

            if (blockRef.IsDynamicBlock)
            {
                ObjectId dynamicBtrId = blockRef.DynamicBlockTableRecord;
                if (!dynamicBtrId.IsNull)
                {
                    var btr = (BlockTableRecord)tr.GetObject(dynamicBtrId, OpenMode.ForRead);
                    return btr.Name;
                }
            }

            var regularBtr = (BlockTableRecord)tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead);
            return regularBtr.Name;
        }

        /// <summary>
        /// Lấy toàn bộ các thuộc tính động và giá trị hiện tại của chúng từ Block.
        /// </summary>
        public static Dictionary<string, object> GetDynamicProperties(BlockReference blockRef)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (blockRef == null || !blockRef.IsDynamicBlock) return result;

            foreach (DynamicBlockReferenceProperty prop in blockRef.DynamicBlockReferencePropertyCollection)
            {
                result[prop.PropertyName] = prop.Value;
            }
            return result;
        }

        /// <summary>
        /// Lấy giá trị của một thuộc tính động (Dynamic Property) theo tên.
        /// </summary>
        public static T GetDynamicProperty<T>(BlockReference blockRef, string propertyName, T defaultValue = default)
        {
            if (blockRef == null || !blockRef.IsDynamicBlock || string.IsNullOrWhiteSpace(propertyName))
                return defaultValue;

            foreach (DynamicBlockReferenceProperty prop in blockRef.DynamicBlockReferencePropertyCollection)
            {
                if (string.Equals(prop.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value is T typedValue) return typedValue;
                    try
                    {
                        return (T)Convert.ChangeType(prop.Value, typeof(T));
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Ghi giá trị mới vào một thuộc tính động (Distance, Visibility, Flip, Angle...).
        /// </summary>
        public static bool SetDynamicProperty(BlockReference blockRef, string propertyName, object value)
        {
            if (blockRef == null || !blockRef.IsDynamicBlock || string.IsNullOrWhiteSpace(propertyName))
                return false;

            foreach (DynamicBlockReferenceProperty prop in blockRef.DynamicBlockReferencePropertyCollection)
            {
                if (string.Equals(prop.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.ReadOnly) return false;

                    try
                    {
                        // Chuyển đổi kiểu dữ liệu nếu cần thiết (ví dụ: double, int, short)
                        Type targetType = prop.Value != null ? prop.Value.GetType() : value.GetType();
                        object convertedValue = Convert.ChangeType(value, targetType);
                        prop.Value = convertedValue;
                        return true;
                    }
                    catch
                    {
                        prop.Value = value;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Lấy danh sách các trạng thái hiển thị (Visibility States) có sẵn trong Block động.
        /// Tìm kiếm theo thứ tự ưu tiên: trước tiên tìm theo tên "Visibility"/"HienThi",
        /// nếu không tìm thấy thì fallback sang property string có AllowedValues đầu tiên.
        /// Rất hữu ích để đưa vào ComboBox/Dropdown trên giao diện UI.
        /// </summary>
        public static List<string> GetVisibilityStates(BlockReference blockRef)
        {
            if (blockRef == null || !blockRef.IsDynamicBlock) return new List<string>();

            // Bước 1: Tìm property có tên chứa "Visibility" hoặc "HienThi" (quy ước đặt tên).
            // Đây là cách đáng tin cậy nhất vì AutoCAD không có PropertyTypeCode công khai
            // phân biệt Visibility khỏi các loại dynamic property khác.
            foreach (DynamicBlockReferenceProperty prop in blockRef.DynamicBlockReferencePropertyCollection)
            {
                if (prop.PropertyName.IndexOf("Visibility", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prop.PropertyName.IndexOf("HienThi", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    object[] allowedValues = prop.GetAllowedValues();
                    if (allowedValues != null && allowedValues.Length > 0 && allowedValues[0] is string)
                    {
                        return allowedValues.Select(v => v.ToString()).ToList();
                    }
                }
            }

            // Bước 2: Fallback — tìm property string có AllowedValues đầu tiên (có thể là Lookup).
            // Dùng GetAllowedValues() để xác định property có danh sách lựa chọn — pattern đầy đủ nhất có thể làm
            // với API công khai của AutoCAD (không có PropertyTypeCode enum phân biệt Visibility/Lookup).
            foreach (DynamicBlockReferenceProperty prop in blockRef.DynamicBlockReferencePropertyCollection)
            {
                object[] allowedValues = prop.GetAllowedValues();
                if (allowedValues != null && allowedValues.Length > 1 && allowedValues[0] is string)
                {
                    return allowedValues.Select(v => v.ToString()).ToList();
                }
            }

            return new List<string>();
        }

        /// <summary>
        /// Lấy danh sách Visibility States theo tên property cụ thể do người dùng chỉ định.
        /// Dùng overload này khi tên Visibility property của block không theo quy ước (ví dụ: "State", "Mode").
        /// </summary>
        public static List<string> GetVisibilityStates(BlockReference blockRef, string visibilityPropertyName)
        {
            if (blockRef == null || !blockRef.IsDynamicBlock || string.IsNullOrWhiteSpace(visibilityPropertyName))
                return new List<string>();

            foreach (DynamicBlockReferenceProperty prop in blockRef.DynamicBlockReferencePropertyCollection)
            {
                if (string.Equals(prop.PropertyName, visibilityPropertyName, StringComparison.OrdinalIgnoreCase))
                {
                    object[] allowedValues = prop.GetAllowedValues();
                    if (allowedValues != null && allowedValues.Length > 0)
                    {
                        return allowedValues.Select(v => v?.ToString() ?? "").ToList();
                    }
                }
            }
            return new List<string>();
        }

        /// <summary>
        /// Gán trạng thái hiển thị (Visibility) mới cho Block động.
        /// </summary>
        public static bool SetVisibilityState(BlockReference blockRef, string stateName)
        {
            if (blockRef == null || !blockRef.IsDynamicBlock || string.IsNullOrWhiteSpace(stateName))
                return false;

            foreach (DynamicBlockReferenceProperty prop in blockRef.DynamicBlockReferencePropertyCollection)
            {
                object[] allowedValues = prop.GetAllowedValues();
                if (allowedValues != null && allowedValues.Any(v => string.Equals(v?.ToString(), stateName, StringComparison.OrdinalIgnoreCase)))
                {
                    prop.Value = stateName;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Lấy trạng thái hiển thị (Visibility) hiện hành của Block động.
        /// Tìm theo tên trước, fallback sang property string có AllowedValues nếu không tìm thấy.
        /// </summary>
        public static string GetVisibilityState(BlockReference blockRef)
        {
            if (blockRef == null || !blockRef.IsDynamicBlock) return null;

            // Bước 1: Tìm theo tên quy ước "Visibility" / "HienThi"
            foreach (DynamicBlockReferenceProperty prop in blockRef.DynamicBlockReferencePropertyCollection)
            {
                if (prop.PropertyName.IndexOf("Visibility", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prop.PropertyName.IndexOf("HienThi", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    object[] allowedValues = prop.GetAllowedValues();
                    if (allowedValues != null && allowedValues.Length > 0 && allowedValues[0] is string)
                    {
                        return prop.Value?.ToString();
                    }
                }
            }

            // Bước 2: Fallback — trả về giá trị hiện tại của property string có AllowedValues đầu tiên
            foreach (DynamicBlockReferenceProperty prop in blockRef.DynamicBlockReferencePropertyCollection)
            {
                object[] allowedValues = prop.GetAllowedValues();
                if (allowedValues != null && allowedValues.Length > 1 && allowedValues[0] is string)
                {
                    return prop.Value?.ToString();
                }
            }

            return null;
        }
    }
}
