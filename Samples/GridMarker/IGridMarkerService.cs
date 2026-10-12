using System.Collections.Generic;
using CadCoreFramework.Core;

namespace CadCoreFramework.Samples.GridMarker
{
    /// <summary>
    /// GOLDEN SAMPLE — HỢP ĐỒNG SERVICE.
    ///
    /// Interface chỉ dùng kiểu dữ liệu của plugin (GridSpec, GridCell, CadResult) — không lộ
    /// Point3d/ObjectId ra ngoài → ViewModel và Command phụ thuộc vào interface này có thể
    /// được test bằng Mock mà không cần AutoCAD.
    ///
    /// KHÔNG có phương thức nào ở đây hỏi người dùng (prompt). Việc thu thập input là trách
    /// nhiệm của điểm vào (Command hoặc ViewModel), Service chỉ nhận dữ liệu đã sẵn sàng.
    /// </summary>
    public interface IGridMarkerService
    {
        /// <summary>
        /// Vẽ các ô đã được <see cref="GridPlanner"/> tính sẵn vào không gian vẽ hiện hành,
        /// trong MỘT transaction và MỘT bước Undo. Publish <see cref="GridDrawnMessage"/> khi thành công.
        /// </summary>
        /// <returns>Số ô đã vẽ, hoặc lỗi (không throw) nếu thao tác với bản vẽ thất bại.</returns>
        CadResult<int> Draw(GridSpec spec, IReadOnlyList<GridCell> cells);

        /// <summary>Đếm số ô Grid Marker (nhận diện qua XData) đang có trong không gian vẽ hiện hành.</summary>
        int CountMarkers();
    }
}
