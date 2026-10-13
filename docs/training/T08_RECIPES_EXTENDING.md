> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← T07](T07_PERFORMANCE_AND_SCALE.md) · **T08** · [T09 →](T09_SENIOR_PLAYBOOK.md)

# T08 — Công Thức Mở Rộng Dự Án

Mỗi công thức gồm **checklist các file phải chạm** và **code mẫu dùng đúng API hiện có**. Phần nào là *đề xuất bổ sung* (chưa có trong v1.0.0) được ghi rõ. Mọi công thức đều tuân thủ quy tắc của [Golden Sample](../09_GOLDEN_SAMPLE.md): khi phân vân, **copy cách Golden Sample làm**.

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
            scope.SetXRecord(frame, "PANEL_DATA", new { spec.Name, spec.Width, spec.Height });
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

**Code-behind**: copy mẫu [`GridMarkerPalette.xaml.cs`](../../Samples/GridMarker/GridMarkerPalette.xaml.cs). Nạp `Styles.xaml` bằng pack URI dựng **lúc runtime** (tên assembly Debug có timestamp), áp theme, lấy ViewModel từ DI và ném lỗi nếu thiếu.

**Lệnh mở palette, nhớ được vị trí** ([B-04](T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng)): `TogglePalette` không nhận Guid. Nhưng nó tìm `PaletteSet` theo **tên** trước khi tạo, nên chỉ cần tạo sẵn PaletteSet *có Guid* bằng `GetOrCreatePaletteSet` trước lần Toggle đầu:

```csharp
private static readonly Guid PanelPaletteGuid = new Guid("8E0B3A0C-2D7F-4F7E-9C41-1B5E6A2F9D10"); // tự sinh, KHÔNG đổi về sau

[CommandMethod("PANEL_PALETTE")]
public void ShowPanelPalette()
{
    ExecuteCommand(() =>
    {
        var palettes = GetRequiredService<IPaletteService>();
        palettes.GetOrCreatePaletteSet("PanelPalette", PanelPaletteGuid);   // lần đầu: tạo kèm Guid
        palettes.TogglePalette("PanelPalette", () => new PanelPalette(), tabTitle: "Tủ điện");
    });
}
```

Thêm test trong `XamlPaletteTests` (đăng ký ViewModel vào container giả) và một dòng `AddTransient<PanelViewModel>()` trong `ConfigureServices`.

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

---

## 6. Thêm Ribbon tab

Hiện trạng v1.0.0: `RibbonService` có đủ hạ tầng nhưng **chưa ai dựng tab** ([B-05](T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng)). Có hai cái bẫy về **thời điểm** và **trùng lặp**:

| Tình huống | `InitializeRibbon()` làm gì | Ai gọi build? |
| :--- | :--- | :--- |
| Plugin nạp lúc AutoCAD **đang khởi động** (Autoloader) | Ribbon chưa có → hook `ItemInitialized` → gọi build action khi Ribbon sẵn sàng | Service, **nếu build action đã đăng ký trước đó** |
| NETLOAD khi AutoCAD **đã mở xong** | Ribbon đã có → không hook gì | **Không ai.** Bạn phải tự gọi |
| Người dùng chạy `CUI` / `MENULOAD` / `WSCURRENT` … | `CommandEnded` → `Application.Idle` → gọi build action | Service |

`AddTab`/`AddPanel` idempotent, nhưng **`AddButton` thì không**: build lại khi tab còn tồn tại sẽ nhân đôi nút. Cách an toàn là **xóa tab cũ rồi dựng lại**. Việc này cần một method `RemoveTab`, là **đề xuất bổ sung** vào `RibbonService` (chưa có trong v1.0.0):

```csharp
// ĐỀ XUẤT bổ sung vào IRibbonService / RibbonService (Lab L11)
public void RemoveTab(string tabId)
{
    var tab = ComponentManager.Ribbon?.Tabs.FirstOrDefault(t => t.Id == tabId);
    if (tab != null) ComponentManager.Ribbon.Tabs.Remove(tab);
}
```

Dựng Ribbon (ví dụ trong một class `CadCoreRibbon`):

```csharp
public static class CadCoreRibbon
{
    public const string TabId = "CADCORE_TAB";

    public static void Build(IRibbonService ribbon)
    {
        ribbon.RemoveTab(TabId);                                   // idempotent: luôn dựng lại từ đầu
        ribbon.AddTab(TabId, "CadCore", TabId);
        ribbon.AddPanel(TabId, "CADCORE_GRID", "Grid Marker");
        ribbon.AddButton(TabId, "CADCORE_GRID", "Vẽ lưới", "SAMPLE_GRID", tooltip: "Vẽ lưới ô đánh nhãn");
        ribbon.AddButton(TabId, "CADCORE_GRID", "Palette", "SHOW_PALETTE");
        ribbon.AddPanel(TabId, "CADCORE_TOOLS", "Công cụ");
        ribbon.AddButton(TabId, "CADCORE_TOOLS", "Debug", "CORE_DEBUG");
    }
}
```

Nối vào khởi động, **thứ tự là quan trọng** (trong `AppBootstrapper.InitializeCore`, thay cho 2 dòng khởi động Ribbon hiện tại):

```csharp
var ribbonService = ServiceProvider.GetService<IRibbonService>();
if (ribbonService != null)
{
    ribbonService.RegisterBuildAction(() => CadCoreRibbon.Build(ribbonService)); // ① đăng ký TRƯỚC
    ribbonService.InitializeRibbon();                                            // ② có thể hoãn build
    if (Autodesk.Windows.ComponentManager.Ribbon != null)                        // ③ Ribbon đã sẵn (NETLOAD muộn)
        CadCoreRibbon.Build(ribbonService);
}
```

Kiểm tra (Lab L11): NETLOAD khi AutoCAD đã mở → tab xuất hiện; chạy `CUI` rồi đóng → tab vẫn còn, **không** nhân đôi nút; cài bằng bundle (nạp lúc khởi động) → tab xuất hiện; nút bấm chạy đúng lệnh.

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
        SetXDataFilter(PanelService.XDataAppName);                         // chỉ áp cho entity có XData của ta
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
- **Giới hạn phạm vi** bằng filter của `Overrule` (đã kiểm chứng trong AutoCAD 2024): `SetXDataFilter(app)`, `SetExtensionDictionaryEntryFilter(key)`, `SetIdFilter(ObjectId[])`, `SetCustomFilter()`. Không có filter thì `WorldDraw` chạy cho *mọi* entity cùng lớp, *rất nhiều lần*.
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

Key có tiền tố theo tính năng. Gọi `Save()` khi người dùng xác nhận thay đổi, không gọi mỗi lần gõ phím. Lưu ý: nhiều phiên AutoCAD cùng ghi thì **phiên ghi sau thắng** ([B-22](T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng)).

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

- [ ] Build `-c Release -warnaserror` 0 warning; `dotnet test` xanh trên cả hai TFM
- [ ] Không prompt trong transaction; không prompt trong Service/ViewModel
- [ ] Mọi thao tác ghi qua `ICadContext`; Service truyền `undoMarkName`
- [ ] Không `MdiActiveDocument` trong Service/ViewModel; không đọc nó sau `await`
- [ ] Đối tượng không vào DB được `Dispose`; `ResultBuffer`/side DB trong `using`
- [ ] Subscription qua `TrackSubscription`; event AutoCAD được gỡ trong `Dispose`/`Shutdown`
- [ ] Logic tách khỏi lambda; có test kết quả cho logic, test hợp đồng cho service
- [ ] Đăng ký DI + `CommandClass`; palette mới có test XAML
- [ ] Smoke test thủ công các kịch bản bị ảnh hưởng ([T05 §7](T05_TESTING_STRATEGY.md#7-những-gì-chỉ-autocad-thật-mới-kiểm-được)), ghi kết quả vào PR
- [ ] CHANGELOG `[Unreleased]`; cập nhật T-module nếu đổi quy ước

Checklist review đầy đủ cho người review: [T09 §2](T09_SENIOR_PLAYBOOK.md#2-checklist-code-review).

---

**Tiếp theo:** [T09 — Sổ tay Senior →](T09_SENIOR_PLAYBOOK.md)
