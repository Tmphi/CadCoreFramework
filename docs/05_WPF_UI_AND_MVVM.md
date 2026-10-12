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
Ribbon là dải menu nằm ngang phía trên cùng. Bạn có thể tự động sinh Tab và Nút bấm (Button) bằng cách bổ sung hàm cấu hình vào [`RibbonService.cs`](../Core/RibbonService.cs).

---

## 5.2 Giải phẫu MVVM & Bí Ẩn `RunSafeAsync`

Trong MVVM, giao diện XAML KHÔNG được chứa logic AutoCAD (Code-behind trống trơn). 

Lớp nền tảng [`ViewModelBase`](../ViewModels/ViewModelBase.cs) giải quyết bài toán giao tiếp bất đồng bộ qua hàm `RunSafeAsync`.

### Phân tích `RunSafeAsync`
Bình thường, nếu bạn gọi một `Task.Run()` trong WPF và Task đó bị lỗi, lỗi sẽ văng ra luồng nền (Background thread) và "nuốt" mất (bạn không hề biết có lỗi xảy ra). Nếu bạn cố gọi `MessageBox.Show` từ luồng nền, phần mềm sẽ crash vì can thiệp vào UI Thread.

Cơ chế của `RunSafeAsync`:
1. Bật cờ `IsBusy = true` (Giao diện hiện xoay xoay vòng Loading).
2. Chạy hàm bạn cung cấp (bên trong `try-catch`).
3. Tự bắt lỗi, và nếu có lỗi, nó dùng `Dispatcher.Invoke` để ép lỗi được hiển thị một cách an toàn trên màn hình chính.
4. Cuối cùng ở `finally`, tắt cờ `IsBusy = false`.

**Ví dụ Code XAML (Cần Binding):**
```xml
<!-- Nút bấm sẽ tự động bị Disable nếu IsBusy = true -->
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

AutoCAD có chế độ Dark Mode. Giao diện tĩnh (hardcode màu Trắng/Đen) sẽ rất chói mắt. Framework giải quyết bằng [`ThemeManager.cs`](../Themes/ThemeManager.cs). Nó lắng nghe biến `COLORTHEME` của hệ thống.

Để áp dụng màu này, trong file XAML của View, hãy dùng `DynamicResource` cho màu nền và màu chữ:
```xml
<!-- Tuyệt đối không viết Background="White" hay Foreground="Black" -->
<UserControl Background="{DynamicResource PaletteBackground}"
             Foreground="{DynamicResource TextBrush}">
    <!-- Các thành phần bên trong sẽ tự thừa kế màu -->
</UserControl>
```
