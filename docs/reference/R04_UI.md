# Phần 5: Giao Diện Người Dùng & MVVM (WPF UI & Themes)

Sử dụng thư viện WPF là cách chuyên nghiệp nhất để tạo giao diện trong AutoCAD .NET hiện đại. Framework này tuân thủ hoàn toàn mô hình MVVM (Model-View-ViewModel).

> [!TIP]
> Phần này là **tham khảo nhanh**. Muốn học bài bản theo quy tắc 20/80, từ WPF cơ bản tới **vẽ thực thể lên UI** (khung xem trước, jig, transient), hãy học **nhánh UI** của Học viện: [U00](training/U00_UI_START_HERE.md) → [U01 WPF & MVVM](training/U01_WPF_MVVM_BASICS.md) → [U02 UI trong AutoCAD](training/U02_UI_IN_AUTOCAD.md) → [U03 Vẽ thực thể lên UI](training/U03_DRAWING_ON_UI.md). Bẫy nhập số thập phân trên palette (gõ `2,5` thành 25): [U01 §5](training/U01_WPF_MVVM_BASICS.md#5-ba-bẫy-khi-nhập-số).

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

Mỗi PaletteSet có **Guid ổn định** (dẫn xuất từ `[assembly: Guid]` + tên palette), nên AutoCAD nhớ vị trí, kích thước, trạng thái dock qua các lần khởi động — **đừng đổi tên palette** sau khi phát hành. Lỗi khi mở palette (`TogglePalette`/`ShowPalette`) được ghi log rồi **ném lại** để lệnh báo cho người dùng.

### Thanh Ribbon (`IRibbonService`)
Ribbon là dải menu nằm ngang phía trên cùng. [`RibbonService.cs`](../Core/RibbonService.cs) cung cấp `AddTab/AddPanel/AddButton/AddSplitButton/RemoveTab` và `RegisterBuildAction` (dựng ngay khi Ribbon sẵn sàng, tự dựng lại sau `CUI`/`MENULOAD`/`WSCURRENT`). **Đừng sửa `RibbonService`** để thêm nút: thêm một dòng vào hàm build của dự án, [`Commands/CadCoreRibbon.cs`](../Commands/CadCoreRibbon.cs) (tab "CadCore"). Vì sao hàm build bắt đầu bằng `RemoveTab`: [T08 §6](training/T08_RECIPES_EXTENDING.md#6-thêm-ribbon-tab).

---

## 5.2 Giải phẫu MVVM & Bí Ẩn `RunSafeAsync`

Trong MVVM, giao diện XAML KHÔNG được chứa logic AutoCAD (Code-behind trống trơn). 

Lớp nền tảng [`ViewModelBase`](../ViewModels/ViewModelBase.cs) giải quyết bài toán giao tiếp bất đồng bộ qua hàm `RunSafeAsync`.

### Phân tích `RunSafeAsync`
Bình thường, nếu bạn gọi một `Task.Run()` trong WPF và Task đó bị lỗi, lỗi sẽ văng ra luồng nền (Background thread) và "nuốt" mất (bạn không hề biết có lỗi xảy ra). Nếu bạn cố gọi `MessageBox.Show` từ luồng nền, phần mềm sẽ crash vì can thiệp vào UI Thread.

Cơ chế của `RunSafeAsync` (và bản đồng bộ `RunSafe`), đúng như code hiện tại:
1. Xóa `ErrorMessage`, bật cờ `IsBusy = true`.
2. `await` hàm bạn cung cấp, bên trong `try-catch`.
3. Có lỗi thì **gán `ErrorMessage`** (kèm tiền tố nếu có) **và ghi log** kèm stack trace nếu ViewModel có logger. Không có `Dispatcher.Invoke` hay MessageBox nào: UI tự hiển thị lỗi nhờ binding vào `ErrorMessage`.
4. `finally`: tắt `IsBusy = false`.

Logger: ViewModel con nhận `ILoggerService? logger = null` trong constructor rồi chuyển cho `base(logger)` — DI tự inject, unit test có thể bỏ qua ([T09 B-24](training/T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng)). Lưu ý: với `RunSafe` (đồng bộ), `IsBusy` bật rồi tắt **trước khi UI kịp vẽ lại**, nên spinner chỉ có ý nghĩa với thao tác async.

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

> [!TIP]
> Ô nhập **số** (số hàng, khoảng cách…) thì bind vào property **chuỗi** rồi tự đọc số bằng `NumberText`, như `RowsText`/`SpacingText` của Golden Sample. Đừng bind thẳng vào `int`/`double`: WPF đọc `2,5` thành 25 và lặng lẽ giữ số cũ khi ô trống ([U01 §5](training/U01_WPF_MVVM_BASICS.md#5-ba-bẫy-khi-nhập-số)).

---

## 5.3 AutoCAD Theme Tự Động (Dark/Light)

AutoCAD có chế độ Dark Mode. Giao diện tĩnh (hardcode màu Trắng/Đen) sẽ rất chói mắt. Framework giải quyết bằng [`ThemeManager.cs`](../Themes/ThemeManager.cs): đọc sysvar **`COLORTHEME`** (0 = tối, 1 = sáng), nạp `AutoCadTheme.xaml` (tối) hoặc `AutoCadLightTheme.xaml` (sáng), và nghe `SystemVariableChanged` để đổi theo lúc đang chạy — kể cả palette **đang mở**.

Trong code-behind của palette, ngay sau `InitializeComponent()`:

```csharp
ThemeManager.Attach(this);   // nạp Styles.xaml + theme hiện hành; tự áp lại khi người dùng đổi COLORTHEME
```

Trong XAML của View, luôn dùng `DynamicResource` với **key có thật** (test `XamlPaletteTests` kiểm mọi key ở cả hai theme). **Một nguồn màu duy nhất**:
- **Màu** nằm trong `Themes/AutoCadTheme.xaml` và `Themes/AutoCadLightTheme.xaml`, **cùng bộ key** ở cả hai file: `BackgroundBrush`, `SurfaceBrush`, `SurfaceAltBrush`, `SurfaceHoverBrush`, `BorderBrush`, `TextPrimaryBrush`, `TextSecondaryBrush`, `AccentBrush`, `AccentHoverBrush`, `OnAccentBrush`, `DangerBrush`, `SuccessBrush`, `WarningBrush`, `OverlayBrush`, cùng bộ `Cad*` (`CadBackground.Primary`, `CadButton`, `CadTextBox`…). Thêm màu mới thì thêm vào **cả hai** file.
- **Style** nằm trong `Themes/Styles.xaml` (`LabelStyle`, `HeaderStyle`, `InputStyle`, `PrimaryButton`, `SecondaryButton`, `TabItemStyle`, `DataGridStyle`…), tham chiếu màu bằng `DynamicResource` — file này không chứa mã màu nào.

> [!NOTE]
> Trên v1.0.0, `IsDarkMode` đọc sysvar **`DARKMODE`** — biến **không tồn tại** — nên theme luôn ra bản sáng, và `Styles.xaml` cố định một bảng màu tối riêng. Sửa ở PR #6; câu chuyện đầy đủ (và bài học): [T03 A13](training/T03_PITFALLS_CASEBOOK.md#a13-palette-luôn-theme-sáng-dù-autocad-đang-tối), [T02 §10](training/T02_ARCHITECTURE_DEEP_DIVE.md#10-thememanager-styles-và-hệ-màu).

```xml
<!-- Tuyệt đối không viết Background="White" hay Foreground="Black" -->
<UserControl Background="{DynamicResource BackgroundBrush}"
             Foreground="{DynamicResource TextPrimaryBrush}">
    <!-- Các thành phần bên trong sẽ tự thừa kế màu -->
</UserControl>
```

`DynamicResource` với key **không tồn tại** sẽ không báo lỗi, chỉ âm thầm dùng màu mặc định (đã xảy ra ở `LayerManagerControl.xaml`, backlog B-03).
