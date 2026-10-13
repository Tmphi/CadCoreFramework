using System.Collections.Generic;
using CadCoreFramework.Utils;

namespace CadCoreFramework.Samples.GridMarker
{
    /// <summary>Cách đánh nhãn cho các ô trong lưới.</summary>
    public enum GridLabelStyle
    {
        /// <summary>Hàng là chữ cái, cột là số: A1, A2, …, B1, … (giống bảng tính).</summary>
        LetterNumber,

        /// <summary>Đánh số liên tục theo hàng: 1, 2, 3, …</summary>
        Sequential
    }

    /// <summary>
    /// GOLDEN SAMPLE — TẦNG MODEL.
    ///
    /// Thông số đầu vào của một lưới Grid Marker. Bất biến (immutable) và KHÔNG chứa bất kỳ
    /// kiểu dữ liệu AutoCAD nào (Point3d, ObjectId…) → có thể tạo, validate và unit test
    /// mà không cần AutoCAD runtime.
    ///
    /// QUY TẮC: validate dữ liệu ở đây, MỘT LẦN, trước khi chạm vào bản vẽ. Command và
    /// ViewModel đều gọi <see cref="Validate"/> — không lặp lại luật validate ở nơi khác.
    /// </summary>
    public sealed class GridSpec
    {
        /// <summary>Số hàng/cột tối đa cho mỗi chiều.</summary>
        public const int MaxDimension = 100;

        /// <summary>Tổng số ô tối đa — chặn việc vô tình tạo hàng chục nghìn entity.</summary>
        public const int MaxCells = 2500;

        public int Rows { get; }
        public int Columns { get; }

        /// <summary>Khoảng cách tâm–tâm giữa hai ô liền kề (đơn vị bản vẽ).</summary>
        public double Spacing { get; }

        /// <summary>Kích thước cạnh của khung vuông mỗi ô.</summary>
        public double CellSize { get; }

        public double TextHeight { get; }
        public string LayerName { get; }
        public GridLabelStyle LabelStyle { get; }

        public GridSpec(
            int rows,
            int columns,
            double spacing = 20,
            double cellSize = 15,
            double textHeight = 4,
            string layerName = "SAMPLE_GRID",
            GridLabelStyle labelStyle = GridLabelStyle.LetterNumber)
        {
            Rows = rows;
            Columns = columns;
            Spacing = spacing;
            CellSize = cellSize;
            TextHeight = textHeight;
            LayerName = layerName ?? string.Empty;
            LabelStyle = labelStyle;
        }

        /// <summary>Tổng số ô của lưới.</summary>
        public int CellCount => Rows * Columns;

        /// <summary>
        /// Kiểm tra toàn bộ luật nghiệp vụ. Trả về danh sách lỗi (rỗng = hợp lệ).
        /// Trả về danh sách thay vì throw để UI có thể hiển thị TẤT CẢ lỗi cùng lúc.
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            if (Rows < 1 || Rows > MaxDimension)
                errors.Add($"Số hàng phải trong khoảng 1–{MaxDimension}.");
            if (Columns < 1 || Columns > MaxDimension)
                errors.Add($"Số cột phải trong khoảng 1–{MaxDimension}.");
            if (Rows >= 1 && Columns >= 1 && CellCount > MaxCells)
                errors.Add($"Tổng số ô ({CellCount}) vượt quá giới hạn {MaxCells}.");

            if (CellSize <= 0)
                errors.Add("Kích thước ô phải lớn hơn 0.");
            if (Spacing < CellSize)
                errors.Add("Khoảng cách phải lớn hơn hoặc bằng kích thước ô (tránh các ô chồng lên nhau).");
            if (TextHeight <= 0 || TextHeight > CellSize)
                errors.Add("Chiều cao chữ phải lớn hơn 0 và không vượt quá kích thước ô.");

            if (string.IsNullOrWhiteSpace(LayerName))
                errors.Add("Tên Layer không được để trống.");
            else if (SymbolNameRules.ContainsInvalidCharacter(LayerName))   // cùng luật với EnsureLayer
                errors.Add("Tên Layer chứa ký tự không hợp lệ (< > / \\ \" : ; ? * | , = `).");

            return errors;
        }

        /// <summary>True nếu <see cref="Validate"/> không trả về lỗi nào.</summary>
        public bool IsValid => Validate().Count == 0;
    }
}
