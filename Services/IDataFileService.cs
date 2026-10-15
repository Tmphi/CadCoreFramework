using System.Collections.Generic;
using CadCoreFramework.Core;
using CadCoreFramework.Models;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Đọc/ghi file dữ liệu JSON, CSV và Excel (.xlsx). Không dùng API AutoCAD, gọi được từ lệnh,
    /// service hay ViewModel, ở bất kỳ thread nào.
    ///
    /// Lỗi thường gặp (không thấy file, file đang mở trong Excel, JSON/CSV sai cấu trúc…) trả về
    /// <see cref="CadResult"/> thất bại với thông điệp tiếng Việt đọc được, không ném exception.
    /// Ghi file luôn ATOMIC (tệp tạm rồi thay thế), không bao giờ để lại file ghi dở.
    /// </summary>
    public interface IDataFileService
    {
        /// <summary>Đọc file JSON thành đối tượng kiểu <typeparamref name="T"/> (Newtonsoft.Json).</summary>
        CadResult<T> ReadJson<T>(string path);

        /// <summary>Ghi đối tượng ra file JSON (UTF-8, thụt lề).</summary>
        CadResult WriteJson<T>(string path, T data);

        /// <summary>
        /// Đọc file CSV thành bảng; mọi ô là chuỗi. <paramref name="delimiter"/> = null: tự nhận ',' ';' hoặc tab.
        /// Nhận UTF-8 (có hoặc không BOM); file ANSI (Excel "CSV (Comma delimited)") bị từ chối kèm hướng dẫn.
        /// </summary>
        CadResult<DataSheet> ReadCsv(string path, char? delimiter = null);

        /// <summary>Ghi bảng ra CSV UTF-8 có BOM (Excel mở đúng tiếng Việt), số theo InvariantCulture.</summary>
        CadResult WriteCsv(string path, DataSheet sheet, char delimiter = ',');

        /// <summary>
        /// Đọc một sheet .xlsx thành bảng (dòng có dữ liệu đầu tiên là tiêu đề). <paramref name="sheetName"/> = null:
        /// sheet đầu tiên. Ô số → <c>double</c>, ô ngày → <c>DateTime</c>, ô rỗng → <c>null</c>.
        /// </summary>
        CadResult<DataSheet> ReadExcel(string path, string sheetName = null);

        /// <summary>Ghi bảng ra file .xlsx có một sheet. Ghi đè file cũ.</summary>
        CadResult WriteExcel(string path, DataSheet sheet, string sheetName = "Sheet1");

        /// <summary>Tên các sheet của file .xlsx, theo thứ tự.</summary>
        CadResult<IReadOnlyList<string>> GetExcelSheetNames(string path);

        /// <summary>
        /// Đọc bảng theo đuôi file: .xlsx → <see cref="ReadExcel"/>, .csv → <see cref="ReadCsv"/>,
        /// .json → mảng đối tượng <c>[{"Cột": giá trị, …}, …]</c>.
        /// </summary>
        CadResult<DataSheet> ReadTable(string path);

        /// <summary>Ghi bảng theo đuôi file (.xlsx, .csv, .json), cùng định dạng với <see cref="ReadTable"/>.</summary>
        CadResult WriteTable(string path, DataSheet sheet);
    }
}
