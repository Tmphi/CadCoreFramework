using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Labs.Grader
{
    /// <summary>
    /// Giao thức giữa lệnh chấm và run-lab.ps1 — mỗi lệnh LAB_GRADE_* in:
    /// <c>LAB|COUNT|Lxx|n</c>, rồi n dòng <c>LAB|id|PASS[|ghi chú]</c> / <c>LAB|id|FAIL|lý do</c> /
    /// <c>LAB|id|SKIP|lý do</c>, rồi <c>LAB|END|Lxx</c>.
    /// Thí nghiệm có thể làm AutoCAD dừng hẳn (L03 TN2, TN3) in thêm <c>LAB|id|BEGIN</c> trước khi gọi bài làm:
    /// runner thấy BEGIN mà không thấy kết quả thì đọc thông báo fatal error trong output.
    /// </summary>
    internal static class Grade
    {
        internal static Editor Editor => Application.DocumentManager.MdiActiveDocument.Editor;

        internal static void Count(string lab, int count) => Write($"LAB|COUNT|{lab}|{count}");
        internal static void End(string lab) => Write($"LAB|END|{lab}");
        internal static void Begin(string id) => Write($"LAB|{id}|BEGIN");

        /// <summary>Chạy một kiểm tra. <paramref name="body"/> trả ghi chú (hoặc null) khi đạt, ném khi không.</summary>
        internal static void Check(string id, Func<string> body)
        {
            try
            {
                string note = body();
                Write(note == null ? $"LAB|{id}|PASS" : $"LAB|{id}|PASS|{OneLine(note)}");
            }
            catch (LabSkip skip) { Write($"LAB|{id}|SKIP|{OneLine(skip.Message)}"); }
            catch (LabFail fail) { Write($"LAB|{id}|FAIL|{OneLine(fail.Message)}"); }
            catch (NotImplementedException) { Write($"LAB|{id}|FAIL|chưa làm: bài làm còn throw NotImplementedException"); }
            catch (System.Exception ex) { Write($"LAB|{id}|FAIL|{ex.GetType().Name}: {OneLine(ex.Message)}"); }
        }

        internal static void Assert(bool condition, string message)
        {
            if (!condition) throw new LabFail(message);
        }

        internal static string Show(IEnumerable<string> items) => "[" + string.Join(", ", items) + "]";

        internal static string Show(IDictionary<string, int> counts)
            => Show(counts.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"));

        /// <summary>So hai bảng đếm; trả mô tả chỗ khác nhau, hoặc null nếu giống hệt.</summary>
        internal static string Diff(IDictionary<string, int> expected, IDictionary<string, int> actual)
        {
            if (actual == null) return "bài làm trả null";
            var lines = new List<string>();
            foreach (var pair in expected.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (!actual.TryGetValue(pair.Key, out int got)) lines.Add($"thiếu {pair.Key} (cần {pair.Value})");
                else if (got != pair.Value) lines.Add($"{pair.Key}: cần {pair.Value}, nhận {got}");
            }
            foreach (string key in actual.Keys.Where(k => !expected.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal))
                lines.Add($"thừa {key}={actual[key]}");
            return lines.Count == 0 ? null : string.Join("; ", lines);
        }

        /// <summary>Đếm entity của một không gian theo DxfName — đáp án của bộ chấm (không mở entity).</summary>
        internal static Dictionary<string, int> CountByDxf(BlockTableRecord space)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (ObjectId id in space)
            {
                string dxf = id.ObjectClass.DxfName;
                counts[dxf] = counts.TryGetValue(dxf, out int n) ? n + 1 : 1;
            }
            return counts;
        }

        /// <summary>Tên thật của một block reference — đáp án của bộ chấm, viết độc lập với DynamicBlockHelper.</summary>
        internal static string EffectiveName(Transaction tr, BlockReference br)
        {
            ObjectId btrId = br.IsDynamicBlock ? br.DynamicBlockTableRecord : br.BlockTableRecord;
            return ((BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead)).Name;
        }

        // Xuống dòng cả trước lẫn sau: AutoCAD có thể in tiếp ngay sau dòng LAB, làm runner không nhận ra kết quả.
        private static void Write(string line) => Editor.WriteMessage("\n" + line + "\n");

        private static string OneLine(string text) => text.Replace("\r", " ").Replace("\n", " ").Replace("|", "/");
    }

    internal sealed class LabFail : System.Exception
    {
        public LabFail(string message) : base(message) { }
    }

    internal sealed class LabSkip : System.Exception
    {
        public LabSkip(string message) : base(message) { }
    }
}
