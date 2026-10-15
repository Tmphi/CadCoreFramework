// C02-E3 — docs/training/C02_DATA_AND_COLLECTIONS.md §10
// Viết lại C01-E4 thành hàm TRẢ VỀ danh sách lỗi (rỗng = hợp lệ) thay vì in ngay.
// Câu chữ và thứ tự lỗi giống GridSpec.Validate. Test so với GridSpec thật trên nhiều cặp
// (hàng, cột) từ -5 tới 120 — kể cả số âm, nên hãy đọc kỹ luật "tổng số ô" của bản thật.
// Chấm: dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C02.E3
namespace Exercises.C02;

public static class E3
{
    public static IReadOnlyList<string> Validate(int rows, int columns)
    {
        // TODO: viết bài làm ở đây, thay cho dòng throw.
        throw new NotImplementedException();
    }
}
