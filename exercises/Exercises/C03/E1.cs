// C03-E1 — docs/training/C03_OBJECT_ORIENTED.md §10
// Mở rộng PanelSpec của C03 §3 (đã chép sẵn dưới đây):
//   1. hằng MaxHeight = 2400;
//   2. luật mới trong Validate(): "Chiều cao phải ≤ 2400." (viết bằng {MaxHeight});
//   3. property IsTall: đúng khi cao hơn 2000.
// Ba luật cũ phải giữ nguyên.
// Chấm: dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C03.E1
namespace Exercises.C03;

public sealed class PanelSpec
{
    public const double MinWidth = 300;

    // TODO: sửa thành giá trị đúng (số 0 chỉ để test biên dịch được).
    public const double MaxHeight = 0;

    public string Name { get; }
    public double Width { get; }
    public double Height { get; }

    public PanelSpec(string name, double width, double height = 2000)
    {
        Name = name ?? string.Empty;
        Width = width;
        Height = height;
    }

    public double Area => Width * Height;

    // TODO: viết bài làm ở đây, thay cho throw.
    public bool IsTall => throw new NotImplementedException();

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name)) errors.Add("Tên tủ không được để trống.");
        if (Width < MinWidth) errors.Add($"Chiều rộng phải ≥ {MinWidth}.");
        if (Height <= 0) errors.Add("Chiều cao phải > 0.");
        // TODO: thêm luật chiều cao tối đa.
        return errors;
    }

    public bool IsValid => Validate().Count == 0;
}
