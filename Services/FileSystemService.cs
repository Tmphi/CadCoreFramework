using System.Collections.Generic;
using System.IO;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Lớp triển khai IFileSystemService thực hiện các thao tác tệp và thư mục trên hệ thống tệp đĩa vật lý của hệ điều hành.
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public void DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive);
        }

        public string ReadAllText(string path) => File.ReadAllText(path);

        public void WriteAllText(string path, string content) => WriteAtomic(path, tmp => File.WriteAllText(tmp, content));

        public byte[] ReadAllBytes(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
        }

        public void WriteAllBytes(string path, byte[] content) => WriteAtomic(path, tmp => File.WriteAllBytes(tmp, content));

        private static void WriteAtomic(string path, System.Action<string> writeTemp)
        {
            // Ghi ATOMIC: ghi ra tệp tạm CÙNG thư mục (cùng ổ đĩa) rồi thay thế tệp đích. Người đọc — hoặc
            // AutoCAD bị tắt ngang — không bao giờ thấy tệp ghi dở. Tên tạm duy nhất: hai tiến trình ghi
            // cùng lúc không giẫm lên tệp tạm của nhau.
            string full = Path.GetFullPath(path);
            string tmp = full + "." + System.Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                writeTemp(tmp);
                if (File.Exists(full))
                {
                    ReplaceFile(tmp, full);
                }
                else
                {
                    try { File.Move(tmp, full); }
                    catch (IOException) when (File.Exists(full)) { ReplaceFile(tmp, full); } // tiến trình khác vừa tạo
                }
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp); // chỉ còn khi có bước lỗi
            }
        }

        private static void ReplaceFile(string source, string destination)
        {
            try { File.Replace(source, destination, null); }
            catch (System.PlatformNotSupportedException) { File.Copy(source, destination, true); } // ổ không hỗ trợ (FAT…)
        }

        public IEnumerable<string> GetFiles(string path, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!Directory.Exists(path)) return new string[0];
            return Directory.GetFiles(path, searchPattern, searchOption);
        }
    }
}
