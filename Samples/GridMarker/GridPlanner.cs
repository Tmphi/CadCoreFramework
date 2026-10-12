using System;
using System.Collections.Generic;
using System.Text;

namespace CadCoreFramework.Samples.GridMarker
{
    /// <summary>Một ô đã được tính toán: vị trí tâm và nhãn. Không chứa kiểu AutoCAD.</summary>
    public sealed class GridCell
    {
        public int Row { get; }
        public int Column { get; }
        public double CenterX { get; }
        public double CenterY { get; }
        public string Label { get; }

        public GridCell(int row, int column, double centerX, double centerY, string label)
        {
            Row = row;
            Column = column;
            CenterX = centerX;
            CenterY = centerY;
            Label = label;
        }
    }

    /// <summary>
    /// GOLDEN SAMPLE — TẦNG LOGIC THUẦN (Functional Core).
    ///
    /// Toàn bộ "bộ não" của tính năng nằm ở đây: tính vị trí và nhãn của từng ô.
    /// KHÔNG gọi AutoCAD API, KHÔNG có side-effect → test được 100% bằng unit test thường.
    ///
    /// VÌ SAO TÁCH RIÊNG?
    /// Code nằm trong lambda của ICadContext.Execute chỉ chạy được khi có AutoCAD thật,
    /// nên unit test chỉ kiểm được "có gọi Execute hay không". Đưa logic ra ngoài lambda
    /// → kiểm được "kết quả có ĐÚNG hay không". Lớp vỏ AutoCAD (Service) chỉ còn là
    /// một vòng lặp mỏng "vẽ những gì Planner đã tính".
    /// </summary>
    public static class GridPlanner
    {
        /// <summary>
        /// Tính danh sách ô của lưới.
        /// Ô [0,0] có tâm tại (originX, originY); cột tăng theo +X, hàng tăng theo −Y
        /// (hàng A ở trên cùng, giống cách đọc bảng).
        /// </summary>
        /// <exception cref="ArgumentNullException">spec null.</exception>
        /// <exception cref="ArgumentException">spec không hợp lệ (xem <see cref="GridSpec.Validate"/>).</exception>
        public static IReadOnlyList<GridCell> Plan(GridSpec spec, double originX, double originY)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            var errors = spec.Validate();
            if (errors.Count > 0)
                throw new ArgumentException(string.Join(" ", errors), nameof(spec));

            var cells = new List<GridCell>(spec.CellCount);
            for (int row = 0; row < spec.Rows; row++)
            {
                for (int col = 0; col < spec.Columns; col++)
                {
                    cells.Add(new GridCell(
                        row,
                        col,
                        originX + col * spec.Spacing,
                        originY - row * spec.Spacing,
                        BuildLabel(spec, row, col)));
                }
            }
            return cells;
        }

        /// <summary>
        /// Đổi chỉ số hàng (0-based) thành chữ cái kiểu bảng tính:
        /// 0 → A, 25 → Z, 26 → AA, 701 → ZZ, 702 → AAA.
        /// </summary>
        public static string ToRowLetters(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), "Chỉ số hàng phải ≥ 0.");

            var sb = new StringBuilder();
            int n = index + 1; // chuyển sang hệ cơ số 26 "không có số 0"
            while (n > 0)
            {
                int rem = (n - 1) % 26;
                sb.Insert(0, (char)('A' + rem));
                n = (n - 1) / 26;
            }
            return sb.ToString();
        }

        private static string BuildLabel(GridSpec spec, int row, int col)
        {
            switch (spec.LabelStyle)
            {
                case GridLabelStyle.Sequential:
                    return (row * spec.Columns + col + 1).ToString();
                case GridLabelStyle.LetterNumber:
                default:
                    return ToRowLetters(row) + (col + 1);
            }
        }
    }
}
