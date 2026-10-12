using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;
using CadCoreFramework.Utils;

namespace CadCoreFramework.Samples.GridMarker
{
    /// <summary>Metadata gắn vào XData của khung mỗi ô — cho phép đọc lại / nhận diện về sau.</summary>
    public sealed class GridMarkerTag
    {
        public string Label { get; set; } = string.Empty;
        public int Row { get; set; }
        public int Column { get; set; }
    }

    /// <summary>
    /// GOLDEN SAMPLE — TẦNG SERVICE (Imperative Shell).
    ///
    /// Lớp vỏ MỎNG giữa logic thuần và AutoCAD:
    ///   • Không tính toán gì — chỉ vẽ những gì GridPlanner đã tính.
    ///   • Không prompt người dùng, không gọi MdiActiveDocument (analyzer CAD001 kiểm tra).
    ///   • Mọi thao tác bản vẽ đi qua ICadContext → tự Lock + Transaction + UndoMark.
    ///   • Dùng được từ CẢ Command (command context) lẫn Palette (application context).
    /// </summary>
    public class GridMarkerService : IGridMarkerService
    {
        /// <summary>Tên RegApp dùng cho XData của Grid Marker.</summary>
        public const string XDataAppName = "CADCORE_GRID_MARKER";

        /// <summary>Tên bước Undo — toàn bộ lưới được Ctrl+Z trong một lần.</summary>
        public const string UndoMarkName = "Grid Marker";

        private readonly ICadContext _cadContext;
        private readonly IEventAggregator _events;
        private readonly ILoggerService _logger;

        public GridMarkerService(ICadContext cadContext, IEventAggregator events, ILoggerService logger)
        {
            _cadContext = cadContext ?? throw new ArgumentNullException(nameof(cadContext));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public CadResult<int> Draw(GridSpec spec, IReadOnlyList<GridCell> cells)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (cells == null) throw new ArgumentNullException(nameof(cells));

            // Không có gì để vẽ → không mở transaction, không tạo bước Undo rỗng.
            if (cells.Count == 0) return CadResult<int>.Success(0);

            // TryExecute trả về CadResult thay vì throw: lỗi AutoCAD (layer bị khóa, bản vẽ
            // read-only…) trở thành giá trị để caller quyết định hiển thị thế nào.
            CadResult<int> result = _cadContext.TryExecute(
                scope => DrawCells(scope, spec, cells),
                undoMarkName: UndoMarkName);

            // Publish SAU KHI commit (Execute đã return) — subscriber đọc bản vẽ sẽ thấy dữ liệu mới.
            if (result.IsSuccess)
                PublishSafely(new GridDrawnMessage(result.Value, spec.LayerName));

            return result;
        }

        /// <inheritdoc/>
        public int CountMarkers()
        {
            int count = 0;

            // ExecuteRead: transaction chỉ đọc, scope.IsReadOnly = true (cấm Append).
            _cadContext.ExecuteRead(scope =>
            {
                RXClass polylineClass = RXObject.GetClass(typeof(Polyline));
                foreach (ObjectId id in scope.CurrentSpace)
                {
                    // So sánh ObjectClass TRƯỚC khi GetObject — rẻ hơn nhiều so với mở từng entity.
                    if (id.ObjectClass != polylineClass) continue;

                    var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForRead);
                    if (XDataHelper.HasMetadata(entity, XDataAppName)) count++;
                }
            });

            return count;
        }

        // ─── Private ──────────────────────────────────────────────────────────────

        // Toàn bộ code chạm vào AutoCAD được gom vào MỘT method chạy bên trong transaction.
        private static int DrawCells(ICadScope scope, GridSpec spec, IReadOnlyList<GridCell> cells)
        {
            scope.EnsureLayer(spec.LayerName, colorIndex: 4);
            double half = spec.CellSize / 2.0;

            foreach (GridCell cell in cells)
            {
                ObjectId frameId = scope.DrawRectangle(
                    new Point3d(cell.CenterX - half, cell.CenterY - half, 0),
                    new Point3d(cell.CenterX + half, cell.CenterY + half, 0),
                    layer: spec.LayerName);

                // XData cần entity đã nằm trong Database (để đăng ký RegApp) → gắn SAU khi append.
                var frame = (Entity)scope.Transaction.GetObject(frameId, OpenMode.ForWrite);
                XDataHelper.SetMetadata(scope.Transaction, frame, XDataAppName,
                    new GridMarkerTag { Label = cell.Label, Row = cell.Row, Column = cell.Column });

                scope.DrawText(
                    cell.Label,
                    new Point3d(cell.CenterX, cell.CenterY, 0),
                    height: spec.TextHeight,
                    layer: spec.LayerName,
                    justification: TextHorizontalMode.TextCenter,
                    configure: t => t.VerticalMode = TextVerticalMode.TextVerticalMid);
            }

            return cells.Count;
        }

        private void PublishSafely(GridDrawnMessage message)
        {
            try
            {
                _events.Publish(message);
            }
            catch (System.Exception ex)
            {
                // Bản vẽ ĐÃ được commit — lỗi của một subscriber không được biến thao tác
                // thành "thất bại" trong mắt người dùng. Ghi log để dev xử lý.
                _logger.Error("Một subscriber của GridDrawnMessage ném exception.", ex);
            }
        }
    }
}
