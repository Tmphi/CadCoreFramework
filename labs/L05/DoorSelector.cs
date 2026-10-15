// L05 phần 1 — Selection filter & dynamic block · docs/training/T10_LABS_AND_ASSESSMENT.md (L05).
// Chọn MỌI block reference có tên thật là blockName, kể cả dynamic block đã chỉnh (AutoCAD đổi tên chúng
// thành *U…). Cách làm của đề bài:
//   1. lọc thô bằng SelectionFilter: tên block "blockName,`*U*" (dấu ` để * đầu tiên là chữ, không phải wildcard);
//   2. lọc lại từng kết quả bằng DynamicBlockHelper.GetEffectiveName, vì *U… cũng có thể là block khác (WINDOW…).
// Lệnh TRAIN_DOORS (Commands/TrainCommands.cs) gọi hàm này với "DOOR" trên bản vẽ mẫu LAB_SAMPLE.
// Phần 2 của L05 (B-17) sửa thẳng Utils/SelectionFilterBuilder.cs trên branch tạo từ v1.0.0, không làm ở đây.
// Chấm: .\labs\run-lab.ps1 -Lab L05
using System;
using Autodesk.AutoCAD.DatabaseServices;
using CadCoreFramework.Core;

namespace CadCoreFramework.Labs.L05
{
    public static class DoorSelector
    {
        // TODO: viết bài làm ở đây, thay cho throw.
        public static ObjectId[] SelectByEffectiveName(ICadScope scope, string blockName) => throw new NotImplementedException();
    }
}
