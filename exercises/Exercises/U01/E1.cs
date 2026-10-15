// U01-E1 — Form tủ điện (phần ViewModel) — docs/training/U01_WPF_MVVM_BASICS.md §12
// Phần giao diện (XAML) làm trên project HocWpf như tài liệu; test ở đây chỉ chấm ViewModel:
//   - Tên tủ (Name): bind thẳng, không có luật.
//   - Số ngăn (CountText): số nguyên 1–30; ô trống hay "2.5", "abc" là lỗi.
//   - Rộng (WidthText): số thực > 0, CHỈ nhận dấu chấm thập phân: "2,5" là lỗi, "2.5" đúng (U01 §5.3).
//   - ValidationSummary: mọi lỗi, mỗi lỗi một dòng; rỗng khi mọi ô đúng.
//   - DrawCommand: chỉ bật khi không có lỗi.
//   - Đổi "theo từng phím gõ": mỗi lần CountText/WidthText đổi, ValidationSummary phải báo
//     PropertyChanged và DrawCommand phải báo CanExecuteChanged.
// Chấm: dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~U01.E1
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Exercises.U01;

public partial class PanelFormViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "MSB-01";

    [ObservableProperty]
    private string _countText = "4";

    [ObservableProperty]
    private string _widthText = "600";

    // TODO: trả về mọi lỗi, mỗi lỗi một dòng (string.Join("\n", …)).
    public string ValidationSummary => "";

    // TODO: chỉ đúng khi không có lỗi.
    private bool CanDraw() => true;

    [RelayCommand(CanExecute = nameof(CanDraw))]
    private void Draw()
    {
        // Phần vẽ không thuộc bài tập.
    }
}
