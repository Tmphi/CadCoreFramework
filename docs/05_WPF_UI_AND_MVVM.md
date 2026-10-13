# Phần 5: Giao Diện Người Dùng & MVVM (WPF UI & Themes)

Sử dụng thư viện WPF là cách chuyên nghiệp nhất để tạo giao diện trong AutoCAD .NET hiện đại. Framework này tuân thủ hoàn toàn mô hình MVVM (Model-View-ViewModel).

---

## 5.1 Palette & Ribbon

### Bảng Neo Palette (`IPaletteService`)
Bảng PaletteSet (giống thanh Properties của CAD) là không gian UI lý tưởng. Bạn gọi Palette thông qua [`PaletteService.cs`](../Core/PaletteService.cs).

> [!NOTE]
> **Under the hood (Tối ưu RAM):** Khởi tạo UI (WPF UserControl) tốn khá nhiều RAM. Framework yêu cầu bạn truyền vào một `Func<FrameworkElement>` thay vì truyền trực tiếp `UserControl`. Nghĩa là chỉ khi nào người dùng thực sự Mở bảng (chuột click vào Tab), Framework mới gọi hàm khởi tạo UI đó. Nó gọi là "Khởi tạo lười" (Lazy Instantiation).

```csharp
// Hàm khởi tạo UI được bọc trong () => ... để trì hoãn việc load RAM
_paletteService.TogglePalette(
    "TuDienPalette", 
    () => new Views.TuDienControl(), 
    tabTitle: "Thiết Kế Tủ"
);
```

### Thanh Ribbon (`IRibbonService`)
Ribbon là dải menu nằm ngang phía trên cùng. [`RibbonService.cs`](../Core/RibbonService.cs) cung cấp `AddTab/AddPanel/AddButton/AddSplitButton` và `RegisterBuildAction` (tự dựng lại sau `CUI`/`MENULOAD`). **Đừng sửa `RibbonService`** để thêm nút: hãy viết một hàm build riêng rồi đăng ký nó. Ở v1.0.0, dự án **chưa dựng tab nào**. Công thức đúng (thứ tự đăng ký, tránh nhân đôi nút): [T08 §6](training/T08_RECIPES_EXTENDING.md#6-thêm-ribbon-tab).

---

## 5.2 Giải phẫu MVVM & Bí Ẩn `RunSafeAsync`

Trong MVVM, giao diện XAML KHÔNG được chứa logic AutoCAD (Code-behind trống trơn). 

Lớp nền tảng [`ViewModelBase`](../ViewModels/ViewModelBase.cs) giải quyết bài toán giao tiếp bất đồng bộ qua hàm `RunSafeAsync`.

### Phân tích `RunSafeAsync`
Bình thường, nếu bạn gọi một `Task.Run()` trong WPF và Task đó bị lỗi, lỗi sẽ văng ra luồng nền (Background thread) và "nuốt" mất (bạn không hề biết có lỗi xảy ra). Nếu bạn cố gọi `MessageBox.Show` từ luồng nền, phần mềm sẽ crash vì can thiệp vào UI Thread.

Cơ chế của `RunSafeAsync` (và bản đồng bộ `RunSafe`), đúng như code hiện tại:
1. Xóa `ErrorMessage`, bật cờ `IsBusy = true`.
2. `await` hàm bạn cung cấp, bên trong `try-catch`.
3. Có lỗi thì **gán `ErrorMessage`** (kèm tiền tố nếu có). Không có `Dispatcher.Invoke` hay MessageBox nào: UI tự hiển thị lỗi nhờ binding vào `ErrorMessage`.
4. `finally`: tắt `IsBusy = false`.

Lưu ý: với `RunSafe` (đồng bộ), `IsBusy` bật rồi tắt **trước khi UI kịp vẽ lại**, nên spinner chỉ có ý nghĩa với thao tác async. Lỗi cũng chỉ vào `ErrorMessage`, không được ghi log ([T09 B-24](training/T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng)).

**Ví dụ Code XAML (Cần Binding):**
```xml
<!-- Nút chỉ bị disable khi CanExecute của lệnh trả false — ví dụ GridMarkerViewModel.CanDraw() kiểm !IsBusy -->
<Button Command="{Binding DrawCommand}" Content="Vẽ Ngay" />

<!-- Khung nhập liệu Tên Tủ -->
<TextBox Text="{Binding TenTu, UpdateSourceTrigger=PropertyChanged}" />
```

**Ví dụ Code ViewModel tương ứng:**
```csharp
public partial class TuDienViewModel : ViewModelBase
{
    private readonly ITuDienService _service;   // ViewModel KHÔNG gọi ICadContext trực tiếp

    [ObservableProperty]
    private string _tenTu = "Tủ Nhánh";

    public TuDienViewModel(ITuDienService service) => _service = service;

    [RelayCommand]
    private void Draw()
    {
        // RunSafe tự bật/tắt IsBusy và đưa exception vào ErrorMessage
        RunSafe(() =>
        {
            CadResult<int> result = _service.Draw(TenTu);   // Service bọc ICadContext.Execute
            if (!result.IsSuccess) ErrorMessage = result.ErrorMessage;
        });
    }
}
```

> Ví dụ đầy đủ (validate + `CanExecute`, trạng thái dạng enum, EventAggregator, unit test):
> xem [Phần 9: Golden Sample](09_GOLDEN_SAMPLE.md) — `Samples/GridMarker/GridMarkerViewModel.cs`.

---

## 5.3 AutoCAD Theme Tự Động (Dark/Light)

AutoCAD có chế độ Dark Mode. Giao diện tĩnh (hardcode màu Trắng/Đen) sẽ rất chói mắt. Framework dự định giải quyết bằng [`ThemeManager.cs`](../Themes/ThemeManager.cs): đọc theme của AutoCAD, nạp `AutoCadTheme.xaml` (tối) hoặc `AutoCadLightTheme.xaml` (sáng), và nghe `SystemVariableChanged` để đổi theo lúc đang chạy.

> [!WARNING]
> **Hiện trạng v1.0.0 (đã kiểm chứng trên AutoCAD 2024):** `CadApplicationHelper.IsDarkMode` đọc sysvar **`DARKMODE`**, mà biến này **không tồn tại** (`getvar` trả `nil`). Biến đúng là **`COLORTHEME`** (0 = tối, 1 = sáng). Hệ quả: theme `Cad*` luôn ra bản sáng và không đổi theo AutoCAD. Đồng thời `Styles.xaml` cố định một bảng màu tối riêng. Chi tiết và hướng sửa: [T03 A13](training/T03_PITFALLS_CASEBOOK.md#a13-palette-luôn-theme-sáng-dù-autocad-đang-tối), backlog B-01/B-02/B-23, Lab L12.

Trong XAML của View, luôn dùng `DynamicResource` với **key có thật**. Hai nhóm key hiện có:
- `Themes/Styles.xaml` (palette dùng chủ yếu): `BackgroundBrush`, `SurfaceBrush`, `TextPrimaryBrush`, `TextSecondaryBrush`, `AccentBrush`, `DangerBrush`, `SuccessBrush`, `WarningBrush`, cùng các style `LabelStyle`, `InputStyle`, `PrimaryButton`…
- `Themes/AutoCadTheme.xaml` / `AutoCadLightTheme.xaml`: `CadBackground.Primary`, `CadForeground.Primary`, `CadAccent.Blue`, `CadButton`, `CadTextBox`…

```xml
<!-- Tuyệt đối không viết Background="White" hay Foreground="Black" -->
<UserControl Background="{DynamicResource BackgroundBrush}"
             Foreground="{DynamicResource TextPrimaryBrush}">
    <!-- Các thành phần bên trong sẽ tự thừa kế màu -->
</UserControl>
```

`DynamicResource` với key **không tồn tại** sẽ không báo lỗi, chỉ âm thầm dùng màu mặc định (đã xảy ra ở `LayerManagerControl.xaml`, backlog B-03).
