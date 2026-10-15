// L03 — Thí nghiệm transaction & context · docs/training/T10_LABS_AND_ASSESSMENT.md (L03), lý thuyết ở T01 §4–§5, T02 §4.
// Mỗi hàm là MỘT thí nghiệm. Viết đúng thao tác đề bài mô tả, KHÔNG try/catch: bộ chấm cần thấy lỗi thật.
// Bộ chấm chạy mỗi thí nghiệm trong một phiên accoreconsole riêng, vì TN2 và TN3 làm AutoCAD DỪNG HẲN
// (fatal error, try/catch không bắt được). Đừng chạy TN2, TN3 trong AutoCAD bạn đang làm việc.
// Chấm: .\labs\run-lab.ps1 -Lab L03        (một thí nghiệm: -Lab L03 -Only TN2)
using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using CadCoreFramework.Core;

namespace CadCoreFramework.Labs.L03
{
    public static class Experiments
    {
        // TN1: API thô (db.TransactionManager), thêm một Line vào model space, rồi KHÔNG Commit.
        // TODO: viết bài làm ở đây, thay cho throw.
        public static void Tn1_NoCommit(Database db) => throw new NotImplementedException();

        // TN2: mở Line lineId bằng OpenMode.ForRead, rồi đổi màu nó (ColorIndex = 1).
        // TODO: viết bài làm ở đây, thay cho throw.
        public static void Tn2_ModifyOpenedForRead(Database db, ObjectId lineId) => throw new NotImplementedException();

        // TN3: mở một transaction, Dispose nó (ra khỏi using), rồi gọi GetObject(lineId, …) trên CHÍNH transaction đó.
        // TODO: viết bài làm ở đây, thay cho throw.
        public static void Tn3_UseTransactionAfterDispose(Database db, ObjectId lineId) => throw new NotImplementedException();

        // TN4: bộ chấm gọi hàm này từ một lệnh CommandFlags.Session. Thêm một Line vào model space
        // của doc mà KHÔNG LockDocument.
        // TODO: viết bài làm ở đây, thay cho throw.
        public static void Tn4_WriteWithoutLock(Document doc) => throw new NotImplementedException();

        // TN5: gọi context.Execute bên trong lambda của một context.Execute khác.
        // TODO: viết bài làm ở đây, thay cho throw.
        public static void Tn5_NestedExecute(ICadContext context) => throw new NotImplementedException();

        // TN6: trong context.ExecuteRead, tự GetObject(lineId, OpenMode.ForWrite) rồi đổi màu Line (ColorIndex = 1).
        // TODO: viết bài làm ở đây, thay cho throw.
        public static void Tn6_WriteInsideExecuteRead(ICadContext context, ObjectId lineId) => throw new NotImplementedException();
    }
}
