> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← T08](T08_RECIPES_EXTENDING.md) · **T09** · [T10 →](T10_LABS_AND_ASSESSMENT.md)

# T09 — Sổ Tay Senior

Senior không chỉ viết code đúng. Senior **giữ cho cả hệ thống đúng theo thời gian**: biết vì sao kiến trúc như hiện tại, nhận ra khi nào nó cần đổi, review để lỗi không lọt vào, và quản lý danh sách nợ kỹ thuật một cách trung thực.

---

## 1. ADR: Vì sao kiến trúc như hiện tại

*Architecture Decision Record*: mỗi quyết định ghi **bối cảnh → quyết định → hệ quả**. Muốn đổi một quyết định, viết ADR mới thay thế, đừng âm thầm làm khác.

| ADR | Quyết định | Bối cảnh / lý do | Hệ quả (cả tốt lẫn xấu) |
| :---: | :--- | :--- | :--- |
| 001 | **Một DLL duy nhất** chứa cả lõi lẫn UI | Nạp đơn giản (một file, tool nạp byte array của nhóm); người dùng chỉ cài một thứ | ✅ Đơn giản. ❌ Tham chiếu `AcMgd` nên **không chạy headless** (B-20); không tách được lõi để tái dùng |
| 002 | Mọi thao tác bản vẽ đi qua **`ICadContext`** (lock + transaction + undo mark) | Loại bỏ lỗi lock/commit/undo lặp lại khắp nơi; test được bằng Fake | ✅ Nguyên tử, nhất quán. ❌ Thêm một lớp trừu tượng; side database phải xử lý riêng |
| 003 | **`Execute*` ném, `TryExecute*` trả `CadResult`** | Tên phải nói lên hành vi; `Execute<T>` cũ trả Result "âm thầm" gây bẫy overload | ✅ Rõ ràng. Lambda biểu thức vẫn có thể chọn nhầm overload (giờ thành lỗi build vì `[Obsolete]` + `-warnaserror`) |
| 004 | **Prompt chỉ ở điểm vào** (Command); Service không prompt | Service dùng chung cho palette (application context, không prompt được); prompt trong transaction treo AutoCAD | ✅ Service test được, dùng được mọi nơi. ❌ Palette cần cách nhập khác (form, gửi lệnh) |
| 005 | **Functional core / imperative shell** (Spec, Planner thuần; Service mỏng) | Code trong lambda không test kết quả được | ✅ Test kết quả 100% cho logic. ❌ Thêm vài class cho mỗi tính năng |
| 006 | **MS DI**; service locator **chỉ ở biên** (lệnh, code-behind) | AutoCAD/WPF tạo instance bằng constructor không tham số | ✅ Bên trong hoàn toàn constructor injection. ❌ Biên phụ thuộc static `ServiceProvider` |
| 007 | **Logger tự viết** thay Serilog | AutoCAD 2024 nạp sẵn Serilog cùng AssemblyVersion ([A1](T03_PITFALLS_CASEBOOK.md#a1-serilog-missingmethodexception-ngay-khi-khởi-động)) | ✅ Không xung đột host. ❌ Ít tính năng (không structured logging) |
| 008 | **Tên assembly có timestamp** ở Debug | .NET Framework không gỡ assembly; cần nạp lại khi dev | ✅ Không phải tắt AutoCAD. ❌ Mọi pack URI phải dựng lúc runtime; nhiều bản cũ còn trong AppDomain |
| 009 | `AssemblyResolve` **ưu tiên assembly đã nạp** | Nạp byte array làm WPF nạp bản thứ hai ([A2](T03_PITFALLS_CASEBOOK.md#a2-palette-lỗi-does-not-have-a-resource-identified-by-the-uri)) | ✅ Palette chạy khi nạp byte array. Không bao giờ "ép" version khác |
| 010 | Demo tách bằng **công tắc MSBuild** `IncludeDemos`, không tách project | Giữ một DLL (ADR-001) và quy trình nạp hiện có của nhóm | ✅ Không đổi cách nạp. ❌ Demo vẫn cùng assembly khi bật |
| 011 | **Analyzer CAD001** cho quy tắc "không `MdiActiveDocument` trong Service/ViewModel" | Quy ước dễ bị quên; analyzer biến quy ước thành lỗi build | ✅ Tự động. ❌ Theo cú pháp, có lỗ hổng (B-18) |
| 012 | **Multi-target `net48;net8.0-windows`** | Sẵn đường cho AutoCAD 2025+ | ✅ Build/test cả hai. ❌ net8 **chưa chạy thật** (B-21); mọi API phải có ở cả hai |
| 013 | **0 warning, CI `-warnaserror`** | Warning bị lờ đi thì sẽ có lỗi thật lẫn trong đó | ✅ Chặn sớm. ❌ Phải sửa cả warning "vô hại" (MSB3270…) |
| 014 | **Phát hành theo tag**, version bundle đóng dấu từ tag | Một nguồn sự thật cho version; tái lập được | ✅ Tự động. ❌ Workflow ít chạy nên dễ có lỗi tiềm ẩn ([A10](T03_PITFALLS_CASEBOOK.md#a10-release-workflow-lỗi-403-resource-not-accessible-by-integration), [A11](T03_PITFALLS_CASEBOOK.md#a11-packageps1-làm-hỏng-xml-version-khi-đóng-dấu-version)) |

**Mẫu ADR mới** (đặt trong PR thay đổi kiến trúc, hoặc `docs/adr/NNN-ten.md` nếu nhóm muốn tách riêng):

```markdown
# ADR-0NN: <Tên quyết định>
- Trạng thái: Đề xuất | Chấp nhận | Thay thế bởi ADR-0MM
- Ngày: YYYY-MM-DD · Người đề xuất: …
## Bối cảnh
<Vấn đề, ràng buộc, số liệu>
## Các lựa chọn đã cân nhắc
1. … (ưu / nhược)
2. …
## Quyết định
<Chọn gì, vì sao>
## Hệ quả
<Tốt, xấu, việc phải làm theo; ảnh hưởng tới quy trình nạp / phát hành / người dùng>
```

---

## 2. Checklist code review

Người review đi theo thứ tự này. Người viết PR **tự chạy checklist trước khi gửi**.

### A. Đúng với AutoCAD
- [ ] Mọi thao tác **ghi** đi qua `ICadContext` (không `LockDocument`/`StartTransaction` thủ công, trừ side database)
- [ ] **Không prompt** trong lambda / Service / ViewModel
- [ ] Không đọc `MdiActiveDocument` sau `await`; code async chụp `Document` trước
- [ ] Không gọi AutoCAD API ngoài main thread; không `ConfigureAwait(false)` trước code chạm AutoCAD
- [ ] Không giữ `DBObject` ngoài transaction; tham chiếu bền dùng `Handle`
- [ ] Đối tượng không vào DB được `Dispose`; `ResultBuffer`/side `Database`/`PlotEngine` trong `using`
- [ ] Bảng ký hiệu: `Has()` trước indexer; tên được validate
- [ ] Event: gỡ đúng delegate; không sửa DB trong handler sự kiện Database; handler có try/catch
- [ ] Service truyền `undoMarkName`; `autoRegen` chỉ bật khi cần
- [ ] Không có `Execute` lồng nhau (publish message *sau* `TryExecute`)

### B. Kiến trúc
- [ ] Phụ thuộc đúng chiều ([T02 §1](T02_ARCHITECTURE_DEEP_DIVE.md#1-bản-đồ-dự-án-và-quy-tắc-phụ-thuộc)); ViewModel không kiểu WPF/AutoCAD
- [ ] Logic nằm **ngoài** lambda (Planner/Spec); Service là vỏ mỏng
- [ ] Lỗi nghiệp vụ dự đoán được → `CadResult`; lỗi lập trình → exception
- [ ] Đăng ký DI đúng lifetime (service Singleton, ViewModel Transient); `CommandClass` đầy đủ
- [ ] Không thêm dependency mới mà chưa qua [chính sách dependency](T06_BUILD_RELEASE_DEVOPS.md#7-chính-sách-dependency)

### C. Chất lượng
- [ ] Build 0 warning; test xanh hai TFM; test mới **fail trên code cũ** (nếu là sửa lỗi)
- [ ] Test kết quả cho logic, test hợp đồng cho service, test XAML cho palette mới
- [ ] Không `catch {}` nuốt lỗi; lỗi được log ở biên
- [ ] Thông điệp người dùng rõ ràng, tiếng Việt nhất quán

### D. Vận hành và tài liệu
- [ ] Mô tả PR có *Summary* + *Verification* (smoke test nào, kết quả)
- [ ] CHANGELOG `[Unreleased]`; T-module / Phần 1–9 cập nhật nếu đổi quy ước
- [ ] Mọi tên API mới trong tài liệu **tồn tại** (kiểm metadata, xem [T04 §6](T04_DEBUGGING_TROUBLESHOOTING.md#6-kiểm-chứng-api-bằng-metadata))
- [ ] Thay đổi mục backlog thì cập nhật bảng §3

---

## 3. Backlog kỹ thuật đã kiểm chứng

Trạng thái tại **v1.0.0** (2026-10-13). Mức độ: 🔴 cao · 🟠 trung bình · 🟡 thấp · 🔵 kiến trúc/tài liệu. Cột **Lab** chỉ bài trong [T10](T10_LABS_AND_ASSESSMENT.md) dùng mục này làm đề.

| Mã | Mức | Vấn đề | Bằng chứng | Hướng sửa | Lab |
| :---: | :---: | :--- | :--- | :--- | :---: |
| B-01 | 🔴 | `IsDarkMode` đọc sysvar `DARKMODE` (**không tồn tại**), nên luôn coi là theme sáng; `ThemeManager` nghe nhầm biến | `(getvar "DARKMODE")` → `nil`, `(getvar "COLORTHEME")` → `0` trên AutoCAD 2024 ([A13](T03_PITFALLS_CASEBOOK.md#a13-palette-luôn-theme-sáng-dù-autocad-đang-tối)) | Đọc `COLORTHEME` (0 = tối); nghe `COLORTHEME`; log khi sysvar không đọc được | L12 |
| B-02 | 🟠 | Hai hệ màu song song: `Styles.xaml` cố định màu tối riêng; `Cad*` của ThemeManager dùng ở vài chỗ | Đọc `Themes/Styles.xaml` | Một nguồn màu duy nhất: brush trong `Styles.xaml` tham chiếu key theme bằng `DynamicResource` | L12 |
| B-03 | 🟡 | `LayerManagerControl.xaml` dùng 3 key không tồn tại | Script so khớp key ([T02 §10](T02_ARCHITECTURE_DEEP_DIVE.md#10-thememanager-styles-và-hệ-màu)) | Dùng key có thật; thêm test "mọi `DynamicResource` đều có định nghĩa" | L12 |
| B-04 | 🟠 | `TogglePalette`/`ShowPalette` tạo `PaletteSet` **không Guid**, nên AutoCAD không nhớ vị trí | `PaletteService.cs` | Thêm tham số Guid cho Toggle/Show; tạm thời dùng `GetOrCreatePaletteSet(name, guid)` trước ([T08 §4](T08_RECIPES_EXTENDING.md#4-thêm-một-palette)) | L08 |
| B-05 | 🟠 | Không ai dựng Ribbon; `AddButton` không idempotent; thứ tự đăng ký build action dễ sai | grep `AddTab`/`RegisterBuildAction` | `RemoveTab` + build idempotent + đăng ký trước `InitializeRibbon` ([T08 §6](T08_RECIPES_EXTENDING.md#6-thêm-ribbon-tab)) | L11 |
| B-06 | 🟠 | `ExecuteAsync` không xác nhận có `SynchronizationContext` của main thread | `CadContext.cs` so với `CadAsyncHelper.cs` | Chụp và kiểm tra `SynchronizationContext.Current` khi bắt đầu; ném lỗi rõ ràng nếu null | L10 |
| B-07 | 🟡 | `ExecuteRead` commit transaction; `IsReadOnly` không chặn `GetObject(…, ForWrite)` | `CadContext.cs` | Ghi rõ trong XML doc; cân nhắc analyzer cấm `ForWrite` trong lambda `ExecuteRead` | L03 |
| B-08 | 🟡 | `DEMO_ASYNC_AWAIT` vẽ vào `MdiActiveDocument` **sau** `await` | `Demos/DemoServicesCommands.cs` | Chụp `Document` trước `await` hoặc dùng `ExecuteAsync`; demo phải dạy mẫu đúng | L10 |
| B-09 | 🟡 | `XRecordHelper.RemoveMetadata` chỉ `Remove` khỏi dictionary, không `Erase` Xrecord (để lại đối tượng mồ côi) | `Utils/XRecordHelper.cs` | Mở Xrecord `ForWrite` rồi `Erase()` | L06 |
| B-10 | 🟠 | `PlotService` không kiểm `ProcessPlotState`, không xử lý `BACKGROUNDPLOT` (mặc định **2**), không Dispose `PlotEngine` | Code + `getvar BACKGROUNDPLOT` → 2 | Kiểm trạng thái, tạm đặt `BACKGROUNDPLOT=0` rồi khôi phục, `using` engine | (mở rộng L15) |
| B-11 | 🟡 | `PathProvider.GetPluginDirectory` hỏng khi nạp từ byte array (`Location == ""`, `Path.GetDirectoryName("")` ném) | Đã kiểm chứng trên .NET Framework; **hiện chưa có code sản phẩm gọi** | Kiểm tra `Location` rỗng, trả lỗi rõ ràng hoặc fallback | — |
| B-12 | 🟡 | `CreateBlockDefinition`: block đã tồn tại thì entity truyền vào không được dùng cũng không được Dispose | `CadScopeLayerExtensions.cs` | Dispose các entity không dùng, hoặc ghi rõ quyền sở hữu trong XML doc | — |
| B-13 | 🟠 | `CopyEntity`/`MirrorEntity` dùng `Clone()` (nông), nên block mất attribute | `CadScopeBlockExtensions.cs` ([B19](T03_PITFALLS_CASEBOOK.md#b19-sao-chép-block-mất-attribute)) | `DeepCloneObjects` + `TransformBy` | (L07 mở rộng) |
| B-14 | 🟡 | `Draw*`: `configure` chạy **trước** append; nếu nó ném lỗi thì entity rò rỉ | `CadScope*Extensions.cs` | `try { configure } catch { entity.Dispose(); throw; }` | — |
| B-15 | 🟡 | `ShowPalette` chỉ log lỗi, `TogglePalette` ném lại: chính sách lỗi không nhất quán | `PaletteService.cs` | Thống nhất: ném lại ở cả hai | — |
| B-16 | 🟡 | `Ensure*` không validate tên ký hiệu | `CadScopeLayerExtensions.cs` | `SymbolUtilityServices.ValidateSymbolName(name, false)` (API đã kiểm chứng) | — |
| B-17 | 🟡 | `SelectionFilterBuilder` không escape wildcard trong tên | `SelectionFilterBuilder.cs` | Thêm overload/escape helper (`` ` `` trước ký tự đặc biệt) | L05 |
| B-18 | 🔵 | CAD001 theo cú pháp, bỏ sót prompt-trong-Service qua `CadPromptHelper` | Analyzer source | Rule CAD002: cấm `CadPromptHelper`/`Editor.Get*` trong Services/ViewModels | (đồ án) |
| B-19 | 🔵 | Comment giải thích sai lý do: `EnsureLayer` (eVetoed), `CadContextGuard` (eLockViolation/deadlock) | So với hành vi AutoCAD ([T01 §4.3](T01_AUTOCAD_NET_FOUNDATIONS.md#43-transaction-lồng-nhau)) | Sửa comment theo lý do thật | — |
| B-20 | 🔵 | Không thể integration test headless (một DLL tham chiếu `AcMgd`) | [A5](T03_PITFALLS_CASEBOOK.md#a5-accoreconsole-crash-0xc00000fd-khi-netload-plugin) | ADR tách DLL lõi không UI ([T05 §9](T05_TESTING_STRATEGY.md#9-integration-test-headless-hiện-trạng-và-hướng-đi)) | — |
| B-21 | 🔵 | AutoCAD 2025+ / `net8.0-windows` chưa chạy thật | — | **Ngoài phạm vi hiện tại** | — |
| B-22 | 🟡 | `SettingsService.Save` ghi không atomic; nhiều phiên AutoCAD thì phiên ghi sau thắng | `SettingsService.cs` | Ghi file tạm rồi `File.Replace`; đọc lại trước khi ghi nếu cần merge | — |
| B-23 | 🟠 | Palette chỉ áp theme **một lần** lúc tạo; không ai nghe `ThemeChanged` | grep `ThemeChanged` | Palette đăng ký `ThemeChanged` và áp lại (`ApplyThemeTo`); gỡ khi đóng | L12 |
| B-24 | 🟡 | `RunSafe` (đồng bộ): `IsBusy` không kịp hiện; exception chỉ vào `ErrorMessage`, không log | `ViewModelBase.cs` | Cho phép inject logger/handler lỗi; tài liệu hóa rằng busy chỉ có ý nghĩa với async | — |
| B-25 | 🟡 | *(Cần kiểm chứng)* Attribute căn lề khác Left có thể lệch sau `InsertBlock` khi chưa `AdjustAlignment` | API tồn tại; chưa thử với block thật | Lab với block attribute căn giữa, rồi quyết định thêm `AdjustAlignment(db)` | (L07 mở rộng) |
| B-26 | 🟡 | `CommonControlsPalette` gán DataContext cho `Grid` qua `ViewModelLocator`, nên PaletteService không Dispose được ViewModel đó. Hiện ViewModel này không subscribe gì nên **tác động rất nhỏ**, nhưng là mẫu dễ bị copy sai | `CommonControlsPalette.xaml` (DataContext chỉ ở `Grid`) + `PaletteService.DisposeExistingContent` | Gán DataContext cho chính UserControl như các palette khác | — |
| B-27 | 🟡 | `PackageContents.xml` chỉ liệt kê 3 lệnh trong `<Commands>` | File manifest | Chỉ quan trọng khi chuyển sang demand-load: liệt kê đủ hoặc bỏ | — |

> [!NOTE]
> **Quy tắc trung thực của backlog:** mục "cần kiểm chứng" phải ghi rõ như vậy (B-25). Không nâng mức độ để gây chú ý, không hạ mức để đỡ việc. Khi sửa xong, ghi PR và ngày, đừng xóa dòng ngay: giữ một phiên bản để người sau biết lịch sử.

---

## 4. Lộ trình kiến trúc

| Giai đoạn | Việc | Mục |
| :--- | :--- | :--- |
| **Ngắn hạn** (1–2 sprint) | Sửa theme (B-01, B-23, rồi B-02/B-03); Guid palette (B-04); dựng Ribbon (B-05); các lỗi đúng/sai nhỏ (B-09, B-13, B-14, B-16); sửa comment (B-19); kiểm context trong `ExecuteAsync` (B-06) | Phần lớn là Lab, vừa đào tạo vừa trả nợ |
| **Trung hạn** | ADR + thử nghiệm tách DLL lõi để test headless (B-20); analyzer CAD002 (B-18); settings atomic (B-22); làm cứng PlotService (B-10) | Cần ADR và thống nhất với quy trình nạp của nhóm |
| **Dài hạn** | Kiểm chứng AutoCAD 2025+/net8 khi vào phạm vi (B-21); báo cáo lỗi có cấu trúc (telemetry nội bộ, chỉ khi người dùng đồng ý); đa ngôn ngữ thông điệp; ngân sách hiệu năng cho từng tính năng | Theo nhu cầu sản phẩm |

---

## 5. Trách nhiệm của Senior

| Mảng | Việc cụ thể |
| :--- | :--- |
| **Kiến trúc** | Giữ ADR; review mọi thay đổi chạm `Core/`; quyết định khi nào phá vỡ quy ước |
| **Chất lượng** | Duy trì 0 warning + test xanh; mở rộng analyzer; checklist review |
| **Phát hành** | Chạy checklist release ([T06 §5.3](T06_BUILD_RELEASE_DEVOPS.md#53-checklist-phát-hành)); quyết định PATCH/MINOR/MAJOR |
| **Sự cố** | Chủ trì chẩn đoán; incident report; cập nhật T03 |
| **Dependency** | Phê duyệt mọi package mới ([T06 §7](T06_BUILD_RELEASE_DEVOPS.md#7-chính-sách-dependency)) |
| **Tri thức** | Giữ bộ tài liệu này đúng; mentoring; đánh giá lên cấp ([T10 §4](T10_LABS_AND_ASSESSMENT.md#4-đề-đánh-giá-lên-cấp)) |
| **Ưu tiên** | Cân giữa tính năng mới và trả nợ backlog; nói "chưa" có lý do |

### Kỹ năng phi kỹ thuật

- **Viết cho người sau:** commit giải thích *vì sao*; PR có *Verification*; comment ghi lý do, không chép lại code.
- **Trung thực về mức độ chắc chắn:** "đã kiểm chứng trên AutoCAD 2024" khác "chắc là chạy". Ghi rõ điều chưa kiểm chứng (net8, B-25).
- **Tái hiện trước khi tranh luận:** báo cáo lỗi của người dùng phải có các bước tái hiện, trước khi bàn cách sửa.
- **Thu nhỏ thay đổi:** PR nhỏ, một mục đích, dễ review, dễ revert.
- **Biết khi nào dừng:** một tối ưu không có số đo là đoán; một abstraction chưa có ca dùng thứ hai là quá sớm.

---

## ✅ Tự kiểm tra T09

1. Chọn một ADR bạn **không đồng ý**. Viết ADR thay thế (bối cảnh, lựa chọn, hệ quả).
2. Bạn có 3 ngày. Chọn mục backlog nào để làm, vì sao? (Có nhiều đáp án đúng; mentor chấm *lập luận*.)
3. Một PR "tối ưu" `EnsureLayer` nhưng không có số đo. Bạn review thế nào?

<details><summary>Gợi ý</summary>

2. Một lựa chọn hợp lý: B-01 + B-23 (lỗi người dùng nhìn thấy, sửa nhỏ, cần kiểm trực quan), rồi B-04 (trải nghiệm hằng ngày). Lý do: tác động lên người dùng / công sức, rủi ro thấp, có cách kiểm chứng rõ.
3. Yêu cầu số đo trước/sau theo phương pháp ở [T07 §8](T07_PERFORMANCE_AND_SCALE.md#8-đo-đạc), trên kích thước dữ liệu thực tế (vd. `MaxCells`). Không có số đo thì không merge với lý do "tối ưu".
</details>

---

**Tiếp theo:** [T10 — Bài lab & đánh giá →](T10_LABS_AND_ASSESSMENT.md)
