# Changelog

All notable changes to **CadCoreFramework** are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versions follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added (CI — kiểm tài liệu)
- Job **`docs`** trong `build-and-test.yml` (ubuntu, Node 24) chạy ba script của `tools/docs/` ở mọi PR, báo lỗi ngay trên dòng của PR:
  - link tương đối và `#anchor`, kể cả sai chữ hoa/thường (Windows bỏ qua lỗi này, GitHub thì không);
  - nhãn 🎯/🔍/📖 cho mọi mục và số phút trên dòng ⏱ so với số từ thật;
  - cú pháp mọi sơ đồ mermaid (mermaid 11.17.2, ghim trong `package-lock.json`).
- Chạy trên máy: `cd tools/docs`, `npm ci`, `npm run check` ([T06 §4](docs/training/T06_BUILD_RELEASE_DEVOPS.md#4-ci-build-and-testyml)).
- Dependabot thêm nhóm `docs-tools` (npm, mỗi tháng). `tools\**` được loại khỏi glob của project plugin; template `dotnet new` bỏ qua `node_modules`.

### Added (tài liệu — bản đồ học)
- **Thẻ module** ở đầu cả 20 module C00–C05, T01–T10, U00–U03: dòng ⏱ thời gian đọc (cả trang và riêng phần cốt lõi), bài tập/lab để thực hành, và nhãn mục. Thời gian tính từ số từ thật của trang (kể cả code) ÷ 100 từ/phút; chưa gồm thời gian chạy thử và làm lab.
- **Nhãn 🎯 cốt lõi / 🔍 đào sâu / 📖 tra cứu** ở cuối từng dòng Mục lục. Giai đoạn 0 (C01–C05) giữ mọi mục là 🎯, vì đã được lọc theo quy tắc 20/80.
- **Mục tiêu đầu ra** ("Sau module này bạn phải làm được") cho T03–T09 và U01–U03, các module trước đây chưa có.
- **Lộ trình nhanh** cho người đã vững C# ([training/README §3](docs/training/README.md#lộ-trình-nhanh)): chỉ đọc các mục 🎯 (≈ 3,5 giờ) rồi làm L01, L03, L04, L07. Đích đến là tự làm một tính năng trọn vẹn theo Golden Sample. Có thêm hướng dẫn mở rộng khi cần làm giao diện xem trước.
- **Bản đồ phụ thuộc** giữa các module (sơ đồ mermaid, lấy từ dòng "Cần trước" của mỗi module) và quy tắc duy trì nhãn/thời gian ở [training/README §7](docs/training/README.md#7-duy-trì-bộ-tài-liệu).

---

## [1.1.1] — 2026-10-15

Bản PATCH: cập nhật dependency và CI. API công khai và hành vi của plugin không đổi. Dự án có test riêng dùng FluentAssertions 6 nếu nâng theo lên 7.2.2 thì nên đọc release notes của FluentAssertions 7 trước. Test của template chạy được trên 7.2.2 mà không phải sửa dòng nào.

- Version assembly `1.1.1.0` (`Properties/AssemblyInfo.cs`) và version MSI mẫu (`Deployment/installer`).

### Changed (dependency — PR #20–#24)
- **Package đi theo bundle:** `CommunityToolkit.Mvvm` 8.2.2 → 8.4.2, `Microsoft.Extensions.DependencyInjection` 8.0.0 → 8.0.1, `Newtonsoft.Json` 13.0.3 → 13.0.4.
  - Trên net48, bundle có `Microsoft.Bcl.AsyncInterfaces` **10.0.0.1** (trước đây 8.0.0.0) và các bản `System.Memory`/`Buffers`/`Unsafe`/`Threading.Tasks.Extensions` mới hơn. Số file không đổi.
  - DI 8.0.1 vẫn tham chiếu `Bcl.AsyncInterfaces` 8.0.0.0; chỗ lệch này được resolver của `AppBootstrapper` xử lý ([T06 §7](docs/training/T06_BUILD_RELEASE_DEVOPS.md#7-chính-sách-dependency)).
  - Đã kiểm trong AutoCAD 2024 (GUI): khởi tạo plugin, palette, `DEMO_ATTR_EXPORT` ra `.json` và `.xlsx`; log không có lỗi.
- **Package test và tool:** FluentAssertions 6.12.0 → 7.2.2 (không lên 8 vì license), xunit 2.9.3, xunit.runner.visualstudio 4.0.1, Microsoft.NET.Test.Sdk 18.11.0, coverlet 10.1.0, Moq 4.21.0, BenchmarkDotNet 0.15.8, ReportGenerator 5.5.11.
- **CI:** GitHub Actions lên các bản chạy Node 24 (checkout v7, setup-dotnet v6, cache v6, upload-artifact v7, action-gh-release v3). Thêm `.github/dependabot.yml`: mỗi tuần mở PR nâng version, có danh sách package bỏ qua hoặc ghim ([T06 §7](docs/training/T06_BUILD_RELEASE_DEVOPS.md#7-chính-sách-dependency)).

---

## [1.1.0] — 2026-10-15

Bản MINOR sau v1.0.0, gồm:
- **Tính năng:** `IDataFileService` (JSON, CSV, Excel), lệnh demo `DEMO_ATTR_EXPORT` / `DEMO_ATTR_IMPORT`.
- **Sửa lỗi:** 32 trên 34 mục backlog đã kiểm chứng ([T09 §3](docs/training/T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng), PR #6–#13). B-20 mới xong một phần (HeadlessLab); B-21 ngoài phạm vi. Thêm: `Install.bat` báo thành công khi chép thiếu file.
- **Tài liệu:** bộ tài liệu đào tạo (Giai đoạn 0, nhánh UI, T01–T10) và bộ tra cứu R01–R06.

Dự án đã dựng từ v1.0.0 cần đọc **"Changed (PR #6) — lưu ý khi nâng cấp"** bên dưới. Ngoài ra, `IFileSystemService` có thêm hai thành viên (`ReadAllBytes`, `WriteAllBytes`): lớp tự viết implement interface này phải thêm hai hàm đó.

### Fixed (gói cài đặt)
- **`Install.bat` báo "CAI DAT HOAN TAT VA THANH CONG 100%" dù chép thiếu file.** Lỗi có từ v1.0.0, tìm thấy khi chạy thử gói 1.1.0 trước khi phát hành. `xcopy` gặp đường dẫn dài quá khoảng 254 ký tự (ví dụ giải nén ZIP vào thư mục sâu) thì báo `Insufficient memory` và bỏ qua file đó. Script chỉ kiểm `PackageContents.xml`, nên vẫn báo thành công. Lần thử cài vào một đường dẫn đích 261 ký tự thiếu 4 trên 22 file.
  - Đã sửa: dùng `robocopy` (chép được đường dẫn dài) và coi mã thoát ≥ 8 là lỗi.
  - Đã kiểm lại: đường dẫn dài và đường dẫn ngắn đều cài đủ 22 file. Khi chép thất bại thật (đích bị một file chặn tên), script báo `[LOI]`.
- Version assembly `1.1.0.0` (`Properties/AssemblyInfo.cs`; log khởi động in version này) và version MSI mẫu (`Deployment/installer`).

### Added (đọc/ghi file JSON, CSV, Excel)
- **`IDataFileService`** (đăng ký DI): `ReadJson<T>`/`WriteJson<T>`, `ReadCsv`/`WriteCsv`, `ReadExcel`/`WriteExcel`/`GetExcelSheetNames` (`.xlsx`), `ReadTable`/`WriteTable` chọn định dạng theo đuôi file. Lỗi thường gặp trả `CadResult` thất bại kèm thông điệp tiếng Việt: không thấy file, file đang mở trong Excel, JSON/CSV sai cấu trúc (có số dòng), `.xls` cũ, CSV không phải UTF-8. Ghi atomic, đọc được file đang mở.
- **`Models/DataSheet`**: bảng dùng chung cho ba định dạng; `GetText`/`TryGetDouble` đọc ô bất kể nguồn. **`Utils/CsvCodec`**: CSV theo RFC 4180, tự viết, tự nhận dấu phân cách `,` `;` hoặc tab; ghi UTF-8 có BOM, số theo InvariantCulture.
- **Dependency mới: MiniExcel 1.44.1** (Apache-2.0). Bản 1.44.1 không kéo theo DLL nào trên net48 và net8; bản 1.45 và 1.46 kéo theo `Microsoft.Bcl.AsyncInterfaces 10.0.9` trên net48. Thư mục cài AutoCAD 2024 không có sẵn MiniExcel, nên không có nguy cơ xung đột phiên bản như Serilog.
- `IFileSystemService`: thêm `ReadAllBytes` (`FileShare.ReadWrite`) và `WriteAllBytes` (atomic, chung đường ghi với `WriteAllText`).
- Lệnh demo **`DEMO_ATTR_EXPORT` / `DEMO_ATTR_IMPORT`**: xuất attribute của mọi block (model + layout) ra `.xlsx`/`.csv`/`.json`, sửa trong Excel, nhập lại theo handle. Phần lõi nằm ở `Demos/BlockAttributeTable.cs`. Tài liệu: T08 §12.6, R03 §8, README §5.4.
- **Kiểm chứng**:
  - 56 unit test mới (376 × 2 runtime).
  - 4 kiểm thử HeadlessLab mới (22/22 trên AutoCAD 2024; MiniExcel chạy trong tiến trình AutoCAD). Ba kiểm thử `ATTR_*` chỉ biên dịch khi còn `Demos/`, để dự án tạo bằng `IncludeDemos=false` vẫn build được lab. Tạm bỏ dòng kiểm layer khóa thì `ATTR_IMPORT_REPORTS_MISSING_LOCKED_UNKNOWN` FAIL.
  - Excel 16 thật mở file do service ghi: `.xlsx` giữ `001`/`2E5` là chữ. CSV thì bị Excel đổi `001` → `1`, `2E5` → `200000`, và trên máy dùng dấu phẩy thập phân thì CSV phân cách `,` bị dồn vào một cột. Đọc `.xlsx` đang mở trong Excel được, ghi đè thì báo lỗi rõ ràng. CSV ANSI do Excel lưu bị từ chối kèm hướng dẫn.
  - Hai lệnh demo chạy trên AutoCAD 2024 có giao diện (2026-10-15). Log xác nhận: xuất 4 block / 2 tag; nhập đổi 3 attribute và bỏ qua block trên layer khóa; ghi đè file đang mở trong Excel thì báo lỗi rõ ràng. Người dùng xác nhận một lần Ctrl+Z trả lại cả 3 attribute. Lần chạy này cho thấy mỗi lỗi bị ghi log hai lần (`WRN` + `ERR`). Đã sửa: `DataFileService` không tự ghi log nữa (constructor chỉ còn `IFileSystemService`), lệnh truyền `CadResult.Exception` cho `Notify.Error`.

### Added (T08 §12 — công thức thao tác bản vẽ)
- Năm công thức thay cho các chủ đề của Cookbook cũ, mỗi công thức gồm phần lõi chạy trên `ICadScope` và một lệnh `TOOLS_*`: khung tên A3/A2/A1 dạng block có attribute; xuất mọi layout ra một PDF qua `PlotService`; purge nhiều lượt + `AUDIT`; attach xref với đường dẫn tương đối; gộp layer (mọi block table record, attribute, layer khóa, layer hiện hành, chỉ xóa layer mà `Purge` cho phép).
- Hành vi AutoCAD đã kiểm và ghi vào tài liệu:
  - `AttachXref` với `REFPATHTYPE = 1` ghi đường dẫn tương đối theo **thư mục hiện hành** khi bản vẽ chủ chưa lưu, nên công thức luôn tự gán `PathName`.
  - Chuyển cả thư mục dự án: xref tương đối vẫn `Resolved`, xref tuyệt đối thành `FileNotFound`.
  - `Purge` một lượt không đủ.
  - .NET API không có `Database.Audit`.
  - `eOnLockedLayer` chỉ xuất hiện khi trạng thái khóa đã commit.
  - `ConstantWidth` đặt trước `AddVertexAt` bị mất.
- Checklist trước khi mở PR chuyển thành T08 §13; thêm hai câu tự kiểm tra.
- **Kiểm chứng**: code chép nguyên văn từ một bản sao dự án, build `-c Release -warnaserror` sạch trên net48 và net8.0-windows; phần lõi chạy trên AutoCAD 2024 bằng HeadlessLab: 19 kiểm thử mới, cùng 18 kiểm thử cũ, 37/37 PASS.
- **Kiểm trên AutoCAD 2024 có giao diện** (2026-10-15, người dùng chạy; log của plugin xác nhận những bước có ghi log): cả 5 lệnh `TOOLS_*` trên các bản vẽ mẫu dựng sẵn, bằng một DLL dựng từ `main` cộng nguyên văn code của T08 §12. Gộp `L1` (đang khóa) và `L2` (hiện hành) vào `DICH` chuyển 4 entity, kể cả entity bên trong định nghĩa block. Một lần Ctrl+Z trả lại toàn bộ. Khi `DICH` đang đóng băng, layer hiện hành thành `0`. Hai điểm "chưa kiểm" cuối cùng của T08 §12.5 đã được kiểm.

### Changed (hợp nhất `docs/` thành một bộ tài liệu hai lớp)
- **Cấu trúc mới**: `docs/README.md` là cửa vào duy nhất; `docs/training/` (Học viện, không đổi tên) và `docs/reference/` (Tra cứu R01–R06) dùng chung một bộ quy ước: thanh điều hướng, dòng "Dùng khi / Học bài bản", mục lục, trích dẫn dạng `R03 §2`.
- **Phần 1–9 cũ chuyển thành R01–R06** (đổi tên bằng `git mv`, giữ lịch sử):
  - `01_GETTING_STARTED` → `R01_QUICKSTART`: cây thư mục đúng với repo (thêm `Samples/`, `Demos/`, các project test/analyzer…), bảng 9 lệnh trong `PackageContents.xml`. Bỏ lộ trình 3 tuần và bài "Vẽ Tủ Điện MSB" (bản đúng chuẩn ở T10 §3 và đồ án C05 §5).
  - `02_CORE_ARCHITECTURE` → `R02_ARCHITECTURE`: thêm bảng hợp đồng `ICadContext` (ném hay trả `CadResult`, lock, chặn thread/lồng nhau). §2.3 cũ ghi lệnh `VE_TU_DIEN` "tại `SampleCommands.cs`", lệnh không tồn tại; giờ trích nguyên văn `CORE_INFO` và `DRAW_CIRCLE_SAMPLE`.
  - `03_DRAWING_AND_INTERACTION` + `04_METADATA_AND_SERVICES` + `07_API_REFERENCE` → `R03_API`: chữ ký chép lại từ code. Sửa: `SelectionFilterBuilder` có `OnLayersExact`/`WithBlockNamesExact`/`EscapeWildcards` (trang cũ nói "không escape wildcard"); Ribbon đã có tab `CadCoreRibbon` (trang cũ nói "chưa dựng tab nào"); bỏ "khớp v1.0.0". Thêm bảng mọi service có sẵn và tiện ích còn thiếu.
  - `05_WPF_UI_AND_MVVM` → `R04_UI`, `09_GOLDEN_SAMPLE` → `R05_GOLDEN_SAMPLE`: nội dung giữ nguyên, đánh số lại.
  - `06_DEVOPS_AND_STANDARDS` → `R06_STANDARDS`: ví dụ test dùng `PanelDrawService` không có trong repo được thay bằng hai test nguyên văn từ `LayerServiceTests`. Thêm §5 Analyzer CAD001–CAD003.
- **Bỏ `08_COOKBOOK`**: chỉ 8.1/8.6/8.7 biên dịch sạch; 8.2, 8.3, 8.5, 8.8, 8.11 lỗi biên dịch (API không tồn tại, sai chữ ký, thiếu member abstract), 8.9 lỗi trên net48 (`record` thiếu `IsExternalInit`), 8.2/8.4/8.9 dùng `Execute<T>` đã obsolete, 8.10 che `ViewModelBase.Dispose`. Công thức đúng nằm ở T08.
- **Bỏ `00_TOC`**: hứa những mục không tồn tại (Selection, Plot/Settings/Logger, EventAggregator, Troubleshooting); thay bằng `docs/README.md`.
- **Link trong code**: `helpLinkUri` của CAD001/CAD002/CAD003 trỏ tới `R06_STANDARDS.md#cad001`…`#cad003`; thông báo `[Obsolete]` của `CadScopeQueryExtensions`, `AssemblyInfo.cs`, template `cad-command` và `.agents/rules` trỏ tới `R05_GOLDEN_SAMPLE.md`.
- **Kiểm chứng**: 1699 link tương đối và anchor, 0 hỏng; mọi khối ghi "nguyên văn" khớp từng dòng với nguồn; số test của R05 khớp `dotnet test --list-tests`; build Release `-warnaserror` sạch, 320 test × 2 runtime xanh.

### Changed (PR #14 — rà soát toàn bộ tài liệu training)
- **Chỗ tài liệu đã lệch với code**:
  - T02 §5: `ExecuteWithScopeAsync` trả `Task<bool>` và báo lỗi lên bản vẽ đang active (từ PR #7).
  - T02 §6.1: mẫu `Draw*` theo `CadScopeDrawingHelpers.AppendNew`, tức bản sửa B-14; tài liệu vẫn dạy mẫu cũ bị rò rỉ entity.
  - T08 §3/§7: Overrule gọi `PanelService.XDataAppName`, hằng không tồn tại (biên dịch báo `CS0117`). Giờ dùng `PanelService.DataKey` và `SetExtensionDictionaryEntryFilter`, khớp với chỗ §3 lưu dữ liệu.
  - T06: smoke test giờ có 17 bước; manifest liệt kê đúng 9 lệnh không phải demo (`PackageManifestTests`).
  - T05: coverage đo lại (21%); ghi rõ chú thích cuối dòng trong ba khối test là của tài liệu.
  - C05: kết quả `dotnet test` (320) và thí nghiệm "Thử ngay" (40/41), chạy lại thật. C03: số class.
- **Trải nghiệm người học**:
  - README: bảng "Bắt đầu từ đâu?" theo từng kiểu người học; bảng trạng thái kiểm chứng gọn lại.
  - Mục lục cho T02–T10.
  - T03: bảng tra nhanh 52 mục theo triệu chứng. T04: cây quyết định thêm hai triệu chứng (ô số, từ khóa).
  - Tự kiểm tra mới ở T01 §9–11 và T08. U00: quy tắc vàng thứ 7 (ô nhập số).
  - C01 nối bẫy định dạng số với lỗi thật B-31. T09: checklist review có mục palette; lộ trình ghi đợt sửa của nhánh UI. T10: quy tắc chọn base cho lab.
  - GLOSSARY thêm NaN, Mock/Fake, fail trước, backlog, DoD.
- **Kiểm chứng**: link và anchor; mọi khối code đối chiếu với nguồn; công thức T08 biên dịch thử trong một bản sao dự án (`-warnaserror` sạch); 68 ví dụ Giai đoạn 0 chạy lại, output không đổi.

### Fixed (PR #13 — `GridSpec` chặn `NaN` và vô cực)
- **`GridSpec.Validate()` coi `NaN` và vô cực là kích thước hợp lệ** (B-34): so sánh `<`, `<=`, `>`, `>=` với `NaN` đều ra `false`, nên `Spacing = NaN` hay `CellSize = NaN` cho 0 lỗi; `Spacing = ∞` cũng lọt. Giờ `Validate()` báo lỗi "Khoảng cách, kích thước ô và chiều cao chữ phải là số hữu hạn…", và `GridPlanner` từ chối spec đó như mọi spec sai khác. Từ PR #11 người dùng không gõ ra được các giá trị này (palette và `GetDouble` đều chặn); bản sửa này chặn cả code gọi thẳng `GridSpec`.
- Test: 315 → **320 × 2 TFM**: `Validate_SizeNotAFiniteNumber_ReturnsError`, 5 trường hợp, đều FAIL trên `74e32ca` (code cũ coi chúng là hợp lệ).
- Tài liệu: bảng đọc `GridSpec.cs` ở C03 §9 theo số dòng mới, thêm giải thích `NaN`; T09 B-34 ✅; Phần 9, T05, T10 L18, U01, README học viện.

### Fixed (PR #11 — ô nhập số trên palette, danh sách từ khóa in hai lần)
- **Ô số thực trên palette Grid Marker đọc `2,5` thành 25** (B-31). Ô bind thẳng vào `double` nên WPF đổi chuỗi theo `Language` mặc định `en-US`, không theo culture `vi-VN` của máy. **Ô số bị xóa trắng hay gõ chữ thì ViewModel lặng lẽ giữ số cũ** (B-32), nút Vẽ vẫn bật. Giờ 7 ô số bind vào property chuỗi (`RowsText`, `SpacingText`…). `ViewModels/NumberText` đọc số như dòng lệnh AutoCAD: dấu chấm thập phân, `InvariantCulture` + `NumberStyles.Float`, chặn cả `NaN`, vô cực và tràn số (WPF mặc định nhận `NaN`). Ô không đọc được thành số là lỗi validate ("Số hàng: nhập một số nguyên."), nên nút Vẽ tắt. Các property số (`Rows`, `Spacing`…) giữ nguyên và đồng bộ hai chiều với chuỗi.
- **`CadPromptHelper.GetKeywords` in danh sách từ khóa hai lần** (B-33): `Vẽ lưới này? [Yes/No] [Yes/No] <Yes>:`. AutoCAD đã tự nối danh sách (`AppendKeywordsToMessage` mặc định `true`), nên helper giờ chỉ truyền phần chữ. Extension cũ `ICadScope.AskKeyword` (`[Obsolete]`) mắc cùng lỗi và được sửa cùng cách.
- Test: 275 → **315 × 2 TFM**: `NumberTextTests` (26); 11 case mới cho `GridMarkerViewModelTests`; `XamlPaletteTests` gõ vào ô thật của palette (`2,5`, ô trống) và chặn mọi TextBox bind thẳng vào property số. HeadlessLab 17 → **18**: `KEYWORDS_PROMPT_LISTS_ONCE` chạy prompt thật qua cả hai helper, runner kiểm output in mỗi danh sách đúng một lần. Test palette và kiểm thử HeadlessLab đã được chạy trên commit `632a6c1` (code cũ) và FAIL đúng chỗ (`Spacing` = 25, nút Vẽ vẫn bật, `[Yes/No] [Yes/No]`). Test ViewModel và `NumberTextTests` dùng API mới nên không biên dịch được trên code cũ.
- Tài liệu: T09 đánh dấu B-31…B-33 đã sửa và thêm **B-34** (còn mở): `GridSpec.Validate()` không chặn `NaN`/vô cực; từ PR này không đường nhập nào tạo ra chúng. Lab L18 và bài tập U02-E1 làm trên `632a6c1` rồi đối chiếu với PR này. U01–U03, T03, T05 (smoke bước 17), T08, Phần 5, Phần 9 và README học viện được cập nhật theo. Các bước của U03 áp lại trên code mới: 330/330 test, ảnh palette trùng từng byte với ảnh cũ, HeadlessLab 20/20.
- Kiểm trong AutoCAD 2024 có giao diện (2026-10-14, người dùng, ghi lại ở PR #12): T05 smoke 17 (ô nhập số trên palette) và bước từ khóa của `DEMO_PROMPT` đạt.

### Added (PR #10 — đào tạo: nhánh UI, vẽ thực thể lên UI)
- **`docs/training/U00`–`U03`**: nhánh UI theo quy tắc **20/80**. Bảng ưu tiên được đếm trên code C# công khai (GitHub) và trong dự án.
  - U00: bắt đầu từ đây (bản đồ UI, 6 quy tắc vàng, lộ trình 3 tuần, bài kiểm tra đầu vào).
  - U01: WPF & MVVM, không cần AutoCAD.
  - U02: UI trong AutoCAD (prompt, palette, hộp thoại có nút chọn điểm, Ribbon, chọn sẵn và zoom).
  - U03: **vẽ thực thể lên UI**. Hướng dẫn từng bước thêm khung xem trước vào palette Grid Marker (`PreviewShape`, `PreviewViewport`, control `CadPreview` tự vẽ bằng `OnRender`); đọc entity và block trong bản vẽ thành hình xem trước; jig và transient.
  - Kiểm chứng: hành vi WPF chạy thật trên net48 + net8; các bước của U03 áp vào bản sao dự án (build `-warnaserror` sạch, 290/290 test, ảnh palette hai theme ở `docs/training/img/`); hành vi AutoCAD chạy trên AutoCAD 2024 (accoreconsole, HeadlessLab 19/19).
  - Các mục chỉ AutoCAD có giao diện mới kiểm được (palette có khung xem trước, xem trước đối tượng đang chọn, transient, hộp thoại chọn điểm, chọn sẵn + zoom từ application context) đã kiểm trên AutoCAD 2024 có giao diện, bằng một DLL kiểm thử chứa nguyên văn code của tài liệu. Thêm phát hiện: `SetImpliedSelection`/`SetCurrentView` chạy được từ application context cả khi **không** lock, vì chúng không ghi database.
- **Backlog mới, còn mở** (PR này chỉ ghi nhận, không sửa code):
  - **B-31**: ô số thực trên palette đọc `2,5` thành 25, vì WPF mặc định dùng `en-US`.
  - **B-32**: ô số bị xóa trắng nhưng ViewModel vẫn giữ giá trị cũ, nút Vẽ vẫn bật.
  - **B-33**: `CadPromptHelper.GetKeywords` in danh sách từ khóa hai lần.
  - T03 thêm bẫy B32–B36: màu ByLayer đọc ra trắng, `Explode` không trả attribute, jig nhận WCS, `CS0104 Polyline`, từ khóa in hai lần.
- **Chuẩn hóa**:
  - Lộ trình ~20 tuần: Giai đoạn 2 thêm tuần 7–9 cho nhánh UI; số tuần trong header T-module, README, Phần 1 và mục lục được cập nhật theo.
  - Khung năng lực thêm dòng "UI & tương tác".
  - T10 thêm lab L17–L19 và câu hỏi Q21–Q25; đồ án cuối khóa yêu cầu khung xem trước.
  - Thuật ngữ thêm phần C (giao diện).
  - Phần 3, Phần 5 và T08 dẫn sang nhánh UI.

### Added (PR #9 — đào tạo: Giai đoạn 0, nhập môn lập trình & C#)
- **`docs/training/C00`–`C05`**: lộ trình 6 tuần cho người **chưa biết lập trình**, dạy C# theo quy tắc **20/80** — chọn và sắp xếp theo số lần tính năng xuất hiện trong 110 file `.cs` của dự án (có lệnh để tự đếm lại). Mỗi khái niệm đi kèm chỗ nó xuất hiện trong code thật; ví dụ là chương trình hoàn chỉnh đã chạy trên .NET 8 với C# 10 như dự án, kèm thông báo lỗi biên dịch thật và các khác biệt trên .NET Framework 4.8 đã chạy thử. C00 bắt đầu từ đây (bài kiểm tra đầu vào), C01 chương trình đầu tiên, C02 dữ liệu & collection, C03 hướng đối tượng (đọc trọn `GridSpec.cs`), C04 C# dự án dùng hằng ngày (giải mã 6 dòng code thật), C05 async, xUnit, Git & PR, đồ án nhập môn "Planner dãy tủ điện" (nửa đầu lab L07) và đánh giá 🌰 → 🌱.
- **Chuẩn hóa bộ đào tạo**: khung năng lực **5 cấp** (thêm 🌰 Nhập môn và năng lực "C# & lập trình"); lộ trình ~18 tuần chia Giai đoạn 0–3; mỗi T-module có dòng *Cấp độ · Cần trước · Trong lộ trình*; T10 §4 thêm bậc 🌰 → 🌱; Thuật ngữ thêm phần A (lập trình & C#); README, `00_TOC`, Phần 1 dẫn người mới về C00. Sửa số tuần sai ở README đào tạo: gộp tuần 1–2 và 4–5 rút 12 tuần xuống **10** (trước ghi 8).

### Fixed (PR #7 — hồi quy của PR #6, phát hiện khi kiểm trong AutoCAD GUI)
- **`DEMO_ASYNC_AWAIT` không vẽ gì khi người dùng chuyển sang bản vẽ khác trong lúc chờ** (B-30). Bản sửa B-08 ở PR #6 ghi thông báo bằng `scope.Editor` trong giai đoạn vẽ; Editor của bản vẽ không active ném `eNotApplicable` (kể cả `WriteMessage`) và hủy cả transaction. Giờ Action vẽ chỉ thao tác database, kết quả được báo sau đó lên bản vẽ đang active, kèm tên bản vẽ đã vẽ vào.
- Lỗi của lệnh async (`CadCommandBase.ExecuteWithScopeAsync`) **không đến được người dùng** nếu họ đã chuyển bản vẽ hoặc đóng bản vẽ gốc: thông báo được ghi vào Editor bản vẽ gốc (bị nuốt), hoặc bỏ qua hẳn khi bản vẽ đã đóng. Giờ ghi lên bản vẽ đang active, kèm tên bản vẽ gốc.
- `autoRegen` sau commit gọi `Editor.Regen` cả khi bản vẽ không còn active → báo lỗi cho một thao tác đã thành công; cảnh báo "không tìm thấy block" của `InsertBlock` có thể hủy transaction tương tự. Cả hai giờ chỉ chạm Editor khi bản vẽ đang active.
- `ExecuteWithScopeAsync` trả `Task<bool>` (đã vẽ và commit hay chưa); `ICadScope.Editor`/`ICadContext.ExecuteAsync` ghi rõ giới hạn. T03 B31 (bẫy mới), T09 B-30.

### Added (PR #6 — sửa backlog T09 §3)
- **HeadlessLab** (`tests/integration/HeadlessLab`, `run-headless-lab.ps1`): DLL chỉ dành cho kiểm thử, **link** lại mã không-UI (`Extensions/`, `Utils/`, `PlotService`…) và chỉ tham chiếu `AcDbMgd` + `AcCoreMgd`, nên chạy được trong accoreconsole (profile `/isolate`, không đụng thiết lập AutoCAD thật). 16 kiểm thử trên AutoCAD 2024 thật, đều PASS; 7 trong số đó FAIL trên v1.0.0. Lab nằm trong solution nên CI luôn biên dịch nó (ADR-015). Plugin vẫn là một DLL.
- **Tab Ribbon "CadCore"** (`Commands/CadCoreRibbon.cs`): Grid Marker, palette, Layer Manager, Debug Inspector, CORE_INFO. `IRibbonService.RemoveTab`.
- **Analyzer CAD002** (không prompt — `CadPromptHelper.*`, `Editor.Get*` — trong Service/ViewModel) và **CAD003** (không `OpenMode.ForWrite`/`UpgradeOpen()` trong lambda của `ExecuteRead`), kèm `AnalyzerTests` (biên dịch mẫu bằng Roslyn, cả CAD001).
- `Utils/SymbolNameRules` (luật tên ký hiệu thuần .NET); `SelectionFilterBuilder.OnLayersExact` / `WithBlockNamesExact` / `EscapeWildcards`; `ThemeManager.Attach(palette)`; `Core/CadThread`; `Core/PaletteGuids`; `ViewModelBase(ILoggerService?)`; token màu `OnAccentBrush`, `SurfaceAltBrush`, `SurfaceHoverBrush`, `OverlayBrush`.
- Test: 196 → **275 × 2 TFM** — theme (cùng bộ key ở hai theme, mọi key XAML resolve ở cả hai theme, đổi theme khi palette đang mở), manifest lệnh, Guid palette, thread guard, settings nhiều phiên, ghi atomic, analyzer…

### Changed (PR #6) — lưu ý khi nâng cấp dự án dựng từ template
- **Một nguồn màu:** `Themes/Styles.xaml` chỉ còn Style; màu chuyển sang `AutoCadTheme.xaml`/`AutoCadLightTheme.xaml` (cùng bộ key). XAML tự viết chỉ nạp `Styles.xaml` sẽ **không còn màu** → gọi `ThemeManager.Attach(this)` sau `InitializeComponent()`.
- **Bỏ `ViewModelLocator`**: `CommonControlsPalette` lấy ViewModel từ DI trong code-behind, gán cho chính UserControl (B-26).
- `Ensure*`, `CreateBlockDefinition`, `EnsureRegApp` **validate tên**: tên sai ném `ArgumentException` nêu rõ ký tự (trước: `eInvalidInput` của AutoCAD). `Ensure*` mở bảng ForRead, `UpgradeOpen` chỉ khi thêm.
- `PaletteService`: mọi PaletteSet có Guid ổn định (từ `[assembly: Guid]` + tên) — đổi tên palette là mất vị trí đã lưu; `ShowPalette` ném lại lỗi như `TogglePalette`.
- `ICadContext`: gọi từ thread khác main thread → `InvalidOperationException` có hướng dẫn; `ExecuteAsync` ném sớm nếu không có `SynchronizationContext`.
- `FileSystemService.WriteAllText` ghi atomic (tệp tạm + `File.Replace`); `SettingsService.Save` đọc lại tệp, chỉ áp các key phiên này đã đổi, named mutex giữa các tiến trình AutoCAD.
- `PlotService`: chỉ in bản vẽ đang làm việc; vùng in `Extents` (trước: theo layout/Display), fit + center; tạm `BACKGROUNDPLOT = 0` và chuyển layout hiện hành, rồi khôi phục.
- `Deployment/PackageContents.xml` liệt kê đủ 9 lệnh không phải demo (test `PackageManifestTests`).
- Nút Primary chữ trắng (`OnAccentBrush`; chữ đen cũ tương phản ~3:1). Template `TabItem` theo theme; nút Secondary có viền.

### Fixed (PR #6)
- **Theme:** `IsDarkMode` đọc `COLORTHEME` thay vì sysvar không tồn tại `DARKMODE` (B-01); hai hệ màu → một (B-02); `LayerManagerControl` dùng 3 key không tồn tại (B-03); palette đang mở không đổi theo theme (B-23); theme sáng thiếu `CadComboBox`.
- **Ribbon:** không ai dựng; NETLOAD muộn không build; `AddButton` nhân đôi nút khi dựng lại (B-05).
- **Palette** không nhớ vị trí (B-04).
- `DEMO_ASYNC_AWAIT` vẽ vào `MdiActiveDocument` sau `await` → dùng `ExecuteWithScopeAsync` (B-08).
- `XRecordHelper.RemoveMetadata` để lại Xrecord mồ côi (B-09).
- **`PlotService.PlotLayoutToPdf`/`PlotMultipleLayouts` luôn thất bại** (B-28, do HeadlessLab phát hiện): `InvalidCastException` (`StringCollection`), `eInvalidInput` ở `SetPlotCentered` với layout giấy, `eLayoutNotCurrent`, số trang truyền vào tham số copies; cùng B-10 (`ProcessPlotState`, `BACKGROUNDPLOT`, Dispose `PlotEngine`).
- `PathProvider.GetPluginDirectory` khi nạp từ byte array (B-11); `CreateBlockDefinition` không Dispose entity không dùng (B-12); `Draw*` rò rỉ entity khi `configure` ném lỗi (B-14); `SelectionFilterBuilder` không escape wildcard (B-17).
- `CopyEntity`/`MirrorEntity` làm **mất Extension Dictionary** (metadata XRecord) → `DeepCloneObjects` (B-13).
- Attribute căn khác Left lệch sau `InsertBlock`/`SetBlockAttribute` (B-25, đã kiểm chứng: tâm X 42.507 thay vì 20).
- `RunSafe`/`RunSafeAsync` không log exception (B-24); comment sai lý do ở `CadContextGuard`/`EnsureLayer` (B-19).
- Debug Inspector: cột Result luôn "True/False" (giá trị local thắng DataTrigger); chữ trong tab đang chọn bị đậm (B-29).
- `CadPromptHelper`, `CoordinateHelper` phụ thuộc ngầm `AcMgd` (namespace `ApplicationServices` thay vì `ApplicationServices.Core`) — lộ ra khi dựng HeadlessLab.
- **Template (`dotnet new cad-plugin`) cắt mất mọi khối `#if`** trong dự án sinh ra (template engine coi chúng là điều kiện template): `SetServiceProviderForTesting`, mức log Debug của bản Debug, `[assembly: SupportedOSPlatform]` — lỗi có từ trước. Các khối giờ được bao bằng `//-:cnd:noEmit` … `//+:cnd:noEmit` (T03 A15). Mẫu loại trừ `**/*.log` (log trong thư mục con từng bị chép vào dự án mới).
- Test theme phụ thuộc thứ tự chạy (chỉ pass nếu test dựng palette chạy trước; lộ ra khi tên test đổi trong dự án sinh ra) — T03 A16.
- Kiểm chứng: cả `dotnet new cad-plugin` (hive cô lập) lẫn `init-template.ps1` (bản sao repo) cho dự án build `-warnaserror` 0 cảnh báo, 275 test pass × 2 TFM, HeadlessLab 16/16 PASS sau khi đổi tên.

### Fixed (docs, PR #6)
- **Đính chính B-13:** `Clone()` **không** làm mất attribute (giả thuyết cũ sai, HeadlessLab chứng minh); cái mất là Extension Dictionary. Đính chính ví dụ wildcard: layer `A*B` không thể tồn tại; ví dụ thật là `LAB#1`, `M.EQUIP`/`M-EQUIP`.
- T01–T10, Phần 3/5/9, README cập nhật theo hành vi mới; T09 §3 có cột Trạng thái (giữ nguyên lịch sử), thêm B-28/B-29, ADR-015; lab sửa backlog làm trên tag `v1.0.0` rồi đối chiếu với PR #6.

### Added
- **🎓 Học viện CadCoreFramework (`docs/training/`)**: bộ tài liệu đào tạo chuyên sâu cho người kế thừa dự án, từ Fresher lên Senior. Gồm lộ trình 12 tuần + khung năng lực 4 cấp (README); T01 Nền tảng AutoCAD .NET; T02 Mổ xẻ kiến trúc; T03 Sổ tay sự cố (14 sự cố thật của dự án + 30 bẫy API); T04 Debug & chẩn đoán; T05 Kiểm thử; T06 Build/phát hành/DevOps; T07 Hiệu năng; T08 Công thức mở rộng; T09 Sổ tay Senior (14 ADR, checklist review, **backlog kỹ thuật 27 mục đã kiểm chứng**); T10 16 bài lab + đồ án + đề đánh giá lên cấp; Thuật ngữ Anh–Việt. Tên API AutoCAD trong tài liệu được đối chiếu với metadata `AcDbMgd`/`AcCoreMgd` 24.0; giá trị sysvar được kiểm bằng accoreconsole `/isolate`; toàn bộ link/anchor được kiểm bằng script (0 lỗi).

### Fixed (docs)
- Phần 1: lộ trình Fresher từng dạy prompt **trong Service** và `ExecuteAsync` cho thao tác đồng bộ (anti-pattern), nay theo đúng tầng Golden Sample; thông điệp `CORE_INFO` đúng với code.
- Phần 2: "quên `Commit()` thì AutoCAD crash" → thực tế là Abort âm thầm; Undo gom theo lệnh (UndoMark cần cho application context); lý do cần `AssemblyResolve`; ESC khi prompt trả `PromptStatus.Cancel`, không phải exception.
- Phần 3: ví dụ "Vẽ Tủ Điện" **không biên dịch được** (`style:` → `textStyle:`; chuỗi bị truyền vào tham số `bool autoRegen` → `undoMarkName:`); Jig chạy tuần tự (không "song song") với constructor `RectangleJig(corner1, polyline)` thật + `Dispose` khi hủy; bỏ lập luận `eVetoed`.
- Phần 4: giới hạn XData 16 KB tính **mỗi đối tượng** (không phải toàn bản vẽ); sơ đồ Xrecord đúng với `XRecordHelper`.
- Phần 5: `RunSafeAsync` không dùng `Dispatcher.Invoke`; resource key có thật; cảnh báo lỗi theme `DARKMODE`; công thức Ribbon.
- Phần 6: không có kiểu `UserBreakException`; prompt thuộc về lệnh chứ không thuộc Service; ví dụ Moq dùng `GetAllLayers`; giải thích đúng gốc rễ của hot-reload.
- Phần 7: viết lại theo chữ ký thật (`InsertBlock` thay cho `DrawBlockReference` không tồn tại; EventAggregator giữ tham chiếu **mạnh**; extension methods **không** tự mở transaction; bỏ `IsAlmostEqualTo` không tồn tại).
- Phần 9: `smoke.scr` không chạy được trong accoreconsole; kiểm tra trên AutoCAD thật là thủ công.
- CHANGELOG: đính chính hai dòng lịch sử mô tả API `Database.BeginUndoMark()` không tồn tại.

---

## [1.0.0] — 2026-10-13

Bản phát hành đầu tiên: [GitHub Release v1.0.0](https://github.com/Tmphi/CadCoreFramework/releases/tag/v1.0.0) (`CadCoreFramework_v1.0.0_Setup.zip`). Gồm toàn bộ các nhóm thay đổi dưới đây, cùng với nền tảng template ban đầu (nhóm "Added (initial template baseline)" ở cuối mục).

### Changed (template hygiene)
- **Demo suite isolated in `Demos/`** (was `Commands/Demos/` + `Commands/DemoCommands.cs`), namespace `CadCoreFramework.Demos`. Its `CommandClass` registrations moved to `Demos/DemoAssemblyInfo.cs`. New MSBuild property **`IncludeDemos`** (default `true`): build with `/p:IncludeDemos=false` — or set it in the csproj — and every `DEMO_*` command drops out of the DLL. Golden Sample, `LayerManager` and `CommonControlsPalette` are unaffected.
- **`CadCommandBase` injection constructor** now takes an optional `ICommandHistoryService` — with all three services injected, a command no longer touches the static `AppBootstrapper.ServiceProvider`.
- **`dotnet new cad-plugin` produced `Author.csproj` / `Author.sln`** instead of the `-n` name: the `AuthorName` symbol replaced the same `"CadCoreFramework"` token as `sourceName`. Symbol removed; new parameters `--GitHubRepo owner/name` (README badge + CAD001 help link) and `--IncludeDemos false`. Analyzer/Benchmarks project GUIDs were missing from `guids` (every generated plugin shared them) — added. Local junk (`packages/`, `nuget.exe`, `build_log.txt`, `.claude/`, nested `bin/obj`) is now excluded. Verified: generated solution builds with `-warnaserror` and passes all tests.
- **`init-template.ps1` failed to parse on Windows PowerShell 5.1** (`#Requires -Version 5.1`): UTF-8 without BOM is read as cp1252, and the `0x94` byte of `—` is a curly quote → unterminated string. All `.ps1` scripts with non-ASCII text now carry a BOM (`.editorconfig` documents why). New `-GitHubRepo` / `-RemoveDemos` switches; the WiX `UpgradeCode` is now regenerated in sync with `PackageContents.xml`. Verified end-to-end on a copy: rename + build + tests pass.
- **MSB3270 under `-warnaserror`** (AnyCPU project referencing AMD64 `AcSeamless.dll`): `Tests` and `Testing` projects now `PlatformTarget=x64`, like the plugin. Debug and Release solution builds are 0 warnings again.
- **Integration runner (`tests/integration`) reported "passed" without ever running a command.** Four bugs: accoreconsole's UTF-16 stdout was never decoded (no marker could match), `;` comments in `smoke.scr` were sent to the command line, blank lines re-ran `NETLOAD` and swallowed the command names, the DLL path was written with doubled `\\`, and auto-discovery picked the last *character* of the path when only one AutoCAD is installed. All fixed; each command must now print its own success message, `Unable to load` / `Unknown command` / `[Lỗi …]` fail the run, and `/isolate` keeps `SECURELOAD 0` out of the real AutoCAD profile. **Known limitation (documented, exit code 3):** the plugin references `acmgd.dll`, and accoreconsole crashes with a stack overflow loading it (NETLOAD of `acmgd.dll` alone reproduces it) — headless testing needs a separate non-UI DLL.
- **Release bundle shipped another product's user guide:** `Deployment/HDSD.txt` advertised features that do not exist here (device library, Excel import/export), and `Install.bat` called the plugin an "electrical device manager" — both left over from the project this template was extracted from. Rewritten to the real command list; hard-coded `v2.0` removed. Install/Uninstall verified against a throwaway `APPDATA`.
- **`release.yml` (never run before):** the tag version was never passed to `package.ps1` (every release was `_v2.0_`), and the release zip re-compressed `dist\*`, nesting `package.ps1`'s own zip inside it. Now `-Version` comes from the tag and the single setup zip is published. `package.ps1` stamps that version into the bundle's `PackageContents.xml` (case-sensitive replace — a case-insensitive one corrupted `<?xml version=…?>`). Simulated locally with tag `v1.3.0`.
- `.gitattributes`: `*.bat` / `*.cmd` always checked out CRLF (cmd.exe mis-parses LF-only batch files).
- Docs: cookbook called a non-existent `DynamicBlockHelper.SetProperty` (→ `SetVisibilityState`); DevOps chapter described the CI badge as something still to add.
- README: "Tạo dự án mới từ template" section; learning-path table lists parts 8–9.
- **No more silent `catch { }` in core infrastructure:** COM `StartUndoMark`/`EndUndoMark` failures (`CadContext`, `UndoGroupManager`), overrule unregister failures (`OverruleManager`) and palette ViewModel dispose failures (`PaletteService`) are now logged. `CadContext`, `OverruleManager` and `UndoGroupManager` accept an optional `ILoggerService` (DI supplies it automatically).

### Fixed (first real AutoCAD run)
- **Plugin failed to initialize on AutoCAD 2024** with `MissingMethodException` in `Serilog.Sinks.File`. AutoCAD 2024 ships and preloads Serilog 2.9 / Serilog.Sinks.File 4.1; Serilog keeps AssemblyVersion 2.0.0.0 across 2.x/3.x, so the plugin was bound to AutoCAD's copy. **Serilog removed**; `LoggerService` is now a dependency-free file logger (same `log-yyyyMMdd.txt` files and line format, `FileShare.ReadWrite`, 31-day retention, never throws). 10 new tests.
- **net8.0-windows output contained only `CadCoreFramework.dll`** (no DI / MVVM / Newtonsoft) → would fail to NETLOAD on AutoCAD 2025+. Enabled `EnableDynamicLoading` for that TFM so dependencies are copied (AutoCAD assemblies still excluded).
- net48: explicit `Microsoft.CSharp` reference (needed by `dynamic` COM calls; previously arrived transitively via Serilog).
- **Palettes failed with "The component '…' does not have a resource identified by the URI '…'"** when the plugin is loaded from a byte array (anti-lock loader / hot-reload). WPF resolves the plugin assembly by name for BAML; the byte-loaded assembly is invisible to the CLR binder, so a second copy got loaded from disk. `AssemblyResolve` now first returns an already-loaded assembly with the same name + version (`AppBootstrapper.FindLoadedAssembly`, 5 tests).
- **`SHOW_UI_TEMPLATE` failed with "Provide value on StaticResourceExtension threw an exception"**: `CommonControlsPalette` referenced 33 `Styles.xaml` keys via `{StaticResource}`, but the styles are merged in code-behind *after* `InitializeComponent`. Switched those to `{DynamicResource}` and load styles right after `InitializeComponent` (no unstyled flash).
- **New `XamlPaletteTests`**: instantiate all 4 palettes for real on an STA thread (DI with mocks, no AutoCAD) — catches XAML/resource/DI-registration errors at test time. Verified it reproduces the exact `SHOW_UI_TEMPLATE` error on the old markup. Test project now `UseWPF`.

### Changed (Consistency pass — align the whole repo with the Golden Sample)
- **`ICadContext` error convention:** `Execute*` throws · new `TryExecute(Action)` / `TryExecute<T>(Func)` return `CadResult` and never throw. `Execute<T>` (which returned `CadResult` despite its name) is now `[Obsolete]` and forwards to `TryExecute<T>`; `CadContext` now has a single throwing core that every overload wraps.
- **`ExecuteRead`** locks the document when called from application context (palette / modeless), detected via `DocumentManager.IsApplicationContext`.
- **`LayerService.ToggleLayerVisibility`** (was `ToggleLayerVisibilityAsync`) — synchronous `TryExecute` + undo mark + regen, returns `CadResult<bool>`; no more sync work wrapped in `ExecuteAsync`. `LayerManagerViewModel` reports missing-layer / CAD-failure cases.
- **`scope.Ask*()`** prompt extensions marked `[Obsolete]` (they prompt while the transaction + lock are open); use `CadPromptHelper.Get*()` before `ICadContext`. Added `CadPromptHelper.GetAngle`.
- **Fail-loud service resolution** across `Commands/` and `Commands/Demos/`: `GetRequiredService<T>()` instead of `ServiceProvider?.GetService<T>()`; `CadCommandBase.CadContext` throws when DI is unavailable. Section banners / "HOÀN TẤT" footers moved inside `ExecuteCommand` so they only print on success (fixes `CORE_TEST_ALL` printing "passed" after a failure — relevant to `smoke.scr` log parsing).
- `PaletteService.TogglePalette` logs **and rethrows** instead of swallowing; `LayerManagerControl` / `DebugInspectorPaletteControl` fail loud when their ViewModel is missing; `DebugInspectorViewModel` takes `IEventAggregator` by constructor injection.
- `DEMO_UNDO_GROUP` used expression lambdas that silently bound to the non-throwing generic overload — now block lambdas.
- `.agents/rules/project_rules.md` updated to the Golden Sample conventions.

### Fixed (build) — clean build is now **0 warnings** on both TFMs
- **NU1608**: `AutoCAD.NET` 25.0.0 → 25.0.1 (25.0.0 pinned the pre-release `AutoCAD.NET.Core 25.0.0-V058`).
- `GenerateDocumentationFile=true` (CS1591 suppressed) so IDE0005 runs on build; removed ~190 unused `using` directives across 40+ files (`dotnet format style --diagnostics IDE0005`).
- Fixed malformed XML doc comments (CS1570: unescaped `&` / raw XAML in `<summary>`) and missing `<param>` tags (CS1573).
- `Templates\**` excluded from the plugin compile — the `cad-command` item-template source was being built into `CadCoreFramework.dll`.

### Added (tests)
- `CadCoreFramework.Tests` / `CadCoreFramework.Testing` multi-target **`net48;net8.0-windows`** — the suite now runs on both AutoCAD runtimes (CI uses `LogFilePrefix` so the two `.trx` files don't collide).
- `LayerManagerViewModelTests`, `DebugInspectorViewModelTests`, `TryExecute` tests; `LayerServiceTests` rewritten for the new API. 176 tests per TFM.

### Added (Golden Sample — Grid Marker)
- **`Samples/GridMarker/`** — reference feature showing the canonical layering end-to-end: `GridSpec` (immutable model + validation) → `GridPlanner` (pure logic, no AutoCAD types) → `GridMarkerService` (thin AutoCAD shell: `Execute<T>` + UndoMark + XData, publishes `GridDrawnMessage`) → `GridMarkerCommands` (`SAMPLE_GRID`, prompts before the transaction) and `GridMarkerViewModel` / `GridMarkerPalette` (validation-driven `CanExecute`, enum status instead of `Brush`, `TrackSubscription`).
- **`docs/09_GOLDEN_SAMPLE.md`** — layer-by-layer walkthrough, linked from `00_TOC.md`.
- **`CadCommandBase.GetRequiredService<T>()`** — fail-loud service resolution for command classes.
- 56 tests under `CadCoreFramework.Tests/Samples/GridMarker/` (164 total). `SAMPLE_GRID` added to `tests/integration/smoke.scr`.

### Changed (Golden Sample)
- `SHOW_PALETTE` now opens the Grid Marker palette.

### Removed (Golden Sample)
- `ISampleDrawingService` / `SampleDrawingService`, `SampleViewModel`, `SamplePaletteControl` — superseded by the Golden Sample (they prompted from inside a service, wrapped sync work in `ExecuteAsync`, and kept WPF `Brush` in the ViewModel).

### Fixed
- `CadCommandBase.ExecuteWithScope[Async]` no longer reports success when `ICadContext` is unavailable; async variant writes errors to the originating document.
- `CadContext.ExecuteAsync` aborts if the captured document was closed during the async phase.
- `EventAggregator.Publish` invokes every subscriber even if one throws (then rethrows); subscription tokens are one-shot on Dispose.

### Added (Developer-ergonomics "ăn chơi" bundle)
- **`.editorconfig`** — extended with async-method naming rule and ~20 CA-rule severity tunings (noisy stylistic rules → `suggestion` or `none`; real-value rules stay `warning`). Analyzer config in main csproj: `EnforceCodeStyleInBuild=true`, `AnalysisLevel=latest`, `AnalysisMode=Recommended`. Clean build 115 warnings → 0.
- **`snippets/*.snippet`** — Visual Studio code snippets: `cadcmd`, `cadsvc`, `cadscope`, `cadvm`. Install docs in `snippets/README.md`.
- **`.vscode/cadcoreframework.code-snippets`** — same four snippets plus `cadasync`, `cadfilter`, `caddraw`, `cadguard` for VS Code / Cursor; picked up automatically when the workspace loads.
- **`Views/DebugInspectorPaletteControl.xaml`** + **`ViewModels/DebugInspectorViewModel.cs`** — WPF palette with three tabs: Command History (DataGrid), DI Services (lifetime/interface/impl), Event Subscribers (handler counts). Opens via new **`CORE_DEBUG`** command. Supported by `AppBootstrapper.RegisteredServices` snapshot and `EventAggregator.GetSubscriberSnapshot()`.
- **`docs/08_COOKBOOK.md`** — 11 copy-paste recipes (Title block A3, Batch PDF export, Purge+Audit, XRef attach, Free polyline jig, Dynamic block attribute update, Layer merge, Selection filter patterns, XData round-trip, Subscription Dispose, Async heavy work). Linked from `00_TOC.md`.
- **`CadCoreFramework.Benchmarks/`** — BenchmarkDotNet project. 3 suites: `CadResultBenchmarks` (Result vs raw vs throw/catch), `EventAggregatorBenchmarks` (publish at 1/10/100 subscribers), `CommandHistoryBenchmarks` (Record steady-state). Pure-C# only — no AutoCAD runtime required.
- **`Deployment/installer/`** — WiX 4 installer scaffold: `CadCoreFramework.wxs` + `build-msi.ps1` + README. Produces a per-user MSI that drops the bundle into `%AppData%\Autodesk\ApplicationPlugins`.
- **`CadCoreFramework.Analyzers/`** — custom Roslyn analyzer. Ships one rule: **CAD001** forbids `Application.DocumentManager.MdiActiveDocument` in classes under `Services/` and `ViewModels/`. Wired into the main project via `OutputItemType="Analyzer"` so the analyzer DLL is not shipped inside `CadCoreFramework.dll`.
- **`Templates/cad-command/`** — `dotnet new cad-command` item template. Generates `Commands/XxxCommand.cs` with `CadCommandBase` skeleton + `ExecuteWithScope` + inline reminder to register in `AssemblyInfo.cs`. Parameters: `--AutoCadCommandName`, `--UndoMark`.
- **`Core/EventAggregator.GetSubscriberSnapshot()`** — read-only subscriber count per message type (used by Debug Inspector).
- **`Core/AppBootstrapper.RegisteredServices`** — snapshot of the `IServiceCollection` used to build the provider (used by Debug Inspector).

### Added (Final 10/10 polish)
- `CadCoreFramework.Tests/LayerServiceTests.cs` — 6 control-flow tests for `LayerService` using `FakeCadContext` (verify ExecuteRead/ExecuteAsync call counts, undoMarkName, exception propagation).
- `CadCoreFramework.Tests/PathProviderTests.cs` — 3 pure-C# tests for `PathProvider`.
- `CadCoreFramework.Tests/FileSystemServiceTests.cs` — 6 pure-C# tests for `FileSystemService` (file I/O round-trips, idempotency, missing-path handling).
- `tests/integration/smoke.scr` + `run-integration-tests.ps1` + `README.md` — accoreconsole-driven integration test scaffolding that NETLOADs the plugin and runs CORE_INFO, CORE_TEST_ALL, DRAW_CIRCLE_SAMPLE; parses the console log for failure markers. Runs locally (self-hosted CI) because GitHub Actions `windows-latest` lacks AutoCAD.

### Changed (Final 10/10 polish)
- `Core/CadContext.cs` — `ExecuteCore` and `ExecuteCore<T>` now open `CurrentSpace` with `OpenMode.ForRead`; `AppendEntityToCurrentSpace` lazy-upgrades to `ForWrite` only when a caller actually writes. Removes unnecessary write locks on pure-read Execute lambdas.
- `Utils/BlockHelper.cs` — `InsertBlock` lazy-upgrades `space` to `ForWrite` to match the new CadContext default.
- `CadCoreFramework.csproj` — `<Nullable>annotations</Nullable>` enabled on the main project. Lets public API use `?` / non-null annotations without flow-analysis noise from the un-annotated AutoCAD API.
- Nullable annotations added to: `AppBootstrapper.ServiceProvider`, `ICadContext.Execute`/`ExecuteAsync`/`ExecuteBatch` (undoMarkName, progress), `IEventAggregator.Subscribe`/`Unsubscribe`, `ICommandHistoryService.Record`, `ViewModelBase.RunSafe`/`RunSafeAsync` (errorPrefix), `Guard.NotNull`/`NotEmpty`, `SelectionFilterBuilder.OfTypes`.
- `CadCoreFramework.Testing/Fakes/FakeCadScope.cs` — properties switched from `Document?` to `Document` + `null!` to match the (correctly non-nullable) `ICadScope` interface contract.
- Tests pass: 90 → 105 (added 15 service/path/filesystem tests).

### Added (Tier 3 — Template polish & docs sync)
- `init-template.ps1` — extended with `-ProjectName` parameter for full namespace/file/directory rename workflow when cloning without `dotnet new`; GUID regeneration now also refreshes all project and solution GUIDs in the `.sln` file.
- `docs/06_DEVOPS_AND_STANDARDS.md` — new §6.3 CI/CD section documenting the GitHub Actions workflows; §6.2 testing section updated with `FakeCadContext` example and coverage-report commands.
- `docs/01_GETTING_STARTED.md` — Bước 1 now uses `dotnet build` instead of the removed `build.ps1` reference.

### Added (Tier 2 — Tooling & safety)
- `Core/CadContextGuard.cs` — `[ThreadStatic]` sentinel that detects and rejects nested `CadContext.Execute` / `ExecuteRead` calls on the same thread with a clear diagnostic message.
- `Core/CadContext.cs` — `CadContextGuard` wired into all three entry points: `ExecuteCore` (void), `ExecuteCore<T>`, and `ExecuteRead`.
- `CadCoreFramework.Tests/CadContextGuardTests.cs` — 5 unit tests covering `IsActive`, `Enter`, `Exit`, nested-throw, and re-enter-after-exit.
- `.template.config/template.json` — `dotnet new cad-plugin` template manifest: `sourceName` drives namespace/file renaming, `guids` array triggers GUID regeneration, `sources.modifiers.exclude` omits build artifacts.
- `.config/dotnet-tools.json` — ReportGenerator 5.3.8 declared as a local dotnet tool.
- `.github/workflows/build-and-test.yml` — `dotnet tool restore` + `--collect:"XPlat Code Coverage"` + `reportgenerator` HTML/Cobertura + coverage artifact upload.

### Added (Tier 1 — Infrastructure)
- `.github/workflows/build-and-test.yml` — CI pipeline: build Release + run tests on `windows-latest`, upload `.trx` test-result artifact.
- `.github/workflows/release.yml` — release pipeline: triggered by `v*` tags, packages `.bundle` and creates GitHub Release.
- `CadCoreFramework.Testing/` — new project with `FakeCadContext` and `FakeCadScope` test doubles implementing `ICadContext` / `ICadScope` without requiring AutoCAD runtime.
- `CadCoreFramework.Tests/FakeCadContextTests.cs` — 7 demonstration tests showing injection of exceptions, `LastUndoMarkName`, `ExecuteCount`, async and batch overloads.
- `Commands/CadCommandBase.cs` — `ExecuteCommand`, `ExecuteWithScope`, `ExecuteWithScopeAsync` helpers with `[CallerMemberName]` auto-capture; `CommandHistory` auto-wired via `AppBootstrapper.ServiceProvider` lazy lookup.

### Added
- `CircleData` model replacing the empty, Vietnamese-named `HinhTron` class.
- `ICommandHistoryService` / `CommandHistoryService` — tracks executed command names, durations, and success state in a fixed-capacity (50) thread-safe in-memory buffer.
- `XRecordExtensions` — `SetXRecord<T>`, `GetXRecord<T>`, `RemoveXRecord` extension methods on `ICadScope` for a consistent XRecord API aligned with `CadScopeDrawingExtensions`.
- `SelectionFilterBuilder.WithXData(appName)` — filters entities by XData RegApp namespace in one fluent call.
- `.gitignore` covering Visual Studio artefacts, build outputs, NuGet packages, machine-specific `.csproj` files, and OS junk files.
- `InternalsVisibleTo("CadCoreFramework.Tests")` in `AssemblyInfo.cs`.
- `AppBootstrapper.SetServiceProviderForTesting()` (DEBUG-only) for xUnit DI isolation.
- Real xUnit tests: `XDataHelperTests` (7 cases) and `SelectionFilterBuilderTests` (8 cases) — run without AutoCAD.

### Changed
- **`EventService`** — fixed lambda event subscription bug. Delegate references are now stored in `Dictionary<Document, Handler>` enabling proper `UnhookDocument` via `-=`. Previously, anonymous lambdas could not be unsubscribed, causing permanent event handler accumulation over long sessions.
- **`CadContext`** — `EndUndoMark` is now in a `finally` block to guarantee pairing even when the action throws. *(Đính chính 2026-10-13: dòng này từng ghi UndoMark được chuyển sang API managed `Database.BeginUndoMark()`. API đó **không tồn tại** trong AutoCAD .NET; UndoMark luôn dùng COM `StartUndoMark`/`EndUndoMark` qua `dynamic`. Xem [T03 A14](docs/training/T03_PITFALLS_CASEBOOK.md#a14-changelog-mô-tả-một-api-không-tồn-tại).)*
- **`.csproj`** — multi-target `net48` (AutoCAD 2021–2024) + `net8.0-windows` (AutoCAD 2025+) with version-gated AutoCAD NuGet references (24.x vs 25.x). Removed hard-coded `HintPath` pointing to `C:\Program Files\Autodesk\AutoCAD 2021\`.
- **`AppBootstrapper`** — `OverruleManager` and `ICommandHistoryService` registered in the DI container. `OverruleManager.Dispose()` now called explicitly in `Terminate()` before the DI container is disposed.
- `CadCoreFramework.Tests.csproj` — upgraded from bare skeleton to full xUnit + FluentAssertions + Moq setup.
- All comments and XML documentation translated from Vietnamese to English.

### Removed
- `CadCoreFramework-DESKTOP-JFVJFBQ.csproj` — machine-specific VS conflict file.
- `Models/HinhTron.cs` — replaced by `Models/CircleData.cs`.

### Fixed
- `EventService` lambda leak — handlers were permanently attached to `Database.ObjectAppended` / `ObjectErased` and could never be removed.
- `CadContext` COM UndoMark — `EndUndoMark` guaranteed in `finally`. *(Đính chính 2026-10-13: không có "managed API" thay thế; vẫn là COM, và từ v1.0.0 lỗi COM được ghi log thay vì bị nuốt.)*
- Build portability — `HintPath` for `Newtonsoft.Json` pointed to a machine-specific path, breaking CI builds and developer onboarding.

### Added (initial template baseline)
- `AppBootstrapper` — `IExtensionApplication` entry point with proper `AssemblyResolve` ordering to prevent JIT FileNotFoundException.
- `ICadContext` / `CadContext` — transaction management abstraction with `Execute`, `ExecuteRead`, and `ExecuteAsync` (2-phase async pattern).
- `CadCommandBase` — base class for all command handlers with built-in error catching, distinguishing `UserBreak` (ESC) from real exceptions.
- `RibbonService` — programmatic Ribbon Tab/Panel/Button management with auto-rebuild after `CUI` / `MENULOAD` commands.
- `EventService` — MDI-aware document event aggregation.
- `PaletteService` — dockable WPF Palette lifecycle management with `IsBusy` integration.
- `XDataHelper` — JSON-over-XData with multi-chunk support and Unicode-safe ASCII escaping.
- `XRecordHelper` — JSON-over-XRecord (no 16 KB limit).
- `CadScopeDrawingExtensions` — 800+ line library of one-liner drawing methods (`DrawLine`, `DrawRectangle`, `DrawMText`, `EnsureLayer`, …).
- `SelectionFilterBuilder` — fluent `SelectionFilter` builder.
- `DynamicBlockHelper` — reads and writes Dynamic Block attribute values.
- `CadPromptHelper` — wraps `Editor.GetPoint`, `GetString`, `GetEntity`, etc.
- WPF MVVM stack: `ViewModelBase`, `IBusyIndicator`, `ViewModelLocator`.
- WPF Hot-Reload trick via timestamped `AssemblyName` in Debug builds.
- Jig system: `CadDrawJigBase`, `CadEntityJigBase`, `BlockPlacementJig`, `RectangleJig`.
- `OverruleManager` — safe registration and cleanup of AutoCAD Overrules.
- File logging via `ILoggerService` / `LoggerService` (originally Serilog; replaced by a self-written file logger because AutoCAD 2024 preloads an incompatible Serilog — see "Fixed (first real AutoCAD run)").
- `ThemeManager` — automatic WPF theme switching following AutoCAD Dark/Light mode.

[Unreleased]: https://github.com/Tmphi/CadCoreFramework/compare/v1.1.1...HEAD
[1.1.1]: https://github.com/Tmphi/CadCoreFramework/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/Tmphi/CadCoreFramework/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Tmphi/CadCoreFramework/releases/tag/v1.0.0
