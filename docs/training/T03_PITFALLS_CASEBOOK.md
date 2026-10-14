> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← T02](T02_ARCHITECTURE_DEEP_DIVE.md) · **T03** · [T04 →](T04_DEBUGGING_TROUBLESHOOTING.md)

# T03 — Sổ Tay Sự Cố & Bẫy (Casebook)

> **Cấp độ:** 🌿 Junior → 🏔️ Senior · **Cần trước:** [T01](T01_AUTOCAD_NET_FOUNDATIONS.md), [T02](T02_ARCHITECTURE_DEEP_DIVE.md) · **Trong lộ trình:** tuần 10 (Phần B) và 11 (Phần A); sau đó dùng như sổ tra cứu ([README §3](README.md#3-lộ-trình-đào-tạo))

Kinh nghiệm của Senior phần lớn là **kho sự cố đã từng gặp**. Module này chuyển kho đó thành tài liệu:
- **Phần A:** 16 sự cố **thật** đã xảy ra với chính dự án này. Mỗi mục có bằng chứng và cách tái hiện.
- **Phần B:** 36 bẫy AutoCAD .NET kinh điển, mỗi bẫy dẫn về chỗ framework đã (hoặc chưa) phòng tránh. B32–B36 được phát hiện và kiểm chứng khi viết [nhánh UI](U00_UI_START_HERE.md).

Mỗi mục theo cùng một format, và đó cũng là format bạn phải dùng khi viết báo cáo sự cố ([T04 §8](T04_DEBUGGING_TROUBLESHOOTING.md#8-mẫu-báo-cáo-sự-cố-incident-report)):

| Mục | Câu hỏi |
| :--- | :--- |
| **Triệu chứng** | Người dùng / dev thấy gì? (thông điệp nguyên văn) |
| **Gốc rễ** | Cơ chế thật sự gây ra nó |
| **Phát hiện** | Làm sao biết đúng là nguyên nhân này |
| **Sửa** | Thay đổi đã làm (hoặc nên làm) |
| **Phòng ngừa** | Cơ chế để không tái diễn (test, analyzer, quy tắc) |

---

## Phần A — Sự cố thật của dự án

### A1. Serilog: MissingMethodException ngay khi khởi động

- **Triệu chứng:** Lần chạy thật đầu tiên trên AutoCAD 2024, plugin không khởi tạo được: `MissingMethodException` trong `Serilog.Sinks.File`.
- **Gốc rễ:** AutoCAD 2024 **tự mang theo và nạp trước** Serilog 2.9 / Serilog.Sinks.File 4.1. Serilog giữ `AssemblyVersion = 2.0.0.0` cho mọi bản 2.x/3.x, nên identity của bản plugin cần (3.x) **trùng** bản AutoCAD đã nạp. CLR trả về bản của AutoCAD, và code plugin gọi một method chỉ có ở 3.x.
- **Phát hiện:** So `AssemblyVersion` của DLL trong `bin` với DLL cùng tên trong `C:\Program Files\Autodesk\AutoCAD 2024\`.
- **Sửa:** Gỡ Serilog. [`LoggerService`](../../Core/LoggerService.cs) là logger tự viết, không phụ thuộc thư viện ngoài, giữ nguyên định dạng file log cũ.
- **Phòng ngừa:** **Chính sách dependency** ([T06 §7](T06_BUILD_RELEASE_DEVOPS.md#7-chính-sách-dependency)): trước khi thêm NuGet, kiểm tra thư mục AutoCAD có DLL cùng `AssemblyVersion` không. Bài học tổng quát: plugin sống chung AppDomain với host ([T01 §1](T01_AUTOCAD_NET_FOUNDATIONS.md#1-plugin-sống-ở-đâu-autocad-net-và-appdomain-dùng-chung)).

### A2. Palette lỗi "does not have a resource identified by the URI"

- **Triệu chứng:** Palette không mở: *The component '…' does not have a resource identified by the URI '…'*. Chỉ xảy ra khi nạp plugin bằng tool nạp từ **byte array** (chống khóa DLL / hot-reload).
- **Gốc rễ:** Khi `InitializeComponent()` chạy, WPF tìm assembly của plugin **theo tên** để lấy BAML. Assembly nạp từ byte array nằm ngoài mọi load context, nên CLR binder không thấy nó. Một handler khác nạp **bản thứ hai** từ đĩa. Kiểu `UserControl` (bản 1) và BAML (bản 2) không khớp nhau.
- **Phát hiện:** Liệt kê `AppDomain.CurrentDomain.GetAssemblies()` thấy hai assembly cùng tên.
- **Sửa:** [`AppBootstrapper.FindLoadedAssembly`](../../Core/AppBootstrapper.cs): `AssemblyResolve` trả về **assembly đã nạp** khớp tên + version trước mọi cách khác. Có test trong `AssemblyResolveTests`.
- **Phòng ngừa:** Test hành vi resolve. Hiểu rằng `Assembly.Location` **rỗng** khi nạp từ byte array (đây cũng là gốc của B-11).

### A3. SHOW_UI_TEMPLATE lỗi "Provide value on StaticResourceExtension"

- **Triệu chứng:** `SHOW_UI_TEMPLATE` báo *Provide value on 'System.Windows.StaticResourceExtension' threw an exception*.
- **Gốc rễ:** `CommonControlsPalette` tham chiếu 33 key của `Styles.xaml` bằng `{StaticResource}`. Nhưng `Styles.xaml` được merge **trong code-behind, sau** `InitializeComponent()`. `StaticResource` phân giải lúc parse XAML, khi đó key chưa tồn tại.
- **Sửa:** Đổi sang `{DynamicResource}`, nạp style ngay sau `InitializeComponent()`.
- **Phòng ngừa:** [`XamlPaletteTests`](../../CadCoreFramework.Tests/XamlPaletteTests.cs) dựng **thật** từng palette trên STA thread lúc chạy test. Test này đã được xác nhận là tái hiện đúng lỗi cũ trên markup cũ.

### A4. Bản net8 thiếu toàn bộ DLL phụ thuộc

- **Triệu chứng:** Output `net8.0-windows` chỉ có `CadCoreFramework.dll`, không có DI / MVVM Toolkit / Newtonsoft, nên chắc chắn sẽ lỗi NETLOAD trên AutoCAD 2025+.
- **Gốc rễ:** Class library .NET (Core) mặc định **không copy** dependency NuGet ra output.
- **Sửa:** `<EnableDynamicLoading>true</EnableDynamicLoading>` cho TFM `net8.0-windows` ([csproj](../../CadCoreFramework.csproj)). Đây là cấu hình Microsoft khuyến nghị cho plugin nạp động. DLL AutoCAD vẫn không bị copy nhờ `ExcludeAssets="runtime"`.
- **Phòng ngừa:** Kiểm tra nội dung thư mục output của từng TFM khi đổi csproj. (net8 vẫn **chưa chạy thật** trên AutoCAD 2025, xem B-21.)

### A5. accoreconsole crash 0xC00000FD khi NETLOAD plugin

- **Triệu chứng:** `accoreconsole.exe` thoát với mã `-1073741571` (`0xC00000FD`) ngay sau dòng `NETLOAD`, không có thông báo. Event Viewer ghi *Faulting module: clr.dll, Exception code: 0xc00000fd*.
- **Gốc rễ:** `0xC00000FD` là **stack overflow**. Plugin tham chiếu `AcMgd.dll` (UI), và chính `AcMgd.dll` không nạp được trong môi trường không giao diện.
- **Phát hiện:** Thu hẹp dần. Trace tạm thời cho thấy crash xảy ra *trước* `Initialize()`. Sau đó NETLOAD **riêng `acmgd.dll`** cũng crash y hệt, tức kết luận độc lập với code plugin.
- **Sửa:** Không sửa được trong kiến trúc một DLL. Runner integration trả **exit code 3** kèm giải thích, và README ghi rõ giới hạn.
- **Phòng ngừa:** Muốn test headless thì phần được test phải không tham chiếu `AcMgd`. Dự án làm điều đó **mà vẫn phát hành một DLL**: [HeadlessLab](../../tests/integration/README.md#headlesslab--kiểm-thử-headless-trên-autocad-thật) link lại mã không-UI vào một DLL chỉ dành cho kiểm thử (PR #6, B-20, ADR-015). Bài học phương pháp: **cô lập biến số** (bỏ plugin ra khỏi thí nghiệm) trước khi kết luận.

### A6. Integration test báo "passed" dù không lệnh nào chạy

- **Triệu chứng:** `run-integration-tests.ps1` luôn in *Integration smoke tests passed*. Thực tế plugin chưa từng nạp được trong accoreconsole.
- **Gốc rễ:** **Năm lỗi chồng lên nhau**:
  1. Output accoreconsole là **UTF-16**, runner đọc như ANSI ra `C o m m a n d`. Không marker lỗi nào khớp được.
  2. Comment `;` trong `smoke.scr` bị gửi thẳng vào dòng lệnh.
  3. **Dòng trống = Enter = lặp lại lệnh trước**: `NETLOAD` chạy lại và "nuốt" tên các lệnh phía sau làm tên file.
  4. Đường dẫn DLL bị nhân đôi `\\`, nên NETLOAD báo *Unable to load*.
  5. Khi máy chỉ có **một** bản AutoCAD, pipeline PowerShell trả về *chuỗi* thay vì mảng, và `$candidates[-1]` lấy **ký tự cuối** của đường dẫn.
- **Sửa:** Đặt `[Console]::OutputEncoding = Unicode`; `smoke.scr` không comment, không dòng trống; bọc kết quả bằng `@(...)`. Quan trọng nhất: **mỗi lệnh phải in ra thông điệp thành công riêng**. Vắng thông điệp đó là FAIL.
- **Phòng ngừa:** Nguyên tắc **"test phải chứng minh được nó đã chạy"**. Một test chỉ quét lỗi mà không xác nhận thành công thì luôn có thể pass giả.

### A7. init-template.ps1 không parse được trên Windows PowerShell 5.1

- **Triệu chứng:** `Missing closing '}'` / *The string is missing the terminator*, dù script khai báo `#Requires -Version 5.1`.
- **Gốc rễ:** File UTF-8 **không BOM**. PowerShell 5.1 đọc theo ANSI (cp1252). Ký tự `—` (E2 80 **94**) chứa byte `0x94`, mà trong cp1252 đó là dấu ngoặc kép cong `"`, nên chuỗi bị cắt ngang.
- **Sửa:** Thêm BOM cho mọi `.ps1` có ký tự ngoài ASCII. `.editorconfig` có `charset = utf-8-bom` cho `*.ps1`, kèm comment giải thích.
- **Phòng ngừa:** Parse-check mọi script bằng `[System.Management.Automation.Language.Parser]::ParseFile` khi review.

### A8. MSB3270 làm build `-warnaserror` thất bại

- **Triệu chứng:** *There was a mismatch between the processor architecture of the project being built "MSIL" and … AcSeamless.dll "AMD64"*.
- **Gốc rễ:** Project `Tests` / `Testing` build **AnyCPU** nhưng tham chiếu DLL AutoCAD **x64**.
- **Sửa:** `<PlatformTarget>x64</PlatformTarget>` cho hai project test, giống plugin.
- **Phòng ngừa:** CI build với `-warnaserror` (một warning mới cũng làm CI đỏ).

### A9. `dotnet new cad-plugin -n MyPlugin` sinh ra `Author.csproj`

- **Gốc rễ:** Symbol `AuthorName` trong `.template.config/template.json` thay thế **cùng chuỗi** `CadCoreFramework` với `sourceName`. Thêm vào đó, danh sách `guids` thiếu GUID của project Analyzers/Benchmarks, nên mọi dự án sinh ra dùng chung các GUID này.
- **Sửa:** Bỏ symbol lỗi, thêm `--GitHubRepo`, `--IncludeDemos`, bổ sung GUID, loại file rác.
- **Phòng ngừa:** Test template trong **hive cô lập**: `dotnet new install . --debug:custom-hive <thư mục>` (không đụng danh sách template thật của máy), rồi build và test dự án sinh ra.

### A10. Release workflow lỗi 403 "Resource not accessible by integration"

- **Triệu chứng:** Lần chạy đầu của `release.yml` (tag `v1.0.0`): build, test, đóng gói đều xanh, chỉ bước *Create GitHub Release* thất bại.
- **Gốc rễ:** Với repo mới, `GITHUB_TOKEN` mặc định **chỉ có quyền đọc**.
- **Sửa:** `permissions: contents: write` cho job release. Tag được chuyển lại sang commit chứa bản sửa (an toàn vì chưa có Release nào dùng tag đó).
- **Phòng ngừa:** Đọc kỹ quyền token khi workflow ghi lên GitHub. Workflow chưa từng chạy thì coi như **chưa được kiểm chứng**.

### A11. `package.ps1` làm hỏng `<?xml version=…?>` khi đóng dấu version

- **Triệu chứng:** Manifest bundle thành `<?xml Version="1.3.0" …`, khiến Autoloader không đọc được.
- **Gốc rễ:** `-replace` của PowerShell **không phân biệt hoa/thường**. Pattern `(\s)Version="…"` khớp luôn `version="1.0"` của khai báo XML.
- **Sửa:** `-creplace` (phân biệt hoa/thường), kèm comment cảnh báo.
- **Phòng ngừa:** Sau khi biến đổi file cấu hình, **parse lại** (`[xml](Get-Content …)`) và diff với bản gốc. Đây đúng là cách lỗi được phát hiện, trước khi phát hành.

### A12. Gói phát hành kèm hướng dẫn của một sản phẩm khác

- **Triệu chứng:** `HDSD.txt` trong bundle quảng cáo "thư viện thiết bị", "nhập/xuất Excel", tính năng không hề có. `Install.bat` gọi plugin là "quản lý thiết bị điện".
- **Gốc rễ:** Template được tách ra từ một dự án khác. File triển khai không được rà lại.
- **Sửa:** Viết lại theo danh sách lệnh thật. Bỏ version cứng `v2.0`.
- **Phòng ngừa:** Checklist release ([T06 §5](T06_BUILD_RELEASE_DEVOPS.md#5-quy-trình-phát-hành-release)) có mục "đọc lại mọi file người dùng cuối sẽ thấy". Chạy thử `Install.bat` trên `APPDATA` giả.

### A13. Palette luôn theme sáng dù AutoCAD đang tối

- **Triệu chứng:** Các thành phần dùng key `Cad*` của `ThemeManager` luôn hiện bản **sáng**. Đổi theme AutoCAD khi đang chạy cũng không có tác dụng.
- **Gốc rễ:** [`CadApplicationHelper.IsDarkMode`](../../Utils/CadApplicationHelper.cs) đọc sysvar **`DARKMODE`**, biến **không tồn tại**. `GetSystemVariable` ném lỗi, helper nuốt lỗi rồi trả mặc định `0`, tức `IsDarkMode == false`. `ThemeManager` cũng chỉ nghe thay đổi của `DARKMODE`, nên không bao giờ nhận được sự kiện.
- **Phát hiện:** Hỏi thẳng AutoCAD bằng accoreconsole `/isolate` (chỉ đọc): `(getvar "DARKMODE")` → `nil`; `(getvar "COLORTHEME")` → `0`.
- **Sửa (PR #6, B-01):** Đọc `COLORTHEME` (0 = tối, 1 = sáng) qua hàm thuần `IsDarkColorTheme`, nghe sự kiện của `COLORTHEME`, log khi không đọc được. Kèm theo: một nguồn màu (B-02), palette tự đổi theme khi đang mở (B-23). Kiểm chứng: HeadlessLab `COLORTHEME_DRIVES_DARK_MODE`, test resource key ở cả hai theme, ảnh render từng palette ở cả hai theme.
- **Phòng ngừa:** **Kiểm chứng giả định với hệ thống thật**, đừng tin comment. Helper "nuốt lỗi rồi trả mặc định" che giấu lỗi cấu hình: hãy log khi sysvar không tồn tại.

### A14. CHANGELOG mô tả một API không tồn tại

- **Triệu chứng:** CHANGELOG (phần 1.0.0) từng ghi `CadContext` "chuyển UndoMark từ COM sang managed `Database.BeginUndoMark()`".
- **Gốc rễ:** **Không có** `BeginUndoMark`/`StartUndoMark` trong bất kỳ DLL managed nào của AutoCAD 2024 (đã grep metadata). Code dùng COM `StartUndoMark` **từ bản đầu**, và chính comment trong code ghi rõ "Database KHÔNG có BeginUndoMark". Dòng CHANGELOG mô tả một thay đổi chưa từng xảy ra.
- **Sửa:** Đính chính CHANGELOG.
- **Phòng ngừa:** Tài liệu cũng cần review như code. **Mọi tên API trong tài liệu phải tồn tại**: kiểm bằng grep metadata DLL hoặc reflection ([T04 §6](T04_DEBUGGING_TROUBLESHOOTING.md#6-kiểm-chứng-api-bằng-metadata)).

### A15. Dự án tạo bằng `dotnet new` mất các khối `#if`

- **Triệu chứng:** Dự án sinh từ template build lỗi `CA1850` trên net8, trong khi chính repo template build sạch. Soi kỹ thì còn tệ hơn: `AppBootstrapper.SetServiceProviderForTesting` biến mất, logger không bao giờ ở mức Debug kể cả bản Debug, `[assembly: SupportedOSPlatform("windows")]` cũng mất — lỗi có từ trước, chỉ là chưa ai thấy.
- **Gốc rễ:** Template engine của `dotnet new` coi `#if/#else/#endif` trong file `.cs` là **điều kiện template**. Ký hiệu nó không biết (`NET`, `DEBUG`) bị tính là `false`: nhánh `#if` bị xóa, nhánh `#else` được giữ, chỉ thị biến mất. Nó dò token này **cả trong comment**: một comment có dấu thăng + "if" liền nhau làm engine crash "Index was out of range".
- **Phát hiện:** Chạy lại kiểm chứng template (`dotnet new install . --debug:custom-hive <thư-mục-tạm>`, `dotnet new cad-plugin`, build `-warnaserror`, test) sau một PR thêm code có `#if` (PR #6).
- **Sửa:** Bao mỗi khối biên dịch có điều kiện bằng hai dòng `//-:cnd:noEmit` … `//+:cnd:noEmit` (tắt xử lý điều kiện của engine, không xuất dòng marker ra dự án sinh ra); code mới tránh `#if` khi có API dùng được cho cả hai TFM (`IncrementalHash`); comment không viết token đó.
- **Phòng ngừa:** PR đổi cấu trúc dự án hoặc thêm `#if` → chạy lại **cả hai** đường tạo dự án (`dotnet new` và `init-template.ps1`) rồi build + test kết quả ([T06 §8](T06_BUILD_RELEASE_DEVOPS.md#8-template-tạo-dự-án-mới)). "Repo template build được" không có nghĩa "dự án sinh ra build được".

### A16. Test chỉ pass nhờ thứ tự chạy

- **Triệu chứng:** `ThemeDictionaries_DefineExactlyTheSameKeys` pass trong repo nhưng fail trong dự án sinh từ template: `NotSupportedException: The URI prefix is not recognized`.
- **Gốc rễ:** `ResourceDictionary.Source` với URI tương đối cần gói `pack://application:,,,/` đã được đăng ký — việc static constructor của `System.Windows.Application` làm. Trong repo, luôn có một test dựng palette chạy trước (`InitializeComponent` chạm tới `Application`), nên test kia pass "ké". xUnit sắp thứ tự test theo ID tính từ tên; dự án sinh ra đổi namespace → đổi thứ tự → lộ lỗi.
- **Phát hiện:** Chạy **riêng** test đó trong repo (`dotnet test --filter "FullyQualifiedName~ThemeDictionaries"`) → fail ngay.
- **Sửa:** Helper `RunOnSta` của test chủ động chạm `System.Windows.Application.Current` trước khi chạy.
- **Phòng ngừa:** Test mới phải pass khi chạy **một mình**, không chỉ khi chạy cả bộ. Trạng thái tĩnh (static) dùng chung giữa các test là nơi hay giấu loại lỗi này.

---

## Phần B — Bẫy AutoCAD API kinh điển

### B1. `eLockViolation` khi bấm nút trên palette
Ghi database từ **application context** mà không `LockDocument()`. → Mọi thao tác ghi đi qua `ICadContext` (luôn lock). [T01 §5](T01_AUTOCAD_NET_FOUNDATIONS.md#5-document-lock-và-hai-ngữ-cảnh-thực-thi)

### B2. `eNotOpenForWrite`
Sửa thuộc tính của đối tượng mở `ForRead`. → Mở `ForWrite` hoặc `UpgradeOpen()`. Lưu ý các helper tự upgrade (`XDataHelper`, `CadScope.AppendEntityToCurrentSpace`).

### B3. Thay đổi biến mất, không có lỗi
Quên `Commit()`, hoặc exception bị nuốt *bên trong* lambda. → `ICadContext` tự Commit. **Đừng** `try/catch` nuốt lỗi bên trong lambda, hãy để nó nổi lên cho transaction rollback đúng.

### B4. AutoCAD "đơ" khi chờ người dùng
Prompt (`GetPoint`…) **bên trong** transaction/lock. → HỎI trước, VẼ sau. `scope.Ask*()` đã `[Obsolete]`. [T01 §7.2](T01_AUTOCAD_NET_FOUNDATIONS.md#72-luật-thép-hỏi-trước-vẽ-sau)

### B5. Dùng `DBObject` sau khi transaction kết thúc
Giữ `Line`/`Entity` trong field để dùng lại. → Giữ `ObjectId` (trong phiên) / `Handle` (bền vững).

### B6. Vẽ nhầm bản vẽ sau `await`
Đọc `MdiActiveDocument` *sau* `await`: người dùng có thể đã đổi tab. → Chụp `Document` trước `await` như `CadContext.ExecuteAsync` và `CadCommandBase.ExecuteWithScopeAsync`. Ví dụ phản diện có thật: `DEMO_ASYNC_AWAIT` trên v1.0.0 (B-08). Bản sửa ở PR #6 lại mắc [B31](#b31-gọi-editor-của-bản-vẽ-không-active); từ PR #7 nó là ví dụ **đúng** (xem `Demos/DemoServicesCommands.cs`). [T01 §8.3](T01_AUTOCAD_NET_FOUNDATIONS.md#83-lệnh-async-thực-chất-chạy-thế-nào)

### B7. Fatal error khi bản vẽ bị đóng trong lúc chờ
`LockDocument()` trên Document đã hủy. → Kiểm tra `doc.IsDisposed` sau `await` (có trong `ExecuteAsync`).

### B8. Gọi AutoCAD API từ thread nền
`Task.Run(() => …GetObject…)`, `System.Timers.Timer`, `ConfigureAwait(false)`. → Chỉ tính toán/I-O ở thread nền, rồi marshal về main thread (`CadAsyncHelper.ExecuteSafeAsync`). Từ PR #6, gọi `ICadContext` từ thread khác ném ngay `InvalidOperationException` có hướng dẫn (`Core/CadThread`, B-06) thay vì hỏng ngẫu nhiên. [T01 §8](T01_AUTOCAD_NET_FOUNDATIONS.md#8-threading-và-async-trong-autocad)

### B9. Deadlock khi `.Result` / `.Wait()`
Chặn main thread chờ một task cần quay về main thread. → `await` từ đầu đến cuối. Ở biên dùng `FireAndForget(onError)`.

### B10. Quên `AddNewlyCreatedDBObject`
→ Dùng `scope.AppendEntityToCurrentSpace` (gom cả hai bước). Với record bảng ký hiệu/Xrecord/Group/Attribute, xem mẫu trong `Ensure*`, `XRecordHelper`, `BlockHelper`.

### B11. Rò rỉ đối tượng chưa vào database
`new Line()` rồi bỏ dở (do lỗi, hoặc chỉ dùng tạm) mà không `Dispose()`. Finalizer sẽ chạy trên thread khác. → `using`/`Dispose` cho mọi đối tượng không được append. Trong framework: B-12, B-14 (sửa ở PR #6 — mọi `Draw*` đi qua `CadScopeDrawingHelpers.AppendNew`, lỗi trước khi append thì Dispose rồi ném lại).

### B12. Lọc block theo tên bỏ sót dynamic block
Dynamic block đã chỉnh tham số tham chiếu BTR ẩn danh `*Uxx`, không còn tên gốc. → Lọc thêm `` `*U* `` rồi lọc lại sau khi chọn bằng `DynamicBlockHelper.GetEffectiveName`.

### B13. Lọc màu bỏ sót entity ByLayer
Mã DXF 62 của entity màu *ByLayer* là 256, không phải màu thật của layer. → Lọc theo layer, hoặc kiểm tra màu hiệu lực sau khi chọn.

### B14. Tên có ký tự đặc biệt bị hiểu là wildcard
Giá trị chuỗi trong `SelectionFilter` là **mẫu wildcard**. Vài ký tự **hợp lệ trong tên** layer/block lại là wildcard: `#` (một chữ số), `@` (một chữ cái), `.` (một ký tự không phải chữ/số), `~`, `[ ]`, `-`. Hệ quả (đã kiểm chứng, HeadlessLab `FILTER_ONLAYERS_IS_WILDCARD`): layer `LAB#1` **không khớp chính nó**, `M.EQUIP` khớp cả `M-EQUIP`. (`* ? ,` thì không hợp lệ trong tên ký hiệu, nên không gặp ở tên.) → Escape bằng `` ` ``: dùng `OnLayersExact` / `WithBlockNamesExact` / `SelectionFilterBuilder.EscapeWildcards` (PR #6, B-17).

### B15. Tọa độ lệch khi UCS bị xoay
Dùng thẳng điểm từ `Editor.GetPoint` (hệ UCS) để vẽ (hệ WCS). → `CadPromptHelper` tự chuyển. Khi dùng `Editor` thô: `TransformBy(ed.CurrentUserCoordinateSystem)`. [T01 §7.3](T01_AUTOCAD_NET_FOUNDATIONS.md#73-ba-hệ-tọa-độ-wcs-ucs-ocs)

### B16. XData vượt giới hạn
Giới hạn 16 KB **mỗi đối tượng** (mọi app gộp lại), mỗi chuỗi mã 1000 ≤ 255 ký tự. → `XDataHelper` chia khúc 250 và chặn ở 15.000 ký tự. Dữ liệu lớn hơn thì dùng XRecord.

### B17. Ghi XData khi RegApp chưa được đăng ký
→ `XDataHelper.EnsureRegApp` tự đăng ký. Entity phải **đã nằm trong database** (helper dùng `obj.Database`), vì vậy Golden Sample gắn XData *sau* khi append.

### B18. Xóa Xrecord khỏi dictionary nhưng đối tượng vẫn còn
`DBDictionary.Remove(key)` gỡ *mục* trong dictionary nhưng không xóa đối tượng (nó thành "mồ côi"). → Mở Xrecord `ForWrite` rồi `Erase()`. `XRecordHelper.RemoveMetadata` từng chỉ `Remove` — sửa ở PR #6 (B-09; HeadlessLab `XRECORD_REMOVE_ERASES` FAIL trên v1.0.0).

### B19. Sao chép entity mất metadata (Extension Dictionary)
`Entity.Clone()` cho một bản sao chưa vào database **không có Extension Dictionary** — mọi metadata XRecord của framework (`XRecordHelper`) mất ở bản sao (XData thì còn). → `Database.DeepCloneObjects` (sao cả đối tượng sở hữu) rồi `TransformBy`. `CopyEntity`/`MirrorEntity` từng dùng `Clone()` — sửa ở PR #6 (B-13; HeadlessLab `COPY_KEEPS_METADATA` FAIL trên v1.0.0).

> **Đính chính.** Bản trước của mục này ghi "sao chép block **mất attribute**". Kiểm chứng trên AutoCAD 2024 cho thấy **sai**: `Clone()` của `BlockReference` vẫn tạo `AttributeReference` **riêng** cho bản sao (ObjectId khác, owner là bản sao — HeadlessLab `COPY_KEEPS_ATTRIBUTES`, `MIRROR_KEEPS_ATTRIBUTES` PASS cả trước lẫn sau khi sửa). Bài học: một kết luận "nghe hợp lý" về API vẫn phải được chạy thử.

### B20. Attribute lệch vị trí sau khi đổi giá trị
Attribute căn lề khác *Left* phải được tính lại căn chỉnh sau khi đổi `TextString`, nếu không hình học giữ chỗ cũ (đã kiểm chứng: tâm X = 42.507 thay vì điểm căn 20). → `BlockHelper.InsertBlock` và `BlockHelper.SetBlockAttribute` tự căn lại (hàm nội bộ `RefreshAlignment`): attribute MText thì `UpdateMTextAttribute()`, còn lại `AdjustAlignment(db)` (PR #6, B-25; HeadlessLab `ATTRIBUTE_REALIGNED_AFTER_SET`). Tự đổi `attRef.TextString` ngoài helper thì phải tự gọi một trong hai hàm đó.

### B21. Hatch lỗi `eNotInDatabase`
Gán `Associative = true` / `AppendLoop` trước khi Hatch vào database. → Append trước, như [`DrawHatch`](../../Extensions/CadScopeBlockExtensions.cs) đang làm.

### B22. Sửa database trong handler sự kiện Database
→ Gom `ObjectId`, xử lý ở `CommandEnded`/`Application.Idle` ([T01 §10.2](T01_AUTOCAD_NET_FOUNDATIONS.md#102-năm-quy-tắc)).

### B23. Không gỡ được event handler
Gỡ bằng một lambda mới (delegate khác). → Lưu delegate rồi gỡ đúng nó (mẫu `EventService`).

### B24. Rò rỉ subscription EventAggregator
EventAggregator giữ **tham chiếu mạnh**. → `TrackSubscription(...)` trong ViewModel. PaletteService Dispose ViewModel khi thay content.

### B25. AutoCAD crash lúc thoát vì Overrule
Overrule còn đăng ký khi plugin tắt. → `OverruleManager` được Dispose trong `Terminate`.

### B26. COM gọi từ finalizer
Gọi COM (ví dụ `EndUndoMark`) từ finalizer chạy trên thread khác. → `UndoGroupManager` **cố ý không có finalizer**, chỉ dùng với `using`.

### B27. Palette không nhớ vị trí
`PaletteSet` tạo không có Guid ổn định. → Guid cố định cho mỗi palette. Từ PR #6, `PaletteService` tự dẫn xuất Guid từ `[assembly: Guid]` + tên palette (`Core/PaletteGuids`, B-04).

### B28. Resource WPF thiếu nhưng không báo lỗi
`{DynamicResource KeyKhôngCó}` âm thầm dùng giá trị mặc định. → Test: mọi key mà XAML tham chiếu phải resolve được, ở **cả hai** theme (`XamlPaletteTests.EveryResourceKeyReferencedByPalettesAndStyles_Resolves`, B-03). Kèm: giá trị **local** trên thẻ luôn thắng setter của Style/DataTrigger — đặt giá trị mặc định *trong* Style nếu muốn trigger đổi được nó (B-29).

### B29. Plot thất bại hoặc chạy nền ngoài ý muốn
Không kiểm tra `PlotFactory.ProcessPlotState` (đang có plot khác thì không plot được); `BACKGROUNDPLOT` mặc định **2** (đã kiểm chứng) nghĩa là publish chạy nền; `PlotEngine` không được Dispose. → Kiểm tra trạng thái, tạm đặt `BACKGROUNDPLOT = 0` rồi khôi phục, `using` cho engine (B-10). Khi in thật còn gặp (HeadlessLab, B-28): `GetCanonicalMediaNameList` trả `StringCollection` (phải `Cast<string>()`); `SetPlotCentered` ném `eInvalidInput` với vùng in `Layout`; `PlotInfoValidator` ném `eLayoutNotCurrent` nếu layout không hiện hành.

### B30. `Assembly.Location` rỗng
Khi nạp từ byte array, `Assembly.Location == ""`, và `Path.GetDirectoryName("")` ném `ArgumentException` trên .NET Framework (đã kiểm chứng). → Không suy ra thư mục plugin từ `Location` mà không kiểm tra. `AppBootstrapper` có kiểm tra; `PathProvider` từ PR #6 ném lỗi nói rõ nguyên nhân (B-11).

### B31. Gọi Editor của bản vẽ không active
Mọi lời gọi `Editor` — **kể cả `WriteMessage`**, không chỉ prompt — trên một `Document` không phải bản vẽ đang active ném `eNotApplicable` (từ `AcMgUserInteraction`; đã kiểm chứng trên AutoCAD 2024). Gặp ngay ở giai đoạn vẽ của `ExecuteAsync` khi người dùng chuyển bản vẽ trong lúc chờ: Document đã chụp vẫn đúng để **vẽ** (`Database`, lock, transaction đều được), nhưng một dòng `scope.Editor.WriteMessage` làm hủy cả transaction — không có hình nào được vẽ. Tệ hơn, nếu cả thông báo lỗi cũng được ghi vào Editor của bản vẽ gốc thì nó cũng bị nuốt: người dùng không thấy gì. (Đây là lỗi PR #6 đưa vào `DEMO_ASYNC_AWAIT`, phát hiện khi người dùng kiểm trong GUI 2026-10-14, nhờ log có đủ stack trace.) → Giai đoạn vẽ chỉ thao tác database; báo kết quả **sau** khi vẽ xong, lên bản vẽ **đang active** (`INotificationService`), kèm tên bản vẽ đã vẽ vào. Framework (PR #7): `CadCommandBase` báo lỗi lệnh async lên bản vẽ đang active (kể cả khi bản vẽ gốc đã bị đóng); `autoRegen` chỉ regen bản vẽ đang active; cảnh báo của `InsertBlock` chỉ ghi khi bản vẽ đang active. So sánh hai `Document`: dùng `==` (`DisposableWrapper` có `op_Equality`).

### B32. Màu ByLayer đọc ra là trắng
`entity.Color.ColorValue` của một entity màu ByLayer trả về **trắng (255, 255, 255)**, không phải màu của layer. Màu 7 cũng ra trắng (`EntityColor.LookUpRgb(7) = 0xFFFFFF`), dù AutoCAD vẽ nó **đen** trên nền sáng. Cả hai đã kiểm chứng trên AutoCAD 2024, khi viết bộ xem trước của [U03](U03_DRAWING_ON_UI.md). → Khi `Color.IsByLayer`, đọc `LayerTableRecord.Color`; màu 7 thì để theme quyết định ([U03 §7](U03_DRAWING_ON_UI.md#7-đọc-entity-trong-bản-vẽ-thành-hình-xem-trước)).

### B33. `Explode` không trả về attribute
`BlockReference.Explode` trả về các entity của định nghĩa block, **kể cả `AttributeDefinition`**, nhưng **không** trả về `AttributeReference`, tức giá trị attribute thật của block đó. Mọi phần trả về đều nằm ngoài database (đã kiểm chứng). → Đọc `block.AttributeCollection` riêng, bỏ qua `AttributeDefinition`, và `Dispose` mọi phần sau khi đọc ([U03 §7](U03_DRAWING_ON_UI.md#7-đọc-entity-trong-bản-vẽ-thành-hình-xem-trước)).

### B34. Jig nhận WCS, `GetPoint` trả UCS
Cùng gõ `10,0` khi UCS xoay 90°: `Editor.GetPoint` trả (10; 0), là **UCS**; `AcquirePoint` trong jig trả (0; 10), là **WCS** (đã kiểm chứng). Đổi kết quả của jig sang WCS thêm một lần nữa là đổi hai lần. → `CadPromptHelper.GetPoint` đã đổi sẵn sang WCS; điểm của jig thì dùng thẳng ([U03 §9.1](U03_DRAWING_ON_UI.md#91-jig-hình-chạy-theo-chuột)). Liên quan: [B15](#b15-tọa-độ-lệch-khi-ucs-bị-xoay).

### B35. `CS0104: 'Polyline' is an ambiguous reference`
`Autodesk.AutoCAD.GraphicsInterface`, namespace cần cho jig và transient, cũng có kiểu tên `Polyline`. File `using` cả hai namespace sẽ không biên dịch được. → Dùng alias: `using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;` ([U03 §9.2](U03_DRAWING_ON_UI.md#92-transient-hình-tạm-đứng-yên)).

### B36. Danh sách từ khóa in hai lần
`PromptKeywordOptions.AppendKeywordsToMessage` mặc định là `true`: AutoCAD tự nối `[Yes/No] <Yes>` vào câu nhắc. Câu nhắc tự viết thêm `[Yes/No]` thì danh sách in hai lần (đã kiểm chứng; `CadPromptHelper.GetKeywords` từng mắc lỗi này, backlog B-33, đã sửa ở PR #11). → Câu nhắc chỉ chứa phần chữ ([U02 §2.3](U02_UI_IN_AUTOCAD.md#23-từ-khóa)). HeadlessLab `KEYWORDS_PROMPT_LISTS_ONCE` giữ cho helper không mắc lại.

> [!NOTE]
> Bẫy của **WPF** khi chạy trong AutoCAD nằm ở [U01 §5 và §8–9](U01_WPF_MVVM_BASICS.md#5-ba-bẫy-khi-nhập-số): dấu phẩy thập phân, ô số bị xóa trắng, đổi dữ liệu từ luồng nền, `DynamicResource` sai key. Bẫy khi vẽ lên UI nằm ở [U03 §11](U03_DRAWING_ON_UI.md#11-bẫy).

---

## ✅ Tự kiểm tra T03

1. Chọn 3 mục trong Phần A có chung một bài học. Bài học đó là gì?
2. Một test integration của bạn "luôn pass". Ba câu hỏi đầu tiên bạn tự đặt ra là gì?
3. Một đồng nghiệp đề xuất thêm `Polly 7.x` vào plugin. Bạn kiểm tra gì trước khi đồng ý?

<details><summary>Gợi ý đáp án</summary>

1. Ví dụ A6 + A13 + A14: **kiểm chứng giả định với hệ thống thật** (đọc output thật, hỏi sysvar thật, grep metadata thật) thay vì tin vào code, comment hay tài liệu.
2. (a) Test có *chứng minh* phần cần test đã chạy không, hay chỉ "không thấy lỗi"? (b) Output có được đọc đúng không (encoding, đường dẫn)? (c) Nếu cố tình làm hỏng code, test có fail không?
3. Thư mục cài AutoCAD có `Polly.dll` cùng `AssemblyVersion` không (A1); dependency bắc cầu của nó; có thật cần không, hay viết vài dòng là đủ; bản net48 và net8 có giống nhau không.
</details>

---

**Tiếp theo:** [T04 — Debug & chẩn đoán →](T04_DEBUGGING_TROUBLESHOOTING.md)
