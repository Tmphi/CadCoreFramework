using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Bộ dựng theo mẫu Fluent Builder cho đối tượng SelectionFilter của AutoCAD.
    /// Trừu tượng hóa các mã DXF thô thành API rõ ràng, an toàn về kiểu dữ liệu giúp lập trình viên
    /// không cần phải tự tra cứu giá trị DxfCode thủ công.
    /// </summary>
    public class SelectionFilterBuilder
    {
        private readonly List<TypedValue> _values = new List<TypedValue>();

        /// <summary>
        /// Khởi tạo một đối tượng SelectionFilterBuilder mới.
        /// </summary>
        public static SelectionFilterBuilder Create() => new SelectionFilterBuilder();

        /// <summary>
        /// Lọc theo kiểu đối tượng (ví dụ: "INSERT" cho block, "LINE", "LWPOLYLINE", "TEXT", "MTEXT").
        /// Nhiều kiểu đối tượng được nối bằng dấu phẩy — AutoCAD tự hiểu đây là phép toán logic OR.
        /// </summary>
        public SelectionFilterBuilder OfTypes(params string[]? entityTypes)
        {
            if (entityTypes == null || entityTypes.Length == 0) return this;
            _values.Add(new TypedValue((int)DxfCode.Start, string.Join(",", entityTypes)));
            return this;
        }

        /// <summary>
        /// Lọc theo MẪU tên Layer — giá trị là wildcard của AutoCAD (ví dụ: "EQUIP_*").
        /// Nhiều mẫu được nối bằng dấu phẩy (tương đương logic OR).
        /// ⚠️ Ký tự HỢP LỆ trong tên layer nhưng là wildcard (<c># @ . ~ [ ] -</c>) bị hiểu theo nghĩa wildcard:
        /// "LAB#1" KHÔNG khớp layer "LAB#1" (# = một chữ số), "M.EQUIP" khớp cả "M-EQUIP".
        /// Cần khớp đúng nguyên văn tên → dùng <see cref="OnLayersExact"/>.
        /// </summary>
        public SelectionFilterBuilder OnLayers(params string[] layerNames)
        {
            if (layerNames == null || layerNames.Length == 0) return this;
            _values.Add(new TypedValue((int)DxfCode.LayerName, string.Join(",", layerNames)));
            return this;
        }

        /// <summary>
        /// Lọc theo TÊN Layer chính xác (escape mọi ký tự wildcard). Nhiều tên = logic OR.
        /// </summary>
        public SelectionFilterBuilder OnLayersExact(params string[] layerNames)
            => OnLayers(layerNames?.Select(EscapeWildcards).ToArray());

        /// <summary>
        /// Lọc các thực thể INSERT (tham chiếu khối - BlockReference) theo MẪU tên khối (wildcard,
        /// ví dụ: "ACB_*", "MCCB"). Phải kết hợp cùng OfTypes("INSERT").
        /// ⚠️ Dynamic block đã chỉnh tham số mang tên ẩn danh "*U…" — tên gốc KHÔNG khớp; thêm mẫu
        /// <c>"`*U*"</c> rồi lọc lại bằng <c>DynamicBlockHelper.GetEffectiveName</c>.
        /// </summary>
        public SelectionFilterBuilder WithBlockNames(params string[] blockNames)
        {
            if (blockNames == null || blockNames.Length == 0) return this;
            _values.Add(new TypedValue((int)DxfCode.BlockName, string.Join(",", blockNames)));
            return this;
        }

        /// <summary>
        /// Lọc theo TÊN khối chính xác (escape mọi ký tự wildcard). Xem lưu ý dynamic block ở <see cref="WithBlockNames"/>.
        /// </summary>
        public SelectionFilterBuilder WithBlockNamesExact(params string[] blockNames)
            => WithBlockNames(blockNames?.Select(EscapeWildcards).ToArray());

        /// <summary>Các ký tự có nghĩa đặc biệt trong wildcard của AutoCAD (wcmatch).</summary>
        private const string WildcardChars = "#@.*?~[]-,`";

        /// <summary>
        /// Escape mọi ký tự wildcard bằng dấu backquote (<c>`</c>) để AutoCAD khớp đúng nguyên văn.
        /// Ví dụ: <c>"LAB#1"</c> → <c>"LAB`#1"</c>.
        /// </summary>
        public static string EscapeWildcards(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var sb = new StringBuilder(value.Length + 4);
            foreach (char c in value)
            {
                if (WildcardChars.IndexOf(c) >= 0) sb.Append('`');
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Lọc theo mã màu AutoCAD Color Index (ACI: 1 = đỏ, 2 = vàng, 3 = xanh lá…).
        /// </summary>
        public SelectionFilterBuilder WithColor(short colorIndex)
        {
            _values.Add(new TypedValue((int)DxfCode.Color, colorIndex));
            return this;
        }

        /// <summary>
        /// Lọc các đối tượng có mang dữ liệu mở rộng XData đã đăng ký dưới tên ứng dụng (RegApp) cho trước.
        /// Rất hữu ích để chỉ chọn các đối tượng được quản lý bởi plugin này.
        /// </summary>
        /// <param name="appName">Tên ứng dụng RegApp đã dùng khi ghi XData (ví dụ: "MY_PLUGIN").</param>
        public SelectionFilterBuilder WithXData(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName)) return this;
            // DxfCode.ExtendedDataRegAppName (1001) trong bộ lọc chỉ định AutoCAD
            // chỉ đưa vào các thực thể có XData thuộc tên ứng dụng này.
            _values.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));
            return this;
        }

        /// <summary>
        /// Bổ sung một điều kiện mã DXF tùy ý. Sử dụng khi các hàm hỗ trợ có sẵn
        /// không bao quát hết yêu cầu lọc chuyên biệt của bạn.
        /// </summary>
        public SelectionFilterBuilder Custom(int dxfCode, object value)
        {
            _values.Add(new TypedValue(dxfCode, value));
            return this;
        }

        /// <summary>
        /// Khởi tạo và trả về đối tượng SelectionFilter sẵn sàng truyền vào các phương thức Editor.Select*().
        /// </summary>
        public SelectionFilter Build()
        {
            return new SelectionFilter(_values.ToArray());
        }
    }
}
