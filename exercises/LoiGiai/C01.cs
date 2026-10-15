// Lời giải mẫu C01 — giống đáp án trong docs/training/C01_FIRST_PROGRAM.md §10.
// Chỉ biên dịch khi chạy với -p:UseSolutions=true (CI dùng để kiểm bộ test).
namespace Exercises.C01;

public static class E1
{
    public static void Run()
    {
        double width = 600, height = 2100;
        Console.WriteLine($"Diện tích: {width * height:F2}");
        Console.WriteLine($"Chu vi: {2 * (width + height):F2}");
    }
}

public static class E2
{
    public static void Run()
    {
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                Console.Write($"{(char)('A' + row)}{col + 1} ");
            }
            Console.WriteLine();
        }
    }
}

public static class E3
{
    public static string ToRowLetters(int index)
    {
        string result = "";
        int n = index + 1;                 // "hệ cơ số 26 không có số 0": A=1 … Z=26
        while (n > 0)
        {
            int rem = (n - 1) % 26;
            result = (char)('A' + rem) + result;
            n = (n - 1) / 26;
        }
        return result;
    }
}

public static class E4
{
    public static void Run(int rows, int columns)
    {
        const int MaxDimension = 100, MaxCells = 2500;

        if (rows < 1 || rows > MaxDimension)
            Console.WriteLine($"Số hàng phải trong khoảng 1–{MaxDimension}.");
        if (columns < 1 || columns > MaxDimension)
            Console.WriteLine($"Số cột phải trong khoảng 1–{MaxDimension}.");
        if (rows * columns > MaxCells)
            Console.WriteLine($"Tổng số ô ({rows * columns}) vượt quá giới hạn {MaxCells}.");
    }
}
