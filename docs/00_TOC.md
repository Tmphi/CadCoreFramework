# 📚 CadCoreFramework - Mục Lục Toàn Thư

Chào mừng bạn đến với tài liệu hướng dẫn chính thức của **CadCoreFramework**! Đây là khung mẫu (Template) chuẩn Enterprise dành cho AutoCAD .NET.

> [!TIP]
> **Người kế thừa dự án — từ người chưa biết lập trình đến Senior:** bắt đầu từ **[🎓 Học viện CadCoreFramework](training/README.md)**. Chưa biết C#: học **Giai đoạn 0** trước ([C00 — Bắt đầu từ đây](training/C00_START_HERE.md) → C05, 6 tuần, không cần AutoCAD). Sau đó là lộ trình đào tạo chuyên sâu 12 tuần gồm T01 Nền tảng AutoCAD .NET · T02 Mổ xẻ kiến trúc · T03 Sổ tay sự cố · T04 Debug · T05 Kiểm thử · T06 Phát hành · T07 Hiệu năng · T08 Công thức mở rộng · T09 Sổ tay Senior · T10 Lab & đánh giá. Phần 1–9 bên dưới là tài liệu *tham khảo nhanh theo chủ đề*.

Hãy duyệt qua các tài liệu dưới đây theo thứ tự từ cơ bản đến nâng cao:

1. [Phần 1: Bắt Đầu & Tư Duy (Getting Started)](01_GETTING_STARTED.md)
   - Khởi động nhanh trong 5 phút
   - Lộ trình làm quen (Fresher Roadmap)
   - Tư duy chuyển đổi từ Scripting sang Enterprise
2. [Phần 2: Kiến Trúc Cốt Lõi (Core Architecture)](02_CORE_ARCHITECTURE.md)
   - Quản lý Giao dịch (Transaction) An Toàn với ICadContext
   - Dependency Injection & Bootstrapper
   - Lớp lệnh CommandBase
3. [Phần 3: Thao Tác Vẽ & Tương Tác (Drawing & Interaction)](03_DRAWING_AND_INTERACTION.md)
   - Nhập liệu từ người dùng an toàn (CadPromptHelper)
   - Vẽ hình cơ bản (CadScopeDrawingExtensions)
   - Quản lý Layer & Style
   - Lọc đối tượng (Selection Filter)
   - Tương tác kéo thả chuột (Jigs)
4. [Phần 4: Siêu Dữ Liệu & Nghiệp Vụ (Metadata & Services)](04_METADATA_AND_SERVICES.md)
   - XData vs XRecord
   - Tương tác Dynamic Blocks
   - Sử dụng các Services có sẵn (Layer, Plot, Settings)
5. [Phần 5: Giao Diện Người Dùng & MVVM (WPF UI & Themes)](05_WPF_UI_AND_MVVM.md)
   - Palette & Ribbon
   - MVVM trong AutoCAD
   - Event Aggregator
   - AutoCad Theme (Dark/Light)
6. [Phần 6: DevOps, Unit Test & Tiêu Chuẩn (DevOps & Standards)](06_DEVOPS_AND_STANDARDS.md)
   - Xử lý sự cố kinh điển (Troubleshooting)
   - Tuyệt chiêu Hot-Reload WPF
   - Hướng dẫn Viết Unit Test cho AutoCAD
   - CI/CD với GitHub Actions
   - Quy chuẩn Viết Code (Coding Conventions)
7. [Phần 7: API Reference](07_API_REFERENCE.md)
   - CadScopeDrawingExtensions, Utils, Builders
8. [Phần 8: Cookbook — Công Thức Thực Chiến](08_COOKBOOK.md)
   - Vẽ Title Block A3, Batch export PDF, Purge+Audit, XRef
   - Jig vẽ Polyline tự do, Dynamic Block, Merge Layers
   - Selection Filter patterns, XData round-trip, Subscription Dispose
   - Chuyển thao tác nặng sang Async
9. [Phần 9: Golden Sample — Khuôn Mẫu Chuẩn](09_GOLDEN_SAMPLE.md)
   - Tính năng Grid Marker đi qua đủ các tầng: Model → Planner → Service → Command / ViewModel → View
   - Functional Core / Imperative Shell: tách logic thuần để test được kết quả
   - Command context vs Application context (Palette)
   - Checklist khi copy làm tính năng mới
