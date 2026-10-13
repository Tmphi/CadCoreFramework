# Phần 8: Cookbook — Công Thức Thực Chiến

Những công thức copy-paste cho các bài toán AutoCAD thường gặp. Mỗi công thức
được thiết kế để **chạy đúng ngay từ lần đầu** — không có mã nguồn giả, không
cắt gọt, không có `TODO`.

> [!NOTE]
> Tất cả các công thức đều giả định bạn đang viết code bên trong một lớp kế
> thừa `CadCommandBase` (dùng `ExecuteWithScope`) hoặc một `Service` được
> inject `ICadContext _cadContext`.

---

## 8.1 Vẽ Title Block (Khung Tên) chuẩn A3

```csharp
[CommandMethod("DRAW_TITLEBLOCK_A3")]
public void DrawTitleBlockA3()
{
    ExecuteWithScope(scope =>
    {
        const double W = 420, H = 297;              // A3 landscape (mm)
        const double M = 10;                        // margin
        scope.EnsureLayer("TITLEBLOCK", colorIndex: 7);

        // Khung ngoài + khung trong
        scope.DrawRectangle(new Point3d(0, 0, 0), new Point3d(W, H, 0),
            layer: "TITLEBLOCK");
        scope.DrawRectangle(new Point3d(M, M, 0), new Point3d(W - M, H - M, 0),
            layer: "TITLEBLOCK", lineWidth: 0.5);

        // Ô tiêu đề góc dưới-phải
        var titleBl = new Point3d(W - 170, M, 0);
        var titleTr = new Point3d(W - M, M + 50, 0);
        scope.DrawRectangle(titleBl, titleTr, layer: "TITLEBLOCK");

        scope.DrawMText("CadCoreFramework\\P\\PRev: 0 — 2026",
            new Point3d(titleBl.X + 5, titleBl.Y + 25, 0),
            textHeight: 4, layer: "TITLEBLOCK");
    }, undoMarkName: "TitleBlock A3");
}
```

---

## 8.2 Batch export tất cả Layout ra PDF

```csharp
[CommandMethod("PLOT_PDF_BATCH")]
public void PlotPdfBatch()
{
    // Lấy danh sách layout TRƯỚC — plotting phải ngoài Transaction.
    List<string> layoutNames = _cadContext.Execute(scope =>
    {
        var dict = (DBDictionary)scope.Transaction.GetObject(
            scope.Database.LayoutDictionaryId, OpenMode.ForRead);
        return dict.Cast<DictionaryEntry>()
                   .Select(e => (string)e.Key)
                   .Where(n => !string.Equals(n, "Model", StringComparison.OrdinalIgnoreCase))
                   .ToList();
    }).Value;

    string outDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PDF_Export");
    Directory.CreateDirectory(outDir);

    foreach (var layout in layoutNames)
    {
        string pdfPath = Path.Combine(outDir, $"{layout}.pdf");
        // PlotService wraps PlotEngine + PlotProgressDialog — see Services/PlotService.cs
        _plotService.PlotLayoutToPdf(layout, pdfPath);
        Editor?.WriteMessage($"\n[PDF] {layout} → {pdfPath}");
    }
}
```

---

## 8.3 Purge all + Audit (dọn dẹp bản vẽ)

```csharp
[CommandMethod("PURGE_AUDIT_ALL")]
public void PurgeAndAudit()
{
    ExecuteWithScope(scope =>
    {
        var db = scope.Database;

        // Lặp Purge cho đến khi không còn gì để xóa (một lần Purge có thể
        // giải phóng dependencies cho lượt kế tiếp).
        ObjectIdCollection purgable;
        int pass = 0;
        do
        {
            purgable = new ObjectIdCollection();
            foreach (ObjectId id in GetAllPurgeableIds(db))
                purgable.Add(id);

            db.Purge(purgable);
            foreach (ObjectId id in purgable)
            {
                var obj = scope.Transaction.GetObject(id, OpenMode.ForWrite);
                obj.Erase();
            }
            pass++;
        } while (purgable.Count > 0 && pass < 10);

        // Audit sau khi purge xong để tự sửa lỗi database.
        db.Audit(new AuditInfo { FixErrors = true, DialogSuppressed = true });
    }, undoMarkName: "Purge+Audit");
}

private static ObjectIdCollection GetAllPurgeableIds(Database db)
{
    var ids = new ObjectIdCollection();
    using (var tr = db.TransactionManager.StartTransaction())
    {
        foreach (var tableId in new[] { db.LayerTableId, db.LinetypeTableId,
                                        db.TextStyleTableId, db.BlockTableId })
        {
            var table = (SymbolTable)tr.GetObject(tableId, OpenMode.ForRead);
            foreach (ObjectId id in table) ids.Add(id);
        }
        tr.Commit();
    }
    return ids;
}
```

---

## 8.4 Attach XRef với đường dẫn tuyệt đối (không gãy khi đổi vị trí)

```csharp
public ObjectId AttachXref(string dwgPath, Point3d insertPoint)
{
    return _cadContext.Execute(scope =>
    {
        string absolutePath = Path.GetFullPath(dwgPath);
        string xrefName = Path.GetFileNameWithoutExtension(absolutePath);

        // AttachXref returns the BlockTableRecord Id of the xref definition.
        ObjectId xrefId = scope.Database.AttachXref(absolutePath, xrefName);
        if (xrefId.IsNull)
        {
            throw new InvalidOperationException($"Không attach được xref: {absolutePath}");
        }

        // Insert the xref reference into Model Space.
        var xrefRef = new BlockReference(insertPoint, xrefId);
        return scope.AppendEntityToCurrentSpace(xrefRef);
    }, autoRegen: true, undoMarkName: "Attach XRef").Value;
}
```

---

## 8.5 Jig vẽ Polyline tự do bằng chuột

```csharp
public class FreePolylineJig : CadDrawJigBase
{
    private readonly Polyline _pline = new Polyline();
    private Point3d _mousePoint;

    public FreePolylineJig(Point3d startPoint)
    {
        _pline.AddVertexAt(0, new Point2d(startPoint.X, startPoint.Y), 0, 0, 0);
    }

    protected override SamplerStatus Sampler(JigPrompts prompts)
    {
        var opts = new JigPromptPointOptions("\nĐiểm kế (ESC để kết thúc): ")
        {
            BasePoint = _pline.GetPoint3dAt(_pline.NumberOfVertices - 1),
            UseBasePoint = true
        };
        var res = prompts.AcquirePoint(opts);
        if (res.Status != PromptStatus.OK) return SamplerStatus.Cancel;
        if (res.Value.DistanceTo(_mousePoint) < Tolerance.Global.EqualPoint)
            return SamplerStatus.NoChange;

        _mousePoint = res.Value;
        return SamplerStatus.OK;
    }

    protected override bool WorldDraw(WorldDraw draw)
    {
        var temp = _pline.Clone() as Polyline;
        temp.AddVertexAt(temp.NumberOfVertices,
            new Point2d(_mousePoint.X, _mousePoint.Y), 0, 0, 0);
        draw.Geometry.Draw(temp);
        return true;
    }

    public Polyline FinalPolyline => _pline;
}

[CommandMethod("DRAW_FREE_POLY")]
public void DrawFreePoly()
{
    var startRes = Editor.GetPoint("\nĐiểm đầu: ");
    if (startRes.Status != PromptStatus.OK) return;

    var jig = new FreePolylineJig(startRes.Value);
    while (Editor.Drag(jig).Status == PromptStatus.OK) { }

    ExecuteWithScope(scope =>
    {
        scope.AppendEntityToCurrentSpace(jig.FinalPolyline);
    }, undoMarkName: "Free Polyline");
}
```

---

## 8.6 Cập nhật thuộc tính Dynamic Block hàng loạt

```csharp
public void UpdatePanelLabels(Dictionary<ObjectId, string> labelMap)
{
    _cadContext.Execute(scope =>
    {
        foreach (var kvp in labelMap)
        {
            var blockRef = scope.Transaction.GetObject(kvp.Key, OpenMode.ForWrite) as BlockReference;
            if (blockRef == null) continue;

            // Cập nhật ATTRIBUTE (text visible trong block).
            foreach (ObjectId attId in blockRef.AttributeCollection)
            {
                var att = (AttributeReference)scope.Transaction.GetObject(attId, OpenMode.ForWrite);
                if (string.Equals(att.Tag, "LABEL", StringComparison.OrdinalIgnoreCase))
                {
                    att.TextString = kvp.Value;
                }
            }

            // Cập nhật Dynamic Property (ví dụ Visibility State).
            DynamicBlockHelper.SetVisibilityState(blockRef, "ShowLabel");
        }
    }, undoMarkName: $"Update {labelMap.Count} panels");
}
```

---

## 8.7 Merge nhiều Layer thành một

```csharp
public void MergeLayers(IEnumerable<string> sourceLayers, string targetLayer)
{
    _cadContext.Execute(scope =>
    {
        scope.EnsureLayer(targetLayer);
        var layerTable = (LayerTable)scope.Transaction.GetObject(
            scope.Database.LayerTableId, OpenMode.ForWrite);

        // Phase 1: di chuyển mọi entity từ layer nguồn sang layer đích.
        var btr = (BlockTableRecord)scope.Transaction.GetObject(
            scope.Database.CurrentSpaceId, OpenMode.ForRead);

        foreach (ObjectId id in btr)
        {
            var ent = scope.Transaction.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent == null) continue;
            if (sourceLayers.Contains(ent.Layer, StringComparer.OrdinalIgnoreCase))
            {
                ent.UpgradeOpen();
                ent.Layer = targetLayer;
            }
        }

        // Phase 2: xóa layer nguồn (giờ đã rỗng).
        foreach (var layerName in sourceLayers)
        {
            if (!layerTable.Has(layerName)) continue;
            var rec = (LayerTableRecord)scope.Transaction.GetObject(
                layerTable[layerName], OpenMode.ForWrite);
            if (!rec.IsErased) rec.Erase();
        }
    }, undoMarkName: $"Merge → {targetLayer}");
}
```

---

## 8.8 Selection Filter — các pattern thường dùng

```csharp
// Chỉ chọn Block Reference có tên "PANEL" trên layer E_PANEL
var filterPanels = SelectionFilterBuilder.Create()
    .OfTypes("INSERT")
    .WithBlockNames("PANEL")
    .OnLayer("E_PANEL")
    .Build();

// Chọn mọi Line + LWPolyline trên bất kỳ layer E_* nào
var filterWires = SelectionFilterBuilder.Create()
    .OfTypes("LINE,LWPOLYLINE")
    .OnLayer("E_*")           // wildcard — AutoCAD hỗ trợ * và ?
    .Build();

// Chọn các entity có XData gắn với appName "MY_PLUGIN"
var filterTagged = SelectionFilterBuilder.Create()
    .WithXData("MY_PLUGIN")
    .Build();

// Dùng filter:
var res = Editor.GetSelection(filterPanels);
if (res.Status != PromptStatus.OK) return;
foreach (ObjectId id in res.Value.GetObjectIds()) { /* ... */ }
```

---

## 8.9 Round-trip metadata JSON qua XData (có validate size)

```csharp
public record PanelMeta(string Name, double Width, double Height, DateTime Installed);

public void SavePanelMeta(ObjectId panelId, PanelMeta meta)
{
    _cadContext.Execute(scope =>
    {
        var ent = (Entity)scope.Transaction.GetObject(panelId, OpenMode.ForWrite);
        // XDataHelper throws if JSON > 15000 chars (XData 16KB hard limit).
        ent.SetMetadata(scope.Transaction, "MY_PANELS", meta);
    });
}

public PanelMeta? LoadPanelMeta(ObjectId panelId)
{
    return _cadContext.Execute(scope =>
    {
        var ent = (Entity)scope.Transaction.GetObject(panelId, OpenMode.ForRead);
        return ent.GetMetadata<PanelMeta>("MY_PANELS");
    }).Value;
}
```

Nếu JSON > 15KB (ví dụ: lưu đường cong lineage chi tiết), dùng
`XRecordHelper` thay vì `XDataHelper` — XRecord không có giới hạn kích thước
và lưu dưới `ExtensionDictionary` của entity.

---

## 8.10 Subscription với auto-Dispose (tránh memory leak)

```csharp
public partial class LivePanelViewModel : ViewModelBase, IDisposable
{
    private readonly IEventAggregator _bus;
    private readonly IDisposable? _subscription;

    public LivePanelViewModel(IEventAggregator bus)
    {
        _bus = bus;
        // Subscribe trả về IDisposable — LƯU LẠI để Dispose khi ViewModel chết.
        _subscription = _bus.Subscribe<PanelUpdated>(OnPanelUpdated);
    }

    private void OnPanelUpdated(PanelUpdated msg)
    {
        RunSafe(() =>
        {
            // Cập nhật UI từ event.
        });
    }

    public void Dispose() => _subscription?.Dispose();
}
```

> [!IMPORTANT]
> `PaletteService` tự gọi `Dispose()` trên ViewModel khi palette đóng — chỉ
> cần implement `IDisposable` đúng cách. **Không được "forget to Dispose":**
> subscription giữ strong reference, VM sẽ không bao giờ được GC và event
> handler sẽ chạy mãi cho đến khi AutoCAD đóng.

---

## 8.11 Chuyển một thao tác nặng ra async (giữ UI responsive)

```csharp
[CommandMethod("GENERATE_REPORT")]
public void GenerateReport()
{
    _ = GenerateReportAsync();   // fire-and-forget (được FireAndForget bọc try-catch)
}

private async Task GenerateReportAsync()
{
    await _cadContext.ExecuteAsync(async () =>
    {
        // Phase 1 — nặng, chạy trên thread pool. KHÔNG đụng AutoCAD API.
        var data = await _httpClient.GetFromJsonAsync<ReportData>(
            "https://api.example.com/reports/current");

        // Phase 2 — Action sẽ chạy trong Transaction (lock + commit tự động).
        return scope =>
        {
            foreach (var row in data!.Rows)
            {
                scope.DrawText(row.Label, new Point3d(0, row.Y, 0),
                    height: 2.5, layer: "REPORT");
            }
        };
    }, undoMarkName: "Generate Report");
}
```

---

## Mẹo chung

- **Luôn** gom nhiều thao tác ghi vào **MỘT** `Execute()` — mỗi `Execute` là
  1 transaction + 1 lock + 1 undo mark. Gọi 1000 lần cho 1000 entities thì
  AutoCAD sẽ đứng hình.
- Khi vẽ hàng loạt (1000+ entities), dùng **`ExecuteBatch`** để tận dụng
  `IProgress<int>` báo tiến trình.
- Prompt (`GetPoint`, `GetEntity`) **ngoài** `Execute()` nếu gọi từ palette
  context — xem Phần 3 để biết chi tiết application vs command context.
- Khi doubt: xem log tại `%appdata%\CadCoreFramework\Logs\log-yyyyMMdd.txt` — `LoggerService`
  ghi lại mọi `ILoggerService.Error()` và `ExecuteCommand`-level exception (giữ 31 ngày).
