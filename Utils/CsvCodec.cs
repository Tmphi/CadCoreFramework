using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CadCoreFramework.Models;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Đọc/ghi CSV theo RFC 4180, không đụng tới file (file do <see cref="Services.IDataFileService"/> lo).
    ///
    /// - Ô chứa dấu phân cách, dấu nháy kép hoặc xuống dòng được bọc trong "…", dấu nháy bên trong viết thành "".
    /// - Dòng đầu là tiêu đề. Dòng trống (kể cả dòng cuối) bị bỏ qua.
    /// - Đọc: mọi ô là <c>string</c>; ô rỗng là "". Thiếu ô ở cuối dòng → "" ; thừa ô → <see cref="FormatException"/>.
    /// - Ghi: số và ngày theo InvariantCulture (<see cref="DataSheet.FormatCell"/>), xuống dòng CRLF.
    /// </summary>
    public static class CsvCodec
    {
        /// <summary>Các dấu phân cách mà <see cref="DetectDelimiter"/> thử, theo thứ tự ưu tiên khi hòa.</summary>
        public static readonly char[] CandidateDelimiters = { ',', ';', '\t' };

        /// <summary>Chuyển bảng thành văn bản CSV.</summary>
        public static string Format(DataSheet sheet, char delimiter = ',')
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            CheckDelimiter(delimiter);
            var sb = new StringBuilder();
            AppendLine(sb, sheet.Columns, delimiter);
            for (int r = 0; r < sheet.RowCount; r++)
                AppendLine(sb, sheet.GetRow(r).Select(DataSheet.FormatCell), delimiter);
            return sb.ToString();
        }

        /// <summary>
        /// Đọc văn bản CSV thành bảng. <paramref name="delimiter"/> = null: tự nhận dấu phân cách từ dòng tiêu đề.
        /// Ném <see cref="FormatException"/> (thông điệp có số dòng) khi CSV sai cấu trúc.
        /// </summary>
        public static DataSheet Parse(string text, char? delimiter = null)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length > 0 && text[0] == '﻿') text = text.Substring(1);
            char sep = delimiter ?? DetectDelimiter(text);
            CheckDelimiter(sep);

            List<(int Line, List<string> Fields)> records = ReadRecords(text, sep)
                .Where(r => !(r.Fields.Count == 1 && r.Fields[0].Length == 0))
                .ToList();
            if (records.Count == 0) throw new FormatException("File CSV rỗng: không có dòng tiêu đề.");

            string headerError = DataSheet.CheckColumns(records[0].Fields);
            if (headerError != null) throw new FormatException($"Dòng tiêu đề (dòng {records[0].Line}): {headerError}");
            var sheet = new DataSheet(records[0].Fields);

            int width = sheet.Columns.Count;
            foreach (var (line, fields) in records.Skip(1))
            {
                if (fields.Count > width)
                    throw new FormatException($"Dòng {line} có {fields.Count} ô, nhiều hơn {width} cột của dòng tiêu đề.");
                while (fields.Count < width) fields.Add("");
                sheet.AddRow(fields.Cast<object>().ToArray());
            }
            return sheet;
        }

        /// <summary>
        /// Đoán dấu phân cách: dấu nào xuất hiện nhiều nhất trong dòng đầu (ngoài dấu nháy) thì chọn;
        /// không có dấu nào thì ','. Excel ở vùng dùng dấu phẩy thập phân thường xuất CSV bằng ';'.
        /// </summary>
        public static char DetectDelimiter(string text)
        {
            var counts = CandidateDelimiters.ToDictionary(c => c, _ => 0);
            bool quoted = false;
            foreach (char ch in text)
            {
                if (ch == '"') quoted = !quoted;
                else if (!quoted && (ch == '\n' || ch == '\r')) break;
                else if (!quoted && counts.ContainsKey(ch)) counts[ch]++;
            }
            char best = ',';
            foreach (char c in CandidateDelimiters)
                if (counts[c] > counts[best]) best = c;
            return best;
        }

        private static IEnumerable<(int Line, List<string> Fields)> ReadRecords(string text, char sep)
        {
            var fields = new List<string>();
            var field = new StringBuilder();
            int line = 1, recordLine = 1, i = 0;
            bool quoted = false, wasQuoted = false;

            while (i < text.Length)
            {
                char ch = text[i];
                if (quoted)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i += 2; continue; }
                        quoted = false; i++; continue;
                    }
                    if (ch == '\n') line++;
                    field.Append(ch); i++; continue;
                }

                if (ch == '"')
                {
                    if (field.Length > 0 || wasQuoted)
                        throw new FormatException($"Dòng {line}: dấu nháy kép nằm giữa ô. Ô có dấu nháy phải bọc cả ô trong \"…\" và viết \"\" cho mỗi dấu nháy bên trong.");
                    quoted = wasQuoted = true; i++; continue;
                }
                if (ch == sep) { fields.Add(field.ToString()); field.Clear(); wasQuoted = false; i++; continue; }
                if (ch == '\r' || ch == '\n')
                {
                    fields.Add(field.ToString()); field.Clear(); wasQuoted = false;
                    yield return (recordLine, fields);
                    fields = new List<string>();
                    i += ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n' ? 2 : 1;
                    line++; recordLine = line;
                    continue;
                }
                if (wasQuoted)
                    throw new FormatException($"Dòng {line}: có ký tự sau dấu nháy đóng của ô.");
                field.Append(ch); i++;
            }

            if (quoted) throw new FormatException($"Dòng {recordLine}: thiếu dấu nháy kép đóng ô.");
            if (field.Length > 0 || fields.Count > 0 || wasQuoted)
            {
                fields.Add(field.ToString());
                yield return (recordLine, fields);
            }
        }

        private static void AppendLine(StringBuilder sb, IEnumerable<string> cells, char sep)
        {
            bool first = true;
            foreach (string cell in cells)
            {
                if (!first) sb.Append(sep);
                first = false;
                bool needQuotes = cell.IndexOf(sep) >= 0 || cell.IndexOf('"') >= 0 || cell.IndexOf('\n') >= 0 || cell.IndexOf('\r') >= 0
                                  || (cell.Length > 0 && (char.IsWhiteSpace(cell[0]) || char.IsWhiteSpace(cell[cell.Length - 1])));
                if (needQuotes) sb.Append('"').Append(cell.Replace("\"", "\"\"")).Append('"');
                else sb.Append(cell);
            }
            sb.Append("\r\n");
        }

        private static void CheckDelimiter(char sep)
        {
            if (sep == '"' || sep == '\r' || sep == '\n')
                throw new ArgumentException("Dấu phân cách không được là dấu nháy kép hoặc ký tự xuống dòng.", nameof(sep));
        }
    }
}
