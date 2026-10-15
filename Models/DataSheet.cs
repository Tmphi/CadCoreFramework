using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CadCoreFramework.Models
{
    /// <summary>
    /// Một bảng dữ liệu: dòng tiêu đề (tên cột) + các dòng giá trị. Dùng chung cho Excel, CSV và
    /// JSON dạng mảng đối tượng (<see cref="Services.IDataFileService"/>).
    ///
    /// Giá trị ô là <c>object</c>: <c>string</c>, <c>double</c>, <c>bool</c>, <c>DateTime</c> hoặc <c>null</c>.
    /// Đọc từ Excel thì ô số là <c>double</c> (kể cả số nguyên), ô rỗng là <c>null</c>; đọc từ CSV thì mọi ô
    /// là <c>string</c>. Dùng <see cref="GetText"/> và <see cref="TryGetDouble"/> để khỏi phải quan tâm nguồn.
    /// </summary>
    public sealed class DataSheet
    {
        private readonly List<string> _columns;
        private readonly Dictionary<string, int> _index;
        private readonly List<object?[]> _rows = new List<object?[]>();

        /// <summary>
        /// Tạo bảng rỗng với các cột cho trước. Tên cột không được rỗng và không được trùng nhau
        /// (không phân biệt hoa thường, sau khi bỏ khoảng trắng hai đầu).
        /// </summary>
        public DataSheet(IEnumerable<string> columns)
        {
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            _columns = columns.Select(c => c?.Trim()).ToList();
            string error = CheckColumns(_columns);
            if (error != null) throw new ArgumentException(error, nameof(columns));
            _index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _columns.Count; i++) _index[_columns[i]] = i;
        }

        /// <summary>
        /// Kiểm tra danh sách tên cột theo đúng luật của constructor. Trả thông điệp lỗi, hoặc null nếu hợp lệ.
        /// Bộ đọc file dùng hàm này để báo lỗi dòng tiêu đề mà không phải bắt exception.
        /// </summary>
        public static string CheckColumns(IReadOnlyList<string> columns)
        {
            if (columns == null || columns.Count == 0) return "Bảng phải có ít nhất một cột.";
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < columns.Count; i++)
            {
                string name = columns[i]?.Trim();
                if (string.IsNullOrEmpty(name)) return $"Cột thứ {i + 1} không có tên.";
                if (!seen.Add(name)) return $"Tên cột '{name}' bị trùng.";
            }
            return null;
        }

        /// <summary>Tên các cột, theo thứ tự.</summary>
        public IReadOnlyList<string> Columns => _columns;

        /// <summary>Số dòng dữ liệu (không tính dòng tiêu đề).</summary>
        public int RowCount => _rows.Count;

        /// <summary>Thêm một dòng; số giá trị phải bằng số cột.</summary>
        public void AddRow(params object?[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Length != _columns.Count)
                throw new ArgumentException($"Dòng có {values.Length} giá trị, bảng có {_columns.Count} cột.", nameof(values));
            _rows.Add((object?[])values.Clone());
        }

        /// <summary>Vị trí của cột (không phân biệt hoa thường); -1 nếu không có.</summary>
        public int IndexOf(string column)
            => column != null && _index.TryGetValue(column.Trim(), out int i) ? i : -1;

        /// <summary>Bảng có cột này không (không phân biệt hoa thường).</summary>
        public bool HasColumn(string column) => IndexOf(column) >= 0;

        /// <summary>Các giá trị của một dòng, theo thứ tự cột.</summary>
        public IReadOnlyList<object?> GetRow(int row) => _rows[row];

        /// <summary>Giá trị thô của một ô. Ném <see cref="KeyNotFoundException"/> nếu không có cột.</summary>
        public object? GetValue(int row, string column) => _rows[row][RequireColumn(column)];

        /// <summary>Gán giá trị một ô.</summary>
        public void SetValue(int row, string column, object? value) => _rows[row][RequireColumn(column)] = value;

        /// <summary>
        /// Giá trị ô dạng chuỗi: <c>null</c> → "", số và ngày định dạng theo InvariantCulture
        /// (12.5, không phải 12,5), <c>DateTime</c> có giờ = 0 → "yyyy-MM-dd".
        /// </summary>
        public string GetText(int row, string column) => FormatCell(GetValue(row, column));

        /// <summary>
        /// Đọc ô dạng số: nhận <c>double</c>/<c>int</c>… và chuỗi số theo InvariantCulture ("12.5").
        /// Trả false nếu ô rỗng hoặc không phải số.
        /// </summary>
        public bool TryGetDouble(int row, string column, out double value)
        {
            object? cell = GetValue(row, column);
            switch (cell)
            {
                case null:
                    value = 0;
                    return false;
                case string s:
                    return double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                case bool _:
                case DateTime _:
                    value = 0;
                    return false;
                case IConvertible c:
                    value = c.ToDouble(CultureInfo.InvariantCulture);
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }

        /// <summary>Định dạng một giá trị ô thành chuỗi, cùng quy tắc với <see cref="GetText"/>.</summary>
        public static string FormatCell(object? value)
        {
            switch (value)
            {
                case null:
                    return "";
                case string s:
                    return s;
                case bool b:
                    return b ? "TRUE" : "FALSE";
                case DateTime d:
                    return d.TimeOfDay == TimeSpan.Zero
                        ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                case double x:
                    // "R" trên .NET Framework có trường hợp không khôi phục đúng giá trị → kiểm lại, sai thì dùng G17.
                    string r = x.ToString("R", CultureInfo.InvariantCulture);
                    return double.Parse(r, CultureInfo.InvariantCulture).Equals(x) ? r : x.ToString("G17", CultureInfo.InvariantCulture);
                case IFormattable f:
                    return f.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }

        private int RequireColumn(string column)
        {
            int i = IndexOf(column);
            if (i < 0)
                throw new KeyNotFoundException($"Không có cột '{column}'. Các cột: {string.Join(", ", _columns.Select(c => "'" + c + "'"))}.");
            return i;
        }
    }
}
