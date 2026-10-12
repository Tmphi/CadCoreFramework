using System;
using CadCoreFramework.Core;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Cung cấp các phương thức mở rộng (Extension Methods) cho ICadScope để đọc và ghi siêu dữ liệu
    /// định dạng JSON vào XRecord (Extension Dictionary). Đây là giải pháp ưu tiên để lưu trữ dữ liệu
    /// có cấu trúc trên các đối tượng AutoCAD do XRecord không bị giới hạn kích thước trên mỗi thực thể
    /// (khác với mức trần 16 KB của XData).
    ///
    /// KHI NÀO NÊN DÙNG XRecord so với XData:
    ///   XRecord — Dữ liệu JSON lớn hoặc phức tạp (thông số thiết bị, dữ liệu đấu nối, thông tin BOM).
    ///   XData  — Các cờ đánh dấu nhỏ dạng vô hướng (scalar flags) hoặc mã định danh ngắn cần tương thích với AutoLISP.
    /// </summary>
    public static class XRecordExtensions
    {
        /// <summary>
        /// Ghi một đối tượng C# dưới dạng chuỗi JSON vào Extension Dictionary của một thực thể AutoCAD,
        /// với khóa xác định bởi <paramref name="key"/>. Ghi đè bất kỳ giá trị hiện có nào trùng khóa.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của đối tượng cần tuần tự hóa (serialize).</typeparam>
        /// <param name="scope">CadScope hiện hành (bắt buộc KHÔNG được là chế độ chỉ đọc - read-only).</param>
        /// <param name="entityId">ObjectId của thực thể mục tiêu.</param>
        /// <param name="key">Khóa Dictionary dùng để lưu trữ và truy xuất bản ghi.</param>
        /// <param name="data">Đối tượng dữ liệu cần tuần tự hóa sang JSON.</param>
        /// <exception cref="InvalidOperationException">
        /// Bắn ra khi được gọi trên một scope chỉ đọc (bên trong ExecuteRead).
        /// </exception>
        public static void SetXRecord<T>(this ICadScope scope, Autodesk.AutoCAD.DatabaseServices.ObjectId entityId, string key, T data)
        {
            if (scope.IsReadOnly)
                throw new InvalidOperationException("Không thể ghi XRecord bên trong một scope ExecuteRead.");

            var obj = scope.Transaction.GetObject(entityId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForWrite);
            Utils.XRecordHelper.SetMetadata(scope.Transaction, obj, key, data);
        }

        /// <summary>
        /// Đọc và giải tuần tự hóa (deserialize) một giá trị JSON từ Extension Dictionary của một thực thể AutoCAD.
        /// Trả về <paramref name="defaultValue"/> nếu khóa không tồn tại hoặc chuỗi JSON không hợp lệ.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu dự kiến của thông tin được lưu trữ.</typeparam>
        /// <param name="scope">CadScope hiện hành (chỉ đọc hoặc ghi).</param>
        /// <param name="entityId">ObjectId của thực thể nguồn.</param>
        /// <param name="key">Khóa Dictionary cần tìm kiếm.</param>
        /// <param name="defaultValue">Giá trị dự phòng khi bản ghi không tồn tại hoặc bị lỗi định dạng.</param>
        public static T GetXRecord<T>(this ICadScope scope, Autodesk.AutoCAD.DatabaseServices.ObjectId entityId, string key, T defaultValue = default)
        {
            var openMode = scope.IsReadOnly
                ? Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead
                : Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead; // Luôn mở ForRead khi truy xuất

            var obj = scope.Transaction.GetObject(entityId, openMode);
            return Utils.XRecordHelper.GetMetadata<T>(scope.Transaction, obj, key, defaultValue);
        }

        /// <summary>
        /// Xóa mục XRecord tương ứng với <paramref name="key"/> khỏi Extension Dictionary của một thực thể.
        /// Không làm gì nếu khóa không tồn tại.
        /// </summary>
        /// <param name="scope">CadScope hiện hành (bắt buộc KHÔNG được là chế độ chỉ đọc).</param>
        /// <param name="entityId">ObjectId của thực thể mục tiêu.</param>
        /// <param name="key">Khóa Dictionary cần xóa.</param>
        public static void RemoveXRecord(this ICadScope scope, Autodesk.AutoCAD.DatabaseServices.ObjectId entityId, string key)
        {
            if (scope.IsReadOnly)
                throw new InvalidOperationException("Không thể xóa XRecord bên trong một scope ExecuteRead.");

            var obj = scope.Transaction.GetObject(entityId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForWrite);
            Utils.XRecordHelper.RemoveMetadata(scope.Transaction, obj, key);
        }
    }
}
