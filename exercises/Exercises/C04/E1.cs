// C04-E1 — LINQ — docs/training/C04_CSHARP_IN_THIS_PROJECT.md §13
// Mỗi hàm viết MỘT dòng LINQ:
//   1. tên các layer bắt đầu bằng "A-", xếp theo bảng chữ cái;
//   2. số layer có tên dài hơn 5 ký tự;
//   3. có layer nào chứa chữ "LIGHT" không;
//   4. danh sách tên viết thường (ToLowerInvariant).
// Run() đã viết sẵn: nó in kết quả của danh sách mẫu, mỗi câu một dòng.
// Chấm: dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C04.E1
namespace Exercises.C04;

public static class E1
{
    // TODO: viết bài làm ở đây, thay cho các throw.
    public static IEnumerable<string> StartingWithA(List<string> layers) => throw new NotImplementedException();

    public static int CountLongerThan5(List<string> layers) => throw new NotImplementedException();

    public static bool AnyContainsLight(List<string> layers) => throw new NotImplementedException();

    public static IEnumerable<string> ToLower(List<string> layers) => throw new NotImplementedException();

    public static void Run()
    {
        var layers = new List<string> { "0", "E-POWER", "A-WALL", "E-LIGHT", "Defpoints", "E-DATA", "A-DOOR" };
        Console.WriteLine(string.Join(", ", StartingWithA(layers)));
        Console.WriteLine(CountLongerThan5(layers));
        Console.WriteLine(AnyContainsLight(layers));
        Console.WriteLine(string.Join(", ", ToLower(layers)));
    }
}
