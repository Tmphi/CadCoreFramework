> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · **Thuật ngữ**

# 📖 Thuật Ngữ AutoCAD .NET (Anh – Việt)

Trong code và khi trao đổi, **giữ nguyên thuật ngữ tiếng Anh**. Phần tiếng Việt để hiểu nghĩa. Cột cuối dẫn tới chỗ giải thích kỹ.

| Thuật ngữ | Nghĩa | Xem |
| :--- | :--- | :--- |
| **accoreconsole** | AutoCAD không giao diện, chạy script `.scr`; không nạp được `AcMgd` | [T01 §13](T01_AUTOCAD_NET_FOUNDATIONS.md#13-accoreconsole-autocad-không-giao-diện) |
| **ADR** (Architecture Decision Record) | Ghi chép một quyết định kiến trúc: bối cảnh → quyết định → hệ quả | [T09 §1](T09_SENIOR_PLAYBOOK.md#1-adr-vì-sao-kiến-trúc-như-hiện-tại) |
| **Anonymous block** | Block ẩn danh (`*U…`, `*D…`) do AutoCAD tự tạo, ví dụ cho dynamic block đã chỉnh tham số | [T01 §3.3](T01_AUTOCAD_NET_FOUNDATIONS.md#33-blocktablerecord-mọi-entity-đều-có-nhà) |
| **AppDomain** | Vùng nạp assembly của .NET Framework; plugin dùng chung với AutoCAD và plugin khác | [T01 §1](T01_AUTOCAD_NET_FOUNDATIONS.md#1-plugin-sống-ở-đâu-autocad-net-và-appdomain-dùng-chung) |
| **Application context** | Ngữ cảnh ngoài lệnh (palette, Idle, lệnh Session): không có lock sẵn | [T01 §5](T01_AUTOCAD_NET_FOUNDATIONS.md#5-document-lock-và-hai-ngữ-cảnh-thực-thi) |
| **Assembly identity** | Tên + AssemblyVersion + culture + public key; CLR coi hai DLL cùng identity là một | [T03 A1](T03_PITFALLS_CASEBOOK.md#a1-serilog-missingmethodexception-ngay-khi-khởi-động) |
| **AssemblyResolve** | Sự kiện CLR gọi khi không tự tìm được DLL phụ thuộc | [T01 §12.4](T01_AUTOCAD_NET_FOUNDATIONS.md#124-phân-giải-assembly-phụ-thuộc) |
| **Attribute definition / reference** | Định nghĩa thuộc tính trong block / giá trị thuộc tính trên từng lần chèn | [`BlockHelper`](../../Utils/BlockHelper.cs) |
| **Autoloader / Bundle** | Cơ chế tự nạp plugin từ thư mục `*.bundle` + `PackageContents.xml` | [T06 §6](T06_BUILD_RELEASE_DEVOPS.md#6-bundle-autoloader-và-cài-đặt) |
| **Block definition** (`BlockTableRecord`) | Định nghĩa block; cũng là "nhà" của Model Space / Paper Space | [T01 §3.3](T01_AUTOCAD_NET_FOUNDATIONS.md#33-blocktablerecord-mọi-entity-đều-có-nhà) |
| **Block reference** | Một lần chèn block vào bản vẽ | — |
| **CadResult** | Kiểu kết quả của framework: thành công + giá trị, hoặc thất bại + thông điệp | [T02 §4.7](T02_ARCHITECTURE_DEEP_DIVE.md#47-cadresult--cadresultt) |
| **Command context** | Ngữ cảnh bên trong lệnh Modal: AutoCAD đã lock bản vẽ, gom Undo theo lệnh | [T01 §5](T01_AUTOCAD_NET_FOUNDATIONS.md#5-document-lock-và-hai-ngữ-cảnh-thực-thi) |
| **CommandClass / CommandMethod** | Attribute khai báo class chứa lệnh / method là lệnh | [T01 §6](T01_AUTOCAD_NET_FOUNDATIONS.md#6-lệnh-command-đăng-ký-instance-commandflags) |
| **CommandFlags** | Cờ hành vi của lệnh (Modal, Session, UsePickSet…) | [T01 §6.3](T01_AUTOCAD_NET_FOUNDATIONS.md#63-commandflags) |
| **Database** | Dữ liệu một file DWG | [T01 §2](T01_AUTOCAD_NET_FOUNDATIONS.md#2-object-model-application--document--database) |
| **DBObject** | Đối tượng .NET bọc đối tượng C++ đang mở; chỉ hợp lệ trong transaction | [T01 §3.1](T01_AUTOCAD_NET_FOUNDATIONS.md#31-objectid-vs-handle-vs-dbobject) |
| **DeepClone / WblockClone** | Sao chép sâu trong một database / giữa hai database | [T01 §9.1](T01_AUTOCAD_NET_FOUNDATIONS.md#91-sao-chép-đối-tượng-clone-vs-deepcloneobjects) |
| **Document** | Một tab bản vẽ đang mở: Database + Editor + lock | [T01 §2](T01_AUTOCAD_NET_FOUNDATIONS.md#2-object-model-application--document--database) |
| **Document lock** | Khóa bản vẽ để ghi; bắt buộc ở application context | [T01 §5.2](T01_AUTOCAD_NET_FOUNDATIONS.md#52-documentlock-và-documentlockmode) |
| **DXF code** | Mã nhóm dữ liệu (0 = loại, 8 = layer, 1001 = XData app…) dùng trong filter, XData | [T01 §7.4](T01_AUTOCAD_NET_FOUNDATIONS.md#74-selection-và-selectionfilter) |
| **Dynamic block** | Block có tham số (kéo giãn, visibility…) | [T08 §10](T08_RECIPES_EXTENDING.md#10-làm-việc-với-dynamic-block) |
| **Effective name** | Tên gốc của dynamic block, khác tên ẩn danh `*U` | [T03 B12](T03_PITFALLS_CASEBOOK.md#b12-lọc-block-theo-tên-bỏ-sót-dynamic-block) |
| **Entity** | Đối tượng hình học (Line, Circle, Polyline, Text…) | — |
| **ErrorStatus** | Mã lỗi của `Autodesk.AutoCAD.Runtime.Exception` (`LockViolation`…) | [T01 §14](T01_AUTOCAD_NET_FOUNDATIONS.md#14-phụ-lục-errorstatus-thường-gặp) |
| **Extension Dictionary** | Dictionary riêng của một đối tượng, chứa Xrecord | [T01 §3.4](T01_AUTOCAD_NET_FOUNDATIONS.md#34-dictionary-extension-dictionary-xrecord) |
| **Functional core / Imperative shell** | Lõi logic thuần + vỏ mỏng chạm hệ thống ngoài | [Phần 9 §9.2](../09_GOLDEN_SAMPLE.md) |
| **Golden Sample** | Tính năng mẫu chuẩn `Samples/GridMarker` | [Phần 9](../09_GOLDEN_SAMPLE.md) |
| **Handle** | Định danh bền vững của đối tượng trong file DWG | [T01 §3.1](T01_AUTOCAD_NET_FOUNDATIONS.md#31-objectid-vs-handle-vs-dbobject) |
| **Implied selection** (pick-first) | Đối tượng đã chọn trước khi gõ lệnh (`UsePickSet` + `SelectImplied`) | [T01 §7.4](T01_AUTOCAD_NET_FOUNDATIONS.md#74-selection-và-selectionfilter) |
| **Incident report** | Báo cáo sự cố theo mẫu | [T04 §8](T04_DEBUGGING_TROUBLESHOOTING.md#8-mẫu-báo-cáo-sự-cố-incident-report) |
| **Jig** | Tương tác kéo-thả xem trước (`EntityJig`, `DrawJig`) | [T02 §6.2](T02_ARCHITECTURE_DEEP_DIVE.md#62-nhóm-chức-năng) |
| **MdiActiveDocument** | Bản vẽ đang được focus; thay đổi khi người dùng đổi tab | [T01 §2.1](T01_AUTOCAD_NET_FOUNDATIONS.md#21-mdiactivedocument-tiện-nhưng-nguy-hiểm) |
| **NETLOAD** | Lệnh nạp DLL .NET thủ công | — |
| **NOD** (Named Objects Dictionary) | Dictionary gốc cấp bản vẽ | [T01 §3.4](T01_AUTOCAD_NET_FOUNDATIONS.md#34-dictionary-extension-dictionary-xrecord) |
| **ObjectId** | Định danh đối tượng trong phiên làm việc | [T01 §3.1](T01_AUTOCAD_NET_FOUNDATIONS.md#31-objectid-vs-handle-vs-dbobject) |
| **OpenMode** | `ForRead` / `ForWrite` / `ForNotify` | [T01 §4.2](T01_AUTOCAD_NET_FOUNDATIONS.md#42-openmode) |
| **Overrule** | Can thiệp hành vi/hiển thị entity (vẽ thêm, grip…) | [T08 §7](T08_RECIPES_EXTENDING.md#7-thêm-overrule) |
| **PaletteSet** | Cửa sổ neo của AutoCAD chứa UserControl WPF | [T02 §8](T02_ARCHITECTURE_DEEP_DIVE.md#8-paletteservice-và-vòng-đời-viewmodel) |
| **Regen** | Tính lại hiển thị toàn bản vẽ (đắt) | [T07 §6](T07_PERFORMANCE_AND_SCALE.md#6-đồ-họa-và-regen) |
| **RegApp** | Tên ứng dụng đăng ký cho XData (`RegAppTable`) | [T03 B17](T03_PITFALLS_CASEBOOK.md#b17-ghi-xdata-khi-regapp-chưa-được-đăng-ký) |
| **SECURELOAD / TRUSTEDPATHS** | Cơ chế bảo mật nạp DLL / danh sách thư mục tin cậy | [T01 §12.3](T01_AUTOCAD_NET_FOUNDATIONS.md#123-bảo-mật-nạp-secureload-và-trustedpaths) |
| **Service locator** | Lấy dependency từ container toàn cục; chỉ cho phép ở biên | [T02 §3.1](T02_ARCHITECTURE_DEEP_DIVE.md#31-service-locator-ở-biên-được-phép-và-có-giới-hạn) |
| **Side database** | `Database` đọc từ file DWG mà không mở Document | [T07 §4](T07_PERFORMANCE_AND_SCALE.md#4-xử-lý-nhiều-bản-vẽ-side-database) |
| **Smoke test** | Kiểm tra nhanh các lệnh chính trên AutoCAD thật | [T05 §7](T05_TESTING_STRATEGY.md#7-những-gì-chỉ-autocad-thật-mới-kiểm-được) |
| **Symbol table** | Bảng ký hiệu: Layer, Block, TextStyle, Linetype, DimStyle, RegApp… | [T01 §3.2](T01_AUTOCAD_NET_FOUNDATIONS.md#32-bảng-ký-hiệu-symbol-tables) |
| **SynchronizationContext** | Cơ chế đưa phần sau `await` về đúng thread | [T01 §8.2](T01_AUTOCAD_NET_FOUNDATIONS.md#82-asyncawait-và-synchronizationcontext) |
| **Sysvar** (system variable) | Biến hệ thống AutoCAD (`COLORTHEME`, `BACKGROUNDPLOT`…) | [T04 §5.3](T04_DEBUGGING_TROUBLESHOOTING.md#53-thí-nghiệm-an-toàn-với-accoreconsole) |
| **Transaction** | Phiên giao dịch: mở đối tượng, commit hoặc rollback | [T01 §4](T01_AUTOCAD_NET_FOUNDATIONS.md#4-transaction-trái-tim-của-mọi-thao-tác) |
| **UCS / WCS / OCS** | Hệ tọa độ người dùng / thế giới / đối tượng | [T01 §7.3](T01_AUTOCAD_NET_FOUNDATIONS.md#73-ba-hệ-tọa-độ-wcs-ucs-ocs) |
| **Undo mark** | Đánh dấu gom nhiều thao tác thành một bước Ctrl+Z | [T01 §11](T01_AUTOCAD_NET_FOUNDATIONS.md#11-undo) |
| **UpgradeOpen** | Chuyển đối tượng đang mở `ForRead` sang `ForWrite` | [T01 §4.2](T01_AUTOCAD_NET_FOUNDATIONS.md#42-openmode) |
| **WorkingDatabase** | Database mà một số API ngầm dùng (`HostApplicationServices`) | [T07 §4](T07_PERFORMANCE_AND_SCALE.md#4-xử-lý-nhiều-bản-vẽ-side-database) |
| **XData** | Dữ liệu mở rộng gắn thẳng vào đối tượng, ≤ 16 KB mỗi đối tượng | [T01 §3.4](T01_AUTOCAD_NET_FOUNDATIONS.md#34-dictionary-extension-dictionary-xrecord) |
| **Xrecord** | Bản ghi dữ liệu tùy ý trong dictionary | [T01 §3.4](T01_AUTOCAD_NET_FOUNDATIONS.md#34-dictionary-extension-dictionary-xrecord) |
