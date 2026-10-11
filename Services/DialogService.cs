using System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Lớp triển khai dịch vụ hộp thoại IDialogService sử dụng các hộp thoại tiêu chuẩn của Windows (WPF và WinForms).
    /// </summary>
    public class DialogService : IDialogService
    {
        /// <summary>
        /// Hiển thị hộp thoại chọn tệp để mở.
        /// </summary>
        public string ShowOpenFileDialog(string filter, string title = "Chọn tệp")
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            return null;
        }

        /// <summary>
        /// Hiển thị hộp thoại chọn tệp để lưu.
        /// </summary>
        public string ShowSaveFileDialog(string filter, string defaultExt = "", string title = "Lưu tệp")
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter,
                DefaultExt = defaultExt,
                Title = title
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            return null;
        }

        /// <summary>
        /// Hiển thị hộp thoại chọn thư mục.
        /// </summary>
        public string ShowFolderBrowserDialog(string description = "Chọn thư mục")
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = description;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.SelectedPath;
                }
            }
            return null;
        }
    }
}
