using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace CadCoreFramework.Utils.Jigs
{
    /// <summary>
    /// Lớp hỗ trợ tạo hiệu ứng kéo lê (Jig) vẽ hình chữ nhật động trên màn hình,
    /// cho phép người dùng kéo giãn hình chữ nhật từ điểm góc này sang góc đối diện.
    /// </summary>
    public class RectangleJig : EntityJig
    {
        private readonly Point3d _corner1;
        private Point3d _corner2;
        private readonly Polyline _polyline;
        private readonly string _promptMessage;

        /// <summary>
        /// Khởi tạo một phiên bản RectangleJig với điểm góc thứ nhất, đối tượng Polyline và câu nhắc tùy chọn.
        /// </summary>
        public RectangleJig(Point3d corner1, Polyline polyline, string promptMessage = "\nChọn góc đối diện: ")
            : base(polyline)
        {
            _corner1 = corner1;
            _corner2 = corner1;
            _polyline = polyline;
            _promptMessage = promptMessage;

            // Khởi tạo polyline với 4 đỉnh nếu đang rỗng
            if (_polyline.NumberOfVertices == 0)
            {
                _polyline.AddVertexAt(0, new Point2d(corner1.X, corner1.Y), 0, 0, 0);
                _polyline.AddVertexAt(1, new Point2d(corner1.X, corner1.Y), 0, 0, 0);
                _polyline.AddVertexAt(2, new Point2d(corner1.X, corner1.Y), 0, 0, 0);
                _polyline.AddVertexAt(3, new Point2d(corner1.X, corner1.Y), 0, 0, 0);
                _polyline.Closed = true;
            }
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var options = new JigPromptPointOptions(_promptMessage)
            {
                UserInputControls = UserInputControls.Accept3dCoordinates |
                                    UserInputControls.NoNegativeResponseAccepted |
                                    UserInputControls.NullResponseAccepted,
                UseBasePoint = true,
                BasePoint = _corner1
            };

            var result = prompts.AcquirePoint(options);

            if (result.Value.IsEqualTo(_corner2))
                return SamplerStatus.NoChange;

            _corner2 = result.Value;
            return SamplerStatus.OK;
        }

        protected override bool Update()
        {
            try
            {
                double minX = Math.Min(_corner1.X, _corner2.X);
                double maxX = Math.Max(_corner1.X, _corner2.X);
                double minY = Math.Min(_corner1.Y, _corner2.Y);
                double maxY = Math.Max(_corner1.Y, _corner2.Y);

                _polyline.SetPointAt(0, new Point2d(minX, minY));
                _polyline.SetPointAt(1, new Point2d(maxX, minY));
                _polyline.SetPointAt(2, new Point2d(maxX, maxY));
                _polyline.SetPointAt(3, new Point2d(minX, maxY));
                _polyline.Elevation = _corner1.Z;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Tọa độ điểm góc thứ nhất.</summary>
        public Point3d Corner1 => _corner1;

        /// <summary>Tọa độ điểm góc đối diện hiện tại.</summary>
        public Point3d Corner2 => _corner2;

        /// <summary>Chiều rộng hiện tại của hình chữ nhật.</summary>
        public double Width => Math.Abs(_corner2.X - _corner1.X);

        /// <summary>Chiều cao hiện tại của hình chữ nhật.</summary>
        public double Height => Math.Abs(_corner2.Y - _corner1.Y);
    }
}
