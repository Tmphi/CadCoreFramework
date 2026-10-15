// Lời giải mẫu C02 — giống đáp án trong docs/training/C02_DATA_AND_COLLECTIONS.md §10.
namespace Exercises.C02;

public static class E1
{
    private static readonly char[] Invalid = { '<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', ',', '=', '`' };

    public static List<string> KeepValid(List<string> names)
    {
        var valid = new List<string>();
        foreach (string name in names)
        {
            if (!string.IsNullOrWhiteSpace(name) && name.IndexOfAny(Invalid) < 0)
                valid.Add(name);
        }
        return valid;
    }

    public static void Run()
    {
        var names = new List<string> { "E-POWER", "A*B", "", "Lưới trục", "LAYER:1", "M.EQUIP" };
        Console.WriteLine(string.Join(", ", KeepValid(names)));
    }
}

public static class E2
{
    public static Dictionary<string, int> CountByLayer(string[] entityLayers)
    {
        var counts = new Dictionary<string, int>();
        foreach (string layer in entityLayers)
        {
            counts.TryGetValue(layer, out int current);   // không có khóa → current = 0
            counts[layer] = current + 1;
        }
        return counts;
    }

    public static void Run()
    {
        string[] entityLayers = { "E-POWER", "A-WALL", "E-POWER", "0", "E-POWER", "A-WALL" };
        foreach (var pair in CountByLayer(entityLayers))
            Console.WriteLine($"{pair.Key}: {pair.Value}");
    }
}

public static class E3
{
    public static IReadOnlyList<string> Validate(int rows, int columns)
    {
        const int MaxDimension = 100, MaxCells = 2500;
        var errors = new List<string>();
        if (rows < 1 || rows > MaxDimension)
            errors.Add($"Số hàng phải trong khoảng 1–{MaxDimension}.");
        if (columns < 1 || columns > MaxDimension)
            errors.Add($"Số cột phải trong khoảng 1–{MaxDimension}.");
        if (rows >= 1 && columns >= 1 && rows * columns > MaxCells)
            errors.Add($"Tổng số ô ({rows * columns}) vượt quá giới hạn {MaxCells}.");
        return errors;
    }
}
