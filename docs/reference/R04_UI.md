> **📘 Tra cứu CadCoreFramework** · [Mục lục tài liệu](../README.md) · [← R03](R03_API.md) · **R04** · [R05 →](R05_GOLDEN_SAMPLE.md)

# R04 — Giao Diện: Palette, Ribbon, MVVM Và Theme

> **Dùng khi:** thêm hoặc sửa palette, ribbon, ViewModel; cần nhớ key màu của theme · **Học bài bản:** nhánh UI [U00](../training/U00_UI_START_HERE.md) → [U01 WPF & MVVM](../training/U01_WPF_MVVM_BASICS.md) → [U02 UI trong AutoCAD](../training/U02_UI_IN_AUTOCAD.md) → [U03 Vẽ thực thể lên UI](../training/U03_DRAWING_ON_UI.md)

Giao diện của dự án dùng WPF theo mô hình MVVM (Model–View–ViewModel): View chỉ có XAML và binding, ViewModel giữ trạng thái và lệnh, Service làm việc với bản vẽ. Trang này là bản tra cứu; muốn học theo quy tắc 20/80, từ WPF cơ bản tới **vẽ thực thể lên UI** (khung xem trước, jig, transient), học nhánh UI ở trên.

---

## Mục lục

1. [Palette và Ribbon](#1-palette-và-ribbon)
2. [ViewModel: `ViewModelBase` và `RunSafe`](#2-viewmodel-viewmodelbase-và-runsafe)
3. [Ô nhập số](#3-ô-nhập-số)
4. [Theme sáng/tối tự động](#4-theme-sángtối-tự-động)

---

## 1. Palette và Ribbon

### Palette (`IPaletteService`)

🔎 [`Core/PaletteService.cs`](../../Core/PaletteService.cs) · vòng đời ViewModel: [T02 §8](../training/T02_ARCHITECTURE_DEEP_DIVE.md#8-paletteservice-và-vòng-đời-viewmodel)

PaletteSet (bảng neo giống thanh Properties của AutoCAD) là nơi đặt UI quen thuộc nhất. `TogglePalette` nhận một `Func<FrameworkElement>` thay vì một `UserControl` đã tạo sẵn: content chỉ được tạo khi người dùng thật sự mở palette lần đầu (tạo lười).

```csharp
// Hàm khởi tạo UI được bọc trong () => ... để trì hoãn việc load RAM
_paletteService.TogglePalette(
    "TuDienPalette", 
    () => new Views.TuDienControl(), 
    tabTitle: "Thiết Kế Tủ"
);
```

- Mỗi PaletteSet có **Guid ổn định** (dẫn xuất từ `[assembly: Guid]` + tên palette), nên AutoCAD nhớ vị trí, kích thước, trạng thái dock qua các lần khởi động. **Đừng đổi tên palette** sau khi phát hành.
- Khi content bị thay, `PaletteService` Dispose ViewModel cũ; subscription đăng ký qua `TrackSubscription` được gỡ theo.
- Lỗi khi mở palette (`TogglePalette`/`ShowPalette`) được ghi log rồi **ném lại** để lệnh báo cho người dùng.
- Toàn bộ thành viên của `IPaletteService`: [R03 §8](R03_API.md#8-service-có-sẵn). Thêm palette mới: [T08 §4](../training/T08_RECIPES_EXTENDING.md#4-thêm-một-palette).

### Ribbon (`IRibbonService`)

🔎 [`Core/RibbonService.cs`](../../Core/RibbonService.cs) · tab của dự án: [`Commands/CadCoreRibbon.cs`](../../Commands/CadCoreRibbon.cs)

`RibbonService` cung cấp `AddTab/AddPanel/AddButton/AddSplitButton/RemoveTab` và `RegisterBuildAction`: hàm build được gọi khi Ribbon sẵn sàng và gọi **lại** sau `CUI`/`MENULOAD`/`WSCURRENT` (các lệnh này dựng lại Ribbon và xóa tab của plugin). **Đừng sửa `RibbonService`** để thêm nút: thêm một dòng vào `CadCoreRibbon.Build` (tab "CadCore"). Vì sao hàm build bắt đầu bằng `RemoveTab`: [T08 §6](../training/T08_RECIPES_EXTENDING.md#6-thêm-ribbon-tab).

---

## 2. ViewModel: `ViewModelBase` và `RunSafe`

🔎 [`ViewModels/ViewModelBase.cs`](../../ViewModels/ViewModelBase.cs)

Trong MVVM, XAML không chứa logic AutoCAD; code-behind chỉ làm hạ tầng. `ViewModelBase` (dựa trên CommunityToolkit.Mvvm) cho mọi ViewModel `IsBusy`, `ErrorMessage`, `TrackSubscription` và hai hàm bọc lỗi `RunSafe` / `RunSafeAsync`.

Nếu một `Task.Run()` trong WPF bị lỗi mà không ai `await`, lỗi bị "nuốt" và bạn không hề biết. `RunSafeAsync` (và bản đồng bộ `RunSafe`) làm đúng bốn bước, như code hiện tại:
1. Xóa `ErrorMessage`, bật `IsBusy = true`.
2. `await` hàm bạn truyền vào, bên trong `try-catch`.
3. Có lỗi thì **gán `ErrorMessage`** (kèm tiền tố nếu có) **và ghi log** kèm stack trace nếu ViewModel có logger. Không có `Dispatcher.Invoke` hay MessageBox nào: UI tự hiển thị lỗi nhờ binding vào `ErrorMessage`.
4. `finally`: tắt `IsBusy = false`.

Logger: ViewModel con nhận `ILoggerService? logger = null` trong constructor rồi chuyển cho `base(logger)`. DI tự inject, unit test có thể bỏ qua ([T09 B-24](../training/T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng)). Với `RunSafe` (đồng bộ), `IsBusy` bật rồi tắt **trước khi UI kịp vẽ lại**, nên spinner chỉ có ý nghĩa với thao tác async.

XAML và ViewModel tương ứng:

```xml
<!-- Nút chỉ bị disable khi CanExecute của lệnh trả false — ví dụ GridMarkerViewModel.CanDraw() kiểm !IsBusy -->
<Button Command="{Binding DrawCommand}" Content="Vẽ Ngay" />

<!-- Khung nhập liệu Tên Tủ -->
<TextBox Text="{Binding TenTu, UpdateSourceTrigger=PropertyChanged}" />
```

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

`ITuDienService` ở đây là service bạn tự viết. Ví dụ đầy đủ, có thật trong dự án (validate + `CanExecute`, trạng thái dạng enum, EventAggregator, unit test): [R05 — Golden Sample](R05_GOLDEN_SAMPLE.md), file [`Samples/GridMarker/GridMarkerViewModel.cs`](../../Samples/GridMarker/GridMarkerViewModel.cs).

---

## 3. Ô nhập số

Ô nhập **số** (số hàng, khoảng cách…) thì bind vào property **chuỗi** rồi tự đọc số bằng [`ViewModels/NumberText`](../../ViewModels/NumberText.cs), như `RowsText`/`SpacingText` của Golden Sample. Đừng bind thẳng vào `int`/`double`:
- WPF đọc theo `Language` của element, mặc định `en-US`, nên `2,5` thành **25** mà không báo lỗi (B-31).
- Ô bị xóa trắng thì binding thất bại và ViewModel **lặng lẽ giữ số cũ**, nút Vẽ vẫn bật (B-32).

`NumberText` đọc số như dòng lệnh AutoCAD: dấu chấm thập phân, không phụ thuộc culture của máy, chặn `NaN`/vô cực/tràn số. Test `EveryPaletteTextBox_BindsToAStringProperty` kiểm luật này cho mọi palette. Ba bẫy, kiểm trên palette thật: [U01 §5](../training/U01_WPF_MVVM_BASICS.md#5-ba-bẫy-khi-nhập-số).

---

## 4. Theme sáng/tối tự động

🔎 [`Themes/ThemeManager.cs`](../../Themes/ThemeManager.cs) · chi tiết: [T02 §10](../training/T02_ARCHITECTURE_DEEP_DIVE.md#10-thememanager-styles-và-hệ-màu)

`ThemeManager` đọc sysvar **`COLORTHEME`** (0 = tối, 1 = sáng), nạp `AutoCadTheme.xaml` (tối) hoặc `AutoCadLightTheme.xaml` (sáng), và nghe `SystemVariableChanged` để đổi theo lúc đang chạy, kể cả palette **đang mở**.

Trong code-behind của palette, ngay sau `InitializeComponent()`:

```csharp
ThemeManager.Attach(this);   // nạp Styles.xaml + theme hiện hành; tự áp lại khi người dùng đổi COLORTHEME
```

Trong XAML của View, luôn dùng `DynamicResource` với **key có thật** (test `XamlPaletteTests` kiểm mọi key ở cả hai theme). **Một nguồn màu duy nhất**:
- **Màu** nằm trong `Themes/AutoCadTheme.xaml` và `Themes/AutoCadLightTheme.xaml`, **cùng bộ key** ở cả hai file: `BackgroundBrush`, `SurfaceBrush`, `SurfaceAltBrush`, `SurfaceHoverBrush`, `BorderBrush`, `TextPrimaryBrush`, `TextSecondaryBrush`, `AccentBrush`, `AccentHoverBrush`, `OnAccentBrush`, `DangerBrush`, `SuccessBrush`, `WarningBrush`, `OverlayBrush`, cùng bộ `Cad*` (`CadBackground.Primary`, `CadButton`, `CadTextBox`…). Thêm màu mới thì thêm vào **cả hai** file.
- **Style** nằm trong `Themes/Styles.xaml` (`LabelStyle`, `HeaderStyle`, `InputStyle`, `PrimaryButton`, `SecondaryButton`, `TabItemStyle`, `DataGridStyle`…), tham chiếu màu bằng `DynamicResource`. File này không chứa mã màu nào.

```xml
<!-- Tuyệt đối không viết Background="White" hay Foreground="Black" -->
<UserControl Background="{DynamicResource BackgroundBrush}"
             Foreground="{DynamicResource TextPrimaryBrush}">
    <!-- Các thành phần bên trong sẽ tự thừa kế màu -->
</UserControl>
```

`DynamicResource` với key **không tồn tại** không báo lỗi, chỉ âm thầm dùng màu mặc định (đã xảy ra ở `LayerManagerControl.xaml`, backlog B-03).

> [!NOTE]
> Trên v1.0.0, `IsDarkMode` đọc sysvar **`DARKMODE`**, một biến **không tồn tại**, nên theme luôn ra bản sáng, và `Styles.xaml` cố định một bảng màu tối riêng. Sửa ở PR #6; câu chuyện đầy đủ (và bài học): [T03 A13](../training/T03_PITFALLS_CASEBOOK.md#a13-palette-luôn-theme-sáng-dù-autocad-đang-tối).
