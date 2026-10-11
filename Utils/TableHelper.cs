using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Bộ dựng bảng (Table) dạng Fluent Builder cho AutoCAD.
    /// Hỗ trợ tạo bảng từ dữ liệu C# collection một cách dễ dàng.
    /// </summary>
    public class CadTableBuilder
    {
        private string _title = "Bảng dữ liệu";
        private readonly List<(string Header, double Width)> _columns = new List<(string, double)>();
        private readonly List<string[]> _rows = new List<string[]>();
        private Point3d _position = Point3d.Origin;
        private double _rowHeight = 8.0;
        private double _textHeight = 2.5;

        /// <summary>Khởi tạo một bộ dựng bảng mới.</summary>
        public static CadTableBuilder Create() => new CadTableBuilder();

        /// <summary>Thiết lập tiêu đề của bảng.</summary>
        public CadTableBuilder SetTitle(string title)
        {
            _title = title;
            return this;
        }

        /// <summary>Thiết lập tọa độ điểm đặt của bảng (WCS).</summary>
        public CadTableBuilder SetPosition(Point3d position)
        {
            _position = position;
            return this;
        }

        /// <summary>Thiết lập chiều cao mỗi dòng dữ liệu.</summary>
        public CadTableBuilder SetRowHeight(double rowHeight)
        {
            _rowHeight = rowHeight;
            return this;
        }
        
        /// <summary>Thiết lập chiều cao chữ (Text Height) trong các ô bảng.</summary>
        public CadTableBuilder SetTextHeight(double textHeight)
        {
            _textHeight = textHeight;
            return this;
        }

        /// <summary>Thêm một cột mới vào bảng cùng độ rộng cột.</summary>
        public CadTableBuilder AddColumn(string header, double width)
        {
            _columns.Add((header, width));
            return this;
        }

        /// <summary>Thêm một dòng dữ liệu vào bảng.</summary>
        public CadTableBuilder AddRow(params string[] cells)
        {
            _rows.Add(cells);
            return this;
        }

        /// <summary>Nạp danh sách dòng dữ liệu từ một tập hợp collection C#.</summary>
        public CadTableBuilder FromCollection<T>(IEnumerable<T> items, Func<T, string[]> rowSelector)
        {
            foreach (var item in items)
            {
                _rows.Add(rowSelector(item));
            }
            return this;
        }

        /// <summary>Khởi tạo và hoàn thiện đối tượng Table AutoCAD sẵn sàng thêm vào database.</summary>
        public Table Build()
        {
            if (_columns.Count == 0)
                throw new InvalidOperationException("Bảng phải có ít nhất một cột.");

            int numRows = _rows.Count + 2; // +1 Title, +1 Header
            int numCols = _columns.Count;

            var table = new Table();
            table.SetDatabaseDefaults();
            table.SetSize(numRows, numCols);
            table.Position = _position;

            // Set column widths
            for (int i = 0; i < numCols; i++)
            {
                table.Columns[i].Width = _columns[i].Width;
            }

            // Set row heights
            for (int i = 0; i < numRows; i++)
            {
                table.Rows[i].Height = _rowHeight;
            }

            // Thiết lập Title (Dòng 0)
            table.Cells[0, 0].TextHeight = _textHeight * 1.5;
            table.Cells[0, 0].TextString = _title;
            table.Cells[0, 0].Alignment = CellAlignment.MiddleCenter;

            // Thiết lập Header (Dòng 1)
            for (int c = 0; c < numCols; c++)
            {
                table.Cells[1, c].TextHeight = _textHeight;
                table.Cells[1, c].TextString = _columns[c].Header;
                table.Cells[1, c].Alignment = CellAlignment.MiddleCenter;
            }

            // Thiết lập Data Rows (Dòng 2 trở đi)
            for (int r = 0; r < _rows.Count; r++)
            {
                var rowData = _rows[r];
                for (int c = 0; c < numCols; c++)
                {
                    if (c < rowData.Length)
                    {
                        table.Cells[r + 2, c].TextHeight = _textHeight;
                        table.Cells[r + 2, c].TextString = rowData[c];
                        table.Cells[r + 2, c].Alignment = CellAlignment.MiddleLeft;
                    }
                }
            }

            table.GenerateLayout();
            return table;
        }
    }
}
