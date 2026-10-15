using System.Globalization;
using Xunit;

// Console.SetOut là trạng thái chung của cả tiến trình: chạy test tuần tự để output không lẫn nhau.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Exercises.Tests;

/// <summary>Chạy một đoạn code, trả về các dòng nó in ra (bỏ khoảng trắng cuối dòng và dòng trống cuối).</summary>
public static class ConsoleOutput
{
    public static string[] Capture(Action action)
    {
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            using (Culture.Invariant())
                action();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var lines = writer.ToString().Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()).ToList();
        while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
            lines.RemoveAt(lines.Count - 1);
        return lines.ToArray();
    }
}

/// <summary>
/// Tài liệu in số theo định dạng bất biến (dấu chấm thập phân). Máy định dạng Việt Nam in dấu phẩy,
/// nên test đặt tạm InvariantCulture để bài làm đúng không bị chấm sai vì máy (C01 §5).
/// </summary>
public static class Culture
{
    public static IDisposable Invariant()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        return new Restore(original);
    }

    private sealed class Restore : IDisposable
    {
        private readonly CultureInfo _original;
        public Restore(CultureInfo original) { _original = original; }
        public void Dispose() => CultureInfo.CurrentCulture = _original;
    }
}
