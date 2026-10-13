using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Core;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Extension methods cho ICadScope: vẽ đường thẳng, polyline, hình chữ nhật, đa giác, slot.
    /// </summary>
    public static class CadScopeLineExtensions
    {
        /// <summary>
        /// Vẽ một đoạn thẳng (Line) 2D/3D trực tiếp vào không gian hiện hành (ModelSpace hoặc PaperSpace).
        /// </summary>
        public static ObjectId DrawLine(this ICadScope scope, Point3d startPoint, Point3d endPoint,
            string layer = null, short? colorIndex = null, LineWeight lineWeight = LineWeight.ByLayer,
            Action<Line> configure = null)
        {
            return CadScopeDrawingHelpers.AppendNew(scope, new Line(startPoint, endPoint), line =>
            {
                line.SetDatabaseDefaults(scope.Database);
                line.LineWeight = lineWeight;
            }, layer, colorIndex, configure);
        }

        /// <summary>
        /// Vẽ một Polyline nhẹ (Lightweight Polyline) từ tập hợp các điểm 2D.
        /// </summary>
        public static ObjectId DrawPolyline(this ICadScope scope, IEnumerable<Point2d> points,
            bool isClosed = true, double constantWidth = 0.0, double elevation = 0.0, string layer = null, short? colorIndex = null,
            Action<Polyline> configure = null)
        {
            var ptList = points?.ToList();
            if (ptList == null || ptList.Count < 2) return ObjectId.Null;

            return CadScopeDrawingHelpers.AppendNew(scope, new Polyline(), pline =>
            {
                pline.SetDatabaseDefaults(scope.Database);
                for (int i = 0; i < ptList.Count; i++)
                {
                    pline.AddVertexAt(i, ptList[i], 0, constantWidth, constantWidth);
                }
                pline.Closed = isClosed;
                pline.Elevation = elevation;
            }, layer, colorIndex, configure);
        }

        /// <summary>
        /// Vẽ Polyline nâng cao hỗ trợ từng đỉnh có độ cong (Bulge) và bề rộng nét riêng.
        /// </summary>
        public static ObjectId DrawPolyline(this ICadScope scope, IEnumerable<PolylineVertex> vertices,
            bool isClosed = true, double elevation = 0.0, string layer = null, short? colorIndex = null,
            Action<Polyline> configure = null)
        {
            var vList = vertices?.ToList();
            if (vList == null || vList.Count < 2) return ObjectId.Null;

            return CadScopeDrawingHelpers.AppendNew(scope, new Polyline(), pline =>
            {
                pline.SetDatabaseDefaults(scope.Database);
                for (int i = 0; i < vList.Count; i++)
                {
                    pline.AddVertexAt(i, vList[i].Point, vList[i].Bulge, vList[i].StartWidth, vList[i].EndWidth);
                }
                pline.Closed = isClosed;
                pline.Elevation = elevation;
            }, layer, colorIndex, configure);
        }

        /// <summary>
        /// Vẽ một hình chữ nhật dạng Polyline khép kín giữa hai điểm góc đối diện.
        /// </summary>
        public static ObjectId DrawRectangle(this ICadScope scope, Point3d corner1, Point3d corner2,
            string layer = null, short? colorIndex = null, double lineWidth = 0.0,
            Action<Polyline> configure = null)
        {
            double minX = Math.Min(corner1.X, corner2.X);
            double maxX = Math.Max(corner1.X, corner2.X);
            double minY = Math.Min(corner1.Y, corner2.Y);
            double maxY = Math.Max(corner1.Y, corner2.Y);

            var vertices = new List<Point2d>
            {
                new Point2d(minX, minY),
                new Point2d(maxX, minY),
                new Point2d(maxX, maxY),
                new Point2d(minX, maxY)
            };

            return CadScopeDrawingHelpers.AppendNew(scope, new Polyline(), rect =>
            {
                rect.SetDatabaseDefaults(scope.Database);
                for (int i = 0; i < vertices.Count; i++)
                {
                    rect.AddVertexAt(i, vertices[i], 0, lineWidth, lineWidth);
                }
                rect.Closed = true;
                rect.Elevation = corner1.Z;
            }, layer, colorIndex, configure);
        }

        /// <summary>
        /// Vẽ hình chữ nhật bo tròn 4 góc với bán kính góc lượn filletRadius.
        /// </summary>
        public static ObjectId DrawFilletRectangle(this ICadScope scope, Point3d corner1, Point3d corner2,
            double filletRadius, string layer = null, short? colorIndex = null, double lineWidth = 0.0,
            Action<Polyline> configure = null)
        {
            double minX = Math.Min(corner1.X, corner2.X);
            double maxX = Math.Max(corner1.X, corner2.X);
            double minY = Math.Min(corner1.Y, corner2.Y);
            double maxY = Math.Max(corner1.Y, corner2.Y);
            double w = maxX - minX;
            double h = maxY - minY;

            if (filletRadius <= 0 || filletRadius * 2 > w || filletRadius * 2 > h)
            {
                return scope.DrawRectangle(corner1, corner2, layer, colorIndex, lineWidth, configure);
            }

            // Bulge cho cung tròn 90 độ = tan(90° / 4) = tan(22.5°) = sqrt(2) - 1 ≈ 0.414213562373095
            const double bulge = 0.414213562373095;

            var vertices = new List<PolylineVertex>
            {
                new PolylineVertex(new Point2d(minX + filletRadius, minY), 0, lineWidth, lineWidth),
                new PolylineVertex(new Point2d(maxX - filletRadius, minY), bulge, lineWidth, lineWidth),
                new PolylineVertex(new Point2d(maxX, minY + filletRadius), 0, lineWidth, lineWidth),
                new PolylineVertex(new Point2d(maxX, maxY - filletRadius), bulge, lineWidth, lineWidth),
                new PolylineVertex(new Point2d(maxX - filletRadius, maxY), 0, lineWidth, lineWidth),
                new PolylineVertex(new Point2d(minX + filletRadius, maxY), bulge, lineWidth, lineWidth),
                new PolylineVertex(new Point2d(minX, maxY - filletRadius), 0, lineWidth, lineWidth),
                new PolylineVertex(new Point2d(minX, minY + filletRadius), bulge, lineWidth, lineWidth)
            };

            return scope.DrawPolyline(vertices, isClosed: true, elevation: corner1.Z, layer: layer, colorIndex: colorIndex, configure: configure);
        }

        /// <summary>
        /// Vẽ đa giác đều n cạnh (Regular Polygon) nội tiếp trong đường tròn bán kính radius.
        /// </summary>
        public static ObjectId DrawRegularPolygon(this ICadScope scope, Point3d center, double radius, int sides,
            double startAngleRad = 0.0, string layer = null, short? colorIndex = null, double lineWidth = 0.0,
            Action<Polyline> configure = null)
        {
            if (sides < 3 || radius <= 0) return ObjectId.Null;

            double step = (Math.PI * 2) / sides;
            var points = new List<Point2d>(sides);
            for (int i = 0; i < sides; i++)
            {
                double angle = startAngleRad + i * step;
                points.Add(new Point2d(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle)));
            }

            return scope.DrawPolyline(points, isClosed: true, constantWidth: lineWidth, elevation: center.Z, layer: layer, colorIndex: colorIndex, configure: configure);
        }

        /// <summary>
        /// Vẽ rãnh xẻ / lỗ bầu dục (Slot) với hai đầu là nửa đường tròn. Thường dùng trong cơ khí và bố trí thiết bị tủ điện.
        /// </summary>
        public static ObjectId DrawSlot(this ICadScope scope, Point3d center, double length, double width,
            double rotationAngleRad = 0.0, string layer = null, short? colorIndex = null, double lineWidth = 0.0,
            Action<Polyline> configure = null)
        {
            if (length <= 0 || width <= 0) return ObjectId.Null;
            double r = width * 0.5;

            if (length <= width)
            {
                return scope.DrawCircle(center, r, layer: layer, colorIndex: colorIndex);
            }

            double d = (length - width) * 0.5;

            var p1 = new Point2d(d, r);
            var p2 = new Point2d(-d, r);
            var p3 = new Point2d(-d, -r);
            var p4 = new Point2d(d, -r);

            Point2d TransformPt(Point2d local)
            {
                double cos = Math.Cos(rotationAngleRad);
                double sin = Math.Sin(rotationAngleRad);
                return new Point2d(
                    center.X + (local.X * cos - local.Y * sin),
                    center.Y + (local.X * sin + local.Y * cos)
                );
            }

            var vertices = new List<PolylineVertex>
            {
                new PolylineVertex(TransformPt(p1), 0.0, lineWidth, lineWidth),
                new PolylineVertex(TransformPt(p2), 1.0, lineWidth, lineWidth),
                new PolylineVertex(TransformPt(p3), 0.0, lineWidth, lineWidth),
                new PolylineVertex(TransformPt(p4), 1.0, lineWidth, lineWidth)
            };

            return scope.DrawPolyline(vertices, isClosed: true, elevation: center.Z, layer: layer, colorIndex: colorIndex, configure: configure);
        }

        /// <summary>
        /// Vẽ một đường tròn (Circle) theo điểm tâm và bán kính.
        /// </summary>
        public static ObjectId DrawCircle(this ICadScope scope, Point3d center, double radius,
            Vector3d? normal = null, string layer = null, short? colorIndex = null,
            Action<Circle> configure = null)
        {
            return CadScopeDrawingHelpers.AppendNew(scope, new Circle(center, normal ?? Vector3d.ZAxis, radius),
                circle => circle.SetDatabaseDefaults(scope.Database), layer, colorIndex, configure);
        }

        /// <summary>
        /// Vẽ một cung tròn (Arc) theo tâm, bán kính, góc bắt đầu và góc kết thúc (tính bằng radian).
        /// </summary>
        public static ObjectId DrawArc(this ICadScope scope, Point3d center, double radius,
            double startAngleRad, double endAngleRad, Vector3d? normal = null, string layer = null, short? colorIndex = null,
            Action<Arc> configure = null)
        {
            return CadScopeDrawingHelpers.AppendNew(scope, new Arc(center, normal ?? Vector3d.ZAxis, radius, startAngleRad, endAngleRad),
                arc => arc.SetDatabaseDefaults(scope.Database), layer, colorIndex, configure);
        }

        /// <summary>
        /// Vẽ một hình elip (Ellipse) dựa trên tâm, vector trục lớn và tỷ lệ trục nhỏ/trục lớn (0 &lt; ratio ≤ 1).
        /// Góc xoay được xác định trực tiếp bởi hướng của majorAxis.
        /// </summary>
        public static ObjectId DrawEllipse(this ICadScope scope, Point3d center, Vector3d majorAxis,
            double minorToMajorRatio = 0.5, Vector3d? normal = null, string layer = null, short? colorIndex = null,
            Action<Ellipse> configure = null)
        {
            return CadScopeDrawingHelpers.AppendNew(scope, new Ellipse(center, normal ?? Vector3d.ZAxis, majorAxis, minorToMajorRatio, 0, Math.PI * 2),
                ellipse => ellipse.SetDatabaseDefaults(scope.Database), layer, colorIndex, configure);
        }

        /// <summary>
        /// Vẽ một đường cong Spline đi qua danh sách các điểm mẫu (fit points).
        /// </summary>
        public static ObjectId DrawSpline(this ICadScope scope, IEnumerable<Point3d> fitPoints,
            string layer = null, short? colorIndex = null, Action<Spline> configure = null)
        {
            var pts = fitPoints?.ToList();
            if (pts == null || pts.Count < 2) return ObjectId.Null;

            var ptCollection = new Point3dCollection(pts.ToArray());
            return CadScopeDrawingHelpers.AppendNew(scope, new Spline(ptCollection, 0, 0.0),
                spline => spline.SetDatabaseDefaults(scope.Database), layer, colorIndex, configure);
        }

        /// <summary>
        /// Vẽ một vùng che (Wipeout mask) hình chữ nhật được xác định bởi hai điểm góc.
        /// Wipeout che khuất các đối tượng nằm bên dưới — rất thích hợp cho bảng chú giải, khung tên.
        /// </summary>
        public static ObjectId DrawWipeout(this ICadScope scope, Point3d corner1, Point3d corner2,
            string layer = null)
        {
            double minX = Math.Min(corner1.X, corner2.X);
            double maxX = Math.Max(corner1.X, corner2.X);
            double minY = Math.Min(corner1.Y, corner2.Y);
            double maxY = Math.Max(corner1.Y, corner2.Y);

            var pts = new Point2dCollection
            {
                new Point2d(minX, minY),
                new Point2d(maxX, minY),
                new Point2d(maxX, maxY),
                new Point2d(minX, maxY),
                new Point2d(minX, minY) // Wipeout yêu cầu vòng lặp khép kín
            };

            return CadScopeDrawingHelpers.AppendNew(scope, new Wipeout(), wipeout =>
            {
                wipeout.SetDatabaseDefaults(scope.Database);
                wipeout.SetFrom(pts, Vector3d.ZAxis);
            }, layer, colorIndex: null, configure: null);
        }
    }
}
