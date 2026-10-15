// C01-E4 — docs/training/C01_FIRST_PROGRAM.md §10
// In các thông báo lỗi theo 3 luật của GridSpec, mỗi lỗi một dòng, câu chữ giống hệt
// GridSpec.Validate (mở Samples/GridMarker/GridSpec.cs để chép):
//   - số hàng và số cột phải trong 1–100;
//   - tổng số ô không quá 2500.
// Ví dụ Run(60, 50) in đúng một dòng: Tổng số ô (3000) vượt quá giới hạn 2500.
// Test so bài làm với GridSpec thật trên nhiều cặp (hàng, cột) từ 0 tới 120.
// Chấm: dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C01.E4
namespace Exercises.C01;

public static class E4
{
    public static void Run(int rows, int columns)
    {
        // TODO: viết bài làm ở đây, thay cho dòng throw.
        throw new NotImplementedException();
    }
}
