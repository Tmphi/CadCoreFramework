> **📚 Tài liệu CadCoreFramework** · [🏠 README dự án](../README.md) · [🎓 Học viện](training/README.md) · [📘 Tra cứu](#-tra-cứu-reference) · [📖 Thuật ngữ](training/GLOSSARY.md)

# 📚 Tài Liệu CadCoreFramework

Toàn bộ tài liệu của dự án nằm trong thư mục này và chia làm **hai lớp**, cùng một bộ quy ước:

| Lớp | Thư mục | Dùng để | Đọc thế nào |
| :--- | :--- | :--- | :--- |
| 🎓 **Học viện** | [`training/`](training/README.md) | Học: từ người chưa biết lập trình đến Senior, có lab và đề đánh giá | Theo lộ trình, từ trên xuống |
| 📘 **Tra cứu** | [`reference/`](#-tra-cứu-reference) | Tra nhanh khi đang làm việc: hợp đồng API, khuôn mẫu, quy ước | Mở đúng trang cần, không cần đọc hết |

Hai lớp dẫn qua lại: trang tra cứu trỏ tới module học khi cần giải thích sâu, module học trỏ tới trang tra cứu khi cần bảng tóm tắt.

> [!IMPORTANT]
> **Nguyên tắc chung của mọi trang:** mọi khẳng định phải kiểm chứng được. Kiến thức về dự án dẫn tới **file nguồn thật**; code mẫu viết bằng C# 10 và biên dịch được; con số (số test, coverage) là kết quả chạy thật. Nếu thấy tài liệu lệch với code: **code là sự thật**, hãy sửa tài liệu bằng một PR.

---

## Bắt đầu từ đâu?

| Bạn là | Bắt đầu ở |
| :--- | :--- |
| Chưa từng lập trình, hoặc chưa biết C# | [C00 — Bắt đầu từ đây](training/C00_START_HERE.md) (Giai đoạn 0, không cần AutoCAD) |
| Biết C#, muốn chạy thử dự án ngay | [R01 — Khởi động nhanh](reference/R01_QUICKSTART.md) |
| Biết C#, muốn học bài bản AutoCAD .NET | [Học viện — Bắt đầu từ đâu?](training/README.md#bắt-đầu-từ-đâu) |
| Biết C#, cần làm được việc trong dự án sớm | [Lộ trình nhanh](training/README.md#lộ-trình-nhanh): chỉ đọc các mục 🎯, đọc ≈ 3,5 giờ rồi làm lab L07 |
| Đã làm AutoCAD .NET, mới vào dự án | [R02 — Kiến trúc](reference/R02_ARCHITECTURE.md) → [R05 — Golden Sample](reference/R05_GOLDEN_SAMPLE.md) → [T08 — Công thức mở rộng](training/T08_RECIPES_EXTENDING.md) |
| Làm giao diện (palette, hộp thoại, xem trước) | [U00 — Nhánh UI](training/U00_UI_START_HERE.md), tra nhanh ở [R04](reference/R04_UI.md) |
| Đang gặp sự cố | [T04 §9 — Cây quyết định](training/T04_DEBUGGING_TROUBLESHOOTING.md#9-cây-quyết-định-triệu-chứng--nguyên-nhân), [T03 — Sổ tay sự cố](training/T03_PITFALLS_CASEBOOK.md) |
| Gặp cảnh báo CAD001–CAD003 | [R06 §5](reference/R06_STANDARDS.md#5-analyzer-cad001cad003) |

---

## 🎓 Học viện (training)

Lộ trình khoảng 20 tuần, khung năng lực 5 cấp 🌰 Nhập môn → 🏔️ Senior. Chi tiết, lộ trình theo tuần và môi trường: [training/README.md](training/README.md).

| Nhóm | Module |
| :--- | :--- |
| **Giai đoạn 0 — Nhập môn lập trình & C#** | [C00](training/C00_START_HERE.md) Bắt đầu · [C01](training/C01_FIRST_PROGRAM.md) Chương trình đầu tiên · [C02](training/C02_DATA_AND_COLLECTIONS.md) Dữ liệu · [C03](training/C03_OBJECT_ORIENTED.md) Hướng đối tượng · [C04](training/C04_CSHARP_IN_THIS_PROJECT.md) C# dự án dùng · [C05](training/C05_ASYNC_TESTING_GIT.md) Async, test, Git |
| **Giai đoạn 1–3 — AutoCAD .NET và dự án** | [T01](training/T01_AUTOCAD_NET_FOUNDATIONS.md) Nền tảng · [T02](training/T02_ARCHITECTURE_DEEP_DIVE.md) Kiến trúc · [T03](training/T03_PITFALLS_CASEBOOK.md) Sự cố & bẫy · [T04](training/T04_DEBUGGING_TROUBLESHOOTING.md) Debug · [T05](training/T05_TESTING_STRATEGY.md) Kiểm thử · [T06](training/T06_BUILD_RELEASE_DEVOPS.md) Build & phát hành · [T07](training/T07_PERFORMANCE_AND_SCALE.md) Hiệu năng · [T08](training/T08_RECIPES_EXTENDING.md) Công thức mở rộng · [T09](training/T09_SENIOR_PLAYBOOK.md) Sổ tay Senior · [T10](training/T10_LABS_AND_ASSESSMENT.md) Lab & đánh giá |
| **Nhánh UI** | [U00](training/U00_UI_START_HERE.md) Bắt đầu · [U01](training/U01_WPF_MVVM_BASICS.md) WPF & MVVM · [U02](training/U02_UI_IN_AUTOCAD.md) UI trong AutoCAD · [U03](training/U03_DRAWING_ON_UI.md) Vẽ thực thể lên UI |
| **Từ điển** | [Thuật ngữ Anh–Việt](training/GLOSSARY.md) |

---

## 📘 Tra cứu (reference)

| Trang | Nội dung | Đọc kèm |
| :--- | :--- | :--- |
| [R01 — Khởi động nhanh](reference/R01_QUICKSTART.md) | Yêu cầu, cấu trúc thư mục, build → `NETLOAD` → `CORE_INFO`, 9 lệnh có sẵn, luồng dữ liệu một chiều | [T01](training/T01_AUTOCAD_NET_FOUNDATIONS.md) |
| [R02 — Kiến trúc cốt lõi](reference/R02_ARCHITECTURE.md) | Hợp đồng `ICadContext` (ném hay trả `CadResult`, lock, undo), khởi động và DI, `CadCommandBase` | [T02](training/T02_ARCHITECTURE_DEEP_DIVE.md) |
| [R03 — API](reference/R03_API.md) | `CadPromptHelper`, hàm `Draw*`/`Ensure*`, `SelectionFilterBuilder`, XData/XRecord, dynamic block, jig, service có sẵn, tiện ích | [T02 §6](training/T02_ARCHITECTURE_DEEP_DIVE.md#6-extensions-và-utils-api-vẽ-một-dòng), [T08](training/T08_RECIPES_EXTENDING.md) |
| [R04 — Giao diện](reference/R04_UI.md) | Palette, Ribbon, `ViewModelBase`/`RunSafe`, ô nhập số, theme sáng/tối | [U00–U03](training/U00_UI_START_HERE.md) |
| [R05 — Golden Sample](reference/R05_GOLDEN_SAMPLE.md) | Khuôn mẫu chuẩn `Samples/GridMarker/`: sáu nguyên tắc, bảng test, checklist khi copy | [T08 §2](training/T08_RECIPES_EXTENDING.md#2-thêm-một-tính-năng-trọn-vẹn), lab L07 |
| [R06 — Chuẩn mực](reference/R06_STANDARDS.md) | Nạp lại không tắt AutoCAD, chiến lược unit test, CI/CD, quy ước code, analyzer CAD001–CAD003 | [T05](training/T05_TESTING_STRATEGY.md), [T06](training/T06_BUILD_RELEASE_DEVOPS.md), [T09](training/T09_SENIOR_PLAYBOOK.md) |

---

## Quy ước chung

- **Tên file** có tiền tố cho biết lớp và thứ tự: `C` = nhập môn C#, `T` = đào tạo AutoCAD .NET, `U` = nhánh UI, `R` = tra cứu. Trích dẫn trong tài liệu dùng dạng `T01 §4.2`, `R03 §2`.
- **Đầu mỗi trang** là thanh điều hướng (lớp · trang trước · trang sau) và một dòng cho biết khi nào dùng trang đó / cần học gì trước.
- **Đường dẫn tới code** là đường dẫn tương đối tới file thật trong repo, bấm vào là mở được.
- **Ngôn ngữ:** tiếng Việt; thuật ngữ tiếng Anh giữ nguyên khi đó là tên trong code hoặc trong AutoCAD (`Transaction`, `ObjectId`), giải nghĩa ở [Thuật ngữ](training/GLOSSARY.md).
- **Code mẫu:** C# 10, không `ImplicitUsings`, `Nullable=annotations`, như dự án. Đoạn ghi "nguyên văn" phải khớp từng dòng với file nguồn.
- **Lịch sử thay đổi** của tài liệu ghi trong [CHANGELOG](../CHANGELOG.md).
