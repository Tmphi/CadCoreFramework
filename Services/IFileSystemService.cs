using System.Collections.Generic;
using System.IO;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Wrapper abstraction cho các thao tác IO (File, Directory) 
    /// giúp dễ dàng Mocking trong Unit Test và không bị phụ thuộc chặt vào đĩa vật lý.
    /// </summary>
    public interface IFileSystemService
    {
        /// <summary>
        /// Kiểm tra xem tệp có tồn tại trên đĩa hay không.
        /// </summary>
        bool FileExists(string path);

        /// <summary>
        /// Kiểm tra xem thư mục có tồn tại trên đĩa hay không.
        /// </summary>
        bool DirectoryExists(string path);

        /// <summary>
        /// Tạo thư mục theo đường dẫn chỉ định.
        /// </summary>
        void CreateDirectory(string path);

        /// <summary>
        /// Xóa tệp nếu tệp tồn tại.
        /// </summary>
        void DeleteFile(string path);

        /// <summary>
        /// Xóa thư mục và tùy chọn xóa đệ quy toàn bộ thư mục con/tệp bên trong.
        /// </summary>
        void DeleteDirectory(string path, bool recursive);

        /// <summary>
        /// Đọc toàn bộ nội dung của tệp văn bản.
        /// </summary>
        string ReadAllText(string path);

        /// <summary>
        /// Ghi chuỗi nội dung vào tệp văn bản.
        /// </summary>
        void WriteAllText(string path, string content);

        /// <summary>
        /// Lấy danh sách đường dẫn các tệp trong thư mục theo mẫu tìm kiếm và tùy chọn duyệt thư mục con.
        /// </summary>
        IEnumerable<string> GetFiles(string path, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    }
}
