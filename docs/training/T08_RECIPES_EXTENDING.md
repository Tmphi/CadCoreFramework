> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← T07](T07_PERFORMANCE_AND_SCALE.md) · **T08** · [T09 →](T09_SENIOR_PLAYBOOK.md)

# T08 — Công Thức Mở Rộng Dự Án

> **Cấp độ:** 🌱 Fresher → 🌳 Middle · **Cần trước:** [T02](T02_ARCHITECTURE_DEEP_DIVE.md), [R05 — Golden Sample](../reference/R05_GOLDEN_SAMPLE.md) · **Trong lộ trình:** tuần 6 (§1–3) và 8 (§4–6) ([README §3](README.md#3-lộ-trình-đào-tạo))

Mỗi công thức gồm **checklist các file phải chạm** và **code mẫu dùng đúng API hiện có**. Phần nào là *đề xuất bổ sung* (chưa có trong v1.0.0) được ghi rõ. Mọi công thức đều tuân thủ quy tắc của [Golden Sample](../reference/R05_GOLDEN_SAMPLE.md): khi phân vân, **copy cách Golden Sample làm**. Code của §1, §3–5, §7, §9–11 đã được biên dịch thử trong một bản sao của dự án (build `-warnaserror` sạch); phần T08 để bạn tự viết (`PanelSpec`, `PanelViewModel`) thì làm theo [Golden Sample](../reference/R05_GOLDEN_SAMPLE.md).

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
12. [Checklist trước khi mở PR](#12-checklist-trước-khi-mở-pr)

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

## 12. Checklist trước khi mở PR

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

<details><summary>Gợi ý</summary>

1. Dòng `[assembly: CommandClass(typeof(PanelCommands))]` trong `Properties/AssemblyInfo.cs` ([§1](#1-thêm-một-lệnh)). Lệnh mới còn phải có trong `Deployment/PackageContents.xml`; `PackageManifestTests` đỏ nếu thiếu ([§4](#4-thêm-một-palette)).
2. Bind vào một property **chuỗi**, rồi đọc số bằng `NumberText` như `SpacingText` của Golden Sample. Bind thẳng vào `double` thì WPF đọc `2,5` thành 25 và lặng lẽ giữ số cũ khi ô trống ([U01 §5](U01_WPF_MVVM_BASICS.md#5-ba-bẫy-khi-nhập-số)).
3. `SetExtensionDictionaryEntryFilter(PanelService.DataKey)`, vì dữ liệu tủ nằm trong Xrecord của Extension Dictionary ([§7](#7-thêm-overrule)). Không filter thì `WorldDraw` chạy cho mọi `Polyline` trong bản vẽ, rất nhiều lần.
4. Service còn được gọi từ palette, tức application context, nơi AutoCAD không tự gom Undo theo lệnh ([T01 §11](T01_AUTOCAD_NET_FOUNDATIONS.md#11-undo)).
</details>

---

**Tiếp theo:** [T09 — Sổ tay Senior →](T09_SENIOR_PLAYBOOK.md)
