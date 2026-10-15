# 🧪 Lab chấm tự động trên AutoCAD (L02–L06)

Bộ chấm cho các lab [L02–L06 của T10](../docs/training/T10_LABS_AND_ASSESSMENT.md#2-danh-sách-bài-lab). Làm xong một lab, chạy một lệnh: bài làm được nạp vào **AutoCAD thật** (accoreconsole, không mở giao diện), chạy trên một bản vẽ mẫu, rồi in từng kiểm tra đạt hay chưa, kèm lý do.

Cần: AutoCAD 2021 trở lên, bản đầy đủ (có `accoreconsole.exe`; AutoCAD LT không có), .NET SDK 8 trở lên. Runner chạy accoreconsole với profile riêng trong `%TEMP%`, **không** đổi thiết lập AutoCAD của bạn ([T10 quy tắc 5](../docs/training/T10_LABS_AND_ASSESSMENT.md#1-quy-tắc-làm-lab)).

## Cách làm

1. Đọc đề trong T10. Đề tóm tắt nằm ở đầu file bài làm.
2. Mở file bài làm và thay các dòng `throw new NotImplementedException();` bằng bài làm của bạn:

   | Lab | File bài làm | Bộ chấm kiểm gì |
   | :--- | :--- | :--- |
   | L02 | [`L02/TrainDump.cs`](L02/TrainDump.cs) | Danh sách layer (tắt/bật, màu); đếm theo `DxfName` mà **không mở entity nào**; đếm theo tên thật, kể cả dynamic block mang tên `*U…`; không mở gì `ForWrite`; ở Layout1 thì đếm đúng paper space |
   | L03 | [`L03/Experiments.cs`](L03/Experiments.cs) | Mỗi thí nghiệm tái hiện **đúng** hiện tượng đề bài nói tới, và in ra điều quan sát được để bạn chép vào bảng |
   | L04 | [`L04/Line2P.cs`](L04/Line2P.cs) | UCS xoay 30°, runner gõ hai điểm: Line phải nằm đúng chỗ (WCS); không prompt khi đang mở transaction; bản thô lệch đúng 30° |
   | L05 phần 1 | [`L05/DoorSelector.cs`](L05/DoorSelector.cs) | Chọn đúng mọi DOOR (3 cái, 2 cái mang tên `*U…`), không chọn nhầm WINDOW; cùng hàm đó với WINDOW và `LAB_COT` |
   | L05 phần 2, L06 | Code framework (`Utils/SelectionFilterBuilder.cs`, `Utils/XRecordHelper.cs`) trên branch từ `v1.0.0` | `OnLayersExact` khớp nguyên văn `LAB#1`, `M.EQUIP`, `LAB@X`, `LAB[1]`; `RemoveMetadata` Erase Xrecord (B-17, B-09) |

3. Chấm (từ thư mục gốc của repo, trong PowerShell):

   ```powershell
   .\labs\run-lab.ps1 -Lab L02
   ```

   `-Lab All` chấm cả năm lab. L03 chấm từng thí nghiệm được: `-Lab L03 -Only TN2`.

4. Đọc từng dòng `[FAIL]`, sửa, chạy lại. Log đầy đủ của mỗi phiên AutoCAD nằm ở `labs\out\`.

> [!TIP]
> `[FAIL] … chưa làm` nghĩa là hàm còn `throw NotImplementedException`, không phải bạn làm sai.

Bộ chấm chỉ kiểm phần **máy kiểm được**. DoD còn những phần sau, mentor chấm:

| Lab | Phần không chấm tự động |
| :--- | :--- |
| L02 | Giải thích vì sao bước 2 không cần mở entity; câu phản tư |
| L03 | Bảng "thí nghiệm → quan sát → giải thích" và phần dẫn tới T01/T02 |
| L04 | Đường cao su (rubber band) và phím ESC ở cả hai prompt. Thử bằng tay, xem [mục dưới](#thử-tay-trong-autocad) |
| L05 | Unit test cho `SelectionFilterBuilder` (chạy không cần AutoCAD); thử tay với layer `LAB#1` |
| L06 | PR, mô tả cách kiểm chứng, CHANGELOG; câu phản tư |

## L03: hai thí nghiệm làm AutoCAD dừng hẳn

TN2 (sửa đối tượng mở `ForRead`) và TN3 (dùng transaction đã Dispose) **không** ném exception. AutoCAD dừng ngay với `INTERNAL ERROR: !dbobji.cpp@…: eNotOpenForWrite` hoặc `FATAL ERROR: Unhandled Access Violation`, và `try/catch` không bắt được. Vì vậy runner chạy mỗi thí nghiệm trong một phiên accoreconsole riêng, rồi đọc thông báo đó trong output. Bản vẽ đang làm của bạn không bị ảnh hưởng.

Khi AutoCAD crash, nó mở hộp thoại **Error Report** để hỏi có gửi báo cáo cho Autodesk không. Runner tự đóng đúng hộp thoại do phiên của nó mở ra, nên không có báo cáo nào được gửi.

**Đừng** chạy `LAB_GRADE_L03_TN2` hay `LAB_GRADE_L03_TN3` trong AutoCAD có giao diện.

## L05 phần 2 và L06: làm trên `v1.0.0`

Hai lab này sửa lỗi có thật trong framework (B-17, B-09). Lỗi đã được sửa trên `main`, nên trên `main` bộ chấm luôn xanh. Làm trên code cũ, trong một **worktree** riêng ([T10 quy tắc 6](../docs/training/T10_LABS_AND_ASSESSMENT.md#1-quy-tắc-làm-lab)):

```powershell
git worktree add ..\cadcore-lab -b lab/L06-ten-ban v1.0.0     # thư mục thứ hai, branch từ v1.0.0
.\labs\run-lab.ps1 -Lab L06 -FrameworkRoot ..\cadcore-lab   # chạy từ repo chính: phải ĐỎ (lỗi có thật)
# sửa Utils/XRecordHelper.cs trong ..\cadcore-lab, rồi chấm lại cho tới khi xanh
```

Bộ chấm và runner nằm ở repo chính. `-FrameworkRoot` bảo nó biên dịch code framework của worktree. Đừng chép `labs/` sang branch `v1.0.0`: csproj plugin của bản đó không loại thư mục `labs\`, nên sẽ biên dịch nhầm cả lab vào plugin. Xong lab thì xoá worktree: `git worktree remove ..\cadcore-lab`.

Với L05 phần 2, bộ chấm gọi `SelectionFilterBuilder.OnLayersExact(params string[] layerNames)`. Hãy đặt đúng tên này để bộ chấm gọi được.

## Thử tay trong AutoCAD

DLL lab chỉ cần AcDbMgd và AcCoreMgd, nên nạp được trong AutoCAD có giao diện như mọi plugin:

1. `.\labs\run-lab.ps1 -SaveSample` build DLL và lưu bản vẽ mẫu ra `labs\out\lab-sample.dwg`.
2. Mở `lab-sample.dwg` trong AutoCAD, `NETLOAD` file `labs\bin\Debug\net48\CadCoreFramework.Labs.dll`.
3. Chạy `TRAIN_DUMP` (L02), `TRAIN_DOORS` (L05: in handle, tên block, vị trí từng cửa tìm được), `TRAIN_LINE2P` và `TRAIN_LINE2P_RAW` (L04: gõ `UCS` → `Z` → `30` trước).

Muốn có bản vẽ mẫu trên một bản vẽ trống thì gõ `LAB_SAMPLE`.

> [!IMPORTANT]
> AutoCAD giữ khoá file DLL đã `NETLOAD` cho tới khi đóng. Muốn build lại hay chấm lại bằng `run-lab.ps1`, hãy **đóng AutoCAD trước**, nếu không bước build báo file đang bị dùng.

## Bản vẽ mẫu

`LAB_SAMPLE` vẽ vào bản vẽ đang mở (phải trống); bộ chấm L02 và L05 cũng dựng nó trước khi chấm:

- Layer `LAB_TUONG` (đỏ), `LAB_CUA` (xanh lá), `LAB_GHICHU` (vàng, **tắt**), `LAB_THIETBI` (xanh dương), cùng layer `0`.
- Model: 4 `LINE`, 1 `LWPOLYLINE`, 3 `CIRCLE`, 2 `TEXT`, 1 `MTEXT`, 2 block thường `LAB_COT`, 3 `DOOR` (2 cái đã đổi thuộc tính động), 2 `WINDOW` (cả hai đã đổi).
- Layout1: 1 `LINE`, 1 `MTEXT` (và 1 `VIEWPORT` khi layout được mở).

Block `DOOR` và `WINDOW` là dynamic block **chép từ bộ mẫu Autodesk cài cùng AutoCAD** (`Sample\<ngôn ngữ>\Dynamic Blocks\Architectural - Metric.dwg`, block "Door - Metric" và "Window - Metric", đổi tên). accoreconsole không có Block Editor để tự dựng dynamic block, nên bản vẽ mẫu không nằm trong repo; mỗi máy tự dựng. Máy không có bộ mẫu thì bản vẽ không có `DOOR`/`WINDOW`, và các kiểm tra cần chúng báo `[SKIP]`.

## Dành cho người bảo trì

- **Chấm lời giải mẫu:** `.\labs\run-lab.ps1 -Solution`. Mọi kiểm tra phải đạt. Với code khởi đầu (không `-Solution`), mọi kiểm tra của L02–L05 phần 1 phải đỏ.
- **CI không có AutoCAD:** nó chỉ biên dịch `Labs.csproj`, cả code khởi đầu lẫn `-p:UseSolutions=true`. Phần chấm phải chạy tay, trên máy có AutoCAD, mỗi khi đổi bộ chấm, lời giải hay API framework mà lab dùng.
- **Hai shim trong [`Headless/AcMgdShims.cs`](Headless/AcMgdShims.cs)** thay hai API của AcMgd mà mã nguồn framework được link còn gọi tới: `GetAcadDocument` (gom Undo của `CadContext`) và `Application` của AcMgd (Utils trên `v1.0.0`). Chúng chỉ có hiệu lực trong DLL lab.
- **Giao thức** giữa lệnh chấm và runner ghi ở đầu [`Grader/Grade.cs`](Grader/Grade.cs). L04: hai điểm runner gõ khớp `TypedStart`/`TypedEnd` trong `GradeL04.cs`; đổi một bên thì đổi cả bên kia.
