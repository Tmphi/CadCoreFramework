> **📘 Tra cứu CadCoreFramework** · [Mục lục tài liệu](../README.md) · [← R05](R05_GOLDEN_SAMPLE.md) · **R06**

# R06 — Chuẩn Mực: Vòng Lặp Phát Triển, Test, CI Và Quy Ước

> **Dùng khi:** chuẩn bị build/test/mở PR, gặp cảnh báo CAD001–CAD003, hoặc cần nhớ quy ước đặt tên · **Học bài bản:** [T05 — Kiểm thử](../training/T05_TESTING_STRATEGY.md), [T06 — Build & phát hành](../training/T06_BUILD_RELEASE_DEVOPS.md), [T09 §2 — Checklist code review](../training/T09_SENIOR_PLAYBOOK.md#2-checklist-code-review) · **Gặp sự cố:** [T04 — Debug & chẩn đoán](../training/T04_DEBUGGING_TROUBLESHOOTING.md), [T03 — Sổ tay sự cố](../training/T03_PITFALLS_CASEBOOK.md)

---

## Mục lục

1. [Nạp lại plugin không cần tắt AutoCAD](#1-nạp-lại-plugin-không-cần-tắt-autocad)
2. [Unit test](#2-unit-test)
3. [CI/CD](#3-cicd)
4. [Quy ước viết code](#4-quy-ước-viết-code)
5. [Analyzer CAD001–CAD003](#5-analyzer-cad001cad003)

---

## 1. Nạp lại plugin không cần tắt AutoCAD

**Vấn đề:** .NET Framework **không gỡ (unload) được** assembly khỏi AppDomain. Khi bạn `NETLOAD` lại một DLL **cùng identity** (cùng tên assembly + version), CLR trả về chính assembly **đã nạp từ trước**: code cũ, BAML (XAML đã biên dịch) cũ. Ngoài ra, DLL nạp bằng `NETLOAD` bị khóa nên không ghi đè được khi build. Kết quả là bạn sửa XAML, build lại, nạp lại mà vẫn thấy giao diện cũ, hoặc gặp lỗi lệch kiểu giữa hai bản.

**Cách của dự án** (cấu hình trong [`CadCoreFramework.csproj`](../../CadCoreFramework.csproj)):
1. Build Debug đặt `AssemblyName` kèm thời điểm build: `CadCoreFramework_20261010_120000.dll`.
2. Target `PostBuild` copy file đó về tên chuẩn `CadCoreFramework.dll` để script `NETLOAD` vẫn dùng được.
3. Mỗi lần build là một **identity mới**, nên `NETLOAD` nạp đúng code + BAML mới mà không cần tắt AutoCAD.

> [!WARNING]
> Các bản cũ **vẫn nằm trong AppDomain** (event đã hook, `Initialize` đã chạy). Khi sửa code khởi động, event, Overrule, Ribbon, hoặc khi thấy hành vi lạ, hãy khởi động lại AutoCAD ([T04 §2](../training/T04_DEBUGGING_TROUBLESHOOTING.md#2-vòng-lặp-phát-triển-build--nạp--thử)). Hệ quả kỹ thuật: pack URI của WPF phải dựng tên assembly **lúc runtime** ([T02 §2.1](../training/T02_ARCHITECTURE_DEEP_DIVE.md#21-hot-reload-tên-assembly-có-timestamp)).

---

## 2. Unit test

Project test chạy ngoài AutoCAD, nên không gọi được `Application.DocumentManager` hay mở database thật. Chiến lược của dự án ([T05 §2](../training/T05_TESTING_STRATEGY.md#2-kim-tự-tháp-test-của-dự-án)):

| Tầng | Test bằng | Kiểm gì |
| :--- | :--- | :--- |
| Model, Planner (logic thuần) | xUnit thuần | **Kết quả** |
| Service (vỏ AutoCAD) | [`FakeCadContext`](../../CadCoreFramework.Testing/Fakes/FakeCadContext.cs) với `ExecuteAction = false` | **Hợp đồng**: gọi overload nào, bao nhiêu lần, undo mark nào, xử lý lỗi ra sao |
| ViewModel | Mock service (Moq) | `CanExecute`, trạng thái, lỗi |
| XAML palette | `XamlPaletteTests` trên thread STA | Nạp được, key theme tồn tại, binding ô số |
| Chỉ AutoCAD thật mới kiểm được | Smoke test thủ công, HeadlessLab | [T05 §7](../training/T05_TESTING_STRATEGY.md#7-những-gì-chỉ-autocad-thật-mới-kiểm-được) |

`FakeCadContext` ghi lại mọi lời gọi: `ExecuteCount` (đếm cả `Execute` lẫn `TryExecute`), `ExecuteReadCount`, `ExecuteAsyncCount`, `ExecuteBatchCount`, `LastUndoMarkName`, `LastAutoRegen`. Gán `ExceptionToThrow` để giả lập lỗi AutoCAD. Hai test có thật trong [`LayerServiceTests.cs`](../../CadCoreFramework.Tests/LayerServiceTests.cs), nguyên văn:

```csharp
[Fact]
public void ToggleLayerVisibility_UsesSingleTryExecute_WithUndoMarkAndRegen()
{
    var fake = new FakeCadContext { ExecuteAction = false };

    NewService(fake).ToggleLayerVisibility("E_LAYER", isOn: true);

    fake.ExecuteCount.Should().Be(1);
    fake.ExecuteAsyncCount.Should().Be(0);
    fake.LastUndoMarkName.Should().Be(LayerService.ToggleUndoMarkName);
    fake.LastAutoRegen.Should().BeTrue();
}

[Fact]
public void ToggleLayerVisibility_WhenCadFails_ReturnsFailure_DoesNotThrow()
{
    var fake = new FakeCadContext
    {
        ExecuteAction = false,
        ExceptionToThrow = new InvalidOperationException("Layer locked")
    };

    var result = NewService(fake).ToggleLayerVisibility("E", true);

    result.IsSuccess.Should().BeFalse();
    result.ErrorMessage.Should().Be("Layer locked");
}
```

`ExecuteAction = false` vì lambda bên trong chạm `scope.Database`, thứ chỉ có trong AutoCAD thật. Đó cũng là giới hạn của kiểu test này: nó không biết lambda làm đúng hay sai. Muốn kiểm được kết quả, đưa logic ra hàm thuần ([R05 §2](R05_GOLDEN_SAMPLE.md#2-nguyên-tắc-1--functional-core-imperative-shell)).

**Chạy test:**

```powershell
# Toàn bộ, cả hai runtime (net48 và net8.0-windows)
dotnet test CadCoreFramework.Tests/CadCoreFramework.Tests.csproj

# Một nhóm test
dotnet test CadCoreFramework.Tests/CadCoreFramework.Tests.csproj --filter "FullyQualifiedName~GridMarker"
```

Coverage và báo cáo HTML (giống CI): [T05 §8](../training/T05_TESTING_STRATEGY.md#8-chạy-test-và-coverage). Quy tắc viết test (tên, một hành vi mỗi test, test phải fail được): [T05 §10](../training/T05_TESTING_STRATEGY.md#10-quy-tắc-viết-test).

---

## 3. CI/CD

Hai workflow trong [`.github/workflows/`](../../.github/workflows/):

| File | Kích hoạt | Làm gì |
| :--- | :--- | :--- |
| `build-and-test.yml` | Push/PR vào `main` | Build Release `-warnaserror` → chạy toàn bộ unit test trên net48 + net8 → thu thập coverage → upload báo cáo HTML |
| `release.yml` | Tag `v*` (ví dụ `v1.2.0`) | Build → Test → `package.ps1` → tạo GitHub Release với `.bundle` đính kèm |

Vì CI build với `-warnaserror`, mọi warning (kể cả CAD001–CAD003 ở §5) làm PR đỏ. Chi tiết từng bước, quy trình release và checklist phát hành: [T06 §4–5](../training/T06_BUILD_RELEASE_DEVOPS.md#4-ci-build-and-testyml). Badge CI của dự án tạo từ template: [T06 §8](../training/T06_BUILD_RELEASE_DEVOPS.md#8-template-tạo-dự-án-mới).

---

## 4. Quy ước viết code

**Đặt tên:**

| Loại | Kiểu | Tiền tố / hậu tố | Ví dụ |
| :--- | :--- | :--- | :--- |
| Class, method, property | `PascalCase` | — | `CadContext`, `DrawRectangle`, `IsBusy` |
| Interface | `PascalCase` | `I` | `ICadScope`, `ILoggerService` |
| Tham số, biến cục bộ | `camelCase` | — | `layerName`, `radius` |
| Field private | `camelCase` | `_` | `_cadContext`, `_logger` |
| Hàm async | `PascalCase` | hậu tố `Async` | `ExecuteAsync`, `LoadDataAsync` |
| Lệnh AutoCAD | `UPPER_SNAKE` | tiền tố riêng của tính năng | `SAMPLE_GRID`, `CADCORE_LAYER_MANAGER` |

**Thứ tự trong một class:** fields (`private readonly …`) → constructors → properties → public methods → private methods.

**Ngôn ngữ:** C# 10 (`<LangVersion>10.0</LangVersion>`), `Nullable=annotations`, không bật `ImplicitUsings`: mỗi file tự khai báo `using`. Định dạng theo [`.editorconfig`](../../.editorconfig).

**Kiến trúc:**
- **Prompt** thuộc về **lệnh** (`[CommandMethod]` trong `ExecuteCommand`), làm *trước* khi mở transaction. **Không bao giờ** prompt trong Service hay ViewModel, vì Service còn được gọi từ palette (application context). **Transaction** đi qua `ICadContext` bên trong Service ([R05 §4](R05_GOLDEN_SAMPLE.md#4-nguyên-tắc-3--hiểu-đúng-ngữ-cảnh-command-vs-palette)).
- ViewModel (kể cả hàm command của nó) chỉ chuẩn bị dữ liệu và gọi Service; không có logic AutoCAD.
- **Không bắt `System.Exception` tràn lan** trong code nghiệp vụ: bắt kiểu cụ thể khi bạn *xử lý được* nó. Bắt mọi lỗi chỉ làm ở **biên** (`ExecuteCommand`, `RunSafe`). Framework xử lý riêng `Autodesk.AutoCAD.Runtime.Exception` có `ErrorStatus.UserBreak` (ESC) để không in lỗi rác; không có kiểu nào tên `UserBreakException`. ESC *khi đang prompt* không ném exception mà trả `PromptStatus.Cancel`.

Checklist review đầy đủ: [T09 §2](../training/T09_SENIOR_PLAYBOOK.md#2-checklist-code-review). Checklist trước khi mở PR: [T08 §13](../training/T08_RECIPES_EXTENDING.md#13-checklist-trước-khi-mở-pr).

---

## 5. Analyzer CAD001–CAD003

🔎 [`CadCoreFramework.Analyzers/`](../../CadCoreFramework.Analyzers/) · test: [`AnalyzerTests.cs`](../../CadCoreFramework.Tests/AnalyzerTests.cs) · thiết kế: [T02 §11](../training/T02_ARCHITECTURE_DEEP_DIVE.md#11-analyzer-cad001cad003), [T09 ADR-011](../training/T09_SENIOR_PLAYBOOK.md#1-adr-vì-sao-kiến-trúc-như-hiện-tại)

Ba quy tắc Roslyn chạy **lúc build**, báo warning (thành lỗi trên CI vì `-warnaserror`). Analyzer chỉ được tham chiếu để phân tích, không đóng gói vào DLL plugin. Liên kết "help" của mỗi cảnh báo trong Visual Studio trỏ về mục tương ứng dưới đây.

"Service/ViewModel" ở CAD001 và CAD002 nghĩa là: class trong namespace chứa `.Services` / `.ViewModels`, hoặc tên kết thúc bằng `Service` / `ViewModel` (xét cả lớp bao ngoài); bỏ qua `CadContext` và `AppBootstrapper` ([`ArchitectureLayers.cs`](../../CadCoreFramework.Analyzers/ArchitectureLayers.cs)).

### CAD001

**Service/ViewModel không được dùng `MdiActiveDocument`.**

```csharp
// ⛔ CAD001
var doc = Application.DocumentManager.MdiActiveDocument;

// ✅ Inject ICadContext, lấy Document từ scope
_cadContext.Execute(scope => { var doc = scope.Document; /* … */ });
```

Vì sao: bản vẽ active đổi khi người dùng chuyển tab (vẽ nhầm file); không có lock tự động (`eLockViolation`); và `FakeCadContext` không giả lập được `MdiActiveDocument`, nên Service hết test được. Nền tảng: [T01 §2.1](../training/T01_AUTOCAD_NET_FOUNDATIONS.md#21-mdiactivedocument-tiện-nhưng-nguy-hiểm).

### CAD002

**Service/ViewModel không được hỏi người dùng qua dòng lệnh.** Bắt các lời gọi `CadPromptHelper.*` và `Editor.GetPoint/GetCorner/GetDistance/GetAngle/GetString/GetKeywords/GetInteger/GetDouble/GetEntity/GetNestedEntity/GetSelection/GetFileNameForOpen/GetFileNameForSave/Drag`.

Sửa: lệnh hỏi người dùng, rồi truyền kết quả (`Point3d`, `GridSpec`…) vào Service; ViewModel nhận dữ liệu qua binding. Vì sao: prompt trong Service khiến nó không dùng được từ palette, batch hay unit test ([R05 §4](R05_GOLDEN_SAMPLE.md#4-nguyên-tắc-3--hiểu-đúng-ngữ-cảnh-command-vs-palette), backlog B-18).

### CAD003

**Không mở đối tượng để ghi bên trong `ExecuteRead`.** Bắt `OpenMode.ForWrite` và `UpgradeOpen()` trong lambda (kể cả lambda lồng) truyền cho `ExecuteRead` của một `*CadContext`.

Sửa: dùng `Execute`/`TryExecute` cho thao tác ghi. Vì sao: transaction của `ExecuteRead` được **Commit**, nên ghi trong đó *sẽ* vào bản vẽ, ngoài undo mark và ngoài lock của `Execute` ([R02 §2](R02_ARCHITECTURE.md#2-hợp-đồng-của-icadcontext), backlog B-07).

**Giới hạn chung:** analyzer chỉ thấy lời gọi **trực tiếp**. Nó không theo vào method được gọi từ đó (Service gọi một helper mà helper prompt thì lọt), và CAD003 không xét method group (`ExecuteRead(DocRead)`). Ngoại lệ có chủ đích được đánh dấu bằng `#pragma warning disable CADxxx` kèm comment giải thích (ví dụ CAD001 trong `NotificationService`).
