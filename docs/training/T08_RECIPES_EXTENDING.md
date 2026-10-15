> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← T07](T07_PERFORMANCE_AND_SCALE.md) · **T08** · [T09 →](T09_SENIOR_PLAYBOOK.md)

# T08 — Công Thức Mở Rộng Dự Án

> **Cấp độ:** 🌱 Fresher → 🌳 Middle · **Cần trước:** [T02](T02_ARCHITECTURE_DEEP_DIVE.md), [R05 — Golden Sample](../reference/R05_GOLDEN_SAMPLE.md) · **Trong lộ trình:** tuần 6 (§1–3) và 8 (§4–6) ([README §3](README.md#3-lộ-trình-đào-tạo))

Mỗi công thức gồm **checklist các file phải chạm** và **code mẫu dùng đúng API hiện có**. Phần nào là *đề xuất bổ sung* (chưa có trong v1.0.0) được ghi rõ. Mọi công thức đều tuân thủ quy tắc của [Golden Sample](../reference/R05_GOLDEN_SAMPLE.md): khi phân vân, **copy cách Golden Sample làm**. Code của §1, §3–5, §7, §9–12 đã được biên dịch thử trong một bản sao của dự án (build `-warnaserror` sạch); phần lõi của §12 còn được chạy trên AutoCAD 2024 thật; phần T08 để bạn tự viết (`PanelSpec`, `PanelViewModel`) thì làm theo [Golden Sample](../reference/R05_GOLDEN_SAMPLE.md).

---

## Mục lục

1. [Thêm một lệnh](#1-thêm-một-lệnh)
2. [Thêm một tính năng trọn vẹn](#2-thêm-một-tính-năng-trọn-vẹn)
3. [Thêm một service](#3-thêm-một-service)
4. [Thêm một palette](#4-thêm-một-palette)
5. [Palette cần người dùng chọn điểm trên màn hình](#5-palette-cần-người-dùng-chọn-điểm-trên-màn-hình)
6. [Thêm Ribbon tab](#6-thêm-ribbon-tab)
7. [Thêm Overrule](#7-thêm-overrule)
8. [Lưu metadata: XData hay XRecord?](#8-lưu-metadata-xdata-hay-xrecord)
9. [Thêm thiết lập người dùng](#9-thêm-thiết-lập-người-dùng)
10. [Làm việc với dynamic block](#10-làm-việc-với-dynamic-block)
11. [Lắng nghe sự kiện bản vẽ đúng cách](#11-lắng-nghe-sự-kiện-bản-vẽ-đúng-cách)
12. [Công thức thao tác bản vẽ](#12-công-thức-thao-tác-bản-vẽ): khung tên, xuất PDF, purge + audit, xref, gộp layer
13. [Checklist trước khi mở PR](#13-checklist-trước-khi-mở-pr)

---

## 1. Thêm một lệnh

**Checklist:**
- [ ] Tạo class kế thừa `CadCommandBase`: `dotnet new cad-command` hoặc snippet `cadcmd` ([Templates/README.md](../../Templates/README.md))
- [ ] Tên lệnh có **tiền tố riêng** (ví dụ `PANEL_`), viết hoa, không trùng lệnh AutoCAD
- [ ] Thêm `[assembly: CommandClass(typeof(…))]` vào [`Properties/AssemblyInfo.cs`](../../Properties/AssemblyInfo.cs). Thiếu dòng này thì `Unknown command`
- [ ] Thân lệnh bọc trong `ExecuteCommand(() => { … })`; service lấy bằng `GetRequiredService<T>()` **bên trong** đó
- [ ] Prompt **trước** khi gọi `ICadContext` (`CadPromptHelper`); `null` thì `return`

```csharp
public class PanelCommands : CadCommandBase
{
    [CommandMethod("PANEL_DRAW")]
    public void DrawPanel()
    {
        ExecuteCommand(() =>
        {
            var service = GetRequiredService<IPanelService>();
            var notify = GetRequiredService<INotificationService>();

            Point3d? origin = CadPromptHelper.GetPoint("Chọn điểm đặt tủ:");   // WCS
            if (origin == null) return;                                          // ESC
            string? name = CadPromptHelper.GetString("Tên tủ", defaultVal: "MSB-01");
            if (name == null) return;

            var spec = new PanelSpec(name, width: 600, height: 2000);
            var errors = spec.Validate();
            if (errors.Count > 0) { foreach (var e in errors) notify.Warn(e); return; }

            CadResult<ObjectId> result = service.Draw(spec, origin.Value.X, origin.Value.Y);
            if (!result.IsSuccess)
                throw new InvalidOperationException($"Không vẽ được tủ: {result.ErrorMessage}", result.Exception);

            notify.Info($"Đã vẽ tủ '{name}'.");
        });
    }
}
```

---

## 2. Thêm một tính năng trọn vẹn

Copy `Samples/GridMarker/` thành `Features/<Tên>/` (hoặc thư mục bạn chọn), đổi tên class và namespace, rồi theo **thứ tự** sau:

| # | File | Quy tắc | Test |
| :---: | :--- | :--- | :--- |
| 1 | `XxxSpec` | Bất biến, **không kiểu AutoCAD**; `Validate()` trả **danh sách** lỗi; có hàng rào kích thước (`MaxCells`) | Mọi luật, biên |
| 2 | `XxxPlanner` | Hàm thuần: Spec → danh sách kết quả (`double`/`string`) | **Kết quả** |
| 3 | `IXxxService` | Chỉ kiểu plugin (Spec, kết quả, `CadResult`); **không prompt** | — |
| 4 | `XxxService` | `TryExecute(…, undoMarkName)`; vòng lặp mỏng "vẽ những gì Planner tính"; publish message **sau** khi `TryExecute` trả về, bọc try/catch | `FakeCadContext` (hợp đồng) |
| 5 | `XxxCommands` | Prompt → validate → plan → service → báo kết quả | Thủ công |
| 6 | `XxxViewModel` | Không WPF/AutoCAD; `CanExecute` gắn validate; `TrackSubscription` | Mock service |
| 7 | `XxxPalette.xaml(.cs)` | Chỉ binding; `DynamicResource`; code-behind lấy ViewModel từ DI (fail-loud) | `XamlPaletteTests` |
| 8 | Đăng ký | `ConfigureServices`: service `AddSingleton`, ViewModel `AddTransient`; `CommandClass` | — |
| 9 | Tài liệu | Mục `[Unreleased]` trong CHANGELOG; cập nhật T-module nếu đổi quy ước | — |

---

## 3. Thêm một service

```csharp
public interface IPanelService
{
    CadResult<ObjectId> Draw(PanelSpec spec, double x, double y);
}

public sealed class PanelService : IPanelService
{
    public const string UndoMarkName = "Vẽ tủ điện";
    public const string DataKey = "PANEL_DATA";          // key XRecord — Overrule (§7) lọc theo key này
    private readonly ICadContext _cad;

    public PanelService(ICadContext cad) => _cad = cad ?? throw new ArgumentNullException(nameof(cad));

    public CadResult<ObjectId> Draw(PanelSpec spec, double x, double y)
        => _cad.TryExecute(scope =>
        {
            scope.EnsureLayer("E-PANEL", colorIndex: 3);
            ObjectId frame = scope.DrawRectangle(new Point3d(x, y, 0),
                new Point3d(x + spec.Width, y + spec.Height, 0), layer: "E-PANEL");
            scope.DrawMText(spec.Name, new Point3d(x + spec.Width / 2, y + spec.Height + 50, 0),
                textHeight: 50, layer: "E-PANEL");
            scope.SetXRecord(frame, DataKey, new { spec.Name, spec.Width, spec.Height });
            return frame;
        }, undoMarkName: UndoMarkName);
}
```

Đăng ký trong `AppBootstrapper.ConfigureServices`: `services.AddSingleton<IPanelService, PanelService>();`

Quy tắc: không đọc `MdiActiveDocument` (CAD001), không prompt, không giữ `DBObject` trong field, luôn truyền `undoMarkName` (service có thể được gọi từ palette).

---

## 4. Thêm một palette

**XAML** (chỉ binding, mọi key style dùng `DynamicResource`):

```xml
<UserControl x:Class="CadCoreFramework.Features.Panel.PanelPalette"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="{DynamicResource BackgroundBrush}">
    <StackPanel Margin="12">
        <TextBlock Text="Tên tủ" Style="{DynamicResource LabelStyle}"/>
        <TextBox Text="{Binding PanelName, UpdateSourceTrigger=PropertyChanged}" Style="{DynamicResource InputStyle}"/>
        <TextBlock Text="{Binding ValidationSummary}" Foreground="{DynamicResource DangerBrush}"/>
        <Button Content="Vẽ" Command="{Binding DrawCommand}" Style="{DynamicResource PrimaryButton}"/>
    </StackPanel>
</UserControl>
```

**Code-behind**: copy mẫu [`GridMarkerPalette.xaml.cs`](../../Samples/GridMarker/GridMarkerPalette.xaml.cs) — ngay sau `InitializeComponent()` gọi `ThemeManager.Attach(this)` (nạp `Styles.xaml` + theme hiện hành bằng pack URI dựng lúc runtime, tự áp lại khi người dùng đổi `COLORTHEME`), rồi gán `DataContext` cho **chính UserControl** từ DI và ném lỗi nếu thiếu. Không viết mã màu trong XAML: dùng key trong `Themes/AutoCadTheme.xaml` (key mới thì thêm vào **cả hai** file theme — test sẽ bắt nếu quên).

**Lệnh mở palette:** `PaletteService` tự gán Guid ổn định (dẫn xuất từ `[assembly: Guid]` + tên palette), nên AutoCAD nhớ vị trí/kích thước qua các lần khởi động mà bạn không phải làm gì ([B-04](T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng)). **Đừng đổi tên palette** sau khi phát hành (đổi tên = Guid mới = mất vị trí đã lưu); nếu buộc phải đổi, truyền Guid cũ qua `GetOrCreatePaletteSet(name, guid)` trước lần Toggle đầu.

```csharp
[CommandMethod("PANEL_PALETTE")]
public void ShowPanelPalette()
{
    ExecuteCommand(() => GetRequiredService<IPaletteService>()
        .TogglePalette("PanelPalette", () => new PanelPalette(), tabTitle: "Tủ điện"));
}
```

Thêm palette vào mảng `Palettes` trong `XamlPaletteTests` (test resource key ở cả hai theme sẽ tự chạy cho nó), đăng ký ViewModel vào container giả, và một dòng `AddTransient<PanelViewModel>()` trong `ConfigureServices`. Lệnh mới nhớ thêm vào `Deployment/PackageContents.xml` (test `PackageManifestTests` kiểm).

Ô nhập **số** trên palette: bind vào chuỗi và tự parse bằng `NumberText`, đừng bind thẳng vào `double` (gõ `2,5` sẽ thành 25 — [U01 §5](U01_WPF_MVVM_BASICS.md#5-ba-bẫy-khi-nhập-số)). Mẫu để chép là `RowsText`/`SpacingText` trong `GridMarkerViewModel` ([R05 §3](../reference/R05_GOLDEN_SAMPLE.md#3-nguyên-tắc-2--validate-một-lần-ở-model)); test `EveryPaletteTextBox_BindsToAStringProperty` sẽ đỏ nếu palette mới của bạn bind ô vào số. Palette cần **khung xem trước** hình sẽ vẽ: [U03](U03_DRAWING_ON_UI.md).

---

## 5. Palette cần người dùng chọn điểm trên màn hình

Palette chạy ở **application context**: không prompt từ ViewModel ([T01 §5](T01_AUTOCAD_NET_FOUNDATIONS.md#5-document-lock-và-hai-ngữ-cảnh-thực-thi)). Ba lựa chọn, theo thứ tự ưu tiên:

1. **Nhập trên form** (như Golden Sample: ô `OriginX/OriginY`). Đơn giản, test được.
2. **Nút bấm kích hoạt một lệnh** có sẵn để lệnh đó prompt. Để ViewModel vẫn test được, đặt việc gửi lệnh sau một interface:

   ```csharp
   using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;   // tránh nhầm System.Windows.Application

   public interface ICommandLauncher { void Launch(string globalCommandName); }

   // Core/ — hạ tầng được phép chạm MdiActiveDocument (CAD001 chỉ áp cho Services/ViewModels)
   public sealed class AutoCadCommandLauncher : ICommandLauncher
   {
       public void Launch(string globalCommandName)
           => Application.DocumentManager.MdiActiveDocument?
                  .SendStringToExecute($"\x03\x03_.{globalCommandName} ", true, false, false);
   }
   ```
   ViewModel nhận `ICommandLauncher` qua constructor và test bằng Mock.
3. **`ExecuteInCommandContextAsync`**, khi cần chạy chuỗi thao tác *như một lệnh* rồi xử lý tiếp kết quả: `await Application.DocumentManager.ExecuteInCommandContextAsync(async _ => { … }, null);` (chữ ký đã kiểm chứng: `Func<object, Task>` + `object`, trả về `ExecutionResult` có `await` được).

Với **hộp thoại modal** (không phải palette) cần chọn điểm, mẫu đơn giản nhất là "đóng → hỏi → mở lại" ngay trong lệnh: [U02 §4](U02_UI_IN_AUTOCAD.md#4-hộp-thoại-modal).

---

## 6. Thêm Ribbon tab

Dự án có sẵn tab **"CadCore"**: [`Commands/CadCoreRibbon.cs`](../../Commands/CadCoreRibbon.cs). Thêm nút = thêm một dòng `AddButton` vào `CadCoreRibbon.Build`. Phần dưới giải thích *vì sao* nó được viết như vậy — hai cái bẫy về **thời điểm** và **trùng lặp** ([B-05](T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng), sửa ở PR #6):

| Tình huống | `RibbonService` làm gì |
| :--- | :--- |
| Plugin nạp lúc AutoCAD **đang khởi động** (Autoloader) | Ribbon chưa có → hook `ItemInitialized` → gọi build action khi Ribbon sẵn sàng |
| NETLOAD khi AutoCAD **đã mở xong** | Ribbon đã có → gọi build action **ngay** (trong `InitializeRibbon` hoặc `RegisterBuildAction`, cái nào đến sau) |
| Người dùng chạy `CUI` / `MENULOAD` / `WSCURRENT` … | `CommandEnded` → `Application.Idle` → gọi lại build action |

Trên v1.0.0, trường hợp NETLOAD muộn **không ai** gọi build, và `AddButton` không idempotent (dựng lại khi tab còn sẽ nhân đôi nút). Từ PR #6: thứ tự `RegisterBuildAction`/`InitializeRibbon` không còn quan trọng; `AddButton` bỏ qua nút trùng lệnh; và có `RemoveTab` để build luôn dựng lại từ đầu. Rút gọn từ [`Commands/CadCoreRibbon.cs`](../../Commands/CadCoreRibbon.cs): code nguyên văn, `…` là phần lược bớt, chú thích cuối dòng là của tài liệu:

```csharp
public static class CadCoreRibbon
{
    public const string TabId = "CADCORE_TAB";

    private const string ToolsPanel = "CADCORE_PANEL_TOOLS";

    public static void Build(IRibbonService ribbon)
    {
        ribbon.RemoveTab(TabId);                                   // idempotent: luôn dựng lại từ đầu
        ribbon.AddTab(TabId, "CadCore", TabId);

        ribbon.AddPanel(TabId, ToolsPanel, "Công cụ");
        ribbon.AddButton(TabId, ToolsPanel, "Grid\nMarker", "SAMPLE_GRID",
            tooltip: "SAMPLE_GRID — Vẽ lưới ô đánh nhãn tại điểm chọn");
        // …
    }
}
```

Nối vào khởi động (đã có trong `AppBootstrapper.InitializeCore`):

```csharp
var ribbonService = ServiceProvider.GetService<IRibbonService>();
if (ribbonService != null)
{
    ribbonService.RegisterBuildAction(() => Commands.CadCoreRibbon.Build(ribbonService));
    ribbonService.InitializeRibbon();   // Ribbon đã sẵn → build ngay; chưa → build khi ItemInitialized
}
```

Kiểm tra (chỉ làm được trong GUI): NETLOAD khi AutoCAD đã mở → tab xuất hiện; chạy `CUI` rồi đóng → tab vẫn còn, **không** nhân đôi nút; cài bằng bundle (nạp lúc khởi động) → tab xuất hiện; nút bấm chạy đúng lệnh.

---

## 7. Thêm Overrule

🔎 [`Demos/DemoOverruleCommands.cs`](../../Demos/DemoOverruleCommands.cs) (`DemoCircleCenterOverrule`), [`Core/OverruleManager.cs`](../../Core/OverruleManager.cs)

```csharp
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;              // DrawableOverrule, Drawable, WorldDraw
using Autodesk.AutoCAD.Runtime;                        // Overrule, RXObject
// ⚠️ GraphicsInterface CŨNG có kiểu tên Polyline → CS0104 "ambiguous reference". Dùng alias:
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

public sealed class PanelTagOverrule : DrawableOverrule, ICadOverrule
{
    public void Register()
    {
        SetExtensionDictionaryEntryFilter(PanelService.DataKey);           // chỉ áp cho entity có XRecord của ta
        Overrule.AddOverrule(RXObject.GetClass(typeof(Polyline)), this, false);
    }
    public void Unregister() => Overrule.RemoveOverrule(RXObject.GetClass(typeof(Polyline)), this);

    public override bool WorldDraw(Drawable drawable, WorldDraw wd)
    {
        bool result = base.WorldDraw(drawable, wd);    // vẽ entity gốc
        // vẽ thêm (KHÔNG mở transaction, KHÔNG sửa database ở đây)
        return result;
    }
}
// Bật: GetRequiredService<OverruleManager>().AddAndRegister(new PanelTagOverrule());
```

Quy tắc:
- **Luôn qua `OverruleManager`** (Singleton). `Terminate` gỡ mọi Overrule. Overrule còn treo khi AutoCAD thoát có thể gây crash ([T03 B25](T03_PITFALLS_CASEBOOK.md#b25-autocad-crash-lúc-thoát-vì-overrule)).
- **Giới hạn phạm vi** bằng filter của `Overrule` (đã kiểm chứng trong AutoCAD 2024): `SetXDataFilter(app)`, `SetExtensionDictionaryEntryFilter(key)`, `SetIdFilter(ObjectId[])`, `SetCustomFilter()`. Chọn filter theo **chỗ lưu dữ liệu**: `PanelService` ở §3 lưu bằng `SetXRecord`, tức một Xrecord tên `PANEL_DATA` trong Extension Dictionary của khung tủ, nên ví dụ trên lọc bằng `SetExtensionDictionaryEntryFilter`; dữ liệu lưu bằng XData thì dùng `SetXDataFilter(app)` ([§8](#8-lưu-metadata-xdata-hay-xrecord)). Không có filter thì `WorldDraw` chạy cho *mọi* entity cùng lớp, *rất nhiều lần*.
- Trong Overrule: không mở transaction thường, không sửa database, không ném exception. Cần đọc dữ liệu thì dùng `StartOpenCloseTransaction` ([T01 §4.5](T01_AUTOCAD_NET_FOUNDATIONS.md#45-startopenclosetransaction)).

---

## 8. Lưu metadata: XData hay XRecord?

| Câu hỏi | XData (`entity.SetMetadata`, `XDataHelper`) | XRecord (`scope.SetXRecord`, `XRecordHelper`) |
| :--- | :--- | :--- |
| Dữ liệu nhỏ (< vài KB), vài trường | ✅ | ✅ |
| Dữ liệu lớn (> 15.000 ký tự JSON) | ❌ | ✅ |
| Lọc chọn đối tượng theo "có dữ liệu của app" (`SelectionFilter` mã 1001) | ✅ `WithXData(app)` | ❌ |
| Overrule lọc theo dữ liệu | ✅ `SetXDataFilter(app)` | ✅ `SetExtensionDictionaryEntryFilter(key)` |
| Dữ liệu cấp **bản vẽ** (không gắn entity) | ❌ | ✅ (Dictionary trong NOD) |

Quy ước: **tên app / key có tiền tố riêng** (`CADCORE_GRID_MARKER`); JSON có **trường version** (`{ "v": 1, … }`) để nâng cấp schema về sau; đọc dữ liệu bằng `defaultValue` khi JSON hỏng (helper bắt `JsonException`).

---

## 9. Thêm thiết lập người dùng

```csharp
var settings = GetRequiredService<ISettingsService>();
double spacing = settings.GetSetting("GridMarker.Spacing", 20.0);   // mặc định nếu chưa có
settings.SetSetting("GridMarker.Spacing", 25.0);
settings.Save();                                                      // ghi %AppData%\CadCoreFramework\settings.json
```

Key có tiền tố theo tính năng. Gọi `Save()` khi người dùng xác nhận thay đổi, không gọi mỗi lần gõ phím. Nhiều phiên AutoCAD cùng mở: `Save()` đọc lại tệp và **chỉ ghi đè các key phiên này đã `SetSetting`**, ghi atomic (tệp tạm rồi thay thế), nên key do phiên khác lưu không bị mất ([B-22](T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng), PR #6). Hai phiên cùng đổi **một** key thì phiên `Save()` sau thắng — đó là ngữ nghĩa mong muốn.

---

## 10. Làm việc với dynamic block

🔎 [`Utils/DynamicBlockHelper.cs`](../../Utils/DynamicBlockHelper.cs): `GetEffectiveName`, `GetDynamicProperties`, `GetDynamicProperty<T>`, `SetDynamicProperty`, `GetVisibilityStates`, `GetVisibilityState`, `SetVisibilityState`.

```csharp
_cad.Execute(scope =>
{
    ObjectId id = scope.InsertBlock("DOOR", new Point3d(0, 0, 0),
        attributes: new Dictionary<string, string> { ["TAG"] = "D-01" },
        dynamicProperties: new Dictionary<string, object> { ["Distance1"] = 900.0 });
    if (id.IsNull) return;                                        // block không có trong bản vẽ (đã cảnh báo)

    var br = (BlockReference)scope.Transaction.GetObject(id, OpenMode.ForWrite);
    DynamicBlockHelper.SetVisibilityState(br, "Open");
});
```

Nhận diện block: **luôn so effective name**, đừng so `br.Name` (tên ẩn danh `*U`, [T03 B12](T03_PITFALLS_CASEBOOK.md#b12-lọc-block-theo-tên-bỏ-sót-dynamic-block)).

---

## 11. Lắng nghe sự kiện bản vẽ đúng cách

```csharp
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;   // Application.Idle của AutoCAD

public sealed class ErasedWatcher : IDisposable
{
    private readonly IEventService _events;
    private readonly List<ObjectId> _pending = new List<ObjectId>();

    public ErasedWatcher(IEventService events)
    {
        _events = events;
        _events.ObjectErased += OnErased;                 // EventService hook DB lười, theo từng bản vẽ
    }

    private void OnErased(object? sender, DocumentObjectErasedEventArgs e)
    {
        if (!e.ObjectErasedEventArgs.Erased) return;      // bỏ qua un-erase (Undo)
        _pending.Add(e.ObjectErasedEventArgs.DBObject.ObjectId);   // CHỈ ghi nhận
        Application.Idle -= OnIdle;
        Application.Idle += OnIdle;                       // xử lý khi AutoCAD rảnh
    }

    private void OnIdle(object? sender, EventArgs e)
    {
        Application.Idle -= OnIdle;
        var ids = _pending.ToArray(); _pending.Clear();
        // An toàn: xử lý qua ICadContext / publish message cho ViewModel
    }

    public void Dispose()
    {
        _events.ObjectErased -= OnErased;                 // gỡ ĐÚNG delegate đã đăng ký
        Application.Idle -= OnIdle;
    }
}
```

---

## 12. Công thức thao tác bản vẽ

Năm việc hay gặp khi làm tool cho người vẽ: chèn khung tên, xuất PDF, dọn bản vẽ, gắn xref, gộp layer. Mỗi công thức có hai phần, theo đúng cách tách của [Golden Sample](../reference/R05_GOLDEN_SAMPLE.md):

- **Phần lõi:** một lớp `static` nhận `ICadScope` (hoặc `Database` + `Transaction`), chạy *bên trong* `ICadContext.Execute*`. Phần tính toán thuần (hình học, đường dẫn, kiểm tra tên) tách thành hàm riêng, không dùng kiểu AutoCAD.
- **Lệnh:** prompt trước, rồi gọi phần lõi trong một transaction có `undoMarkName`. Muốn gọi từ palette thì bọc phần lõi vào một service dùng `TryExecute` như [§3](#3-thêm-một-service). Prompt vẫn ở lệnh, ViewModel nhận dữ liệu qua binding.

Chép vào dự án: đặt các file vào `Features/DrawingTools/`, thêm `[assembly: CommandClass(typeof(CadCoreFramework.Features.DrawingTools.DrawingToolsCommands))]` vào `Properties/AssemblyInfo.cs`, thêm 5 lệnh `TOOLS_*` vào `Deployment/PackageContents.xml` ([§1](#1-thêm-một-lệnh)).

> [!NOTE]
> **Đã kiểm chứng thế nào.** Toàn bộ code dưới đây được chép vào một bản sao của dự án và build `-c Release -warnaserror` sạch trên cả `net48` lẫn `net8.0-windows`. Phần lõi được link vào [HeadlessLab](../../tests/integration/README.md#headlesslab--kiểm-thử-headless-trên-autocad-thật) và chạy trên AutoCAD 2024 thật: 19 kiểm thử, tất cả PASS. Các kiểm thử này dựng sẵn bản vẽ mẫu: block chưa dùng, layer đang khóa, file xref thật, thư mục dự án bị chuyển đi nơi khác. Những câu "đã kiểm" trong mục này đều là kết quả của các lần chạy đó. Ở lần chạy đầu có ba kiểm thử FAIL. Một lần cho thấy công thức xref sai, và công thức đã được sửa ([§12.4](#124-attach-xref-với-đường-dẫn-tương-đối)). Hai lần còn lại cho thấy hành vi của API mà kiểm thử phải tính tới ([§12.4](#124-attach-xref-với-đường-dẫn-tương-đối), [§12.5](#125-gộp-nhiều-layer-thành-một)). Riêng **lệnh** (prompt, hộp thoại chọn file, `SendStringToExecute`) cần AutoCAD có giao diện, nên chỉ được biên dịch, chưa chạy thử.

### 12.1 Khung tên A3/A2/A1 dạng block có attribute

Đừng vẽ khung tên bằng các nét rời. Hãy tạo **một block có attribute**:
- Người dùng sửa nội dung bằng `ATTEDIT` hoặc bảng Properties.
- Code đọc lại được: `BlockHelper.GetBlockAttributes`.
- Mọi lần chèn dùng chung một định nghĩa.

Kích thước khung tách thành `TitleBlockLayout`, chỉ chứa số, nên test được bằng xUnit thuần.

<details><summary><code>Features/DrawingTools/TitleBlock.cs</code></summary>

```csharp
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;

namespace CadCoreFramework.Features.DrawingTools
{
    /// <summary>Khổ giấy ISO 216, đặt ngang, đơn vị mm.</summary>
    public sealed class SheetSize
    {
        public static readonly SheetSize A3 = new SheetSize("A3", 420, 297);
        public static readonly SheetSize A2 = new SheetSize("A2", 594, 420);
        public static readonly SheetSize A1 = new SheetSize("A1", 841, 594);

        public SheetSize(string name, double width, double height)
        {
            Name = name;
            Width = width;
            Height = height;
        }

        public string Name { get; }
        public double Width { get; }
        public double Height { get; }
    }

    /// <summary>Một ô của bảng tên: nhãn cố định + attribute có tag.</summary>
    public sealed class TitleField
    {
        public TitleField(string tag, string label)
        {
            Tag = tag;
            Label = label;
        }

        public string Tag { get; }
        public string Label { get; }
    }

    /// <summary>
    /// Hình học khung tên: chỉ có số, không có kiểu AutoCAD → test được bằng xUnit thuần.
    /// Lề trái 20 mm (chừa chỗ đóng tập), các lề khác 10 mm; bảng tên ở góc dưới phải.
    /// </summary>
    public sealed class TitleBlockLayout
    {
        public const double LeftMargin = 20;
        public const double Margin = 10;
        public const double BoxWidth = 180;
        public const double RowHeight = 8;
        public const double LabelWidth = 40;
        public const double TextHeight = 3;

        public static readonly IReadOnlyList<TitleField> Fields = new[]
        {
            new TitleField("DU_AN", "Dự án"),
            new TitleField("BAN_VE", "Bản vẽ"),
            new TitleField("TY_LE", "Tỷ lệ"),
            new TitleField("NGAY", "Ngày"),
            new TitleField("SO_BV", "Số bản vẽ"),
        };

        private TitleBlockLayout(SheetSize sheet)
        {
            Sheet = sheet;
            FrameMinX = LeftMargin;
            FrameMinY = Margin;
            FrameMaxX = sheet.Width - Margin;
            FrameMaxY = sheet.Height - Margin;
            BoxMinX = FrameMaxX - BoxWidth;
            BoxMaxY = FrameMinY + Fields.Count * RowHeight;
        }

        public static TitleBlockLayout For(SheetSize sheet)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (sheet.Width < LeftMargin + Margin + BoxWidth)
                throw new ArgumentException($"Khổ {sheet.Name} quá nhỏ cho bảng tên rộng {BoxWidth} mm.", nameof(sheet));
            return new TitleBlockLayout(sheet);
        }

        public SheetSize Sheet { get; }
        public double FrameMinX { get; }
        public double FrameMinY { get; }
        public double FrameMaxX { get; }
        public double FrameMaxY { get; }
        public double BoxMinX { get; }
        public double BoxMaxY { get; }

        /// <summary>Cạnh dưới của hàng thứ <paramref name="row"/> (0 = hàng trên cùng).</summary>
        public double RowBottom(int row) => BoxMaxY - (row + 1) * RowHeight;
    }

    /// <summary>
    /// Khung tên là MỘT block có attribute: sửa nội dung bằng ATTEDIT/Properties, đọc lại được bằng code,
    /// và mọi bản vẽ dùng chung một định nghĩa. Gọi bên trong ICadContext.Execute / TryExecute.
    /// </summary>
    public static class TitleBlockBuilder
    {
        public static string BlockName(SheetSize sheet) => "CADCORE_KHUNG_" + sheet.Name;

        /// <summary>Tạo định nghĩa block nếu chưa có, chèn tại <paramref name="origin"/>, điền attribute.</summary>
        public static ObjectId Insert(ICadScope scope, SheetSize sheet, Point3d origin,
            Dictionary<string, string> values)
        {
            string name = BlockName(sheet);
            if (!scope.HasBlock(name))
                scope.CreateBlockDefinition(name, CreateEntities(scope.Database, TitleBlockLayout.For(sheet)),
                    Point3d.Origin, "Khung tên " + sheet.Name);

            return scope.InsertBlock(name, origin, attributes: values);
        }

        private static List<Entity> CreateEntities(Database db, TitleBlockLayout l)
        {
            var entities = new List<Entity>
            {
                Rectangle(0, 0, l.Sheet.Width, l.Sheet.Height, 0),               // mép giấy
                Rectangle(l.FrameMinX, l.FrameMinY, l.FrameMaxX, l.FrameMaxY, 0.5), // khung bản vẽ
                Rectangle(l.BoxMinX, l.FrameMinY, l.FrameMaxX, l.BoxMaxY, 0.35),    // bảng tên
                new Line(new Point3d(l.BoxMinX + TitleBlockLayout.LabelWidth, l.FrameMinY, 0),
                         new Point3d(l.BoxMinX + TitleBlockLayout.LabelWidth, l.BoxMaxY, 0)),
            };

            for (int row = 0; row < TitleBlockLayout.Fields.Count; row++)
            {
                TitleField field = TitleBlockLayout.Fields[row];
                double y = l.RowBottom(row);
                if (row > 0)
                    entities.Add(new Line(new Point3d(l.BoxMinX, y + TitleBlockLayout.RowHeight, 0),
                                          new Point3d(l.FrameMaxX, y + TitleBlockLayout.RowHeight, 0)));

                double textY = y + (TitleBlockLayout.RowHeight - TitleBlockLayout.TextHeight) / 2;
                entities.Add(new DBText
                {
                    TextString = field.Label,
                    Position = new Point3d(l.BoxMinX + 2, textY, 0),
                    Height = TitleBlockLayout.TextHeight,
                });
                entities.Add(new AttributeDefinition
                {
                    Tag = field.Tag,
                    Prompt = field.Label,
                    TextString = "",
                    Position = new Point3d(l.BoxMinX + TitleBlockLayout.LabelWidth + 2, textY, 0),
                    Height = TitleBlockLayout.TextHeight,
                });
            }

            // Nội dung block nằm trên layer 0: block reference đặt ở layer nào thì khung mang màu/kiểu của layer đó.
            foreach (Entity e in entities)
            {
                e.SetDatabaseDefaults(db);
                e.LayerId = db.LayerZero;
            }
            return entities;
        }

        private static Polyline Rectangle(double x1, double y1, double x2, double y2, double width)
        {
            var pl = new Polyline(4);
            pl.AddVertexAt(0, new Point2d(x1, y1), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(x2, y1), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(x2, y2), 0, 0, 0);
            pl.AddVertexAt(3, new Point2d(x1, y2), 0, 0, 0);
            pl.Closed = true;
            pl.ConstantWidth = width;   // SAU AddVertexAt: mỗi AddVertexAt ghi đè bề rộng của đỉnh nó thêm
            return pl;
        }
    }
}
```

</details>

```csharp
[CommandMethod("TOOLS_TITLEBLOCK")]
public void InsertTitleBlock()
{
    ExecuteCommand(() =>
    {
        string size = CadPromptHelper.GetKeywords("Khổ giấy", new[] { "A3", "A2", "A1" }, "A3");
        if (size == null) return;
        Point3d? origin = CadPromptHelper.GetPoint("Góc dưới trái của tờ giấy:");
        if (origin == null) return;
        string project = CadPromptHelper.GetString("Tên dự án:");
        if (project == null) return;
        string title = CadPromptHelper.GetString("Tên bản vẽ:");
        if (title == null) return;

        SheetSize sheet = size == "A1" ? SheetSize.A1 : size == "A2" ? SheetSize.A2 : SheetSize.A3;
        var values = new Dictionary<string, string>
        {
            ["DU_AN"] = project,
            ["BAN_VE"] = title,
            ["TY_LE"] = "1:100",
            ["NGAY"] = DateTime.Today.ToString("dd/MM/yyyy"),
            ["SO_BV"] = "",
        };
        ExecuteWithScope(scope => TitleBlockBuilder.Insert(scope, sheet, origin.Value, values),
            undoMarkName: "Khung tên " + sheet.Name);
    });
}
```

Đã kiểm trên AutoCAD 2024:
- Reference có đủ 5 attribute với đúng giá trị.
- Mọi entity trong định nghĩa nằm trên layer 0.
- Khung A3 đi từ (20, 10) tới (410, 287).
- Lần chèn thứ hai dùng lại định nghĩa cũ, không thêm entity vào định nghĩa.

Một bẫy nhỏ, cũng đã kiểm: đặt `ConstantWidth` **trước** `AddVertexAt` thì mất. Mỗi lần `AddVertexAt(…, startWidth: 0, endWidth: 0)` ghi bề rộng 0 cho đỉnh mới thêm, nên phải đặt `ConstantWidth` sau cùng.

Chèn khung tên vào **layout** (paper space) thì chạy lệnh khi đang ở tab layout đó, vì `InsertBlock` chèn vào không gian hiện hành. `TitleBlockBuilder.Insert` chèn ở tỷ lệ 1, kích thước tính bằng mm. Muốn đặt khung trong model space theo tỷ lệ bản vẽ (ví dụ 1:100), thêm một tham số `scale` và truyền nó cho `InsertBlock`.

### 12.2 Xuất mọi layout ra một file PDF

[`PlotService`](../../Services/PlotService.cs) đã lo phần khó của việc in. Nó chỉ cho chạy một job in mỗi lúc, tạm đặt `BACKGROUNDPLOT = 0` rồi khôi phục, trả lại layout hiện hành, và gộp nhiều layout thành một PDF nhiều trang. Công thức chỉ cần chọn layout và đặt tên file.

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;

namespace CadCoreFramework.Features.DrawingTools
{
    public static class PdfExport
    {
        /// <summary>
        /// Tên các layout giấy theo thứ tự tab (bỏ Model). Gọi trong ExecuteRead.
        /// </summary>
        public static List<string> PaperLayoutNames(ICadScope scope)
            => scope.Database.GetPaperLayouts(scope.Transaction).Select(l => l.LayoutName).ToList();

        /// <summary>
        /// "D:\DuAn\MB-01.dwg" → "D:\DuAn\MB-01.pdf". Bản vẽ chưa lưu (Document.Name chỉ là "Drawing1.dwg",
        /// không có thư mục) → đặt vào <paramref name="fallbackDirectory"/>.
        /// </summary>
        public static string PdfPathFor(string drawingPath, string fallbackDirectory)
        {
            if (string.IsNullOrWhiteSpace(drawingPath)) throw new ArgumentException("Thiếu tên bản vẽ.", nameof(drawingPath));
            string directory = Path.GetDirectoryName(drawingPath);
            if (string.IsNullOrEmpty(directory)) directory = fallbackDirectory;
            return Path.Combine(directory, Path.GetFileNameWithoutExtension(drawingPath) + ".pdf");
        }
    }
}
```

```csharp
[CommandMethod("TOOLS_EXPORT_PDF")]
public void ExportPdf()
{
    ExecuteCommand(() =>
    {
        var plot = GetRequiredService<IPlotService>();
        var notify = GetRequiredService<INotificationService>();

        List<string> layouts = null;
        CadContext.ExecuteRead(scope => layouts = PdfExport.PaperLayoutNames(scope));
        if (layouts.Count == 0)
        {
            notify.Warn("Bản vẽ không có layout giấy nào.");
            return;
        }

        string pdf = PdfExport.PdfPathFor(Document.Name,
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        // In NGOÀI mọi ICadContext.Execute: PlotService tự mở transaction và tự đổi layout hiện hành.
        if (!plot.PlotMultipleLayouts(Database, layouts, pdf))
            throw new InvalidOperationException("Xuất PDF thất bại, xem log để biết lý do.");
        notify.Info($"Đã xuất {layouts.Count} layout ra {pdf}");
    });
}
```

- **In ngoài transaction.** Tên layout đọc trong `ExecuteRead`, còn lệnh in gọi **sau** khi lambda đã xong. Lý do: `PlotService` tự mở transaction và đổi layout hiện hành. Đây là chỗ lệnh được dùng `Document`/`Database` trực tiếp. Service thì không được (CAD001), nên phần in nằm ở lệnh.
- **Đã kiểm:**
  - Bản vẽ mới có đúng hai layout giấy `Layout1`, `Layout2`, theo thứ tự tab.
  - Đi đúng luồng của lệnh thì ra một PDF có số trang bằng số layout.
  - Bản vẽ chưa lưu có `Document.Name = "Drawing1.dwg"`, không có thư mục, nên cần `fallbackDirectory`.
- `PlotService` chép page setup của từng layout rồi đặt máy in thành `DWG To PDF.pc3` (tham số `printerName`). Muốn đổi cách thiết lập trang in thì mở rộng `PlotService`, đừng chép code in vào lệnh.

### 12.3 Purge nhiều lượt và Audit

```csharp
using Autodesk.AutoCAD.DatabaseServices;
using CadCoreFramework.Core;

namespace CadCoreFramework.Features.DrawingTools
{
    public static class DrawingCleanup
    {
        /// <summary>
        /// Purge mọi block, layer, linetype, text style, dim style, RegApp không còn được dùng.
        /// Lặp nhiều lượt: xóa một block không dùng có thể làm layer/linetype mà chỉ block đó dùng trở thành
        /// "không dùng" ở lượt sau. Trả về số bản ghi đã xóa. Gọi trong ICadContext.Execute / TryExecute.
        /// </summary>
        public static int PurgeAll(ICadScope scope, int maxPasses = 10)
        {
            Database db = scope.Database;
            Transaction tr = scope.Transaction;
            int erased = 0;

            for (int pass = 0; pass < maxPasses; pass++)
            {
                ObjectIdCollection ids = Candidates(db, tr);
                db.Purge(ids);              // Purge KHÔNG xóa gì: nó bỏ khỏi ids những bản ghi còn được tham chiếu
                if (ids.Count == 0) break;

                foreach (ObjectId id in ids)
                    tr.GetObject(id, OpenMode.ForWrite).Erase();
                erased += ids.Count;
            }
            return erased;
        }

        private static ObjectIdCollection Candidates(Database db, Transaction tr)
        {
            var ids = new ObjectIdCollection();
            ObjectId[] tables =
            {
                db.BlockTableId, db.LayerTableId, db.LinetypeTableId,
                db.TextStyleTableId, db.DimStyleTableId, db.RegAppTableId,
            };
            foreach (ObjectId tableId in tables)
            {
                var table = (SymbolTable)tr.GetObject(tableId, OpenMode.ForRead);
                foreach (ObjectId id in table)
                {
                    var record = (SymbolTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (record.IsErased || record.IsDependent) continue;          // "XREF|LAYER": thuộc file xref
                    if (record is BlockTableRecord block && (block.IsLayout || block.IsFromExternalReference))
                        continue;                                                   // xref: tháo bằng XREF → Detach
                    ids.Add(id);
                }
            }
            return ids;
        }
    }
}
```

```csharp
[CommandMethod("TOOLS_PURGE_AUDIT")]
public void PurgeAndAudit()
{
    ExecuteCommand(() =>
    {
        int erased = 0;
        CadContext.Execute(scope => { erased = DrawingCleanup.PurgeAll(scope); }, undoMarkName: "Purge");
        GetRequiredService<INotificationService>().Info($"Đã purge {erased} bản ghi không dùng.");

        // .NET API không có hàm Audit cho cả bản vẽ → xếp lệnh AUDIT chạy ngay sau lệnh này.
        Document.SendStringToExecute("_.AUDIT _Y ", true, false, false);
    });
}
```

- **`Database.Purge` không xóa gì.** Nó lọc tập id bạn đưa vào, chỉ giữ lại những bản ghi không còn được tham chiếu. Việc `Erase` là của bạn.
- **Một lượt là không đủ (đã kiểm).** Ví dụ: layer chỉ được dùng bởi entity *bên trong* một block chưa ai chèn. Ở lượt 1, `Purge` cho xóa block nhưng giữ layer. Sang lượt 2, sau khi block đã bị xóa trong cùng transaction, layer mới được xóa.
- Kiểm thử `PurgeAll` cũng xác nhận layer 0 và layer hiện hành không bị đụng tới. Gọi `PurgeAll` lần hai trả về 0.
- **Không có API Audit cho cả bản vẽ.** Metadata `AcDbMgd.dll` (AutoCAD 2024) không có `Database.Audit`. `AuditInfo` không có constructor public; nó chỉ là tham số của `DBObject.Audit`, phần mà người viết custom object override. Vì vậy công thức gửi lệnh `AUDIT` qua `SendStringToExecute`, và lệnh này chạy **sau** khi lệnh hiện tại kết thúc. Chạy `_.AUDIT` rồi `_Y` trong accoreconsole cho prompt `Fix any errors detected? [Yes/No] <N>:` và kết thúc bằng `Total errors found 0 fixed 0`.
- Purge chạy trong một `Execute` có `undoMarkName`; `AUDIT` là một lệnh riêng, chạy sau đó.

### 12.4 Attach xref với đường dẫn tương đối

```csharp
using System;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Core;
using CadCoreFramework.Utils;

namespace CadCoreFramework.Features.DrawingTools
{
    public static class XrefAttacher
    {
        /// <summary>
        /// Attach <paramref name="xrefPath"/> rồi chèn một tham chiếu tại <paramref name="position"/> vào không gian
        /// hiện hành. Gọi trong ICadContext.Execute*. Xem <see cref="AttachDefinition"/> về đường dẫn.
        /// </summary>
        public static ObjectId Attach(ICadScope scope, string xrefPath, Point3d position, string? hostDrawingPath)
        {
            ObjectId definitionId = AttachDefinition(scope.Database, scope.Transaction, xrefPath, hostDrawingPath);
            var reference = new BlockReference(position, definitionId);
            reference.SetDatabaseDefaults(scope.Database);
            return scope.AppendEntityToCurrentSpace(reference);
        }

        /// <summary>
        /// Tạo (hoặc dùng lại) định nghĩa xref và ấn định đường dẫn lưu trong bản vẽ:
        /// bản vẽ chủ đã lưu (<paramref name="hostDrawingPath"/> có thư mục) và cùng ổ đĩa → đường dẫn TƯƠNG ĐỐI
        /// với thư mục bản vẽ chủ; còn lại → đường dẫn TUYỆT ĐỐI. Không để mặc AutoCAD: với bản vẽ chưa lưu,
        /// AttachXref ghi đường dẫn tương đối theo thư mục hiện hành của tiến trình.
        /// </summary>
        public static ObjectId AttachDefinition(Database db, Transaction tr, string xrefPath, string? hostDrawingPath)
        {
            string fullPath = Path.GetFullPath(xrefPath);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Không tìm thấy file xref.", fullPath);
            string name = SymbolNameRules.Validate(Path.GetFileNameWithoutExtension(fullPath), nameof(xrefPath));

            var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            if (blockTable.Has(name))
            {
                var existing = (BlockTableRecord)tr.GetObject(blockTable[name], OpenMode.ForRead);
                if (!existing.IsFromExternalReference)
                    throw new InvalidOperationException($"Bản vẽ đã có block thường tên '{name}'. Đổi tên file xref hoặc block.");
                return existing.ObjectId;                    // đã attach: chỉ chèn thêm tham chiếu
            }

            ObjectId definitionId = db.AttachXref(fullPath, name);
            var definition = (BlockTableRecord)tr.GetObject(definitionId, OpenMode.ForWrite);
            definition.PathName = RelativePath(hostDrawingPath, fullPath) ?? fullPath;
            return definitionId;
        }

        /// <summary>
        /// Đường dẫn của <paramref name="xrefPath"/> tính từ thư mục chứa bản vẽ chủ, theo cách AutoCAD ghi:
        /// <c>.\XREF\nen.dwg</c>, <c>..\Chung\khung.dwg</c>. Trả null khi bản vẽ chủ chưa lưu (null, hoặc tên
        /// không có thư mục như "Drawing1.dwg") hoặc hai file nằm khác ổ đĩa.
        /// </summary>
        public static string? RelativePath(string? hostDrawingPath, string xrefPath)
        {
            if (string.IsNullOrEmpty(hostDrawingPath) || !Path.IsPathRooted(hostDrawingPath)) return null;
            string hostDir = Path.GetDirectoryName(Path.GetFullPath(hostDrawingPath));
            string target = Path.GetFullPath(xrefPath);
            if (!string.Equals(Path.GetPathRoot(hostDir), Path.GetPathRoot(target), StringComparison.OrdinalIgnoreCase))
                return null;

            string[] from = hostDir.TrimEnd('\\').Split('\\');
            string[] to = target.Split('\\');
            int common = 0;
            while (common < from.Length && common < to.Length - 1 &&
                   string.Equals(from[common], to[common], StringComparison.OrdinalIgnoreCase))
                common++;

            string tail = string.Join("\\", to.Skip(common));
            return common == from.Length
                ? ".\\" + tail
                : string.Join("\\", Enumerable.Repeat("..", from.Length - common)) + "\\" + tail;
        }
    }
}
```

```csharp
[CommandMethod("TOOLS_XREF")]
public void AttachXref()
{
    ExecuteCommand(() =>
    {
        string path = GetRequiredService<IDialogService>().ShowOpenFileDialog("Bản vẽ AutoCAD (*.dwg)|*.dwg", "Chọn file xref");
        if (string.IsNullOrEmpty(path)) return;
        Point3d? position = CadPromptHelper.GetPoint("Điểm chèn xref:");
        if (position == null) return;

        // DWGTITLED = 1: bản vẽ đã được lưu ít nhất một lần, Document.Name là đường dẫn đầy đủ.
        bool saved = Convert.ToInt32(CadApplicationHelper.GetSystemVariable("DWGTITLED")) == 1;
        string host = saved ? Document.Name : null;
        if (!saved)
            GetRequiredService<INotificationService>().Warn("Bản vẽ chưa lưu: xref được ghi đường dẫn tuyệt đối. Lưu bản vẽ trước nếu muốn đường dẫn tương đối.");

        ExecuteWithScope(scope => XrefAttacher.Attach(scope, path, position.Value, host),
            undoMarkName: "Attach " + Path.GetFileName(path));
    });
}
```

**Tương đối hay tuyệt đối?**
- **Đường dẫn tương đối** (`.\XREF\nen.dwg`) không gãy khi bạn chuyển **cả thư mục dự án** sang ổ khác, sang máy khác hay nén gửi đi. Nó chỉ gãy khi tách bản vẽ chủ khỏi thư mục `XREF`.
- **Đường dẫn tuyệt đối** thì ngược lại.

Kiểm thử đã làm đúng kịch bản đó:
1. Lưu bản vẽ chủ (một side database) trong `proj1\`, gắn một xref tương đối và một xref tuyệt đối, cả hai nằm trong `proj1\XREF\`.
2. Chép cả thư mục sang `proj2\`, rồi **xóa** `proj1\`.
3. Mở lại `proj2\host.dwg` và gọi `ResolveXrefs`. Kết quả: xref tương đối `Resolved`, xref tuyệt đối `FileNotFound`.

**Vì sao không để mặc AutoCAD chọn.** Đây là kiểm thử đã FAIL ở bản đầu của công thức. Bản đầu chỉ đổi đường dẫn khi tính được đường dẫn tương đối, và giả định `AttachXref` ghi đường dẫn tuyệt đối. Thực tế với `REFPATHTYPE = 1` (mặc định), `AttachXref` **tự** ghi đường dẫn tương đối, kể cả khi bản vẽ chủ **chưa lưu**. Khi đó nó tính theo *thư mục hiện hành của tiến trình*. Lần chạy thử ghi `..\CadCoreFramework_accoreconsole_profile\Temp\…\LAB_XREF_RAW.dwg`, tính từ thư mục làm việc của accoreconsole. Người dùng lưu bản vẽ ở chỗ khác là xref gãy. Vì vậy công thức luôn tự gán `PathName`:
- Bản vẽ chủ đã lưu và cùng ổ đĩa: đường dẫn tương đối với bản vẽ chủ.
- Còn lại: đường dẫn tuyệt đối, kèm cảnh báo cho người dùng.

Hai chi tiết cũng đã kiểm:
- Mở một bản vẽ đã lưu thì `DWGTITLED = 1` và `Document.Name` là đường dẫn đầy đủ. Với `Drawing1.dwg` chưa lưu, `DWGTITLED = 0`, còn `Database.Filename` lại là đường dẫn **template** (`…\Template\acadiso.dwt`), nên đừng dùng nó làm "đường dẫn bản vẽ chủ".
- Sau `Database.SaveAs`, cả `Document.Name` lẫn `Database.Filename` đều **không** đổi.

Gắn lại cùng một file chỉ chèn thêm tham chiếu, dùng chung định nghĩa. Trùng tên với một block thường thì báo lỗi rõ ràng.

### 12.5 Gộp nhiều layer thành một

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;
using CadCoreFramework.Utils;

namespace CadCoreFramework.Features.DrawingTools
{
    public sealed class LayerMergeResult
    {
        public LayerMergeResult(int entitiesMoved, IReadOnlyList<string> erased,
            IReadOnlyList<string> kept, IReadOnlyList<string> missing)
        {
            EntitiesMoved = entitiesMoved;
            Erased = erased;
            Kept = kept;
            Missing = missing;
        }

        public int EntitiesMoved { get; }
        /// <summary>Layer nguồn đã xóa.</summary>
        public IReadOnlyList<string> Erased { get; }
        /// <summary>Layer nguồn còn được tham chiếu nên giữ lại (đã hết entity, nhưng Purge chưa cho xóa).</summary>
        public IReadOnlyList<string> Kept { get; }
        /// <summary>Tên nguồn không có trong bản vẽ.</summary>
        public IReadOnlyList<string> Missing { get; }
    }

    public static class LayerMerger
    {
        /// <summary>"A, b ,,B" → ["A", "b"]: bỏ khoảng trắng, bỏ mục rỗng, bỏ trùng (không phân biệt hoa thường).</summary>
        public static List<string> ParseNames(string? text)
            => (text ?? "").Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>Luật gộp layer, logic thuần. Trả danh sách lỗi (rỗng = hợp lệ).</summary>
        public static List<string> Validate(IReadOnlyCollection<string> sources, string target)
        {
            var errors = new List<string>();
            string? targetError = SymbolNameRules.GetError(target);
            if (targetError != null) errors.Add("Layer đích: " + targetError);
            if (sources.Count == 0) errors.Add("Chưa có layer nguồn nào.");

            foreach (string source in sources)
            {
                if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"'{source}' vừa là nguồn vừa là đích.");
                else if (string.Equals(source, "0", StringComparison.OrdinalIgnoreCase))
                    errors.Add("Không gộp layer 0: entity nằm trên layer 0 trong block lấy layer của block reference, gộp sẽ đổi cách hiển thị.");
                else if (string.Equals(source, "Defpoints", StringComparison.OrdinalIgnoreCase))
                    errors.Add("Không gộp Defpoints: AutoCAD đặt điểm định nghĩa của kích thước lên đó và không in layer này.");
                else if (source.IndexOf('|') >= 0)
                    errors.Add($"'{source}' là layer của xref: sửa trong file xref.");
            }
            return errors;
        }

        /// <summary>
        /// Chuyển mọi entity (trong model, mọi layout, mọi định nghĩa block, kể cả attribute) từ các layer nguồn
        /// sang layer đích, rồi xóa các layer nguồn mà Purge xác nhận không còn tham chiếu.
        /// Gọi trong ICadContext.Execute / TryExecute (một transaction, một bước Undo).
        /// </summary>
        public static LayerMergeResult Merge(ICadScope scope, IReadOnlyCollection<string> sources, string target)
        {
            List<string> errors = Validate(sources, target);
            if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors), nameof(sources));

            Database db = scope.Database;
            Transaction tr = scope.Transaction;
            ObjectId targetId = scope.EnsureLayer(target);

            var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            var sourceIds = new Dictionary<ObjectId, string>();
            var missing = new List<string>();
            foreach (string name in sources)
            {
                if (layerTable.Has(name)) sourceIds[layerTable[name]] = name;
                else missing.Add(name);
            }

            // Layer hiện hành không xóa được → chuyển layer hiện hành sang đích (layer đang đóng băng thì sang 0).
            if (sourceIds.ContainsKey(db.Clayer))
            {
                var targetLayer = (LayerTableRecord)tr.GetObject(targetId, OpenMode.ForRead);
                db.Clayer = targetLayer.IsFrozen ? db.LayerZero : targetId;
            }

            int moved = 0;
            var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            foreach (ObjectId blockId in blockTable)
            {
                var block = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
                if (block.IsFromExternalReference || block.IsDependent) continue;

                foreach (ObjectId id in block)
                {
                    if (!(tr.GetObject(id, OpenMode.ForRead) is Entity entity)) continue;
                    if (sourceIds.ContainsKey(entity.LayerId)) moved += MoveToLayer(tr, id, targetId);

                    // Attribute không nằm trong block table record mà thuộc về block reference.
                    if (entity is BlockReference reference)
                    {
                        foreach (ObjectId attributeId in reference.AttributeCollection)
                        {
                            var attribute = (Entity)tr.GetObject(attributeId, OpenMode.ForRead);
                            if (sourceIds.ContainsKey(attribute.LayerId)) moved += MoveToLayer(tr, attributeId, targetId);
                        }
                    }
                }
            }

            var purgeable = new ObjectIdCollection(sourceIds.Keys.ToArray());
            db.Purge(purgeable);
            var erased = new List<string>();
            foreach (ObjectId id in purgeable)
            {
                tr.GetObject(id, OpenMode.ForWrite).Erase();
                erased.Add(sourceIds[id]);
            }
            List<string> kept = sourceIds.Values.Except(erased).ToList();
            return new LayerMergeResult(moved, erased, kept, missing);
        }

        private static int MoveToLayer(Transaction tr, ObjectId id, ObjectId layerId)
        {
            // forceOpenOnLockedLayer = true: entity trên layer KHÓA vẫn mở để ghi được (nếu không: eOnLockedLayer).
            var entity = (Entity)tr.GetObject(id, OpenMode.ForWrite, false, true);
            entity.LayerId = layerId;
            return 1;
        }
    }
}
```

```csharp
[CommandMethod("TOOLS_MERGE_LAYERS")]
public void MergeLayers()
{
    ExecuteCommand(() =>
    {
        var notify = GetRequiredService<INotificationService>();
        string sourceText = CadPromptHelper.GetString("Các layer nguồn, cách nhau bằng dấu phẩy:");
        if (sourceText == null) return;
        string target = CadPromptHelper.GetString("Layer đích:");
        if (target == null) return;

        List<string> sources = LayerMerger.ParseNames(sourceText);
        List<string> errors = LayerMerger.Validate(sources, target.Trim());
        if (errors.Count > 0)
        {
            foreach (string error in errors) notify.Warn(error);
            return;
        }

        LayerMergeResult result = null;
        CadContext.Execute(scope => { result = LayerMerger.Merge(scope, sources, target.Trim()); },
            undoMarkName: "Gộp layer → " + target.Trim());

        notify.Info($"Đã chuyển {result.EntitiesMoved} entity sang '{target.Trim()}'. Đã xóa: {string.Join(", ", result.Erased)}.");
        if (result.Kept.Count > 0) notify.Warn("Còn được tham chiếu nên chưa xóa: " + string.Join(", ", result.Kept));
        if (result.Missing.Count > 0) notify.Warn("Không có trong bản vẽ: " + string.Join(", ", result.Missing));
    });
}
```

Những chỗ một bản "gộp layer" đơn giản hay làm sai, và công thức này xử lý:

| Chỗ dễ sót | Công thức làm gì | Đã kiểm |
| :--- | :--- | :---: |
| Chỉ duyệt không gian hiện hành | Duyệt **mọi** block table record: model, mọi layout, mọi định nghĩa block (bỏ xref) | ✅ entity ở model, ở `Layout1` và trong định nghĩa block đều chuyển |
| Attribute | Attribute thuộc về block reference, không nằm trong block table record, nên duyệt `AttributeCollection` riêng | ✅ |
| Layer nguồn đang **khóa** | `GetObject(id, ForWrite, openErased: false, forceOpenOnLockedLayer: true)` | ✅ xem dưới bảng |
| Layer nguồn là layer hiện hành | Chuyển `Clayer` sang layer đích trước (layer đích đang đóng băng thì sang 0) | ✅ nhánh thường; nhánh layer đích đóng băng chưa kiểm |
| Xóa layer vẫn còn được tham chiếu | Chỉ xóa những layer mà `Purge` cho phép; còn lại báo trong `Kept` | ✅ hai layer nguồn đều xóa được, `Kept` rỗng |
| Layer 0, Defpoints, layer của xref | Từ chối ngay ở `Validate` | ✅ logic thuần |

**Khóa layer: bẫy trong chính kiểm thử.** Đây là kiểm thử thứ hai đã FAIL ở lần chạy đầu. Kiểm thử khóa layer rồi mở entity `ForWrite` *trong cùng transaction*, và AutoCAD **không** báo lỗi. Làm lại với layer khóa **đã commit** thì đúng như mong đợi: `GetObject(…, ForWrite)` ném `eOnLockedLayer`, còn bản có `forceOpenOnLockedLayer: true` mở được. Bài học cho người viết kiểm thử: muốn tái hiện trạng thái của bản vẽ thật, hãy commit phần chuẩn bị trước, rồi mới chạy phần cần kiểm trong một transaction mới.

Toàn bộ thao tác gộp (chuyển entity, đổi layer hiện hành, xóa layer) nằm trong một `Execute` với `undoMarkName`: một transaction, một undo mark. Ctrl+Z trên AutoCAD có giao diện chưa được kiểm trong lần chạy này; thêm nó vào smoke test thủ công khi bạn chép công thức vào dự án.

---

## 13. Checklist trước khi mở PR

- [ ] Build `-c Release -warnaserror` 0 warning (analyzer CAD001–CAD003 chạy trong build); `dotnet test` xanh trên cả hai TFM
- [ ] Không prompt trong transaction; không prompt trong Service/ViewModel (CAD002)
- [ ] Mọi thao tác ghi qua `ICadContext`; Service truyền `undoMarkName`; không ghi trong `ExecuteRead` (CAD003)
- [ ] Không `MdiActiveDocument` trong Service/ViewModel (CAD001); không đọc nó sau `await`
- [ ] Đối tượng không vào DB được `Dispose`; `ResultBuffer`/side DB trong `using`
- [ ] Subscription qua `TrackSubscription`; event AutoCAD được gỡ trong `Dispose`/`Shutdown`
- [ ] Logic tách khỏi lambda; có test kết quả cho logic, test hợp đồng cho service
- [ ] Đăng ký DI + `CommandClass`; palette mới có test XAML (`ThemeManager.Attach`, không mã màu cứng); lệnh mới có trong `PackageContents.xml`
- [ ] Ô nhập số trên palette bind vào chuỗi và đọc bằng `NumberText` ([§4](#4-thêm-một-palette)); có validate, nút gắn `CanExecute`
- [ ] Sửa helper trong `Extensions/`/`Utils/` → chạy `tests\integration\run-headless-lab.ps1` (nếu có AutoCAD) và thêm kiểm thử lab FAIL trên code cũ
- [ ] Smoke test thủ công các kịch bản bị ảnh hưởng ([T05 §7](T05_TESTING_STRATEGY.md#7-những-gì-chỉ-autocad-thật-mới-kiểm-được)), ghi kết quả vào PR
- [ ] CHANGELOG `[Unreleased]`; cập nhật T-module nếu đổi quy ước

Checklist review đầy đủ cho người review: [T09 §2](T09_SENIOR_PLAYBOOK.md#2-checklist-code-review).

---

## ✅ Tự kiểm tra T08

1. Bạn thêm class `PanelCommands`, build, NETLOAD, gõ `PANEL_DRAW` thì báo `Unknown command`. Bạn đã quên gì? Còn file nào phải thêm tên lệnh, và test nào sẽ nhắc bạn?
2. Palette mới có ô "Chiều rộng". Bind ô đó thế nào, và vì sao không bind thẳng vào `double`?
3. Bạn viết một Overrule vẽ thêm nhãn cho khung tủ của §3. Dùng filter nào, và chuyện gì xảy ra nếu không đặt filter?
4. Vì sao `PanelService` luôn truyền `undoMarkName`, dù lệnh `PANEL_DRAW` đã được AutoCAD gom Undo theo lệnh?
5. Bạn gọi `db.Purge(ids)` một lần rồi `Erase` mọi id còn lại, nhưng sau đó lệnh `PURGE` của AutoCAD vẫn tìm thấy layer để xóa. Vì sao?
6. Người dùng attach xref vào bản vẽ `Drawing1.dwg` chưa lưu, rồi lưu bản vẽ vào thư mục dự án. Mở lại thì xref báo "Not Found". Chuyện gì đã xảy ra, và công thức §12.4 tránh nó thế nào?

<details><summary>Gợi ý</summary>

1. Dòng `[assembly: CommandClass(typeof(PanelCommands))]` trong `Properties/AssemblyInfo.cs` ([§1](#1-thêm-một-lệnh)). Lệnh mới còn phải có trong `Deployment/PackageContents.xml`; `PackageManifestTests` đỏ nếu thiếu ([§4](#4-thêm-một-palette)).
2. Bind vào một property **chuỗi**, rồi đọc số bằng `NumberText` như `SpacingText` của Golden Sample. Bind thẳng vào `double` thì WPF đọc `2,5` thành 25 và lặng lẽ giữ số cũ khi ô trống ([U01 §5](U01_WPF_MVVM_BASICS.md#5-ba-bẫy-khi-nhập-số)).
3. `SetExtensionDictionaryEntryFilter(PanelService.DataKey)`, vì dữ liệu tủ nằm trong Xrecord của Extension Dictionary ([§7](#7-thêm-overrule)). Không filter thì `WorldDraw` chạy cho mọi `Polyline` trong bản vẽ, rất nhiều lần.
4. Service còn được gọi từ palette, tức application context, nơi AutoCAD không tự gom Undo theo lệnh ([T01 §11](T01_AUTOCAD_NET_FOUNDATIONS.md#11-undo)).
5. Xóa một block không dùng làm layer chỉ được block đó dùng trở thành "không dùng", nhưng chỉ ở lượt `Purge` kế tiếp. Phải lặp cho tới khi `Purge` không trả về gì ([§12.3](#123-purge-nhiều-lượt-và-audit)).
6. Với `REFPATHTYPE = 1`, `AttachXref` ghi đường dẫn tương đối ngay cả khi bản vẽ chủ chưa lưu, và tính theo thư mục hiện hành của tiến trình chứ không theo thư mục dự án. Công thức kiểm `DWGTITLED`: bản vẽ chưa lưu thì luôn ghi đường dẫn tuyệt đối và cảnh báo người dùng ([§12.4](#124-attach-xref-với-đường-dẫn-tương-đối)).
</details>

---

**Tiếp theo:** [T09 — Sổ tay Senior →](T09_SENIOR_PLAYBOOK.md)
