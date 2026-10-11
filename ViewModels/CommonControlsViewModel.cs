using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CadCoreFramework.ViewModels
{
    /// <summary>
    /// ViewModel minh họa các điều khiển giao diện WPF thông dụng (TextBox, CheckBox, RadioButton, ComboBox, Slider, DataGrid).
    /// </summary>
    public partial class CommonControlsViewModel : ViewModelBase
    {
        // 1. Ô nhập văn bản (Text Inputs)
        [ObservableProperty]
        private string _singleLineText = "Mặc định";

        [ObservableProperty]
        private string _multiLineText = "Dòng 1\nDòng 2\nDòng 3";

        // 2. Tùy chọn chuyển đổi (Selectors / Toggles)
        [ObservableProperty]
        private bool _isOptionAEnabled = true;

        [ObservableProperty]
        private bool _isOptionBEnabled = false;

        [ObservableProperty]
        private string _selectedRadioGroup = "Radio 1";

        // 3. Danh sách thả xuống (ComboBox)
        [ObservableProperty]
        private ObservableCollection<string> _availableLayers;

        [ObservableProperty]
        private string _selectedLayer;

        // 4. Thanh trượt giá trị (Slider)
        [ObservableProperty]
        private double _sliderValue = 50.0;

        // 5. Bảng dữ liệu (DataGrid)
        [ObservableProperty]
        private ObservableCollection<SampleDataItem> _dataItems;

        /// <summary>
        /// Khởi tạo một phiên bản CommonControlsViewModel với dữ liệu mẫu.
        /// </summary>
        public CommonControlsViewModel()
        {
            Title = "UI Controls Showcase";
            
            // Dữ liệu mẫu cho ComboBox
            AvailableLayers = new ObservableCollection<string>
            {
                "0", "Defpoints", "WALLS", "DOORS", "EQUIPMENT", "DIMENSIONS"
            };
            SelectedLayer = "EQUIPMENT";

            // Dữ liệu mẫu cho DataGrid
            DataItems = new ObservableCollection<SampleDataItem>
            {
                new SampleDataItem { Id = 1, Name = "Panel A", Power = 100 },
                new SampleDataItem { Id = 2, Name = "Panel B", Power = 150 },
                new SampleDataItem { Id = 3, Name = "Motor M1", Power = 55 },
                new SampleDataItem { Id = 4, Name = "Pump P1", Power = 22 }
            };
        }

        /// <summary>
        /// Lệnh gửi và áp dụng dữ liệu nhập.
        /// </summary>
        [RelayCommand]
        private void SubmitData()
        {
            IsBusy = true;
            // Giả lập lưu dữ liệu
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => 
            {
                IsBusy = false;
            });
        }
    }

    /// <summary>
    /// Lớp đối tượng dữ liệu mẫu hiển thị trên DataGrid.
    /// </summary>
    public class SampleDataItem
    {
        /// <summary>Mã định danh.</summary>
        public int Id { get; set; }

        /// <summary>Tên thiết bị.</summary>
        public string Name { get; set; }

        /// <summary>Công suất (kW).</summary>
        public double Power { get; set; }
    }
}
