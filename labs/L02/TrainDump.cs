// L02 — Đọc database · docs/training/T10_LABS_AND_ASSESSMENT.md (L02), lý thuyết ở T01 §4–§6.
// Lệnh TRAIN_DUMP (Commands/TrainCommands.cs) gọi ba hàm dưới đây trong MỘT ICadContext.ExecuteRead.
//   1. ListLayers: mọi layer của bản vẽ — tên, tắt hay bật (IsOff), số màu (Color.ColorIndex).
//   2. CountByDxfName: đếm entity trong scope.CurrentSpace theo id.ObjectClass.DxfName — KHÔNG GetObject entity.
//   3. CountByEffectiveName: đếm block reference trong scope.CurrentSpace theo tên THẬT (DynamicBlockHelper.GetEffectiveName),
//      để dynamic block đã chỉnh (tên ẩn danh *U…) vẫn được đếm vào tên gốc.
// Không mở gì ForWrite. Bộ chấm kiểm bằng danh sách đối tượng mà transaction đã mở.
// Chấm: .\labs\run-lab.ps1 -Lab L02
using System;
using System.Collections.Generic;
using CadCoreFramework.Core;

namespace CadCoreFramework.Labs.L02
{
    public sealed class LayerInfo
    {
        public LayerInfo(string name, bool isOff, short colorIndex)
        {
            Name = name;
            IsOff = isOff;
            ColorIndex = colorIndex;
        }

        public string Name { get; }
        public bool IsOff { get; }
        public short ColorIndex { get; }
    }

    public static class TrainDump
    {
        // TODO: viết bài làm ở đây, thay cho throw.
        public static List<LayerInfo> ListLayers(ICadScope scope) => throw new NotImplementedException();

        // TODO: viết bài làm ở đây, thay cho throw. Khóa là DxfName ("LINE", "INSERT"…), giá trị là số lượng.
        public static Dictionary<string, int> CountByDxfName(ICadScope scope) => throw new NotImplementedException();

        // TODO: viết bài làm ở đây, thay cho throw. Khóa là tên block thật ("DOOR"…), giá trị là số lượng.
        public static Dictionary<string, int> CountByEffectiveName(ICadScope scope) => throw new NotImplementedException();
    }
}
