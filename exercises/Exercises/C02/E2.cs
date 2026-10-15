// C02-E2 — docs/training/C02_DATA_AND_COLLECTIONS.md §10
// Dùng Dictionary<string, int> đếm số entity trên mỗi layer.
// Run() in kết quả của mảng mẫu, mỗi layer một dòng, dạng "E-POWER: 3".
// Chấm: dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C02.E2
namespace Exercises.C02;

public static class E2
{
    public static Dictionary<string, int> CountByLayer(string[] entityLayers)
    {
        // TODO: viết bài làm ở đây, thay cho dòng throw.
        throw new NotImplementedException();
    }

    public static void Run()
    {
        string[] entityLayers = { "E-POWER", "A-WALL", "E-POWER", "0", "E-POWER", "A-WALL" };
        // TODO: in từng cặp của CountByLayer(entityLayers), dạng "E-POWER: 3".
        throw new NotImplementedException();
    }
}
