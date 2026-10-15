// C03-E3 — docs/training/C03_OBJECT_ORIENTED.md §10
// Interface IPanelRepository đã có sẵn. Việc của bạn:
//   1. InMemoryPanelRepository: lưu tên tủ trong một List<string>, GetAll() trả đúng thứ tự đã thêm;
//   2. PanelReport: NHẬN IPanelRepository qua constructor, giữ trong một field;
//      Summary() trả về dạng "2 tủ: MSB-01, DB-02".
// Test còn đưa cho PanelReport một repository GIẢ do test tự viết: bài làm chỉ đúng khi
// PanelReport dùng đúng cái repository được truyền vào (C03 §8).
// Chấm: dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C03.E3
namespace Exercises.C03;

public interface IPanelRepository
{
    void Add(string name);
    IReadOnlyList<string> GetAll();
}

public sealed class InMemoryPanelRepository : IPanelRepository
{
    // TODO: viết bài làm ở đây, thay cho các throw.
    public void Add(string name) => throw new NotImplementedException();
    public IReadOnlyList<string> GetAll() => throw new NotImplementedException();
}

public sealed class PanelReport
{
    public PanelReport(IPanelRepository repository)
    {
        // TODO: giữ repository lại để Summary() dùng.
    }

    // TODO: viết bài làm ở đây, thay cho throw.
    public string Summary() => throw new NotImplementedException();
}
