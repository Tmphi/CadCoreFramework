using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;
using CadCoreFramework.Services;
using CadCoreFramework.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.Commands.Demos
{
    /// <summary>
    /// Các lệnh minh họa lưu trữ và truy xuất siêu dữ liệu (Metadata):
    /// XData (giới hạn 16KB), XRecord (Extension Dictionary - không giới hạn),
    /// và thao tác với Dynamic Block (Block động).
    /// </summary>
    public class DemoDataCommands : CadCommandBase
    {
        private INotificationService Notify
            => AppBootstrapper.ServiceProvider?.GetService<INotificationService>();

        /// <summary>
        /// DEMO_XDATA — Minh họa cách ghi/đọc metadata JSON vào XData của Entity.
        /// XData giới hạn 16KB nhưng rất nhanh và dễ truy vấn qua SelectionFilter.
        /// </summary>
        [CommandMethod("DEMO_XDATA")]
        public void DemoXData()
        {
            Notify?.Section("DEMO: XDATA — GHI/ĐỌC METADATA JSON (GIỚI HẠN 16KB)");

            ExecuteWithScope(scope =>
            {
                // 1. Vẽ một hình chữ nhật làm đối tượng thử nghiệm
                var p1 = new Point3d(0, 0, 0);
                var p2 = new Point3d(80, 40, 0);
                ObjectId rectId = scope.DrawRectangle(p1, p2, layer: "DEMO_XDATA", colorIndex: 6);
                Notify?.Info($"1. Đã vẽ hình chữ nhật demo | ID: {rectId}");

                // 2. Mở entity để ghi XData
                var entity = scope.Transaction.GetObject(rectId, OpenMode.ForWrite) as Entity;
                if (entity == null) { Notify?.Error("Không thể mở entity."); return; }

                // 3. Tạo dữ liệu metadata mẫu
                var deviceInfo = new
                {
                    MaThietBi = "MCB-1P-16A",
                    TenThietBi = "Aptomat 1 pha 16A",
                    HangSanXuat = "Schneider Electric",
                    CongSuat = 3.5,
                    DonGia = 125000,
                    NgayLapDat = DateTime.Now.ToString("yyyy-MM-dd")
                };

                // 4. Ghi metadata vào XData bằng extension method
                const string appName = "DEMO_METADATA";
                entity.SetMetadata(scope.Transaction, appName, deviceInfo);
                Notify?.Info($"2. Đã ghi metadata XData (AppName: '{appName}').");

                // 5. Đọc lại metadata từ XData
                var readBack = entity.GetMetadata<dynamic>(appName);
                if (readBack != null)
                {
                    Notify?.Info($"3. Đọc lại thành công:");
                    Notify?.Info($"   Mã TB: {readBack.MaThietBi}");
                    Notify?.Info($"   Tên:   {readBack.TenThietBi}");
                    Notify?.Info($"   Hãng:  {readBack.HangSanXuat}");
                    Notify?.Info($"   Giá:   {readBack.DonGia:N0} VNĐ");
                }

                // 6. Kiểm tra HasMetadata
                bool hasData = entity.HasMetadata(appName);
                Notify?.Info($"4. HasMetadata('{appName}'): {hasData}");
            });

            Notify?.Section("HOÀN TẤT: DEMO XDATA — GHI/ĐỌC METADATA JSON");
        }

        /// <summary>
        /// DEMO_XRECORD — Minh họa cách ghi/đọc metadata JSON vào XRecord (Extension Dictionary).
        /// Không giới hạn 16KB — lý tưởng cho dữ liệu lớn (cấu hình tủ điện, BOM phức tạp...).
        /// </summary>
        [CommandMethod("DEMO_XRECORD")]
        public void DemoXRecord()
        {
            Notify?.Section("DEMO: XRECORD — METADATA LỚN KHÔNG GIỚI HẠN 16KB");

            ExecuteWithScope(scope =>
            {
                // 1. Vẽ hình tròn làm đối tượng thử nghiệm
                ObjectId circleId = scope.DrawCircle(new Point3d(200, 0, 0), radius: 25, layer: "DEMO_XRECORD", colorIndex: 4);
                Notify?.Info($"1. Đã vẽ hình tròn demo | ID: {circleId}");

                // 2. Mở entity
                var entity = scope.Transaction.GetObject(circleId, OpenMode.ForWrite) as DBObject;
                if (entity == null) { Notify?.Error("Không thể mở entity."); return; }

                // 3. Tạo dữ liệu lớn (mô phỏng cấu hình tủ điện)
                var panelConfig = new
                {
                    TenTuDien = "Tủ điện chính MDB-01",
                    LoaiTu = "Main Distribution Board",
                    DongDinhMuc = "630A",
                    SoNgach = 24,
                    DanhSachThietBi = new[]
                    {
                        new { Stt = 1, Ten = "MCCB 3P 630A", Hang = "Schneider", SoLuong = 1 },
                        new { Stt = 2, Ten = "MCCB 3P 250A", Hang = "Schneider", SoLuong = 2 },
                        new { Stt = 3, Ten = "MCB 3P 63A",   Hang = "ABB",       SoLuong = 6 },
                        new { Stt = 4, Ten = "MCB 1P 16A",   Hang = "ABB",       SoLuong = 12 },
                        new { Stt = 5, Ten = "SPD Type 2",   Hang = "ABB",       SoLuong = 1 }
                    },
                    GhiChu = "Tủ điện lắp đặt tầng 1, khu A. Thiết kế theo TCVN 7447."
                };

                // 4. Ghi vào XRecord (Extension Dictionary)
                const string xrecKey = "PANEL_CONFIG";
                XRecordHelper.SetMetadata(scope.Transaction, entity, xrecKey, panelConfig);
                Notify?.Info($"2. Đã ghi XRecord (Key: '{xrecKey}') — dữ liệu phức tạp với 5 dòng thiết bị.");

                // 5. Đọc lại từ XRecord
                var readBack = XRecordHelper.GetMetadata<dynamic>(scope.Transaction, entity, xrecKey);
                if (readBack != null)
                {
                    Notify?.Info($"3. Đọc lại thành công:");
                    Notify?.Info($"   Tên tủ: {readBack.TenTuDien}");
                    Notify?.Info($"   Dòng định mức: {readBack.DongDinhMuc}");
                    Notify?.Info($"   Số ngạch: {readBack.SoNgach}");
                    Notify?.Info($"   Ghi chú: {readBack.GhiChu}");
                }

                // 6. So sánh XData vs XRecord
                Notify?.Info("─── SO SÁNH XDATA vs XRECORD ───");
                Notify?.Info("  XData:   Giới hạn 16KB tổng cộng | Tìm kiếm nhanh qua SelectionFilter");
                Notify?.Info("  XRecord: Không giới hạn kích thước | Lưu trong Extension Dictionary");
                Notify?.Info("  → Khuyên dùng XRecord cho dữ liệu lớn (BOM, cấu hình tủ điện).");

                // 7. Demo RemoveMetadata
                XRecordHelper.RemoveMetadata(scope.Transaction, entity, xrecKey);
                var afterRemove = XRecordHelper.GetMetadata<dynamic>(scope.Transaction, entity, xrecKey);
                Notify?.Info($"4. RemoveMetadata → Đọc lại: {(afterRemove == null ? "null (đã xóa thành công)" : "vẫn còn!?")}");
            });

            Notify?.Section("HOÀN TẤT: DEMO XRECORD");
        }

        /// <summary>
        /// DEMO_DYNAMIC_BLOCK — Minh họa cách chọn một BlockReference và đọc các thuộc tính động
        /// (Dynamic Properties) hoặc danh sách trạng thái hiển thị (Visibility States) qua DynamicBlockHelper.
        /// </summary>
        [CommandMethod("DEMO_DYNAMIC_BLOCK")]
        public void DemoDynamicBlock()
        {
            Notify?.Section("DEMO: DYNAMICBLOCKHELPER — THAO TÁC BLOCK ĐỘNG");

            ExecuteCommand(() =>
            {
                var blockId = CadPromptHelper.GetEntity("Chọn một Block (BlockReference) trên bản vẽ:", typeof(BlockReference));
                if (blockId == ObjectId.Null)
                {
                    Notify?.Warn("Chưa chọn Block hợp lệ.");
                    return;
                }

                CadContext?.ExecuteRead(scope =>
                {
                    var blockRef = scope.Transaction.GetObject(blockId, OpenMode.ForRead) as BlockReference;
                    if (blockRef == null) return;

                    // Lấy tên hiệu dụng (Effective Name)
                    string effectiveName = DynamicBlockHelper.GetEffectiveName(scope.Transaction, blockRef);
                    Notify?.Info($"Block Name: '{blockRef.Name}' | Effective Name: '{effectiveName}'");
                    Notify?.Info($"Là Dynamic Block: {blockRef.IsDynamicBlock}");

                    if (blockRef.IsDynamicBlock)
                    {
                        // 1. Lấy tất cả thuộc tính động
                        var props = DynamicBlockHelper.GetDynamicProperties(blockRef);
                        Notify?.Info($"Số lượng Dynamic Properties: {props.Count}");
                        foreach (var kvp in props)
                        {
                            Notify?.Info($"  - {kvp.Key} = {kvp.Value}");
                        }

                        // 2. Lấy danh sách Visibility States
                        var visStates = DynamicBlockHelper.GetVisibilityStates(blockRef);
                        if (visStates.Count > 0)
                        {
                            Notify?.Info($"Danh sách Visibility States ({visStates.Count}): {string.Join(", ", visStates)}");
                            string currentVis = DynamicBlockHelper.GetVisibilityState(blockRef);
                            Notify?.Info($"Trạng thái hiện hành: '{currentVis}'");
                        }
                    }
                    else
                    {
                        Notify?.Info("Đối tượng là Standard Block (không phải Dynamic Block).");
                    }
                });
            });

            Notify?.Section("HOÀN TẤT: DEMO DYNAMIC BLOCK");
        }
    }
}
