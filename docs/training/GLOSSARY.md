> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · **Thuật ngữ**

# 📖 Thuật Ngữ (Anh – Việt)

Trong code và khi trao đổi, **giữ nguyên thuật ngữ tiếng Anh**. Phần tiếng Việt để hiểu nghĩa. Cột cuối dẫn tới chỗ giải thích kỹ.

- [A. Lập trình & C# cho người mới](#a-lập-trình--c-cho-người-mới) — dùng trong Giai đoạn 0 (C00–C05).
- [B. AutoCAD .NET & dự án](#b-autocad-net--dự-án) — dùng từ T01 trở đi.
- [C. Giao diện: WPF & UI trong AutoCAD](#c-giao-diện-wpf--ui-trong-autocad) — dùng trong nhánh UI (U00–U03).

---

## A. Lập trình & C# cho người mới

| Thuật ngữ | Nghĩa | Xem |
| :--- | :--- | :--- |
| **Abstract class** | Class chỉ để kế thừa, không `new` trực tiếp được | [C03 §7](C03_OBJECT_ORIENTED.md#7-kế-thừa-abstract-virtual-override) |
| **Access modifier** (`public`, `internal`, `protected`, `private`) | Phạm vi được phép dùng một thành viên | [C03 §5](C03_OBJECT_ORIENTED.md#5-namespace-using-và-phạm-vi-truy-cập) |
| **`Action` / `Func`** | Delegate có sẵn: `Action<…>` không trả về gì, `Func<…, TResult>` trả về `TResult` | [C04 §1](C04_CSHARP_IN_THIS_PROJECT.md#1-delegate-hàm-là-một-giá-trị) |
| **Array** (mảng) | Dãy phần tử cùng kiểu, số phần tử cố định | [C02 §2](C02_DATA_AND_COLLECTIONS.md#2-mảng) |
| **`async` / `await`, `Task`** | Chờ một việc mất thời gian mà không chặn thread; `Task<T>` là việc sẽ cho ra một `T` | [C05 §1](C05_ASYNC_TESTING_GIT.md#1-async-và-await-chờ-mà-không-đứng-hình) |
| **Attribute** | Nhãn `[…]` gắn lên code để một công cụ đọc (`[CommandMethod]`, `[Fact]`) | [C04 §10](C04_CSHARP_IN_THIS_PROJECT.md#10-attribute-nhãn-cho-công-cụ-đọc) |
| **Branch / Commit / Push / Pull Request** | Nhánh làm việc / ảnh chụp thay đổi / đẩy lên GitHub / đề nghị gộp, có review | [C05 §4](C05_ASYNC_TESTING_GIT.md#4-git-và-pull-request) |
| **Breakpoint / Debugger** | Điểm dừng / công cụ chạy chương trình từng bước, xem giá trị biến | [C01 §9](C01_FIRST_PROGRAM.md#9-debug-xem-chương-trình-chạy-từng-bước) |
| **Class / Object** | Bản thiết kế / một thứ cụ thể tạo từ bản thiết kế bằng `new` | [C03 §1](C03_OBJECT_ORIENTED.md#1-class-và-object) |
| **Closure** (bao đóng) | Lambda dùng ("bắt") biến ở bên ngoài nó — bắt chính biến, không phải bản sao | [C04 §2](C04_CSHARP_IN_THIS_PROJECT.md#2-lambda-và-closure) |
| **Compile / Compiler error** (`CSxxxx`) | Biên dịch: kiểm tra và dịch code; mỗi lỗi có mã `CS…` tra cứu được | [C01 §3](C01_FIRST_PROGRAM.md#3-đọc-lỗi-biên-dịch) |
| **`const` / `readonly`** | Hằng số lúc biên dịch / field chỉ gán được một lần | [C02 §8](C02_DATA_AND_COLLECTIONS.md#8-const-và-readonly) |
| **Constructor** | Hàm trùng tên class, chạy đúng một lần khi `new` | [C03 §2](C03_OBJECT_ORIENTED.md#2-property-và-constructor) |
| **Culture** (`CultureInfo`, `InvariantCulture`) | Định dạng khu vực khi đổi số ↔ chữ; dữ liệu dùng chung giữa các máy luôn dùng `InvariantCulture` | [C01 §5](C01_FIRST_PROGRAM.md#5-chuỗi-và-định-dạng-số--bẫy-định-dạng-việt-nam) |
| **Deadlock** | Hai bên chờ nhau mãi, chương trình treo — ví dụ `.Result` trên main thread | [C05 §1](C05_ASYNC_TESTING_GIT.md#1-async-và-await-chờ-mà-không-đứng-hình) |
| **Deferred execution** (thực thi trì hoãn) | `Where`, `Select`… chỉ chạy khi có người duyệt kết quả | [C04 §4](C04_CSHARP_IN_THIS_PROJECT.md#4-linq-chạy-lúc-nào-thực-thi-trì-hoãn) |
| **Delegate** | Kiểu của một biến chứa **hàm** | [C04 §1](C04_CSHARP_IN_THIS_PROJECT.md#1-delegate-hàm-là-một-giá-trị) |
| **Dependency Injection** (DI), **singleton / transient** | Nhận phụ thuộc qua constructor; container tạo object một lần duy nhất / mỗi lần cần | [C03 §8](C03_OBJECT_ORIENTED.md#8-nhận-phụ-thuộc-qua-constructor), [T02 §3](T02_ARCHITECTURE_DEEP_DIVE.md#3-dependency-injection) |
| **`Dictionary<TKey, TValue>`** | Tập cặp khóa → giá trị, tra nhanh theo khóa | [C02 §4](C02_DATA_AND_COLLECTIONS.md#4-dictionary-tra-theo-khóa) |
| **Encapsulation** (đóng gói) | Dữ liệu và luật của nó nằm cùng một class | [C03 §3](C03_OBJECT_ORIENTED.md#3-đóng-gói-đối-tượng-tự-giữ-luật-của-mình) |
| **`enum`** | Tập lựa chọn có tên (`GridLabelStyle`, `OpenMode`) | [C02 §7](C02_DATA_AND_COLLECTIONS.md#7-enum-tập-lựa-chọn-có-tên) |
| **Event** (`+=` / `-=`) | Cơ chế báo tin cho những ai đăng ký; gỡ bằng đúng delegate đã đăng ký | [C04 §9](C04_CSHARP_IN_THIS_PROJECT.md#9-event-báo-tin-cho-người-đăng-ký) |
| **Exception** (`throw`, `try`/`catch`/`finally`) | Báo lỗi và bắt lỗi | [C04 §7](C04_CSHARP_IN_THIS_PROJECT.md#7-exception-báo-lỗi-đúng-cách) |
| **Extension method** | Hàm `static` có `this` ở tham số đầu, gọi như hàm của kiểu đó: `scope.DrawLine(…)` | [C04 §5](C04_CSHARP_IN_THIS_PROJECT.md#5-extension-method-thêm-hàm-cho-kiểu-có-sẵn) |
| **Field / Property** | Biến bên trong object / "mặt tiền" dữ liệu của object (`{ get; }`) | [C03 §2](C03_OBJECT_ORIENTED.md#2-property-và-constructor) |
| **Generic** (`<T>`, `where`) | Code viết một lần cho nhiều kiểu; `where` giới hạn kiểu được dùng | [C04 §6](C04_CSHARP_IN_THIS_PROJECT.md#6-generic-một-code-cho-nhiều-kiểu) |
| **Immutable** (bất biến) | Tạo xong không đổi được nữa (`GridSpec`, `Point3d`, `string`) | [C03 §3](C03_OBJECT_ORIENTED.md#3-đóng-gói-đối-tượng-tự-giữ-luật-của-mình) |
| **Inheritance** (kế thừa), **`virtual` / `override` / `sealed`** | Class con nhận lại và viết đè hành vi của class cha | [C03 §7](C03_OBJECT_ORIENTED.md#7-kế-thừa-abstract-virtual-override) |
| **Interface** | Hợp đồng "phải có những gì"; tên bắt đầu bằng `I` | [C03 §6](C03_OBJECT_ORIENTED.md#6-interface-hợp-đồng-và-bản-giả) |
| **Lambda** (`=>`) | Hàm ngắn viết tại chỗ, không tên: `x => x > 0` | [C04 §2](C04_CSHARP_IN_THIS_PROJECT.md#2-lambda-và-closure) |
| **LangVersion** | Phiên bản ngôn ngữ C#; dự án dùng **C# 10** | [C04](C04_CSHARP_IN_THIS_PROJECT.md) |
| **LINQ** | Bộ hàm hỏi đáp trên danh sách: `Where`, `Select`, `ToList`, `FirstOrDefault`… | [C04 §3](C04_CSHARP_IN_THIS_PROJECT.md#3-linq-hỏi-đáp-trên-danh-sách) |
| **`List<T>` / `IReadOnlyList<T>` / `IEnumerable<T>`** | Danh sách sửa được / chỉ đọc, có `Count` và `[i]` / chỉ duyệt được | [C02 §3](C02_DATA_AND_COLLECTIONS.md#3-list-danh-sách-co-giãn), [C02 §5](C02_DATA_AND_COLLECTIONS.md#5-trả-về-danh-sách-chỉ-đọc-ireadonlylist) |
| **Main thread / ThreadPool** | Thread chính của AutoCAD — nơi duy nhất được chạm API / bể thread nền | [C05 §1](C05_ASYNC_TESTING_GIT.md#1-async-và-await-chờ-mà-không-đứng-hình) |
| **Method** (hàm), **parameter** (tham số), **return value** | Khối lệnh có tên, nhận tham số, trả kết quả | [C01 §8](C01_FIRST_PROGRAM.md#8-hàm-method) |
| **Namespace / `using`** | Nhóm tên của các class / cho phép gọi tắt tên | [C03 §5](C03_OBJECT_ORIENTED.md#5-namespace-using-và-phạm-vi-truy-cập) |
| **`nameof`** | Lấy tên biến, hàm thành chuỗi — đổi tên không bị sót | [C04 §11](C04_CSHARP_IN_THIS_PROJECT.md#11-cú-pháp-nhỏ-hay-gặp-is-nameof-tuple) |
| **NuGet package** | Thư viện cài vào project bằng `dotnet add package` | [C05 §2](C05_ASYNC_TESTING_GIT.md#2-unit-test-với-xunit) |
| **`null`, `?.`, `??`, `NullReferenceException`** | "Không có đối tượng", các toán tử xử lý nó, và lỗi khi quên xử lý | [C02 §6](C02_DATA_AND_COLLECTIONS.md#6-null-giá-trị-không-có) |
| **Nullable** (`string?`, `short?`) | Ghi chú hoặc kiểu cho phép null; dự án đặt `<Nullable>annotations</Nullable>` | [C02 §6](C02_DATA_AND_COLLECTIONS.md#6-null-giá-trị-không-có) |
| **`partial` / Source generator** | Class chia nhiều phần / công cụ sinh code lúc build (`[ObservableProperty]`) | [C04 §10](C04_CSHARP_IN_THIS_PROJECT.md#10-attribute-nhãn-cho-công-cụ-đọc) |
| **Pattern matching** (`is`) | Kiểm tra kiểu và lấy giá trị cùng lúc | [C04 §11](C04_CSHARP_IN_THIS_PROJECT.md#11-cú-pháp-nhỏ-hay-gặp-is-nameof-tuple) |
| **Solution / Project** (`.sln`/`.slnx`, `.csproj`) | Tập hợp các project / một đơn vị build ra DLL hoặc EXE | [C05 §3](C05_ASYNC_TESTING_GIT.md#3-đọc-solution-của-dự-án) |
| **Stack trace** | Dấu vết chuỗi hàm đang gọi nhau tại lúc lỗi xảy ra | [C04 §7](C04_CSHARP_IN_THIS_PROJECT.md#7-exception-báo-lỗi-đúng-cách) |
| **`static`** | Thuộc về class, không cần object | [C03 §4](C03_OBJECT_ORIENTED.md#4-static-hàm-không-cần-đối-tượng) |
| **String interpolation** (`$"…{x}…"`) | Ghép giá trị vào chuỗi | [C01 §5](C01_FIRST_PROGRAM.md#5-chuỗi-và-định-dạng-số--bẫy-định-dạng-việt-nam) |
| **`struct` / `class`** (value type / reference type) | Kiểu giá trị — sao chép khi gán / kiểu tham chiếu — dùng chung một đối tượng | [C02 §1](C02_DATA_AND_COLLECTIONS.md#1-giá-trị-và-tham-chiếu-struct-và-class) |
| **Target framework** (`net48`, `net8.0-windows`) | Nền .NET mà project build ra | [C05 §3](C05_ASYNC_TESTING_GIT.md#3-đọc-solution-của-dự-án) |
| **Thread** | Một luồng chạy của chương trình | [C05 §1](C05_ASYNC_TESTING_GIT.md#1-async-và-await-chờ-mà-không-đứng-hình) |
| **Tuple** | Gói vài giá trị nhỏ: `(row, column)` | [C04 §11](C04_CSHARP_IN_THIS_PROJECT.md#11-cú-pháp-nhỏ-hay-gặp-is-nameof-tuple) |
| **Unit test** (`[Fact]`, `[Theory]`, Arrange–Act–Assert) | Code kiểm code, chạy tự động mỗi lần build | [C05 §2](C05_ASYNC_TESTING_GIT.md#2-unit-test-với-xunit) |
| **`using` / `IDisposable`** | Khối bảo đảm `Dispose()` luôn được gọi, kể cả khi lỗi | [C04 §8](C04_CSHARP_IN_THIS_PROJECT.md#8-using-và-idisposable-dọn-dẹp-chắc-chắn) |
| **`var`** | Để trình biên dịch tự suy ra kiểu từ vế phải | [C01 §4](C01_FIRST_PROGRAM.md#4-biến-và-kiểu-dữ-liệu) |
| **Variable** (biến), **type** (kiểu) | Hộp có tên và kiểu, chứa một giá trị | [C01 §4](C01_FIRST_PROGRAM.md#4-biến-và-kiểu-dữ-liệu) |

---

## B. AutoCAD .NET & dự án

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
| **CAD001 / CAD002 / CAD003** | Analyzer của dự án: không `MdiActiveDocument` / không prompt trong Service-ViewModel; không ghi trong `ExecuteRead` | [T02 §11](T02_ARCHITECTURE_DEEP_DIVE.md#11-analyzer-cad001cad003) |
| **CadResult** | Kiểu kết quả của framework: thành công + giá trị, hoặc thất bại + thông điệp | [T02 §4.7](T02_ARCHITECTURE_DEEP_DIVE.md#47-cadresult--cadresultt) |
| **COLORTHEME** | Sysvar theme giao diện AutoCAD: 0 = tối, 1 = sáng (không có sysvar `DARKMODE`) | [T02 §10](T02_ARCHITECTURE_DEEP_DIVE.md#10-thememanager-styles-và-hệ-màu) |
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
| **HeadlessLab** | DLL chỉ dành cho kiểm thử, link mã không-UI của framework, chạy kiểm thử trên AutoCAD thật trong accoreconsole | [T05 §9](T05_TESTING_STRATEGY.md#9-integration-test-headless-hiện-trạng-và-hướng-đi) |
| **Implied selection** (pick-first) | Đối tượng đã chọn trước khi gõ lệnh (`UsePickSet` + `SelectImplied`) | [T01 §7.4](T01_AUTOCAD_NET_FOUNDATIONS.md#74-selection-và-selectionfilter) |
| **Incident report** | Báo cáo sự cố theo mẫu | [T04 §8](T04_DEBUGGING_TROUBLESHOOTING.md#8-mẫu-báo-cáo-sự-cố-incident-report) |
| **Jig** | Tương tác kéo-thả xem trước (`EntityJig`, `DrawJig`) | [T02 §6.2](T02_ARCHITECTURE_DEEP_DIVE.md#62-nhóm-chức-năng), [U03 §9.1](U03_DRAWING_ON_UI.md#91-jig-hình-chạy-theo-chuột) |
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
| **Token màu** (design token) | Key màu dùng chung (`BackgroundBrush`, `AccentBrush`…) định nghĩa ở cả hai file theme; XAML chỉ tham chiếu key, không viết mã màu | [T02 §10](T02_ARCHITECTURE_DEEP_DIVE.md#10-thememanager-styles-và-hệ-màu) |
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

---

## C. Giao diện: WPF & UI trong AutoCAD

| Thuật ngữ | Nghĩa | Xem |
| :--- | :--- | :--- |
| **`AffectsRender`** | Cờ của một DependencyProperty: giá trị đổi thì WPF tự vẽ lại control | [U03 §4](U03_DRAWING_ON_UI.md#4-bước-3--control-cadpreview) |
| **Attached property** (`Grid.Column`) | Property mà phần tử cha "gắn" thêm cho phần tử con | [U01 §2](U01_WPF_MVVM_BASICS.md#2-bố-cục-grid-và-stackpanel) |
| **Binding** (`{Binding X}`) | Sợi dây tự động nối một property của View với property `X` của ViewModel | [U01 §4](U01_WPF_MVVM_BASICS.md#4-binding-nối-view-với-viewmodel) |
| **Code-behind** | File `.xaml.cs` đi cặp với XAML; trong dự án chỉ làm hạ tầng | [U01 §11](U01_WPF_MVVM_BASICS.md#11-code-behind-chỉ-làm-hạ-tầng) |
| **Command** (`ICommand`, `RelayCommand`, `CanExecute`) | Hành động mà nút bấm gọi; `CanExecute` quyết định nút sáng hay mờ | [U01 §6](U01_WPF_MVVM_BASICS.md#6-mvvm-với-communitytoolkitmvvm) |
| **Converter** (`IValueConverter`) | Đổi kiểu dữ liệu giữa View và ViewModel, ví dụ `bool` → `Visibility` | [U01 §4](U01_WPF_MVVM_BASICS.md#4-binding-nối-view-với-viewmodel) |
| **DataContext** | Đối tượng mà mọi `{Binding}` bên trong đọc dữ liệu từ đó; phần tử con thừa hưởng | [U01 §4](U01_WPF_MVVM_BASICS.md#4-binding-nối-view-với-viewmodel) |
| **DataTrigger** | Đổi giao diện theo giá trị dữ liệu (màu theo trạng thái) | [U01 §4](U01_WPF_MVVM_BASICS.md#4-binding-nối-view-với-viewmodel) |
| **DependencyProperty** | Loại property của WPF hỗ trợ binding, style và `DynamicResource` | [U03 §4](U03_DRAWING_ON_UI.md#4-bước-3--control-cadpreview) |
| **DIP** (pixel độc lập thiết bị) | Đơn vị đo của WPF, bằng 1/96 inch, tự co giãn theo DPI | [U01 §2](U01_WPF_MVVM_BASICS.md#2-bố-cục-grid-và-stackpanel) |
| **Dispatcher / UI thread** | Luồng sở hữu các control WPF; trong AutoCAD đó là main thread | [U01 §8](U01_WPF_MVVM_BASICS.md#8-luồng-thread) |
| **`DynamicResource` / `StaticResource`** | Tham chiếu resource: tìm lúc chạy và theo dõi thay đổi / tìm một lần lúc nạp XAML | [U01 §9](U01_WPF_MVVM_BASICS.md#9-resource-style-và-theme) |
| **Freeze** (`Freezable`) | "Đóng băng" brush, pen, geometry của WPF: không đổi được nữa, vẽ nhanh hơn | [U03 §4](U03_DRAWING_ON_UI.md#4-bước-3--control-cadpreview) |
| **`Language`** (của phần tử WPF) | Ngôn ngữ dùng để đổi chuỗi ↔ số; mặc định `en-US`, **không** theo Windows | [U01 §5](U01_WPF_MVVM_BASICS.md#5-ba-bẫy-khi-nhập-số) |
| **Modal / modeless** | Hộp thoại chặn AutoCAD tới khi đóng / cửa sổ không chặn | [U02 §4](U02_UI_IN_AUTOCAD.md#4-hộp-thoại-modal), [§5](U02_UI_IN_AUTOCAD.md#5-cửa-sổ-modeless) |
| **MVVM** (Model–View–ViewModel) | Chia màn hình thành View (XAML), ViewModel (dữ liệu và hành động), Model | [U01 §1](U01_WPF_MVVM_BASICS.md#1-wpf-trong-5-phút) |
| **`ObservableCollection<T>`** | Danh sách báo thay đổi (thêm/xóa) cho giao diện | [U01 §7](U01_WPF_MVVM_BASICS.md#7-danh-sách-observablecollection) |
| **`[ObservableProperty]`** | Attribute của CommunityToolkit.Mvvm: từ field `_rows` sinh property `Rows` biết báo thay đổi | [U01 §6](U01_WPF_MVVM_BASICS.md#6-mvvm-với-communitytoolkitmvvm) |
| **`OnRender` / `DrawingContext`** | Hàm mà control tự vẽ nội dung của mình / "cây bút" để vẽ | [U03 §4](U03_DRAWING_ON_UI.md#4-bước-3--control-cadpreview) |
| **Preview** (xem trước) | Vẽ kết quả lên UI hoặc lên màn hình trước khi chốt vào bản vẽ | [U03](U03_DRAWING_ON_UI.md) |
| **Prompt, `PromptStatus`, keyword** | Câu hỏi trên dòng lệnh; kết quả (`OK`, `Cancel`, `None`…); từ khóa chọn được | [U02 §2](U02_UI_IN_AUTOCAD.md#2-dòng-lệnh-bề-mặt-ui-dùng-nhiều-nhất) |
| **`SetImpliedSelection`** | Chọn sẵn đối tượng cho người dùng (hiện grip) | [U02 §7.3](U02_UI_IN_AUTOCAD.md#73-chọn-sẵn-và-zoom-tới-entity) |
| **Transient graphics** (`TransientManager`) | Hình vẽ tạm lên màn hình bản vẽ, không vào database | [U03 §9.2](U03_DRAWING_ON_UI.md#92-transient-hình-tạm-đứng-yên) |
| **`UpdateSourceTrigger`** | Lúc binding đẩy giá trị về ViewModel; `TextBox` mặc định là khi mất focus | [U01 §4](U01_WPF_MVVM_BASICS.md#4-binding-nối-view-với-viewmodel) |
| **Viewport transform** ("zoom extents") | Biến đổi tọa độ bản vẽ → pixel cho hình vừa khung: một scale chung, lật trục Y | [U03 §3](U03_DRAWING_ON_UI.md#3-bước-2--biến-đổi-tọa-độ) |
| **XAML** | Ngôn ngữ đánh dấu mô tả cây đối tượng giao diện của WPF | [U01 §1](U01_WPF_MVVM_BASICS.md#1-wpf-trong-5-phút) |
