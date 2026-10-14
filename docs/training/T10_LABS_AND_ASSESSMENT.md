> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← T09](T09_SENIOR_PLAYBOOK.md) · **T10**

# T10 — Bài Lab & Đánh Giá

> **Cấp độ:** mọi cấp · **Cần trước:** module ghi ở từng lab · **Trong lộ trình:** suốt Giai đoạn 1–3; đánh giá 🌰 → 🌱 nằm ở [C05 §6](C05_ASYNC_TESTING_GIT.md#6-đánh-giá-nhập-môn--fresher) ([README §3](README.md#3-lộ-trình-đào-tạo))

Học AutoCAD .NET chỉ thật sự diễn ra khi **làm**. Các bài lab ở đây đi từ thí nghiệm nhỏ tới sửa **lỗi có thật** mà dự án đã gặp ([backlog T09](T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng)) — làm trên code cũ (thường là `v1.0.0`, [quy tắc 6](#1-quy-tắc-làm-lab)), rồi đối chiếu với lời giải đã merge. Lỗi thật dạy nhiều hơn bài tập bịa: chúng có bằng chứng, có cạm bẫy, và có lời giải để so.

---

## Mục lục

1. [Quy tắc làm lab](#1-quy-tắc-làm-lab)
2. [Danh sách bài lab](#2-danh-sách-bài-lab)
3. [Đồ án cuối khóa: Bộ công cụ Tủ điện MSB](#3-đồ-án-cuối-khóa-bộ-công-cụ-tủ-điện-msb)
4. [Đề đánh giá lên cấp](#4-đề-đánh-giá-lên-cấp)
5. [Ngân hàng câu hỏi](#5-ngân-hàng-câu-hỏi)

---

## 1. Quy tắc làm lab

1. **Mỗi lab một branch**: `lab/Lxx-<tên-bạn>`. Lab thí nghiệm (L02, L03) **không merge**. Lab sửa backlog thì mở PR như công việc thật, nhưng **base là branch tạo từ commit cũ** ghi ở quy tắc 6 (`v1.0.0`; riêng B-31…B-33 là `632a6c1`) để mentor review — không merge vào `main`. Lab xây tính năng mới (L07, L09, L13, L14, L17, L19) theo quy trình bình thường.
2. **Definition of Done (DoD)** của từng lab là tiêu chí chấm. Chưa đạt DoD thì lab chưa xong.
3. **Báo cáo lab** trong mô tả PR (hoặc issue "Lab report"): đã làm gì, kết quả thí nghiệm, trả lời câu hỏi phản tư.
4. **Tái hiện trước, sửa sau.** Với lab sửa lỗi: viết test hoặc bước tái hiện *fail trước* rồi mới sửa.
5. Thử nghiệm sysvar / script thì dùng accoreconsole `/isolate` ([T04 §5.3](T04_DEBUGGING_TROUBLESHOOTING.md#53-thí-nghiệm-an-toàn-với-accoreconsole)). **Không** thay đổi thiết lập AutoCAD thật của máy (`SECURELOAD`…).
6. **Các mục backlog trong lab đã được sửa ở PR #6** ([T09 §3](T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng)). Làm lab trên branch tạo từ tag **`v1.0.0`** (`git switch -c lab/L05-ten v1.0.0`), tự giải, **rồi mới** đọc lời giải trong PR #6 và ghi vào báo cáo: chỗ bạn làm khác, vì sao, cách nào tốt hơn. Để có kiểm thử FAIL trên code cũ, chép `tests/integration/HeadlessLab/` từ `main` sang và giữ các kiểm thử liên quan ([cách chạy](../../tests/integration/README.md#headlesslab--kiểm-thử-headless-trên-autocad-thật)). Lab sửa backlog **không merge** vào `main` nữa. **Riêng B-31…B-33** (tìm thấy khi viết nhánh UI, sửa ở **PR #11**): lab L18 và bài tập U02-E1 làm trên commit **`632a6c1`** (`git switch -c lab/L18-ten 632a6c1`, tức `main` ngay trước PR #11), rồi đối chiếu với PR #11 theo cùng cách.

---

## 2. Danh sách bài lab

| Lab | Tên | Cấp | Thời lượng | Backlog |
| :---: | :--- | :---: | :---: | :---: |
| L01 | Môi trường & chạy plugin | 🌱 | 2 giờ | — |
| L02 | Đọc database | 🌱 | 3 giờ | — |
| L03 | Thí nghiệm transaction & context | 🌱→🌿 | 3 giờ | B-07 |
| L04 | Prompt & UCS | 🌿 | 2 giờ | — |
| L05 | Selection filter & dynamic block | 🌿 | 3 giờ | B-17 |
| L06 | Metadata XRecord, xóa đúng cách | 🌿 | 3 giờ | B-09 |
| L07 | Nhân bản Golden Sample | 🌿→🌳 | 2–3 ngày | — |
| L08 | Palette nhớ vị trí | 🌿 | 0.5–1 ngày | B-04 |
| L09 | Sự kiện bản vẽ | 🌳 | 1 ngày | — |
| L10 | Async đúng chuẩn | 🌳 | 1 ngày | B-06, B-08 |
| L11 | Ribbon | 🌳 | 1 ngày | B-05 |
| L12 | Theme | 🌳 | 1–2 ngày | B-01, B-02, B-03, B-23 |
| L13 | Đo và tối ưu hiệu năng | 🌳 | 1 ngày | — |
| L14 | Xử lý hàng loạt bằng side database | 🌳→🏔️ | 1 ngày | — |
| L15 | Phát hành thử | 🏔️ | 0.5 ngày | B-10 (mở rộng) |
| L16 | Pháp y sự cố | 🏔️ | 0.5 ngày | — |
| L17 | Xem trước trên palette | 🌿→🌳 | 1–2 ngày | — |
| L18 | Sửa bẫy ô nhập số | 🌿 | 0.5 ngày | B-31, B-32 |
| L19 | Xem trước trên màn hình bản vẽ | 🌳 | 1 ngày | — |

### L01 — Môi trường & chạy plugin

- **Mục tiêu:** chạy được dự án, biết nhìn vào đâu khi có vấn đề.
- **Đề bài:** clone, build Debug, nạp vào AutoCAD 2024, chạy 6 lệnh smoke (`CORE_INFO`, `CORE_TEST_ALL`, `DRAW_CIRCLE_SAMPLE`, `SAMPLE_GRID`, `DEMO_HELP`, `SHOW_PALETTE`). Mở `CORE_DEBUG`. Mở file log hôm nay.
- **DoD:** (1) cả 6 lệnh chạy đúng như [T05 §7](T05_TESTING_STRATEGY.md#7-những-gì-chỉ-autocad-thật-mới-kiểm-được); (2) chỉ ra dòng `initialized` trong log; (3) giải thích **mỗi** lệnh smoke chứng minh phần nào của hệ thống hoạt động.
- **Phản tư:** nạp DLL hai lần liên tiếp thì `CORE_DEBUG` hiển thị gì khác? Vì sao?

### L02 — Đọc database

- **Đề bài:** lệnh thí nghiệm `TRAIN_DUMP` dùng `ExecuteRead`:
  1. Liệt kê layer: tên, On/Off, màu.
  2. Đếm entity trong `CurrentSpace` theo `id.ObjectClass.DxfName`, **không** `GetObject`.
  3. Đếm block reference theo **effective name** (dùng `DynamicBlockHelper.GetEffectiveName`).
- **DoD:** chạy trên một bản vẽ thật có dynamic block; không có `ForWrite` nào; giải thích vì sao bước 2 không cần mở entity.
- **Phản tư:** chạy `TRAIN_DUMP` khi đang ở Layout thì kết quả bước 2 khác gì?

### L03 — Thí nghiệm transaction & context

Làm từng thí nghiệm, ghi bảng **thí nghiệm → kết quả quan sát → giải thích**:

| # | Thí nghiệm | Kỳ vọng tự kiểm |
| :---: | :--- | :--- |
| 1 | API thô: vẽ Line, **không** `Commit` | Không có Line, không crash |
| 2 | Mở Line `ForRead`, đổi màu | `eNotOpenForWrite` |
| 3 | Dùng `DBObject` sau khi transaction Dispose | Lỗi (ghi lại thông điệp) |
| 4 | Lệnh `CommandFlags.Session`, ghi DB **không** lock | `eLockViolation` |
| 5 | Gọi `ICadContext.Execute` bên trong lambda của `Execute` khác | Thông điệp của `CadContextGuard` |
| 6 | Trong `ExecuteRead`: `GetObject(id, ForWrite)` rồi đổi màu | **Thay đổi được commit** (B-07). Đề xuất cách phòng (trên `main`: analyzer CAD003 cảnh báo ngay lúc build — so với đề xuất của bạn) |

- **DoD:** bảng đủ 6 dòng, phần giải thích dẫn được tới đúng mục T01/T02. Code thí nghiệm **không merge**.

### L04 — Prompt & UCS

- **Đề bài:** `TRAIN_LINE2P` vẽ Line giữa 2 điểm pick; prompt thứ hai có `basePoint` (đường cao su). Thử với `UCS Z 30`. Làm thêm một bản dùng `Editor.GetPoint` thô không đổi hệ để so sánh.
- **DoD:** bản dùng `CadPromptHelper` vẽ đúng chỗ click khi UCS xoay; ESC ở cả 2 prompt đều thoát êm, không ghi lịch sử lệnh.
- **Phản tư:** vì sao `basePoint` phải đổi WCS → UCS, ngược chiều với kết quả?

### L05 — Selection filter & dynamic block

- **Đề bài:** (1) lệnh chọn mọi block "DOOR" kể cả dynamic block đã chỉnh (`` "DOOR,`*U*" `` + lọc lại bằng effective name); (2) sửa **B-17**: thêm cách escape ký tự wildcard trong `SelectionFilterBuilder` (ví dụ `OnLayersExact(...)`) kèm unit test (`SelectionFilterBuilderTests` chạy được không cần AutoCAD).
- **DoD:** test escape cho `* ? # @ . ~ [ ] - , `` ` ``; thử tay với layer `LAB#1` (không khớp chính nó nếu không escape — `#` là "một chữ số"), `M.EQUIP` và `M-EQUIP` (`.` khớp mọi ký tự không phải chữ/số). Tên kiểu `A*B` **không tồn tại được**: `*` không hợp lệ trong tên ký hiệu.

### L06 — Metadata XRecord, xóa đúng cách

- **Đề bài:** sửa **B-09**: `RemoveMetadata` phải `Erase` Xrecord (mở `ForWrite` rồi `Erase()`). Viết lệnh thử: Set → Get → Remove → kiểm `extDict.Contains(key) == false` **và** Xrecord cũ có `IsErased == true`.
- **DoD:** PR sửa + mô tả cách kiểm chứng trong AutoCAD; CHANGELOG.
- **Phản tư:** có nên `ReleaseExtensionDictionary()` khi dictionary rỗng không? Ưu/nhược?

### L07 — Nhân bản Golden Sample

- **Đề bài:** tính năng **"Dãy tủ điện"**: vẽ N tủ cạnh nhau (rộng × cao, khoảng hở), nhãn tên, XRecord `{ v, name, index, width, height }` trên khung. Đi đủ các tầng của [T08 §2](T08_RECIPES_EXTENDING.md#2-thêm-một-tính-năng-trọn-vẹn): Spec (có `MaxPanels`), Planner, Service (`TryExecute` + undo mark), lệnh `PANEL_ROW`, ViewModel, palette. Đã làm [đồ án nhập môn](C05_ASYNC_TESTING_GIT.md#5-đồ-án-nhập-môn-planner-dãy-tủ-điện) thì lấy nó làm điểm xuất phát: `PanelRowSpec` + `PanelRowPlanner` vào `Samples/PanelRow/`, test vào `CadCoreFramework.Tests/Samples/PanelRow/`, đổi namespace thành `CadCoreFramework.Samples.PanelRow` và viết dạng khối `namespace … { }` như mọi file của dự án.
- **DoD:** ≥ 15 unit test (Spec/Planner/Service/ViewModel); test XAML cho palette; smoke tay; một Ctrl+Z xóa cả dãy (cả từ lệnh lẫn từ palette).
- **Mở rộng:** sửa **B-13** (copy dãy tủ bằng `DeepCloneObjects`), kiểm chứng **B-25** với block có attribute căn giữa. Tự kiểm chứng *trước*: `Clone()` làm mất gì? (Giả thuyết ban đầu của dự án là "mất attribute" — và nó **sai**; xem B-13.)

### L08 — Palette nhớ vị trí

- **Đề bài:** sửa **B-04** tận gốc: `TogglePalette`/`ShowPalette` nhận `Guid?`; mỗi palette của dự án có Guid **cố định**; cập nhật `IPaletteService`.
- **DoD:** dời palette → đóng AutoCAD → mở lại → palette ở đúng chỗ cũ. Các test hiện có vẫn xanh.
- **So sánh:** PR #6 chọn cách khác — không thêm tham số, mà **dẫn xuất** Guid từ `[assembly: Guid]` + tên palette (`Core/PaletteGuids`). Ưu/nhược so với Guid viết tay? (Gợi ý: đổi tên palette, hai plugin cùng template, quên truyền Guid.)

### L09 — Sự kiện bản vẽ

- **Đề bài:** palette Grid Marker tự cập nhật số ô khi người dùng **xóa** khung ô (và khi Undo việc xóa). Theo mẫu [T08 §11](T08_RECIPES_EXTENDING.md#11-lắng-nghe-sự-kiện-bản-vẽ-đúng-cách): ghi nhận ở `ObjectErased`, xử lý ở `Application.Idle`, gỡ đăng ký khi Dispose.
- **DoD:** xóa 3 ô thì số đếm giảm 3; Undo thì tăng lại; mở/đóng palette 5 lần, `CORE_DEBUG` không cho thấy subscriber tăng dần; không sửa DB trong handler.

### L10 — Async đúng chuẩn

- **Đề bài:** (1) sửa **B-08**: `DEMO_ASYNC_AWAIT` dùng `CadContext.ExecuteAsync` (hoặc chụp Document trước `await`); (2) sửa **B-06**: `ExecuteAsync` kiểm tra `SynchronizationContext.Current` lúc bắt đầu, ném lỗi rõ ràng nếu thiếu.
- **DoD:** chạy demo, **chuyển sang bản vẽ khác trong 3 giây chờ**, hình tròn xuất hiện ở bản vẽ **ban đầu** và thông báo kết quả hiện ở bản vẽ đang mở (nêu tên bản vẽ ban đầu); đóng bản vẽ ban đầu trong lúc chờ thì có thông báo lỗi rõ ràng, không crash.
- **Cạm bẫy (chính dự án đã mắc — B-30):** ghi thông báo bằng `scope.Editor` trong giai đoạn vẽ. Nó chạy được khi bạn *không* chuyển bản vẽ, nên chỉ kiểm kịch bản "không chuyển" là chưa đủ. Kiểm cả hai, và đọc log.

### L11 — Ribbon

- **Đề bài:** dựng tab "CadCore" theo [T08 §6](T08_RECIPES_EXTENDING.md#6-thêm-ribbon-tab), thêm `RemoveTab` vào `IRibbonService`/`RibbonService`.
- **DoD:** cả 3 kịch bản: NETLOAD khi AutoCAD đã mở; chạy `CUI` rồi đóng (tab còn, **không** nhân đôi nút); nạp lúc khởi động qua bundle. Nút chạy đúng lệnh.

### L12 — Theme

- **Đề bài:** sửa **B-01** (`COLORTHEME`), **B-23** (palette đang mở đổi theme theo), **B-03** (key không tồn tại) + test tĩnh "mọi `DynamicResource` trong XAML đều được định nghĩa" (đọc file XAML như text, so với key trong `Themes/*.xaml`). Viết ADR cho **B-02** (thống nhất hệ màu).
- **DoD:** ảnh chụp mỗi palette ở **cả hai** theme AutoCAD; đổi theme khi palette đang mở thì cập nhật ngay; test tĩnh fail trên code cũ (B-03), pass sau khi sửa.

### L13 — Đo và tối ưu hiệu năng

- **Đề bài:** đo vẽ lưới 50 × 50 (`MaxCells`) theo [T07 §8](T07_PERFORMANCE_AND_SCALE.md#8-đo-đạc): bản gốc vs bản `EnsureLayer`/`EnsureRegApp` một lần ngoài vòng lặp. Thêm benchmark cho `GridPlanner.Plan` bằng BenchmarkDotNet.
- **DoD:** bảng số liệu (máy, AutoCAD, 5 lần đo, trung bình) trước/sau; kết luận *có đáng* thay đổi không. Không có số đo thì không đạt.

### L14 — Xử lý hàng loạt bằng side database

- **Đề bài:** lệnh `TRAIN_COUNT_FOLDER`: chọn thư mục, đếm block theo effective name trong mọi `*.dwg` **không mở chúng lên giao diện** ([T07 §4](T07_PERFORMANCE_AND_SCALE.md#4-xử-lý-nhiều-bản-vẽ-side-database)). Có thanh tiến trình; lỗi một file không dừng cả lô; báo cáo tổng kết.
- **DoD:** chạy trên ≥ 20 file (có cả một file hỏng và một file đang mở trong AutoCAD); không file nào bị khóa sau khi chạy; `Database` luôn được Dispose.

### L15 — Phát hành thử

- **Đề bài:** trên **repo fork / repo thử** (không phải repo chính): chạy đủ checklist [T06 §5.3](T06_BUILD_RELEASE_DEVOPS.md#53-checklist-phát-hành) với tag `v0.1.0`. Mở rộng tùy chọn: làm cứng `PlotService` (**B-10**) — viết kiểm thử HeadlessLab in thật ra PDF *trước*; bạn sẽ gặp các lỗi của **B-28** mà đọc code không thấy.
- **DoD:** Release trên fork có đúng một zip, manifest đúng version; release note; tự kiểm cài đặt từ zip.

### L16 — Pháp y sự cố

- **Đề bài:** (1) tái hiện crash `0xC00000FD` bằng NETLOAD `acmgd.dll` trong accoreconsole `/isolate`, tìm entry trong Event Viewer ([T04 §5.2](T04_DEBUGGING_TROUBLESHOOTING.md#52-crash-không-có-thông-báo)); (2) lấy runner integration **cũ** từ lịch sử git (`git log -- tests/integration`), chỉ ra đủ **5 lỗi** của [A6](T03_PITFALLS_CASEBOOK.md#a6-integration-test-báo-passed-dù-không-lệnh-nào-chạy) bằng thí nghiệm.
- **DoD:** một incident report theo [mẫu T04 §8](T04_DEBUGGING_TROUBLESHOOTING.md#8-mẫu-báo-cáo-sự-cố-incident-report), mục "Vì sao không bị phát hiện sớm hơn" đầy đủ.

### L17 — Xem trước trên palette

- **Mục tiêu:** vẽ thực thể lên UI theo đúng kiến trúc "một kế hoạch, hai nơi vẽ".
- **Đề bài:** làm trọn [U03 §2–6](U03_DRAWING_ON_UI.md#2-bước-1--mô-hình-hình-học-thuần) trên palette Grid Marker: `PreviewShape`, `PreviewViewport`, `CadPreview`, `GridPreview`, nối ViewModel và XAML. Thêm `EntityPreviewBuilder` ([U03 §7](U03_DRAWING_ON_UI.md#7-đọc-entity-trong-bản-vẽ-thành-hình-xem-trước)) thì nhớ link mô hình cho HeadlessLab. **Mở rộng** (chọn một): (a) zoom bằng con lăn chuột và kéo để pan trong `CadPreview`; (b) tô ô theo màu layer (ACI 4) mà vẫn đọc rõ ở cả hai theme; (c) palette "xem trước đối tượng đang chọn" dùng `EntityPreviewBuilder`.
- **DoD:** ≥ 10 test mới (viewport, `GridPreview`, ViewModel, render pixel) xanh trên net48 và net8.0-windows; CI xanh, kể cả HeadlessLab; ảnh palette ở **cả hai** theme; gõ số hàng/cột thì khung đổi ngay, input sai thì khung trống; smoke trong AutoCAD: bấm Vẽ xong, hình trong bản vẽ khớp hình xem trước.
- **Phản tư:** vì sao đổi TÂM A1 thì khung xem trước không cần vẽ lại? Test pixel của bạn có **fail được** không? Chứng minh bằng cách làm hỏng code ([U03 §6](U03_DRAWING_ON_UI.md#6-bước-5--kiểm-thử)).

### L18 — Sửa bẫy ô nhập số

- **Đề bài:** trên commit `632a6c1` ([quy tắc 6](#1-quy-tắc-làm-lab)), sửa **B-31** và **B-32** trên palette Grid Marker theo [U01 §5.3](U01_WPF_MVVM_BASICS.md#53-cách-sửa-2080-ô-số-bind-vào-chuỗi): ô số bind vào chuỗi, parse `InvariantCulture` + `NumberStyles.Float`, lỗi đi vào `ValidationSummary`. Viết test **fail trước**: gõ `"2,5"` hoặc `""` vào ô thì phải có lỗi và nút Vẽ phải tắt. Tìm ô theo **nhãn** trên palette, không theo tên property, để cùng một test chạy được trên code cũ lẫn code mới.
- **DoD:** test fail trước, pass sau; gõ `2,5` thì có thông báo rõ ràng và nút tắt; xóa trắng ô thì nút tắt; `2.5` vẫn đúng; lệnh `SAMPLE_GRID` không đổi hành vi; các test hiện có vẫn xanh.
- **So sánh:** đọc PR #11 (`ViewModels/NumberText.cs`, `GridMarkerViewModel`). PR giữ nguyên các property số (`Rows`, `Spacing`…) và thêm property chuỗi (`RowsText`…) đồng bộ hai chiều, thay vì đổi hẳn sang chuỗi. Được và mất gì? (Gợi ý: test và tài liệu đang dùng `vm.Rows`, thiết lập đã lưu sau này, độ dài code.) PR còn chặn `NaN`, và PR #13 thêm luật "số hữu hạn" vào chính `GridSpec` ([B-34](T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng)). Bạn có nghĩ tới trường hợp đó không?
- **Phản tư:** luật "phải là số" thuộc về ViewModel hay `GridSpec`? Lệnh `SAMPLE_GRID` (prompt bằng `GetDouble`) có cần luật đó không, và vì sao?

### L19 — Xem trước trên màn hình bản vẽ

- **Đề bài:** (1) lệnh `SAMPLE_GRID_PREVIEW`: như `SAMPLE_GRID`, nhưng hiện cả lưới bằng `TransientPreview` rồi hỏi "Vẽ lưới này? [Yes/No]" ([U03 §9.2](U03_DRAWING_ON_UI.md#92-transient-hình-tạm-đứng-yên)); (2) lệnh `SAMPLE_GRID_DRAG`: một `DrawJig` kéo cả lưới theo chuột, click để đặt ([U03 §9.1](U03_DRAWING_ON_UI.md#91-jig-hình-chạy-theo-chuột)), `Sampler` có kiểm `Status`.
- **DoD:** trả lời No hoặc nhấn ESC thì không có entity mới (đếm trước/sau) và hình tạm biến mất; Yes thì vẽ đúng chỗ, một lần Ctrl+Z xóa hết; jig đúng khi UCS xoay 90° (không đổi hệ hai lần); ESC trong jig thoát êm; smoke ghi trong mô tả PR.
- **Phản tư:** khi nào chọn transient, khi nào chọn jig, khi nào chọn khung xem trước trên palette?

---

## 3. Đồ án cuối khóa: Bộ công cụ Tủ điện MSB

> Bản viết lại đúng chuẩn của bài tập "Vẽ Tủ Điện MSB" từng nằm trong trang Getting Started cũ (nay là [R01](../reference/R01_QUICKSTART.md), không còn bài tập này). Bản cũ từng hướng dẫn prompt *trong Service* và dùng `ExecuteAsync` cho thao tác đồng bộ; cả hai là anti-pattern ([ADR-004](T09_SENIOR_PLAYBOOK.md#1-adr-vì-sao-kiến-trúc-như-hiện-tại)).

**Yêu cầu chức năng:**
1. Lệnh `MSB_DRAW`: pick điểm → nhập tên tủ, số lộ ra, dòng định mức → vẽ thân tủ + các lộ ra (Planner tính vị trí) + nhãn.
2. Palette "Tủ điện": cùng chức năng, nhập trên form, **nhớ giá trị lần trước** (`ISettingsService`), nhớ vị trí (Guid).
3. Dữ liệu tủ lưu bằng **XRecord** có trường version; lệnh `MSB_INFO` chọn tủ và in thông tin đọc lại.
4. Nút trên **Ribbon** tab "CadCore".
5. Palette hiển thị số tủ trong bản vẽ, tự cập nhật khi vẽ thêm hoặc xóa.
6. Palette có **khung xem trước** thân tủ và các lộ ra, vẽ từ **cùng Planner** với lệnh `MSB_DRAW` ([U03](U03_DRAWING_ON_UI.md)); ô số nhập an toàn theo [U01 §5.3](U01_WPF_MVVM_BASICS.md#53-cách-sửa-2080-ô-số-bind-vào-chuỗi).

**Yêu cầu kỹ thuật:** đúng mọi mục checklist [T09 §2](T09_SENIOR_PLAYBOOK.md#2-checklist-code-review); ≥ 30 unit test; một ADR cho một quyết định thiết kế bạn đã cân nhắc; release note như cho một bản MINOR.

| Tiêu chí | Điểm |
| :--- | :---: |
| Đúng với AutoCAD (context, lock, undo, ESC, UCS, đa bản vẽ) | 30 |
| Kiến trúc (tầng, phụ thuộc, logic ngoài lambda) | 25 |
| Kiểm thử (kết quả, hợp đồng, XAML, smoke ghi lại) | 20 |
| Trải nghiệm & xử lý lỗi (thông điệp, validate, không crash) | 10 |
| Tài liệu (ADR, CHANGELOG, mô tả PR) | 10 |
| Trình bày & trả lời vấn đáp | 5 |

Đạt: ≥ 70 và **không** có lỗi nghiêm trọng ở mục đầu tiên.

---

## 4. Đề đánh giá lên cấp

| Lên cấp | Điều kiện | Thực hành | Vấn đáp (chọn 5) |
| :--- | :--- | :--- | :--- |
| 🌰 → 🌱 Fresher | Bài tập C01–C05; đồ án nhập môn đạt DoD ([C05 §5](C05_ASYNC_TESTING_GIT.md#5-đồ-án-nhập-môn-planner-dãy-tủ-điện)) | Sửa một test fail do mentor cài vào đồ án (20 phút) | Đủ 10 câu ở [C05 §6](C05_ASYNC_TESTING_GIT.md#6-đánh-giá-nhập-môn--fresher), đạt ≥ 7 |
| 🌱 → 🌿 Junior | L01–L06 đạt; trắc nghiệm T01 ≥ 80% | Sửa một bug được giao trong 1 ngày (có test) | Q1, Q2, Q3, Q5, Q7, Q9, Q12, Q21 |
| 🌿 → 🌳 Middle | L07–L10 và L17–L19 đạt; review thành công bài "code có lỗi cài sẵn" bên dưới (≥ 7/9 lỗi) | Một tính năng nhỏ trọn vẹn theo Golden Sample, có palette và khung xem trước | Q4, Q6, Q8, Q10, Q11, Q13, Q14, Q22–Q25 |
| 🌳 → 🏔️ Senior | L11–L16 + đồ án đạt; chủ trì một release thật; một incident report; một ADR được chấp nhận | Chẩn đoán một sự cố "lạ" do mentor dựng sẵn | Q15–Q20 + bảo vệ ADR |

### Bài review: code có lỗi cài sẵn

Tìm **tất cả** vấn đề trong đoạn code sau (một Service được gọi từ palette):

```csharp
public class BadPanelService
{
    private readonly IEventAggregator _events;                              // (inject qua constructor — không phải lỗi)
    private Polyline _lastFrame;                                            // (1)

    public async Task DrawAsync(string name)
    {
        var doc = Application.DocumentManager.MdiActiveDocument;              // (2)
        var pt = doc.Editor.GetPoint("\nChọn điểm: ").Value;                // (3)
        await Task.Delay(500).ConfigureAwait(false);                          // (4)
        using (var tr = doc.Database.TransactionManager.StartTransaction())   // (5)
        {
            var ms = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForRead);
            var pl = new Polyline();
            pl.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(pt.X + 600, pt.Y), 0, 0, 0);
            ms.AppendEntity(pl);                                              // (6)
            _lastFrame = pl;
            var lt = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
            pl.Layer = lt["E-PANEL"].ToString();                              // (7)
            try { tr.Commit(); } catch { }                                    // (8)
        }
        _events.Publish(new PanelDrawnMessage(name));                         // (9)
    }
}
```

<details><summary>Đáp án (9 vấn đề)</summary>

1. Giữ `DBObject` trong field qua các transaction ([B5](T03_PITFALLS_CASEBOOK.md#b5-dùng-dbobject-sau-khi-transaction-kết-thúc)). Hãy lưu `ObjectId`/`Handle`.
2. `MdiActiveDocument` trong Service (CAD001), và không có `ICadContext`, nên không lock, không undo, không test được.
3. **Prompt trong Service**, mà Service lại được gọi từ palette (application context) ([ADR-004](T09_SENIOR_PLAYBOOK.md#1-adr-vì-sao-kiến-trúc-như-hiện-tại)). `.Value` không kiểm `Status` nên ESC vẫn vẽ ở (0,0). Điểm UCS chưa đổi sang WCS ([B15](T03_PITFALLS_CASEBOOK.md#b15-tọa-độ-lệch-khi-ucs-bị-xoay)).
4. `ConfigureAwait(false)` khiến phần sau chạy trên **ThreadPool**, rồi gọi AutoCAD API ([B8](T03_PITFALLS_CASEBOOK.md#b8-gọi-autocad-api-từ-thread-nền)). Sau `await`, `doc` có thể đã đóng hoặc không còn active ([B6](T03_PITFALLS_CASEBOOK.md#b6-vẽ-nhầm-bản-vẽ-sau-await), [B7](T03_PITFALLS_CASEBOOK.md#b7-fatal-error-khi-bản-vẽ-bị-đóng-trong-lúc-chờ)).
5. Ghi DB từ application context **không `LockDocument`**, nên `eLockViolation` ([B1](T03_PITFALLS_CASEBOOK.md#b1-elockviolation-khi-bấm-nút-trên-palette)). Không undo mark.
6. `ms` mở `ForRead` rồi `AppendEntity`, nên `eNotOpenForWrite`. Thiếu `tr.AddNewlyCreatedDBObject(pl, true)` ([B10](T03_PITFALLS_CASEBOOK.md#b10-quên-addnewlycreateddbobject)). Thiếu `SetDatabaseDefaults`.
7. Indexer layer không kiểm `Has` nên `eKeyNotFound` nếu layer chưa có ([T01 §3.2](T01_AUTOCAD_NET_FOUNDATIONS.md#32-bảng-ký-hiệu-symbol-tables)). `.ToString()` của `ObjectId` **không phải tên layer**: phải gán `pl.Layer = "E-PANEL"` hoặc `pl.LayerId = id`.
8. `catch { }` nuốt lỗi commit, nên thay đổi có thể biến mất mà không ai biết ([B3](T03_PITFALLS_CASEBOOK.md#b3-thay-đổi-biến-mất-không-có-lỗi)).
9. `Publish` không bọc try/catch (subscriber lỗi làm thao tác đã commit trông như "thất bại"), và publish ngay cả khi commit thất bại vì lỗi ở (8) bị nuốt.
</details>

---

## 5. Ngân hàng câu hỏi

<details><summary><b>Q1.</b> Command context và application context khác nhau thế nào? Cho 3 ví dụ application context.</summary>

Command context: trong lệnh Modal, AutoCAD đã lock bản vẽ, gom Undo theo lệnh, prompt được. Application context: không có lock, phải tự lock khi ghi, không nên prompt. Ví dụ: nút bấm palette, `Application.Idle`, lệnh `CommandFlags.Session`, cửa sổ modeless. ([T01 §5](T01_AUTOCAD_NET_FOUNDATIONS.md#5-document-lock-và-hai-ngữ-cảnh-thực-thi))
</details>

<details><summary><b>Q2.</b> Quên <code>Commit()</code> thì chuyện gì xảy ra?</summary>

Transaction bị Abort khi Dispose, mọi thay đổi rollback, **không** crash. Nguy hiểm vì im lặng. ([T01 §4.1](T01_AUTOCAD_NET_FOUNDATIONS.md#41-vòng-đời))
</details>

<details><summary><b>Q3.</b> Khi nào phải tự <code>Dispose()</code> một entity?</summary>

Khi nó **không** được thêm vào database (bỏ dở vì lỗi, chỉ dùng tạm). Đối tượng mở qua transaction hoặc đã add + `AddNewlyCreatedDBObject` thì không cần. ([T01 §9](T01_AUTOCAD_NET_FOUNDATIONS.md#9-bộ-nhớ-và-vòng-đời-đối-tượng))
</details>

<details><summary><b>Q4.</b> Vì sao <code>CadContextGuard</code> cấm Execute lồng nhau, khi AutoCAD cho phép transaction lồng nhau?</summary>

Quy tắc thiết kế: một `Execute` là một đơn vị công việc (lock, transaction, bước Undo, kết quả). Lồng nhau là dấu hiệu Service gọi Service trong lambda, làm mờ ranh giới commit/undo. ([T02 §4.6](T02_ARCHITECTURE_DEEP_DIVE.md#46-cadcontextguard-chặn-lồng-nhau))
</details>

<details><summary><b>Q5.</b> <code>ObjectId</code> vs <code>Handle</code>: lưu cái nào vào file JSON bên ngoài?</summary>

`Handle`: bền theo file. `ObjectId` chỉ sống trong phiên. ([T01 §3.1](T01_AUTOCAD_NET_FOUNDATIONS.md#31-objectid-vs-handle-vs-dbobject))
</details>

<details><summary><b>Q6.</b> Giới hạn 16 KB của XData tính trên cái gì?</summary>

Trên **mỗi đối tượng**, gộp mọi ứng dụng gắn XData lên đối tượng đó. Mỗi chuỗi mã 1000 ≤ 255 ký tự. ([T01 §3.4](T01_AUTOCAD_NET_FOUNDATIONS.md#34-dictionary-extension-dictionary-xrecord))
</details>

<details><summary><b>Q7.</b> Vì sao không prompt bên trong transaction?</summary>

AutoCAD chờ người dùng vô thời hạn trong khi lock và transaction vẫn mở, gây treo hoặc khóa bản vẽ. Hỏi trước, vẽ sau. ([T01 §7.2](T01_AUTOCAD_NET_FOUNDATIONS.md#72-luật-thép-hỏi-trước-vẽ-sau))
</details>

<details><summary><b>Q8.</b> Lệnh async: chuyện gì xảy ra với lock và Undo tại <code>await</code> đầu tiên?</summary>

AutoCAD coi lệnh đã kết thúc: nhả lock, đóng bước Undo. Phần sau chạy ở application context, phải tự lock (`ICadContext` lo) và phải dùng Document đã chụp. ([T01 §8.3](T01_AUTOCAD_NET_FOUNDATIONS.md#83-lệnh-async-thực-chất-chạy-thế-nào))
</details>

<details><summary><b>Q9.</b> Thêm class lệnh mới, gõ lệnh báo <code>Unknown command</code>. Kiểm tra gì đầu tiên?</summary>

`[assembly: CommandClass(typeof(…))]` trong `Properties/AssemblyInfo.cs`. ([T01 §6.1](T01_AUTOCAD_NET_FOUNDATIONS.md#61-đăng-ký))
</details>

<details><summary><b>Q10.</b> Vì sao <code>Publish</code> trong Golden Sample nằm sau <code>TryExecute</code> và bọc try/catch?</summary>

Sau commit để subscriber đọc được dữ liệu mới và không bị coi là lồng nhau (subscriber gọi `ExecuteRead`). Try/catch vì dữ liệu đã commit: lỗi của subscriber không được biến thao tác thành thất bại. ([T02 §7.1](T02_ARCHITECTURE_DEEP_DIVE.md#71-eventaggregator-pubsub-trong-tiến-trình))
</details>

<details><summary><b>Q11.</b> EventAggregator giữ tham chiếu gì tới subscriber, và hệ quả?</summary>

Tham chiếu **mạnh**: không gỡ thì rò rỉ và vẫn nhận message. Dùng `TrackSubscription` + Dispose. ([T02 §7.1](T02_ARCHITECTURE_DEEP_DIVE.md#71-eventaggregator-pubsub-trong-tiến-trình))
</details>

<details><summary><b>Q12.</b> <code>CadPromptHelper.GetPoint</code> trả về hệ tọa độ nào?</summary>

WCS (đã đổi từ UCS), trừ khi `convertToWcs: false`. ([T01 §7.3](T01_AUTOCAD_NET_FOUNDATIONS.md#73-ba-hệ-tọa-độ-wcs-ucs-ocs))
</details>

<details><summary><b>Q13.</b> Vì sao <code>Styles.xaml</code> phải được tham chiếu bằng <code>DynamicResource</code>?</summary>

Nó được merge trong code-behind **sau** `InitializeComponent()`. `StaticResource` phân giải lúc parse nên chưa thấy key. ([A3](T03_PITFALLS_CASEBOOK.md#a3-show_ui_template-lỗi-provide-value-on-staticresourceextension))
</details>

<details><summary><b>Q14.</b> Không sửa database trong handler <code>ObjectErased</code> thì xử lý thế nào?</summary>

Ghi nhận `ObjectId`, đăng ký `Application.Idle` (one-shot) hoặc `CommandEnded`, xử lý ở đó qua `ICadContext`. ([T01 §10.2](T01_AUTOCAD_NET_FOUNDATIONS.md#102-năm-quy-tắc))
</details>

<details><summary><b>Q15.</b> Vì sao Serilog 3.x không dùng được trên AutoCAD 2024 nhưng Newtonsoft 13 thì được?</summary>

Identity trùng (Serilog giữ AssemblyVersion 2.0.0.0) với bản AutoCAD đã nạp, còn Newtonsoft 12 và 13 khác AssemblyVersion nên nạp song song được. ([A1](T03_PITFALLS_CASEBOOK.md#a1-serilog-missingmethodexception-ngay-khi-khởi-động))
</details>

<details><summary><b>Q16.</b> Vì sao plugin không chạy được trong accoreconsole, và đã chứng minh thế nào?</summary>

Plugin tham chiếu `AcMgd`. NETLOAD **riêng** `acmgd.dll` cũng crash `0xC00000FD`, chứng tỏ nguyên nhân độc lập với code plugin. ([A5](T03_PITFALLS_CASEBOOK.md#a5-accoreconsole-crash-0xc00000fd-khi-netload-plugin))
</details>

<details><summary><b>Q17.</b> Một test "luôn pass". Làm sao chứng minh nó thật sự kiểm tra?</summary>

Cố tình làm hỏng code và xác nhận test fail; test phải đòi bằng chứng thành công, không chỉ "không thấy lỗi". ([A6](T03_PITFALLS_CASEBOOK.md#a6-integration-test-báo-passed-dù-không-lệnh-nào-chạy))
</details>

<details><summary><b>Q18.</b> Phân tích đánh đổi của ADR-001 (một DLL). Khi nào nên tách?</summary>

Một DLL: nạp đơn giản, nhưng không test headless và không tái dùng lõi. Nên tách khi cần CI có AutoCAD headless, xử lý server, hoặc tái dùng lõi cho sản phẩm khác, và chấp nhận đổi quy trình nạp. ([T09 §1](T09_SENIOR_PLAYBOOK.md#1-adr-vì-sao-kiến-trúc-như-hiện-tại))
</details>

<details><summary><b>Q19.</b> AutoCAD crash ngẫu nhiên sau khi thêm một tính năng async. Quy trình chẩn đoán của bạn?</summary>

Tái hiện, đọc Event Viewer (mã lỗi, stack managed), thu hẹp (tắt tính năng), giả thuyết: gọi API từ thread nền / đối tượng đã đóng / document đã đổi, rồi thí nghiệm bác bỏ, sửa, chứng minh, viết incident report. ([T04](T04_DEBUGGING_TROUBLESHOOTING.md))
</details>

<details><summary><b>Q20.</b> Bạn phát hiện CHANGELOG mô tả một API không tồn tại. Bạn làm gì, và rút ra quy tắc gì?</summary>

Kiểm chứng bằng metadata, đính chính tài liệu (ghi rõ là đính chính), và thêm vào checklist review: mọi tên API trong tài liệu phải tồn tại. ([A14](T03_PITFALLS_CASEBOOK.md#a14-changelog-mô-tả-một-api-không-tồn-tại))
</details>

<details><summary><b>Q21.</b> Người dùng gõ <code>2,5</code> vào một ô bind thẳng vào <code>double</code> (như palette Grid Marker trước PR #11). Chuyện gì xảy ra, vì sao, và sửa thế nào?</summary>

Giá trị thành **25**, không có lỗi nào. WPF đổi số theo `Language` mặc định `en-US` (không theo culture `vi-VN` của Windows), nên dấu phẩy là dấu phân cách hàng nghìn. Sửa: bind ô vào chuỗi, tự parse bằng `InvariantCulture` + `NumberStyles.Float`, lỗi đi vào validate. Đó là cách PR #11 sửa Golden Sample (B-31, [U01 §5](U01_WPF_MVVM_BASICS.md#5-ba-bẫy-khi-nhập-số)).
</details>

<details><summary><b>Q22.</b> Nút trên palette cần người dùng chọn một điểm trên bản vẽ. Vì sao không gọi <code>GetPoint</code> trong ViewModel, và cách đúng là gì?</summary>

Palette chạy ở application context: prompt ở đó sai ngữ cảnh, và ViewModel không được biết AutoCAD (CAD002). Cách đúng: nhập tọa độ trên form, hoặc để nút gửi một lệnh (`SendStringToExecute` sau interface `ICommandLauncher`) để lệnh prompt, hoặc dùng `ExecuteInCommandContextAsync` ([U02 §7.2](U02_UI_IN_AUTOCAD.md#72-nút-palette-chạy-một-lệnh-để-prompt), [T08 §5](T08_RECIPES_EXTENDING.md#5-palette-cần-người-dùng-chọn-điểm-trên-màn-hình)).
</details>

<details><summary><b>Q23.</b> Vì sao khung xem trước vẽ từ <code>PreviewShape</code> chứ không giữ <code>Entity</code>?</summary>

`DBObject` chỉ dùng được trong transaction đã mở nó; ViewModel không được biết AutoCAD và phải test được; và preview cùng nơi vẽ thật đọc chung một kết quả của Planner nên không thể lệch nhau ([U03 §1](U03_DRAWING_ON_UI.md#1-kiến-trúc-một-kế-hoạch-hai-nơi-vẽ)).
</details>

<details><summary><b>Q24.</b> Lật trục Y bằng <code>ScaleTransform(1, -1)</code> cho cả canvas thì hỏng gì? Cách đúng?</summary>

Mọi thứ bị lật theo: chữ lộn ngược, và độ dày nét bị nhân theo scale. Cách đúng là đổi **từng điểm** sang pixel (`MaxY − y`) rồi mới vẽ ([U03 §3](U03_DRAWING_ON_UI.md#3-bước-2--biến-đổi-tọa-độ)).
</details>

<details><summary><b>Q25.</b> Chọn jig, transient hay khung xem trước trên palette cho: (a) đặt block theo chuột; (b) xem trước cả lưới rồi hỏi Yes/No trên dòng lệnh; (c) người dùng chỉnh 10 thông số trên form?</summary>

(a) **Jig**; (b) **transient**; (c) **khung xem trước trên palette** (`CadPreview`). Cả ba đều không ghi vào database cho tới khi chốt ([U03](U03_DRAWING_ON_UI.md)).
</details>

---

**Quay về:** [Lộ trình đào tạo](README.md) · [Mục lục tài liệu](../README.md)
