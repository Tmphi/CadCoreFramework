using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace CadCoreFramework.Utils.Jigs
{
    /// <summary>
    /// Lớp cơ sở (Base Class) cho DrawJig.
    /// Khác với EntityJig (chỉ thao tác 1 đối tượng duy nhất), DrawJig cho phép vẽ nhiều đối tượng ảo
    /// trên màn hình cùng lúc trước khi người dùng thực sự xác nhận tạo ra chúng.
    /// </summary>
    public abstract class CadDrawJigBase : DrawJig
    {
        protected Point3d CurrentPoint;

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var options = new JigPromptPointOptions("\nChọn điểm: ")
            {
                UserInputControls = UserInputControls.Accept3dCoordinates | UserInputControls.NoNegativeResponseAccepted
            };

            var result = prompts.AcquirePoint(options);

            if (result.Value.IsEqualTo(CurrentPoint))
                return SamplerStatus.NoChange;

            CurrentPoint = result.Value;
            return SamplerStatus.OK;
        }

        /// <summary>
        /// Hàm này cho phép vẽ tất cả các đối tượng hình học ảo bằng hệ thống đồ họa WorldDraw.
        /// Thường bạn sẽ dùng WorldGeometry để vẽ Lines, Polylines ảo.
        /// </summary>
        protected override bool WorldDraw(WorldDraw draw)
        {
            return DrawGraphics(draw.Geometry);
        }

        /// <summary>
        /// Override hàm này để vẽ các hình học đồ họa (ảo) tùy chỉnh dựa trên CurrentPoint.
        /// </summary>
        /// <param name="geometry">Giao diện vẽ (DrawLine, DrawCircle...)</param>
        /// <returns>True nếu vẽ thành công.</returns>
        protected abstract bool DrawGraphics(WorldGeometry geometry);
    }
}
