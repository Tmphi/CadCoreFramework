> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← U01](U01_WPF_MVVM_BASICS.md) · **U02** · [U03 →](U03_DRAWING_ON_UI.md)

# U02 — Giao Diện Trong AutoCAD: Dòng Lệnh, Palette, Hộp Thoại, Ribbon

> **Cấp độ:** 🌱 Fresher → 🌳 Middle · **Cần trước:** [U01](U01_WPF_MVVM_BASICS.md), [T01 §5–7](T01_AUTOCAD_NET_FOUNDATIONS.md#5-document-lock-và-hai-ngữ-cảnh-thực-thi) · **Trong lộ trình:** tuần 8 ([README §3](README.md#3-lộ-trình-đào-tạo))

U01 dạy WPF như một thư viện độc lập. Module này đưa UI vào **bên trong AutoCAD**, nơi có thêm ba ràng buộc: **ngữ cảnh** (command hay application context), **lock** của bản vẽ, và **nhiều bản vẽ** cùng mở. Thứ tự các mục theo đúng bảng 20/80 ở [U00 §2](U00_UI_START_HERE.md#2-quy-tắc-2080-số-liệu): dòng lệnh trước, vì đó là UI mà tool AutoCAD dùng nhiều nhất.

Các hành vi AutoCAD trong module (prompt, từ khóa, UCS, chọn sẵn, đổi khung nhìn) đã **chạy thật** trên AutoCAD 2024 trong accoreconsole: input được gõ từ file script, giống người dùng gõ phím. Chỗ nào chỉ kiểm được trong AutoCAD có giao diện thì có ghi rõ.

---

## Mục lục

1. [Chọn bề mặt UI](#1-chọn-bề-mặt-ui)
2. [Dòng lệnh: bề mặt UI dùng nhiều nhất](#2-dòng-lệnh-bề-mặt-ui-dùng-nhiều-nhất)
3. [Palette](#3-palette)
4. [Hộp thoại modal](#4-hộp-thoại-modal)
5. [Cửa sổ modeless](#5-cửa-sổ-modeless)
6. [Ribbon](#6-ribbon)
7. [Từ UI tới bản vẽ và ngược lại](#7-từ-ui-tới-bản-vẽ-và-ngược-lại)
8. [Phản hồi cho người dùng](#8-phản-hồi-cho-người-dùng)
9. [Bẫy UI trong AutoCAD](#9-bẫy-ui-trong-autocad)
10. [Bài tập](#10-bài-tập)

---

## 1. Chọn bề mặt UI

| Yêu cầu | Chọn | Ví dụ trong dự án |
| :--- | :--- | :--- |
| Vài thông số, cần **pick điểm/đối tượng** trên bản vẽ | **Lệnh + prompt** | `SAMPLE_GRID` ([`GridMarkerCommands.cs`](../../Samples/GridMarker/GridMarkerCommands.cs)) |
| Nhiều thông số, dùng lặp lại, cần **xem kết quả liên tục** | **Palette** | Grid Marker (`SHOW_PALETTE`) |
| Thiết lập **một lần** trước khi chạy, có OK/Hủy | **Hộp thoại modal** | `DEMO_SHOW_WINDOW_DIALOG` |
| Truy cập nhanh một lệnh có sẵn | **Nút Ribbon** gửi lệnh | Tab "CadCore" |
| Báo kết quả, cảnh báo | **Dòng lệnh** (`INotificationService`) | Mọi lệnh của dự án |

Golden Sample làm **cả hai**: lệnh `SAMPLE_GRID` (pick điểm) và palette (nhập trên form). Cả hai dùng chung `GridSpec.Validate()`, `GridPlanner` và `GridMarkerService` ([Phần 9](../09_GOLDEN_SAMPLE.md)). Đây là mẫu nên theo: **logic dùng chung, chỉ lớp UI khác nhau**.

---

## 2. Dòng lệnh: bề mặt UI dùng nhiều nhất

### 2.1 Mẫu prompt

Mọi prompt có cùng ba bước: tạo *options* → gọi `Editor.GetXxx` → **kiểm `Status`** rồi mới dùng giá trị.

| `PromptStatus` | Khi nào |
| :--- | :--- |
| `OK` | Người dùng nhập hợp lệ |
| `Cancel` | Nhấn ESC |
| `None` | Nhấn Enter khi prompt cho phép bỏ trống (`AllowNone = true`) |
| `Keyword` | Gõ một từ khóa của prompt (prompt điểm có kèm từ khóa) |
| `Error` | Lỗi khác |

[`CadPromptHelper`](../../Utils/CadPromptHelper.cs) gói ba bước đó và trả `null` khi không phải `OK`, nên lệnh chỉ cần `if (x == null) return;` (nguyên văn, [`GridMarkerCommands.cs`](../../Samples/GridMarker/GridMarkerCommands.cs)):

```csharp
Point3d? origin = CadPromptHelper.GetPoint("Chọn tâm ô đầu tiên (A1):");
if (origin == null) return;

int? rows = CadPromptHelper.GetInteger("Số hàng", defaultValue: 3, lowerLimit: 1, upperLimit: GridSpec.MaxDimension);
if (rows == null) return;
```

Prompt luôn nằm **trước** transaction ([T01 §7.2](T01_AUTOCAD_NET_FOUNDATIONS.md#72-luật-thép-hỏi-trước-vẽ-sau)).

### 2.2 Giá trị mặc định

Có `defaultValue` thì AutoCAD hiện `<3>` cuối prompt, và **Enter** nhận giá trị đó. Đã kiểm: prompt in `Số hàng <3>:`, nhấn Enter, kết quả `Status=OK Value=3`. Dùng giá trị mặc định cho mọi thông số có giá trị "thường dùng": người dùng chỉ cần Enter liên tiếp.

### 2.3 Từ khóa

Kết quả đã kiểm với từ khóa `Yes`/`No`, mặc định `Yes`:
- Nhấn Enter → `Yes`.
- Gõ `n` → `No`: chỉ cần gõ **phần đầu** của từ khóa, không phân biệt hoa thường.

⚠️ **Lỗi của dự án (B-33):** `CadPromptHelper.GetKeywords` tự nối `[Yes/No]` vào câu nhắc, trong khi AutoCAD **cũng tự nối** danh sách từ khóa: `PromptOptions.AppendKeywordsToMessage` mặc định là `true` (đã kiểm). Kết quả là danh sách bị in hai lần (nguyên văn, AutoCAD 2024):

```text
Vẽ lưới này? [Yes/No] [Yes/No] <Yes>:
```

Viết đúng là để AutoCAD tự nối, câu nhắc chỉ chứa phần chữ (đã kiểm, in `Vẽ lưới này? [Yes/No] <Yes>:`):

```csharp
var options = new PromptKeywordOptions("\nVẽ lưới này?");
options.Keywords.Add("Yes");
options.Keywords.Add("No");
options.Keywords.Default = "Yes";
PromptResult answer = Editor.GetKeywords(options);
```

### 2.4 Hệ tọa độ: `GetPoint` trả về UCS

Thí nghiệm thật: xoay UCS 90° quanh trục Z, gõ `10,0`. `Editor.GetPoint` trả về **(10; 0)**, tức tọa độ **UCS** đúng như người dùng gõ. Đổi sang WCS (`TransformBy(ed.CurrentUserCoordinateSystem)`) thì được **(0; 10)**. Entity luôn được vẽ bằng WCS, nên `CadPromptHelper.GetPoint` đổi sẵn cho bạn ([T01 §7.3](T01_AUTOCAD_NET_FOUNDATIONS.md#73-ba-hệ-tọa-độ-wcs-ucs-ocs), lab L04). Jig thì **khác**: `AcquirePoint` trong jig trả thẳng WCS ([U03 §9.1](U03_DRAWING_ON_UI.md#91-jig-hình-chạy-theo-chuột)).

### 2.5 Số thực: dòng lệnh từ chối dấu phẩy

Trên dòng lệnh, dấu phẩy là dấu ngăn cách tọa độ. Gõ `1,5` vào `GetDouble` thì AutoCAD báo `Requires numeric value.` và hỏi lại. Palette WPF thì ngược lại: **âm thầm** đổi `2,5` thành 25 ([U01 §5](U01_WPF_MVVM_BASICS.md#5-ba-bẫy-khi-nhập-số)). Người dùng cùng một tool không nên gặp hai luật khác nhau, nên palette cũng nên chỉ nhận dấu chấm, giống dòng lệnh.

### 2.6 Chọn đối tượng và thông báo

- **Chọn:** `CadPromptHelper.GetSelection(message, filter)` kết hợp [`SelectionFilterBuilder`](../../Utils/SelectionFilterBuilder.cs): lọc theo loại, layer, màu, XData ([T01 §7.4](T01_AUTOCAD_NET_FOUNDATIONS.md#74-selection-và-selectionfilter), lab L05). Xem 5 cách lọc đang chạy: lệnh `DEMO_SELECTION_FILTER`.
- **Thông báo:** `WriteMessage` là API được dùng nhiều nhất trong code AutoCAD công khai (4.008 file). Luôn bắt đầu chuỗi bằng `"\n"`, nếu không chữ sẽ dính vào dòng prompt trước. Trong dự án, hãy dùng `INotificationService` (`Info`/`Warn`/`Error`): nó ghi cả log và chỉ ghi vào bản vẽ **đang active** ([T03 B31](T03_PITFALLS_CASEBOOK.md#b31-gọi-editor-của-bản-vẽ-không-active)).

---

## 3. Palette

**Mở và đóng.** Mỗi palette có một lệnh bật/tắt, gọi `TogglePalette` với một **hàm tạo** View. View chỉ được tạo khi người dùng thật sự mở palette (nguyên văn, [`SampleCommands.cs`](../../Commands/SampleCommands.cs)):

```csharp
[CommandMethod("SHOW_PALETTE")]
public void ShowPaletteCommand()
{
    ExecuteCommand(() =>
    {
        PaletteService.TogglePalette(
            "CadCoreFrameworkPalette",
            () => new Samples.GridMarker.GridMarkerPalette(),
            tabTitle: "Grid Marker"
        );
    });
}
```

Những điều phải biết (chi tiết ở [T02 §8](T02_ARCHITECTURE_DEEP_DIVE.md#8-paletteservice-và-vòng-đời-viewmodel) và công thức ở [T08 §4](T08_RECIPES_EXTENDING.md#4-thêm-một-palette)):
- Tên palette (`"CadCoreFrameworkPalette"`) sinh ra **Guid** để AutoCAD nhớ vị trí và kích thước. **Đừng đổi tên** sau khi phát hành.
- Nút X chỉ **ẩn** palette. ViewModel vẫn sống và vẫn nhận message.
- Palette là **application context**. Nút bấm trên palette ghi bản vẽ qua `ICadContext` (tự lock, tự gom undo), **không** prompt. Cần pick điểm thì làm theo [§7.2](#72-nút-palette-chạy-một-lệnh-để-prompt).

**Một palette, nhiều bản vẽ.** Palette chỉ có một, bản vẽ thì nhiều. Mỗi lần bấm nút, `ICadContext` làm việc với bản vẽ **đang active** *tại lúc bấm*. Nhưng thứ palette đang *hiển thị* (ví dụ "Số ô trong bản vẽ: 12") có thể là của bản vẽ trước, nếu không có gì làm mới nó. Code công khai giải quyết việc này bằng sự kiện `DocumentCollection.DocumentActivated` (398 file). `IEventService` của dự án chưa có sự kiện này, và đó là [bài tập U02-E2](#10-bài-tập).

---

## 4. Hộp thoại modal

Mở hộp thoại modal **bằng API của AutoCAD**, không gọi `Window.ShowDialog()` trực tiếp. Dự án gói API đó trong [`IWindowManagerService`](../../Core/IWindowManagerService.cs) (nguyên văn, [`WindowManagerService.cs`](../../Core/WindowManagerService.cs)):

```csharp
public bool? ShowModalDialog(Window window)
{
    if (window == null) return null;
    return Application.ShowModalWindow(window);
}
```

`Application` ở đây là `Autodesk.AutoCAD.ApplicationServices.Core.Application`. `ShowModalWindow` có các overload nhận cửa sổ cha (`owner`) và cờ `persistSizeAndPosition` (đã kiểm trong metadata của AutoCAD 2024). Code công khai dùng `ShowModalWindow` (WPF) ở 189 file và `ShowModalDialog` (WinForms) ở 278 file. [`WpfCadHelper`](../../Utils/WpfCadHelper.cs) của dự án có thêm một cách khác: tự gán owner rồi gọi `ShowDialog()`. Cách đó không dùng API dành riêng cho việc này của AutoCAD, nên code mới hãy dùng `IWindowManagerService`.

**Hộp thoại cần pick điểm trên bản vẽ.** Hộp thoại modal đang mở thì người dùng không click được vào bản vẽ. Mẫu đơn giản nhất là **đóng → hỏi → mở lại**: nút "Chọn điểm" đóng hộp thoại kèm một cờ; lệnh thấy cờ thì prompt, rồi mở lại một hộp thoại **mới** với dữ liệu cũ. Phải tạo cửa sổ mới vì một `Window` đã đóng thì không `Show` lại được.

```csharp
[CommandMethod("PANEL_SETUP")]
public void PanelSetup()
{
    ExecuteCommand(() =>
    {
        var vm = new PanelSetupViewModel();                  // dữ liệu sống qua các lần mở lại
        var windows = GetRequiredService<IWindowManagerService>();
        while (true)
        {
            bool? ok = windows.ShowModalDialog(new PanelSetupWindow { DataContext = vm });
            if (!vm.PickRequested) { if (ok != true) return; break; }   // OK → vẽ; Hủy → thoát

            vm.PickRequested = false;                         // nút "Chọn điểm" đặt cờ rồi Close()
            Point3d? p = CadPromptHelper.GetPoint("Chọn điểm chèn:");
            if (p != null) vm.SetInsertionPoint(p.Value.X, p.Value.Y);   // ESC: giữ điểm cũ, mở lại
        }
        // … vẽ bằng Service với dữ liệu trong vm (ngoài vòng lặp, ngoài hộp thoại)
    });
}
```

Lệnh này chạy trong **command context**: prompt hợp lệ, AutoCAD tự lock khi vẽ. Hàm `Editor.StartUserInteraction(window)` (27 file công khai) là cách thứ hai. Nó tạm ẩn cửa sổ trong lúc prompt (trả về `EditorUserInteraction`, gọi `End()`/`Dispose()` khi xong), nhưng hiếm gặp hơn nhiều. Hành vi đóng/mở lại của mẫu trên chỉ kiểm được trong AutoCAD có giao diện: xem [trạng thái kiểm chứng](#trạng-thái-kiểm-chứng) cuối module.

---

## 5. Cửa sổ modeless

`Application.ShowModelessWindow(window)` (86 file công khai) mở một cửa sổ **không chặn** AutoCAD. Mọi quy tắc của palette áp dụng y hệt: application context, ghi qua `ICadContext`, không prompt. Một cửa sổ modeless nổi thường kém tiện hơn palette: palette neo được, nhớ vị trí, ẩn tự động. Nên dùng palette, trừ khi bạn thật sự cần một cửa sổ riêng.

---

## 6. Ribbon

Nút Ribbon **không** gọi code trực tiếp. Nó gửi tên lệnh vào hàng đợi, để lệnh chạy đúng command context: `SendStringToExecute("\x03\x03_.TÊN_LỆNH ", …)` ([T01 §5.3](T01_AUTOCAD_NET_FOUNDATIONS.md#53-đi-từ-application-context-sang-command-context)). Thêm nút là thêm **một dòng** vào [`CadCoreRibbon.Build`](../../Commands/CadCoreRibbon.cs). Vì sao hàm build bắt đầu bằng `RemoveTab`, và các bẫy về thời điểm dựng Ribbon: [T08 §6](T08_RECIPES_EXTENDING.md#6-thêm-ribbon-tab), lab L11.

---

## 7. Từ UI tới bản vẽ và ngược lại

Bốn công thức chiếm phần lớn việc "UI chạm vào bản vẽ":

### 7.1 Nút palette vẽ vào bản vẽ

ViewModel → Service → `ICadContext.TryExecute(…, undoMarkName)`. Đây là đúng đường đi của nút "▶ Vẽ lưới" ([T01 §5.1](T01_AUTOCAD_NET_FOUNDATIONS.md#51-hai-ngữ-cảnh), sơ đồ tuần tự). `undoMarkName` làm cả lưới mất trong **một** lần Ctrl+Z: từ palette, AutoCAD không tự gom như khi chạy lệnh.

### 7.2 Nút palette chạy một lệnh để prompt

Nút gửi lệnh bằng `SendStringToExecute`, đặt sau một interface để ViewModel vẫn test được. Đây là công thức `ICommandLauncher` ở [T08 §5](T08_RECIPES_EXTENDING.md#5-palette-cần-người-dùng-chọn-điểm-trên-màn-hình). Cần chờ kết quả của chuỗi thao tác đó thì dùng `ExecuteInCommandContextAsync` (56 file công khai, cùng mục T08).

### 7.3 Chọn sẵn và zoom tới entity

Mẫu "bảng trên palette → bấm một dòng → chọn đối tượng đó trên bản vẽ" rất hay gặp: `SetImpliedSelection` có ở 381 file, `SetCurrentView` ở 271 file. Hai thao tác này dùng `Editor`, nên trong dự án chúng đi qua `scope.Editor` bên trong `ICadContext`: Service không đụng `MdiActiveDocument` (CAD001).

```csharp
// Trong một Service: ids là ObjectId của các entity cần chọn, extents là khung bao của chúng.
_cadContext.ExecuteRead(scope =>
{
    scope.Editor.SetImpliedSelection(ids);    // entity được chọn sẵn, hiện grip

    // Zoom cho thấy trọn khung bao. Chỉ đúng cho góc nhìn bằng (PLAN, không xoay) — mặc định của bản vẽ 2D.
    using (ViewTableRecord view = scope.Editor.GetCurrentView())
    {
        view.CenterPoint = new Point2d((extents.MinPoint.X + extents.MaxPoint.X) / 2,
                                       (extents.MinPoint.Y + extents.MaxPoint.Y) / 2);
        view.Width = (extents.MaxPoint.X - extents.MinPoint.X) * 1.1;   // chừa 10% lề
        view.Height = (extents.MaxPoint.Y - extents.MinPoint.Y) * 1.1;
        scope.Editor.SetCurrentView(view);
    }
});
```

Đã kiểm trên AutoCAD 2024 (accoreconsole): sau `SetImpliedSelection` với 2 Line, `SelectImplied()` trả về đúng 2 đối tượng. Xin khung nhìn 110 × 55 thì AutoCAD giữ rộng 110 và **tự nới** cao thành 62,3 cho đúng tỉ lệ khung nhìn. Vì vậy chỉ cần đặt khung bao, phần còn lại AutoCAD lo. `GetCurrentView()` trả về một `ViewTableRecord` **không nằm trong database**, nên phải `using` ([T03 B11](T03_PITFALLS_CASEBOOK.md#b11-rò-rỉ-đối-tượng-chưa-vào-database)). Gọi từ nút palette (application context): xem [trạng thái kiểm chứng](#trạng-thái-kiểm-chứng).

### 7.4 Bản vẽ đổi thì palette cập nhật

- Lệnh vẽ thêm → **message** qua `IEventAggregator` (`GridDrawnMessage`): palette nghe và đếm lại ([T02 §7](T02_ARCHITECTURE_DEEP_DIVE.md#7-eventaggregator-và-eventservice)).
- Người dùng xóa entity → `ObjectErased`, xử lý ở `Application.Idle` ([T08 §11](T08_RECIPES_EXTENDING.md#11-lắng-nghe-sự-kiện-bản-vẽ-đúng-cách), lab L09).
- Người dùng chuyển bản vẽ → `DocumentActivated` (bài tập U02-E2).

---

## 8. Phản hồi cho người dùng

| Tình huống | Dùng | Ghi chú |
| :--- | :--- | :--- |
| Kết quả, cảnh báo của lệnh | `INotificationService.Info/Warn/Error` | Ghi dòng lệnh **và** log |
| Thao tác lâu trên palette | `IsBusy` + lớp phủ ([U01 §4](U01_WPF_MVVM_BASICS.md#4-binding-nối-view-với-viewmodel)) | Chỉ hiện được nếu phần nặng là `async` ([Phần 5 §5.2](../05_WPF_UI_AND_MVVM.md#52-giải-phẫu-mvvm--bí-ẩn-runsafeasync)) |
| Vòng lặp lâu trong lệnh (hàng nghìn entity) | `IProgressMeterService` (thanh tiến trình ở thanh trạng thái) | 91 file công khai dùng `ProgressMeter` |
| Xác nhận trước khi xóa hoặc ghi đè | `INotificationService.Confirm` (hộp Yes/No) | Đừng dùng hộp thoại cho thông báo thường: nó chặn người dùng |
| Lỗi validate trên palette | Dòng chữ đỏ + nút tắt (`CanExecute`) | Báo **trước** khi bấm, không phải sau |

---

## 9. Bẫy UI trong AutoCAD

| Triệu chứng | Gốc rễ | Sửa | Xem |
| :--- | :--- | :--- | :--- |
| `eLockViolation` khi bấm nút palette | Ghi bản vẽ từ application context mà không lock | Mọi ghi đi qua `ICadContext` | [T03 B1](T03_PITFALLS_CASEBOOK.md#b1-elockviolation-khi-bấm-nút-trên-palette) |
| Prompt từ palette chạy lung tung | Prompt ở application context | Nhập trên form, hoặc nút gửi lệnh | [§7.2](#72-nút-palette-chạy-một-lệnh-để-prompt) |
| Palette không mở, lỗi `StaticResourceExtension` | `StaticResource` tới key nạp sau `InitializeComponent` | `DynamicResource` | [T03 A3](T03_PITFALLS_CASEBOOK.md#a3-show_ui_template-lỗi-provide-value-on-staticresourceextension) |
| Gõ `2,5` thành 25 trên palette | WPF dùng `en-US` mặc định | Ô số bind vào chuỗi, parse InvariantCulture | [U01 §5](U01_WPF_MVVM_BASICS.md#5-ba-bẫy-khi-nhập-số), B-31 |
| Danh sách từ khóa in hai lần | Tự nối `[…]` trong khi AutoCAD cũng nối | Để `AppendKeywordsToMessage` làm việc đó | [§2.3](#23-từ-khóa), B-33 |
| Thông báo hoặc vẽ "biến mất" sau khi người dùng đổi bản vẽ | Gọi `Editor` của bản vẽ **không** active | Báo kết quả lên bản vẽ đang active | [T03 B31](T03_PITFALLS_CASEBOOK.md#b31-gọi-editor-của-bản-vẽ-không-active) |
| Palette mất vị trí sau mỗi lần mở AutoCAD | `PaletteSet` không có Guid cố định | `PaletteService` tự dẫn xuất Guid; đừng đổi tên palette | [T03 B27](T03_PITFALLS_CASEBOOK.md#b27-palette-không-nhớ-vị-trí) |
| Hiển thị số liệu của bản vẽ trước | Palette không biết người dùng đã chuyển bản vẽ | Nghe `DocumentActivated` | Bài tập U02-E2 |

---

## 10. Bài tập

**U02-E1 — Sửa B-33.** Sửa `CadPromptHelper.GetKeywords` để danh sách từ khóa chỉ in một lần ([§2.3](#23-từ-khóa)).
- **DoD:** chạy `DEMO_PROMPT` tới bước từ khóa, chụp dòng lệnh trước/sau; Enter vẫn chọn từ khóa mặc định; gõ chữ đầu vẫn chọn đúng từ khóa.

**U02-E2 — Palette theo bản vẽ đang mở.** Thêm sự kiện `DocumentActivated` vào [`IEventService`](../../Core/IEventService.cs) và [`EventService`](../../Core/EventService.cs) (gỡ đăng ký trong `Shutdown`, như các sự kiện sẵn có). Cho palette Grid Marker tự đếm lại số ô khi người dùng chuyển bản vẽ. Mẫu tham khảo: cách `GridMarkerViewModel` nghe `GridDrawnMessage` và tự gỡ khi `Dispose` (`TrackSubscription`).
- **DoD:** mở 2 bản vẽ có số ô khác nhau; chuyển qua lại thì số đếm đổi theo; mở/đóng palette 5 lần, `CORE_DEBUG` không cho thấy subscriber tăng dần; có unit test cho ViewModel (giả lập sự kiện bằng Mock).

**U02-E3 — Hộp thoại có nút "Chọn điểm".** Làm lệnh `PANEL_SETUP` theo mẫu [§4](#4-hộp-thoại-modal): hộp thoại có Tên tủ, Rộng (ô số an toàn theo [U01 §5.3](U01_WPF_MVVM_BASICS.md#53-cách-sửa-2080-ô-số-bind-vào-chuỗi)), điểm chèn (hiển thị X, Y), các nút "Chọn điểm", OK, Hủy. OK thì vẽ một hình chữ nhật tại điểm chèn qua Service.
- **DoD:** chọn điểm 2 lần liên tiếp mà dữ liệu đã nhập vẫn giữ nguyên; ESC lúc chọn điểm thì quay lại hộp thoại với điểm cũ; Hủy thì không vẽ gì; một Ctrl+Z xóa hình vừa vẽ.

---

## ✅ Tự kiểm tra

1. Một tool cần 12 thông số, người dùng chỉnh qua lại nhiều lần rồi mới vẽ. Bạn chọn bề mặt UI nào? Còn nếu chỉ cần 2 điểm?
2. `GetPoint` trả về hệ tọa độ nào? `CadPromptHelper.GetPoint` thì sao?
3. Vì sao hộp thoại cần pick điểm thì phải *đóng rồi mở lại*, và vì sao lại là một `Window` **mới**?
4. Nút "Chọn" trên palette gọi `SetImpliedSelection`. Vì sao nên gọi qua `scope.Editor` trong `ICadContext` chứ không qua `MdiActiveDocument.Editor` ngay trong ViewModel?
5. Palette ghi "Số ô trong bản vẽ: 12". Người dùng chuyển sang bản vẽ khác. Con số đó còn đúng không?

<details><summary>Đáp án</summary>

1. 12 thông số, chỉnh nhiều lần: **palette** (giữ giá trị, xem kết quả liên tục). 2 điểm: **lệnh + prompt** ([§1](#1-chọn-bề-mặt-ui)).
2. `Editor.GetPoint` trả **UCS**: gõ `10,0` khi UCS xoay 90° thì nhận (10; 0), trong khi WCS là (0; 10). `CadPromptHelper.GetPoint` đổi sẵn sang **WCS** ([§2.4](#24-hệ-tọa-độ-getpoint-trả-về-ucs)).
3. Hộp thoại modal chặn tương tác với bản vẽ. Một `Window` đã đóng thì không `Show` lại được, nên tạo cửa sổ mới, còn dữ liệu nằm trong ViewModel dùng lại ([§4](#4-hộp-thoại-modal)).
4. ViewModel không được biết AutoCAD (test được, analyzer CAD001/CAD002). `ICadContext` bảo đảm đúng bản vẽ, đúng thread và lock khi cần. Service chỉ nhận `ObjectId` ([§7.3](#73-chọn-sẵn-và-zoom-tới-entity)).
5. Không chắc: palette không tự biết bản vẽ đã đổi. Phải nghe `DocumentActivated` (bài tập U02-E2), hoặc bấm "Đếm lại" ([§3](#3-palette)).
</details>

---

### Trạng thái kiểm chứng

- **Đã chạy trên AutoCAD 2024 (accoreconsole, input từ script):** giá trị mặc định khi Enter (§2.2); từ khóa, kể cả lỗi in hai lần và cách sửa (§2.3); `GetPoint` trả UCS so với `AcquirePoint` trả WCS (§2.4); dòng lệnh từ chối `1,5` (§2.5); `SetImpliedSelection`/`SelectImplied` và `SetCurrentView` tự nới tỉ lệ (§7.3), khi gọi bên trong một lệnh.
- **Chỉ kiểm được trong AutoCAD có giao diện:** vòng "đóng → hỏi → mở lại" của hộp thoại (§4); `SetImpliedSelection` và zoom khi gọi từ nút **palette** (§7.3). Mentor dùng chúng làm bước smoke khi chấm U02-E3.

---

**Tiếp theo:** [U03 — Vẽ thực thể lên UI →](U03_DRAWING_ON_UI.md) · **Quay về:** [U01](U01_WPF_MVVM_BASICS.md) · [Lộ trình](README.md)
