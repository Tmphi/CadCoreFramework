using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using CadCoreFramework.Core;
using CadCoreFramework.Models;
using CadCoreFramework.Utils;
using MiniExcelLibs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Triển khai <see cref="IDataFileService"/>: JSON qua Newtonsoft.Json, CSV qua <see cref="CsvCodec"/>,
    /// Excel qua MiniExcel (chỉ .xlsx). Mọi thao tác đĩa đi qua <see cref="IFileSystemService"/>.
    /// </summary>
    public class DataFileService : IDataFileService
    {
        private static readonly UTF8Encoding Utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        private static readonly char[] InvalidSheetChars = { ':', '\\', '/', '?', '*', '[', ']' };

        private readonly IFileSystemService _fileSystem;

        /// <summary>
        /// Service KHÔNG tự ghi log: lỗi trả về trong <see cref="CadResult"/>, exception gốc nằm ở
        /// <c>CadResult.Exception</c>. Người gọi hiển thị bằng <c>INotificationService.Error(result.ErrorMessage, result.Exception)</c>
        /// → đúng MỘT dòng log, có stack trace (tự log ở đây thì mỗi lỗi bị ghi hai lần — đã gặp khi kiểm trong AutoCAD).
        /// </summary>
        public DataFileService(IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        // ─── JSON ────────────────────────────────────────────────────────────

        public CadResult<T> ReadJson<T>(string path) => Run(path, "Đọc", () =>
        {
            string text = ReadText(path);
            if (string.IsNullOrWhiteSpace(text)) throw new FormatException("File JSON rỗng.");
            return JsonConvert.DeserializeObject<T>(text);
        });

        public CadResult WriteJson<T>(string path, T data) => RunVoid(path, "Ghi", () =>
            _fileSystem.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented)));

        // ─── CSV ─────────────────────────────────────────────────────────────

        public CadResult<DataSheet> ReadCsv(string path, char? delimiter = null) => Run(path, "Đọc", () =>
            CsvCodec.Parse(ReadText(path), delimiter));

        public CadResult WriteCsv(string path, DataSheet sheet, char delimiter = ',') => RunVoid(path, "Ghi", () =>
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            byte[] body = Encoding.UTF8.GetBytes(CsvCodec.Format(sheet, delimiter));
            byte[] bom = Encoding.UTF8.GetPreamble(); // BOM: thiếu nó, Excel đọc UTF-8 thành ký tự rác
            _fileSystem.WriteAllBytes(path, bom.Concat(body).ToArray());
        });

        // ─── Excel ───────────────────────────────────────────────────────────

        public CadResult<IReadOnlyList<string>> GetExcelSheetNames(string path) => Run(path, "Đọc", () =>
        {
            RequireXlsx(path);
            using (var stream = new MemoryStream(_fileSystem.ReadAllBytes(path)))
                return (IReadOnlyList<string>)stream.GetSheetNames().ToList();
        });

        public CadResult<DataSheet> ReadExcel(string path, string sheetName = null) => Run(path, "Đọc", () =>
        {
            RequireXlsx(path);
            using (var stream = new MemoryStream(_fileSystem.ReadAllBytes(path)))
            {
                List<string> sheets = stream.GetSheetNames().ToList();
                if (sheets.Count == 0) throw new FormatException("File Excel không có sheet nào.");
                if (sheetName != null && !sheets.Contains(sheetName))
                    throw new FormatException($"Không có sheet '{sheetName}'. Các sheet: {string.Join(", ", sheets.Select(s => "'" + s + "'"))}.");

                stream.Position = 0;
                var rows = stream.Query(useHeaderRow: false, sheetName: sheetName ?? sheets[0], excelType: ExcelType.XLSX)
                                 .Cast<IDictionary<string, object?>>();
                return ToSheet(rows);
            }
        });

        public CadResult WriteExcel(string path, DataSheet sheet, string sheetName = "Sheet1") => RunVoid(path, "Ghi", () =>
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            RequireXlsx(path);
            CheckSheetName(sheetName);

            var table = new DataTable(sheetName);
            foreach (string column in sheet.Columns) table.Columns.Add(column, typeof(object));
            for (int r = 0; r < sheet.RowCount; r++)
                table.Rows.Add(sheet.GetRow(r).Select(v => v ?? DBNull.Value).ToArray());

            using (var stream = new MemoryStream())
            {
                stream.SaveAs(table, sheetName: sheetName, excelType: ExcelType.XLSX);
                _fileSystem.WriteAllBytes(path, stream.ToArray());
            }
        });

        // ─── Theo đuôi file ──────────────────────────────────────────────────

        public CadResult<DataSheet> ReadTable(string path)
        {
            switch (Extension(path))
            {
                case ".xlsx": return ReadExcel(path);
                case ".csv": return ReadCsv(path);
                case ".json": return Run(path, "Đọc", () => JsonToSheet(ReadText(path)));
                default: return CadResult<DataSheet>.Fail(UnsupportedExtension(path));
            }
        }

        public CadResult WriteTable(string path, DataSheet sheet)
        {
            switch (Extension(path))
            {
                case ".xlsx": return WriteExcel(path, sheet);
                case ".csv": return WriteCsv(path, sheet);
                case ".json":
                    return RunVoid(path, "Ghi", () =>
                    {
                        if (sheet == null) throw new ArgumentNullException(nameof(sheet));
                        _fileSystem.WriteAllText(path, SheetToJson(sheet).ToString(Formatting.Indented));
                    });
                default: return CadResult.Fail(UnsupportedExtension(path));
            }
        }

        // ─── Chuyển đổi (internal để test) ───────────────────────────────────

        /// <summary>Hàng MiniExcel (khóa "A", "B"…) → bảng. Bỏ dòng trống; dòng có dữ liệu đầu tiên là tiêu đề.</summary>
        internal static DataSheet ToSheet(IEnumerable<IDictionary<string, object?>> rows)
        {
            DataSheet sheet = null;
            List<string> headerKeys = null;
            int rowNumber = 0;
            foreach (IDictionary<string, object?> row in rows)
            {
                rowNumber++;
                if (row.Values.All(IsBlank)) continue;

                if (sheet == null)
                {
                    List<string> keys = row.Keys.ToList();
                    int last = keys.FindLastIndex(k => !IsBlank(row[k]));
                    headerKeys = keys.Take(last + 1).ToList();
                    List<string> names = headerKeys.Select(k => DataSheet.FormatCell(row[k]).Trim()).ToList();
                    string error = DataSheet.CheckColumns(names);
                    if (error != null) throw new FormatException($"Dòng tiêu đề (dòng {rowNumber}): {error}");
                    sheet = new DataSheet(names);
                    continue;
                }

                foreach (KeyValuePair<string, object?> cell in row)
                    if (!headerKeys.Contains(cell.Key) && !IsBlank(cell.Value))
                        throw new FormatException($"Ô {cell.Key}{rowNumber} có dữ liệu nhưng cột {cell.Key} không có tiêu đề.");

                sheet.AddRow(headerKeys.Select(k => row.TryGetValue(k, out object? v) ? Normalize(v) : null).ToArray());
            }
            return sheet ?? throw new FormatException("Sheet trống: không có dòng tiêu đề.");
        }

        /// <summary>Bảng → mảng JSON các đối tượng, giữ thứ tự cột.</summary>
        internal static JArray SheetToJson(DataSheet sheet)
        {
            var array = new JArray();
            for (int r = 0; r < sheet.RowCount; r++)
            {
                var item = new JObject();
                for (int c = 0; c < sheet.Columns.Count; c++)
                {
                    object? v = sheet.GetRow(r)[c];
                    // JSON không có kiểu ngày: ghi DateTime thành chuỗi giống CSV ("2026-10-15"), đọc lại vẫn là chuỗi đó.
                    item[sheet.Columns[c]] = v == null ? JValue.CreateNull()
                                           : v is DateTime ? new JValue(DataSheet.FormatCell(v))
                                           : JToken.FromObject(v);
                }
                array.Add(item);
            }
            return array;
        }

        /// <summary>
        /// Mảng JSON các đối tượng → bảng. Cột = hợp các khóa theo thứ tự gặp lần đầu; khóa thiếu ở một đối tượng → null.
        /// Chuỗi dạng ngày giữ nguyên là chuỗi (không tự đổi sang DateTime); giá trị lồng (object/array) giữ dạng chuỗi JSON.
        /// </summary>
        internal static DataSheet JsonToSheet(string json)
        {
            JToken root;
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
                root = JToken.ReadFrom(reader);
            if (!(root is JArray array))
                throw new FormatException("JSON dạng bảng phải là một mảng đối tượng: [{\"Cột\": giá trị, …}, …].");

            var columns = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < array.Count; i++)
            {
                if (!(array[i] is JObject obj))
                    throw new FormatException($"Phần tử thứ {i + 1} của mảng không phải đối tượng {{…}}.");
                foreach (JProperty p in obj.Properties())
                    if (seen.Add(p.Name.Trim())) columns.Add(p.Name.Trim());
            }
            if (columns.Count == 0) throw new FormatException("Mảng JSON không có đối tượng nào có thuộc tính.");
            string error = DataSheet.CheckColumns(columns);
            if (error != null) throw new FormatException(error);

            var sheet = new DataSheet(columns);
            foreach (JObject obj in array.Cast<JObject>())
            {
                var values = new object?[columns.Count];
                foreach (JProperty p in obj.Properties())
                    values[sheet.IndexOf(p.Name)] = FromJson(p.Value);
                sheet.AddRow(values);
            }
            return sheet;
        }

        // ─── Hạ tầng ─────────────────────────────────────────────────────────

        private string ReadText(string path)
        {
            byte[] bytes = _fileSystem.ReadAllBytes(path);
            int start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            try { return Utf8Strict.GetString(bytes, start, bytes.Length - start); }
            catch (DecoderFallbackException)
            {
                throw new FormatException("File không phải UTF-8 (có thể là ANSI do Excel lưu bằng \"CSV (Comma delimited)\"). " +
                                          "Trong Excel chọn Save As → \"CSV UTF-8 (Comma delimited)\" rồi thử lại.");
            }
        }

        private CadResult<T> Run<T>(string path, string verb, Func<T> body)
        {
            try { return CadResult<T>.Success(body()); }
            catch (Exception ex) { return CadResult<T>.Fail(Describe(ex, path, verb), ex); }
        }

        private CadResult RunVoid(string path, string verb, Action body)
        {
            try { body(); return CadResult.Ok(); }
            catch (Exception ex) { return CadResult.Fail(Describe(ex, path, verb), ex); }
        }

        private static string Describe(Exception ex, string path, string verb)
        {
            string name = string.IsNullOrEmpty(path) ? "(không có đường dẫn)" : Path.GetFileName(path);
            switch (ex)
            {
                case FileNotFoundException _:
                case DirectoryNotFoundException _:
                    return $"Không tìm thấy file '{path}'.";
                case UnauthorizedAccessException _:
                    return $"Không có quyền {verb.ToLowerInvariant()} file '{path}' (file chỉ đọc, hoặc thư mục cần quyền quản trị).";
                case IOException io when IsSharingViolation(io):
                    return $"File '{name}' đang được mở trong chương trình khác (Excel?). Đóng file rồi thử lại.";
                case JsonException _:
                    return $"File '{name}' không phải JSON hợp lệ: {ex.Message}";
                case FormatException _:
                case ArgumentException _:
                    return $"File '{name}': {ex.Message}";
                default:
                    return $"{verb} file '{name}' thất bại: {ex.Message}";
            }
        }

        // ERROR_SHARING_VIOLATION (32), ERROR_LOCK_VIOLATION (33), ERROR_UNABLE_TO_REMOVE_REPLACED (1175).
        private static bool IsSharingViolation(IOException ex)
        {
            int code = ex.HResult & 0xFFFF;
            return code == 32 || code == 33 || code == 1175;
        }

        private static object? Normalize(object? value) => value is string s && s.Length == 0 ? null : value;

        private static bool IsBlank(object? value) => value == null || value is DBNull || (value is string s && s.Trim().Length == 0);

        private static object? FromJson(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;
                case JTokenType.Integer:
                case JTokenType.Float:
                    return token.Value<double>();
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.String:
                    return token.Value<string>();
                default:
                    return token.ToString(Formatting.None);
            }
        }

        private static void RequireXlsx(string path)
        {
            string ext = Extension(path);
            if (ext == ".xls")
                throw new FormatException("Định dạng .xls (Excel 97–2003) không được hỗ trợ. Mở trong Excel và lưu lại dạng .xlsx.");
            if (ext != ".xlsx")
                throw new FormatException($"File Excel phải có đuôi .xlsx (đang là '{ext}').");
        }

        private static void CheckSheetName(string sheetName)
        {
            if (string.IsNullOrWhiteSpace(sheetName) || sheetName.Length > 31 || sheetName.IndexOfAny(InvalidSheetChars) >= 0)
                throw new ArgumentException($"Tên sheet '{sheetName}' không hợp lệ: 1–31 ký tự, không chứa : \\ / ? * [ ].");
        }

        private static string Extension(string path) => (Path.GetExtension(path ?? "") ?? "").ToLowerInvariant();

        private static string UnsupportedExtension(string path)
            => $"Không hỗ trợ đuôi '{Extension(path)}'. Dùng .xlsx, .csv hoặc .json.";
    }
}
