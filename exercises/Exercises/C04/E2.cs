// C04-E2 — Extension method — docs/training/C04_CSHARP_IN_THIS_PROJECT.md §13
// IsValidSymbolName đúng khi tên không rỗng (kể cả chỉ có dấu cách), dài tối đa 255 ký tự,
// và không chứa ký tự cấm (danh sách ở C02-E1). Gọi được như "E-POWER".IsValidSymbolName().
// Test so bài làm với SymbolNameRules.IsValid thật (Utils/SymbolNameRules.cs) trên nhiều chuỗi.
// Chấm: dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C04.E2
namespace Exercises.C04;

public static class SymbolNameExtensions
{
    public static bool IsValidSymbolName(this string name)
    {
        // TODO: viết bài làm ở đây, thay cho dòng throw.
        throw new NotImplementedException();
    }
}
