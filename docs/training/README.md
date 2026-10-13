> **📚 Học viện CadCoreFramework** · [🏠 README dự án](../../README.md) · [📑 Giáo trình thực chiến (Phần 1–9)](../00_TOC.md) · [📖 Thuật ngữ](GLOSSARY.md)

# 🎓 Lộ Trình Đào Tạo: Từ Fresher Đến Senior AutoCAD .NET

Bộ tài liệu này dành cho người **kế thừa và phát triển tiếp** CadCoreFramework. Mục tiêu không dừng ở "chạy được dự án". Sau lộ trình, bạn phải **hiểu vì sao từng dòng code được viết như vậy**, **tự chẩn đoán được sự cố**, và **ra quyết định kiến trúc** như một Senior trong lĩnh vực plugin AutoCAD .NET (C#).

> [!IMPORTANT]
> **Nguyên tắc số 1 của bộ tài liệu: mọi khẳng định đều phải kiểm chứng được.**
> - Kiến thức về dự án luôn dẫn tới **file nguồn thật** (bấm vào link để mở).
> - Kiến thức về AutoCAD API đã được đối chiếu với metadata của `AcDbMgd.dll` / `AcCoreMgd.dll` (AutoCAD 2024, `AutoCAD.NET 24.0.0`).
> - Các sự cố ghi trong [T03](T03_PITFALLS_CASEBOOK.md) là **sự cố đã thật sự xảy ra** với dự án này, kèm cách tái hiện.
>
> Nếu bạn thấy tài liệu lệch với code: **code là sự thật**. Hãy sửa tài liệu bằng một PR. Đó cũng là một bài tập.

---

## 1. Bộ tài liệu gồm những gì

| Module | Nội dung | Cấp độ |
| :--- | :--- | :---: |
| [T01 — Nền tảng AutoCAD .NET](T01_AUTOCAD_NET_FOUNDATIONS.md) | Object model, Database, Transaction, Lock, Command/Application context, Prompt & UCS, Threading, bộ nhớ, Events, Undo, vòng đời plugin, accoreconsole | Fresher → Junior |
| [T02 — Mổ xẻ kiến trúc CadCoreFramework](T02_ARCHITECTURE_DEEP_DIVE.md) | Đọc code từng tầng: Bootstrapper, DI, `ICadContext`, `CadCommandBase`, Extensions, EventAggregator, Palette, Ribbon, Theme, Logger, Golden Sample | Fresher → Middle |
| [T03 — Sổ tay sự cố & bẫy](T03_PITFALLS_CASEBOOK.md) | 16 sự cố thật của dự án + 30 bẫy API kinh điển. Format: Triệu chứng → Gốc rễ → Sửa → Phòng ngừa | Junior → Senior |
| [T04 — Debug & chẩn đoán](T04_DEBUGGING_TROUBLESHOOTING.md) | Attach debugger, log, Debug Inspector, đọc `ErrorStatus`, phân tích crash (Event Viewer, mã lỗi), Fusion log, cây quyết định | Junior → Senior |
| [T05 — Chiến lược kiểm thử](T05_TESTING_STRATEGY.md) | Kim tự tháp test cho plugin CAD, `FakeCadContext`, test ViewModel, test XAML trên STA, smoke test thủ công | Junior → Middle |
| [T06 — Build, phát hành & DevOps](T06_BUILD_RELEASE_DEVOPS.md) | Giải phẫu `.csproj`, CI, quy trình release theo tag, bundle Autoloader, template `dotnet new`, chính sách dependency | Junior → Senior |
| [T07 — Hiệu năng & quy mô](T07_PERFORMANCE_AND_SCALE.md) | Mô hình chi phí, batch, lọc theo `ObjectClass`, side database, đo đạc | Middle → Senior |
| [T08 — Công thức mở rộng dự án](T08_RECIPES_EXTENDING.md) | Checklist thêm lệnh / tính năng / service / palette / ribbon / overrule / metadata / settings | Fresher → Middle |
| [T09 — Sổ tay Senior](T09_SENIOR_PLAYBOOK.md) | ADR (vì sao kiến trúc như hiện tại), checklist code review, **backlog kỹ thuật đã kiểm chứng**, lộ trình kiến trúc | Middle → Senior |
| [T10 — Bài lab & đánh giá](T10_LABS_AND_ASSESSMENT.md) | 16 bài lab có tiêu chí đạt, đồ án cuối khóa, ngân hàng câu hỏi, đề đánh giá lên cấp | Mọi cấp |
| [Thuật ngữ](GLOSSARY.md) | Từ điển Anh–Việt cho AutoCAD .NET | — |

Giáo trình **Phần 1–9** (`docs/00`–`09`) là tài liệu *tham khảo nhanh theo chủ đề*. Bộ **T01–T10** là tài liệu *đào tạo theo chiều sâu*. Hai bộ bổ sung cho nhau, và T-modules thường dẫn ngược về Phần 1–9 khi cần ví dụ ngắn.

---

## 2. Khung năng lực 4 cấp

Mỗi cấp có **tiêu chí đo được**. Mentor dùng bảng này khi đánh giá (đề cụ thể ở [T10 §4](T10_LABS_AND_ASSESSMENT.md#4-đề-đánh-giá-lên-cấp)).

| Năng lực | 🌱 Fresher | 🌿 Junior | 🌳 Middle | 🏔️ Senior |
| :--- | :--- | :--- | :--- | :--- |
| **AutoCAD API** | Biết `Document`/`Database`/`Transaction`/`ObjectId`; vẽ được entity qua `ICadScope` | Hiểu OpenMode, lock, command vs application context; dùng Prompt, SelectionFilter, XData/XRecord đúng | Làm chủ events, Undo, dynamic block, side database, Jig, Overrule | Giải thích được hành vi "lạ" của AutoCAD từ gốc (threading, lifetime, AppDomain dùng chung) |
| **Kiến trúc dự án** | Chạy và đọc được Golden Sample | Tự làm tính năng trọn vẹn theo Golden Sample | Thiết kế service/abstraction mới, giữ đúng quy tắc phụ thuộc | Viết ADR, thay đổi kiến trúc có kiểm soát, mở rộng analyzer |
| **Chất lượng** | Code build 0 warning | Viết unit test cho Planner/Service/ViewModel | Review code người khác bằng checklist T09 | Đặt chuẩn, nâng mức đảm bảo chất lượng (test, CI, release) |
| **Chẩn đoán** | Biết đọc log và Debug Inspector | Attach debugger, đọc `ErrorStatus`, tự sửa lỗi lock/open mode | Chẩn đoán lỗi nạp assembly, xung đột DLL với host | Phân tích crash không có stack trace (mã lỗi, dump, tái hiện cô lập) |
| **Vận hành** | Biết build & nạp plugin | Tạo PR đạt CI | Thực hiện release theo checklist | Chịu trách nhiệm phát hành, sự cố, chính sách dependency |
| **Con người** | Hỏi đúng câu hỏi | Viết mô tả PR rõ ràng | Hướng dẫn Fresher | Mentoring, chuyển giao tri thức, quyết định ưu tiên |

> [!TIP]
> Dấu hiệu bạn đã là Senior **không phải** là thuộc API. Đó là khi bạn **dự đoán được** một thay đổi sẽ hỏng ở đâu *trước khi* chạy nó. Ví dụ: "palette này mở được từ lệnh, nhưng nếu người dùng bấm nút khi đang ở bản vẽ thứ hai thì sẽ vẽ nhầm bản vẽ".

---

## 3. Lộ trình 12 tuần (gợi ý)

Mỗi tuần ≈ 60% đọc và thực hành, 40% làm lab. Đầu ra mỗi tuần là một **PR nhỏ** (lab, test, hoặc sửa docs) được mentor review.

| Tuần | Đọc | Lab ([T10](T10_LABS_AND_ASSESSMENT.md)) | Đầu ra kiểm chứng được |
| :---: | :--- | :--- | :--- |
| 1 | README dự án, [Phần 1](../01_GETTING_STARTED.md), [T01 §1–3](T01_AUTOCAD_NET_FOUNDATIONS.md) | L01 Môi trường | Chạy đủ 6 lệnh smoke; chỉ ra được file log |
| 2 | [T01 §4–6](T01_AUTOCAD_NET_FOUNDATIONS.md) | L02 Đọc database, L03 Thí nghiệm transaction | Giải thích được `eNotOpenForWrite`, `eLockViolation` bằng ví dụ tự chạy |
| 3 | [T01 §7–9](T01_AUTOCAD_NET_FOUNDATIONS.md), [Phần 3](../03_DRAWING_AND_INTERACTION.md) | L04 Prompt & UCS, L05 Selection filter | Lệnh vẽ đúng khi UCS xoay; chọn được dynamic block |
| 4 | [T02 §1–5](T02_ARCHITECTURE_DEEP_DIVE.md) | — | Vẽ lại sơ đồ khởi động (từ trí nhớ) và được mentor xác nhận |
| 5 | [T02 §6–12](T02_ARCHITECTURE_DEEP_DIVE.md), [Phần 9](../09_GOLDEN_SAMPLE.md) | L06 Metadata | XRecord ghi/đọc/xóa đúng |
| 6 | [T05](T05_TESTING_STRATEGY.md), [T08 §1–3](T08_RECIPES_EXTENDING.md) | L07 Nhân bản Golden Sample | Tính năng mới + ≥ 15 unit test |
| 7 | [T08 §4–6](T08_RECIPES_EXTENDING.md), [Phần 5](../05_WPF_UI_AND_MVVM.md) | L08 Palette, L11 Ribbon | Palette nhớ vị trí; Ribbon sống sót qua `CUI` |
| 8 | [T01 §10–11](T01_AUTOCAD_NET_FOUNDATIONS.md), [T03 Phần B](T03_PITFALLS_CASEBOOK.md#phần-b--bẫy-autocad-api-kinh-điển) | L09 Events, L10 Async | Palette tự cập nhật khi xóa entity; async không vẽ nhầm bản vẽ |
| 9 | [T04](T04_DEBUGGING_TROUBLESHOOTING.md), [T03 Phần A](T03_PITFALLS_CASEBOOK.md#phần-a--sự-cố-thật-của-dự-án) | L12 Sửa lỗi theme, L16 Pháp y crash | PR sửa một mục backlog thật (trên `v1.0.0`, đối chiếu với PR #6) + incident report |
| 10 | [T07](T07_PERFORMANCE_AND_SCALE.md) | L13 Hiệu năng, L14 Side database | Báo cáo đo đạc có số liệu trước/sau |
| 11 | [T06](T06_BUILD_RELEASE_DEVOPS.md), [T09](T09_SENIOR_PLAYBOOK.md) | L15 Release thử | Release candidate trên branch thử + release note |
| 12 | Ôn tập | **Đồ án cuối khóa** | Demo + ADR + bảo vệ trước mentor |

Người đã có nền C# vững có thể rút xuống 8 tuần bằng cách gộp tuần 1–2 và 4–5. Người mới học C# nên dành thêm 2–4 tuần học C# (generics, lambda, LINQ, async/await, `IDisposable`) **trước** khi bắt đầu.

---

## 4. Chuẩn bị môi trường

| Thành phần | Phiên bản | Ghi chú |
| :--- | :--- | :--- |
| Windows | 10/11 x64 | AutoCAD chỉ chạy 64-bit |
| AutoCAD | **2024** (đã kiểm chứng) | 2021–2024 dùng bản `net48`. AutoCAD 2025+ (`net8.0-windows`) **chưa được kiểm chứng**, nằm ngoài phạm vi hiện tại |
| Visual Studio | 2022 (khuyến nghị) | Workload ".NET desktop development" |
| .NET SDK | 8.x trở lên | CI dùng 8.0.x; máy dev có thể dùng SDK mới hơn |
| Git + GitHub CLI | mới nhất | `gh` để tạo PR, xem CI |
| Quyền | Không cần admin | Plugin, log và settings đều nằm trong `%AppData%` |

```powershell
git clone https://github.com/Tmphi/CadCoreFramework.git
cd CadCoreFramework
dotnet build CadCoreFramework.sln -c Debug
dotnet test CadCoreFramework.Tests/CadCoreFramework.Tests.csproj
```

Nạp `bin\Debug\net48\CadCoreFramework.dll` vào AutoCAD bằng `NETLOAD` hoặc bằng tool nạp DLL của nhóm. Chi tiết ở [T04 §2](T04_DEBUGGING_TROUBLESHOOTING.md#2-vòng-lặp-phát-triển-build--nạp--thử).

---

## 5. Quy ước đọc

| Ký hiệu | Ý nghĩa |
| :--- | :--- |
| 🔎 **Đọc code** | Mở file được dẫn và đọc *trước khi* đọc tiếp |
| 🧪 **Thử ngay** | Thí nghiệm 5–15 phút trên AutoCAD hoặc unit test |
| ⚠️ **Bẫy** | Lỗi đã từng xảy ra (với dự án này hoặc rất phổ biến) |
| ✅ **Tự kiểm tra** | Câu hỏi cuối mục. Đáp án nằm trong khối thu gọn, hãy tự trả lời trước khi mở |
| `B-xx` | Mã một mục trong [backlog kỹ thuật](T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng): vấn đề thật đã kiểm chứng trong code (B-01…B-27 lập tại v1.0.0; hầu hết đã sửa ở PR #6 — cột Trạng thái) |
| Lab `TÊN` / HeadlessLab | Một kiểm thử trên AutoCAD thật trong [`tests/integration/HeadlessLab`](../../tests/integration/README.md#headlesslab--kiểm-thử-headless-trên-autocad-thật) |

---

## 6. Dành cho mentor

1. **Đừng đưa đáp án code.** Đưa *câu hỏi dẫn đường* ("Lambda này đang chạy trong context nào?"). Các câu hỏi chuẩn ở cuối mỗi module.
2. **Review bằng checklist** [T09 §2](T09_SENIOR_PLAYBOOK.md#2-checklist-code-review). Yêu cầu học viên tự review PR của mình bằng checklist trước khi gửi.
3. **Ưu tiên lab "sửa backlog thật"** (L06, L08, L11, L12…). Học viên học sâu hơn khi sửa vấn đề có thật. Các mục đó đã được sửa ở PR #6, nên học viên làm trên tag `v1.0.0` rồi **đối chiếu** với lời giải đã merge ([T10 quy tắc 6](T10_LABS_AND_ASSESSMENT.md#1-quy-tắc-làm-lab)) — chấm cả lời giải lẫn chất lượng phần so sánh.
4. **Bắt buộc tái hiện trước khi sửa.** Mọi PR sửa lỗi phải mô tả cách tái hiện, như các mục trong T03.
5. **Đánh giá lên cấp** theo [T10 §4](T10_LABS_AND_ASSESSMENT.md#4-đề-đánh-giá-lên-cấp): phần vấn đáp nặng hơn phần trắc nghiệm.

---

## 7. Duy trì bộ tài liệu

- Tài liệu sống cùng code: **PR thay đổi hành vi phải cập nhật T-module liên quan** (checklist review có mục này).
- Khi gặp sự cố mới: thêm một mục vào [T03](T03_PITFALLS_CASEBOOK.md) theo đúng format, *sau khi* đã có cách tái hiện.
- Khi sửa một mục backlog `B-xx`: cập nhật trạng thái trong [T09 §3](T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng) và mọi chỗ trong T-modules nhắc tới nó.
- Trạng thái kiểm chứng của bộ tài liệu: viết dựa trên `main` tại thời điểm phát hành **v1.0.0** (2026-10-13), AutoCAD 2024; cập nhật theo PR #6 (sửa backlog, HeadlessLab — 2026-10-13/14). Chỗ nào mô tả hành vi của v1.0.0 thì ghi rõ "trên v1.0.0".
