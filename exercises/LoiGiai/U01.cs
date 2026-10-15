// Lời giải mẫu U01-E1 (ViewModel) — theo cách sửa 20/80 ở docs/training/U01_WPF_MVVM_BASICS.md §5.3.
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Exercises.U01;

public partial class PanelFormViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "MSB-01";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValidationSummary))]
    [NotifyCanExecuteChangedFor(nameof(DrawCommand))]
    private string _countText = "4";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValidationSummary))]
    [NotifyCanExecuteChangedFor(nameof(DrawCommand))]
    private string _widthText = "600";

    public string ValidationSummary => string.Join("\n", Validate());

    private List<string> Validate()
    {
        var errors = new List<string>();
        if (!int.TryParse(CountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) || count < 1 || count > 30)
            errors.Add("Số ngăn: nhập một số nguyên từ 1 đến 30.");
        if (!double.TryParse(WidthText, NumberStyles.Float, CultureInfo.InvariantCulture, out double width))
            errors.Add("Rộng: nhập một số, phần thập phân dùng dấu chấm (ví dụ 2.5).");
        else if (width <= 0)
            errors.Add("Rộng phải lớn hơn 0.");
        return errors;
    }

    private bool CanDraw() => Validate().Count == 0;

    [RelayCommand(CanExecute = nameof(CanDraw))]
    private void Draw()
    {
        // Phần vẽ không thuộc bài tập.
    }
}
