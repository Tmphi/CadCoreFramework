using System;
using System.Collections.Generic;
using System.ComponentModel;
using CadCoreFramework.Core;
using CadCoreFramework.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CadCoreFramework.Samples.GridMarker
{
    /// <summary>Trạng thái hiển thị của thao tác gần nhất. View tự map sang màu (DataTrigger).</summary>
    public enum GridMarkerStatus
    {
        Idle,
        Success,
        Error
    }

    /// <summary>
    /// GOLDEN SAMPLE — TẦNG VIEWMODEL (điểm vào Palette, application context).
    ///
    /// NGUYÊN TẮC:
    ///   • Không dùng kiểu WPF (Brush, Visibility…) và không gọi AutoCAD API → test được bằng xUnit.
    ///     Trạng thái là enum <see cref="GridMarkerStatus"/>; View quyết định màu.
    ///   • Validate bằng chính <see cref="GridSpec.Validate"/> — một nguồn luật duy nhất.
    ///   • Nút Vẽ tự disable khi input sai (CanExecute) thay vì báo lỗi sau khi bấm.
    ///   • Không prompt: Palette là modeless, prompt từ đây sai ngữ cảnh. Tọa độ gốc được nhập
    ///     trực tiếp; muốn pick điểm bằng chuột thì dùng lệnh SAMPLE_GRID.
    ///   • Subscription EventAggregator đăng ký qua TrackSubscription → tự gỡ khi Dispose
    ///     (PaletteService Dispose ViewModel khi đóng/thay palette).
    /// </summary>
    public partial class GridMarkerViewModel : ViewModelBase
    {
        private readonly IGridMarkerService _service;

        // Các property là input của GridSpec — thay đổi bất kỳ cái nào cũng phải validate lại.
        private static readonly HashSet<string> SpecInputs = new HashSet<string>
        {
            nameof(Rows), nameof(Columns), nameof(Spacing), nameof(CellSize),
            nameof(TextHeight), nameof(LayerName), nameof(LabelStyle)
        };

        [ObservableProperty] private int _rows = 3;
        [ObservableProperty] private int _columns = 4;
        [ObservableProperty] private double _spacing = 20;
        [ObservableProperty] private double _cellSize = 15;
        [ObservableProperty] private double _textHeight = 4;
        [ObservableProperty] private string _layerName = "SAMPLE_GRID";
        [ObservableProperty] private GridLabelStyle _labelStyle = GridLabelStyle.LetterNumber;
        [ObservableProperty] private double _originX;
        [ObservableProperty] private double _originY;

        /// <summary>Các lỗi validate hiện tại, nối thành một chuỗi để hiển thị.</summary>
        [ObservableProperty] private string _validationSummary = string.Empty;

        /// <summary>True khi input đang có lỗi.</summary>
        [ObservableProperty] private bool _hasValidationErrors;

        [ObservableProperty] private GridMarkerStatus _status = GridMarkerStatus.Idle;
        [ObservableProperty] private string _statusMessage = "Sẵn sàng";

        /// <summary>Số ô Grid Marker hiện có trong bản vẽ (đọc qua XData).</summary>
        [ObservableProperty] private int _markerCount;

        /// <summary>Danh sách lựa chọn cho ComboBox kiểu nhãn.</summary>
        public IReadOnlyList<GridLabelStyle> LabelStyles { get; } =
            new[] { GridLabelStyle.LetterNumber, GridLabelStyle.Sequential };

        public GridMarkerViewModel(IGridMarkerService service, IEventAggregator events)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            if (events == null) throw new ArgumentNullException(nameof(events));

            Title = "Grid Marker";

            // Lưới có thể được vẽ từ lệnh SAMPLE_GRID — palette vẫn cập nhật số đếm nhờ message.
            TrackSubscription(events.Subscribe<GridDrawnMessage>(_ => RefreshCount()));

            Revalidate();
        }

        /// <summary>Tạo GridSpec từ input hiện tại.</summary>
        public GridSpec BuildSpec()
            => new GridSpec(Rows, Columns, Spacing, CellSize, TextHeight, LayerName, LabelStyle);

        // ─── Commands ─────────────────────────────────────────────────────────────

        private bool CanDraw() => !HasValidationErrors && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanDraw))]
        private void Draw()
        {
            RunSafe(() =>
            {
                GridSpec spec = BuildSpec();
                var cells = GridPlanner.Plan(spec, OriginX, OriginY);

                CadResult<int> result = _service.Draw(spec, cells);
                if (result.IsSuccess)
                    SetStatus(GridMarkerStatus.Success, $"Đã vẽ {result.Value} ô.");
                else
                    SetStatus(GridMarkerStatus.Error, $"Không vẽ được: {result.ErrorMessage}");
            }, "Lỗi vẽ lưới");

            // RunSafe đưa exception bất ngờ vào ErrorMessage — phản ánh lên Status luôn.
            if (!string.IsNullOrEmpty(ErrorMessage))
                SetStatus(GridMarkerStatus.Error, ErrorMessage);
        }

        [RelayCommand]
        private void RefreshCount()
        {
            try
            {
                MarkerCount = _service.CountMarkers();
            }
            catch (Exception ex)
            {
                // Ví dụ: không có bản vẽ nào đang mở — không phải lỗi nghiêm trọng của palette.
                MarkerCount = 0;
                SetStatus(GridMarkerStatus.Error, $"Không đọc được bản vẽ: {ex.Message}");
            }
        }

        // ─── Validation ───────────────────────────────────────────────────────────

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName != null && SpecInputs.Contains(e.PropertyName))
                Revalidate();
            else if (e.PropertyName == nameof(IsBusy))
                DrawCommand.NotifyCanExecuteChanged();
        }

        private void Revalidate()
        {
            var errors = BuildSpec().Validate();
            ValidationSummary = string.Join(Environment.NewLine, errors);
            HasValidationErrors = errors.Count > 0;
            DrawCommand.NotifyCanExecuteChanged();
        }

        private void SetStatus(GridMarkerStatus status, string message)
        {
            Status = status;
            StatusMessage = message;
        }
    }
}
