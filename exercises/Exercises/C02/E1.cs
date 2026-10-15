// C02-E1 — docs/training/C02_DATA_AND_COLLECTIONS.md §10
// Giữ lại các tên HỢP LỆ: không rỗng (kể cả chỉ có dấu cách), không chứa ký tự cấm
//   < > / \ " : ; ? * | , = `
// (đúng danh sách trong Utils/SymbolNameRules.cs). Giữ nguyên thứ tự ban đầu.
// Dùng foreach, List<string>, string.IsNullOrWhiteSpace và IndexOfAny.
// Run() in kết quả của danh sách mẫu, cách nhau ", ":  E-POWER, Lưới trục, M.EQUIP
// Chấm: dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C02.E1
namespace Exercises.C02;

public static class E1
{
    public static List<string> KeepValid(List<string> names)
    {
        // TODO: viết bài làm ở đây, thay cho dòng throw.
        throw new NotImplementedException();
    }

    public static void Run()
    {
        var names = new List<string> { "E-POWER", "A*B", "", "Lưới trục", "LAYER:1", "M.EQUIP" };
        // TODO: in KeepValid(names), cách nhau ", ".
        throw new NotImplementedException();
    }
}
