// Lời giải mẫu C04 — giống đáp án trong docs/training/C04_CSHARP_IN_THIS_PROJECT.md §13.
namespace Exercises.C04;

public static class E1
{
    public static IEnumerable<string> StartingWithA(List<string> layers) => layers.Where(l => l.StartsWith("A-")).OrderBy(l => l);

    public static int CountLongerThan5(List<string> layers) => layers.Count(l => l.Length > 5);

    public static bool AnyContainsLight(List<string> layers) => layers.Any(l => l.Contains("LIGHT"));

    public static IEnumerable<string> ToLower(List<string> layers) => layers.Select(l => l.ToLowerInvariant());

    public static void Run()
    {
        var layers = new List<string> { "0", "E-POWER", "A-WALL", "E-LIGHT", "Defpoints", "E-DATA", "A-DOOR" };
        Console.WriteLine(string.Join(", ", StartingWithA(layers)));
        Console.WriteLine(CountLongerThan5(layers));
        Console.WriteLine(AnyContainsLight(layers));
        Console.WriteLine(string.Join(", ", ToLower(layers)));
    }
}

public static class SymbolNameExtensions
{
    private static readonly char[] Invalid = { '<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', ',', '=', '`' };

    public static bool IsValidSymbolName(this string name)
        => !string.IsNullOrWhiteSpace(name) && name.Length <= 255 && name.IndexOfAny(Invalid) < 0;
}

public sealed class DrawJob
{
    public event EventHandler<int>? Progress;

    public void Run(int steps)
    {
        for (int i = 1; i <= steps; i++)
            Progress?.Invoke(this, i * 100 / steps);
    }
}
