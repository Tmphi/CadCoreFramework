using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Giao diện dịch vụ in bản vẽ theo chương trình (Programmatic Plotting).
    /// Cho phép in một hoặc nhiều layout ra thiết bị hoặc file PDF/DWF.
    /// </summary>
    public interface IPlotService
    {
        /// <summary>
        /// In một Layout chỉ định ra một file PDF.
        /// </summary>
        /// <param name="db">Database của bản vẽ cần in.</param>
        /// <param name="layoutName">Tên Layout cần in ("Model" hoặc tên Paper Space).</param>
        /// <param name="outputFilePath">Đường dẫn file PDF đầu ra (tuyệt đối).</param>
        /// <param name="printerName">
        /// Tên máy in/plotter PC3. Mặc định dùng "DWG To PDF.pc3" có sẵn trong AutoCAD.
        /// Có thể dùng "DWF6 ePlot.pc3" để xuất DWF.
        /// </param>
        /// <returns>True nếu in thành công.</returns>
        bool PlotLayoutToPdf(Database db, string layoutName, string outputFilePath,
            string printerName = "DWG To PDF.pc3");

        /// <summary>
        /// In nhiều Layout cùng lúc vào một file PDF nhiều trang (batch plot).
        /// </summary>
        /// <param name="db">Database của bản vẽ.</param>
        /// <param name="layoutNames">Danh sách tên Layout cần in theo thứ tự trang.</param>
        /// <param name="outputFilePath">Đường dẫn file PDF đầu ra.</param>
        /// <param name="printerName">Tên máy in/plotter PC3.</param>
        /// <returns>True nếu in thành công.</returns>
        bool PlotMultipleLayouts(Database db, IEnumerable<string> layoutNames,
            string outputFilePath, string printerName = "DWG To PDF.pc3");

        /// <summary>
        /// Lấy danh sách tên máy in/plotter có sẵn trong hệ thống.
        /// </summary>
        IEnumerable<string> GetAvailablePlotters();

        /// <summary>
        /// Lấy danh sách tên khổ giấy hỗ trợ bởi một plotter.
        /// </summary>
        IEnumerable<string> GetMediaNames(string plotterName);
    }
}
