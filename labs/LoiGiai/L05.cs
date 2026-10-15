// Lời giải mẫu L05 phần 1. Làm bài trong ../L05/DoorSelector.cs trước, rồi mới đọc file này.
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using CadCoreFramework.Core;
using CadCoreFramework.Utils;

namespace CadCoreFramework.Labs.L05
{
    public static class DoorSelector
    {
        public static ObjectId[] SelectByEffectiveName(ICadScope scope, string blockName)
        {
            // Bước 1, lọc thô: block tên blockName, hoặc block ẩn danh *U… (dấu ` biến * đầu tiên thành chữ).
            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.BlockName, blockName + ",`*U*"),
            });
            PromptSelectionResult result = scope.Editor.SelectAll(filter);
            if (result.Status != PromptStatus.OK) return new ObjectId[0];

            // Bước 2, lọc lại: *U… cũng có thể là dynamic block khác, chỉ giữ cái có tên thật đúng.
            return result.Value.GetObjectIds()
                .Where(id => DynamicBlockHelper.GetEffectiveName(
                    scope.Transaction, (BlockReference)scope.Transaction.GetObject(id, OpenMode.ForRead)) == blockName)
                .ToArray();
        }
    }
}
