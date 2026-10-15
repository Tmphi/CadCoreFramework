// L04 — Prompt & UCS · docs/training/T10_LABS_AND_ASSESSMENT.md (L04), lý thuyết ở T01 §7.
// Lệnh TRAIN_LINE2P và TRAIN_LINE2P_RAW (Commands/TrainCommands.cs) gọi hai hàm dưới đây.
//   Run:    hỏi 2 điểm bằng CadPromptHelper.GetPoint; prompt thứ hai có basePoint (đường cao su).
//           HỎI TRƯỚC, VẼ SAU: chỉ gọi context.Execute khi đã có đủ 2 điểm. ESC ở prompt nào cũng
//           trả ObjectId.Null, không vẽ gì.
//   RunRaw: bản để so sánh: Editor.GetPoint thô, KHÔNG đổi UCS → WCS, rồi vẽ Line như Run.
// Cả hai trả ObjectId của Line vừa vẽ.
// Bộ chấm đặt UCS xoay 30° quanh Z, gõ hai điểm "10,0" và "20,0", rồi kiểm toạ độ WCS của Line.
// Đường cao su và phím ESC chỉ thử được bằng tay trong AutoCAD (labs/README.md).
// Chấm: .\labs\run-lab.ps1 -Lab L04
using System;
using Autodesk.AutoCAD.DatabaseServices;
using CadCoreFramework.Core;

namespace CadCoreFramework.Labs.L04
{
    public static class Line2P
    {
        // TODO: viết bài làm ở đây, thay cho throw.
        public static ObjectId Run(ICadContext context) => throw new NotImplementedException();

        // TODO: viết bài làm ở đây, thay cho throw.
        public static ObjectId RunRaw(ICadContext context) => throw new NotImplementedException();
    }
}
