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
    ///   • Ô số bind vào CHUỖI (RowsText…), ViewModel tự đọc số bằng <see cref="NumberText"/>: dấu chấm
    ///     thập phân như dòng lệnh AutoCAD; ô gõ sai/để trống là lỗi validate, không âm thầm giữ số cũ (B-31/B-32).
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

        // Ô số trên palette bind vào các CHUỖI này, không bind thẳng vào số ở trên (B-31/B-32): WPF đổi
        // chuỗi → số theo Language mặc định en-US ("2,5" thành 25) và bỏ qua chuỗi không đọc được (giữ số cũ).
        // Giá trị ban đầu khớp số mặc định ở trên (test NewViewModel_TextsShowTheValues kiểm).
        [ObservableProperty] private string _rowsText = "3";
        [ObservableProperty] private string _columnsText = "4";
        [ObservableProperty] private string _spacingText = "20";
        [ObservableProperty] private string _cellSizeText = "15";
        [ObservableProperty] private string _textHeightText = "4";
        [ObservableProperty] private string _originXText = "0";
        [ObservableProperty] private string _originYText = "0";

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

        public GridMarkerViewModel(IGridMarkerService service, IEventAggregator events, ILoggerService? logger = null)
            : base(logger)
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
            List<string> errors = NumberInputErrors();    // ô số không đọc được (B-31/B-32)…
            errors.AddRange(BuildSpec().Validate());      // …cộng luật của GridSpec — một nguồn luật duy nhất
            ValidationSummary = string.Join(Environment.NewLine, errors);
            HasValidationErrors = errors.Count > 0;
            DrawCommand.NotifyCanExecuteChanged();
        }

        /// <summary>Lỗi của các ô số mà chuỗi không đọc được thành số, theo thứ tự trên palette.</summary>
        private List<string> NumberInputErrors()
        {
            var errors = new List<string>();
            if (!NumberText.TryParseInt(RowsText, out _)) errors.Add("Số hàng: nhập một số nguyên.");
            if (!NumberText.TryParseInt(ColumnsText, out _)) errors.Add("Số cột: nhập một số nguyên.");
            AddIfNotNumber(errors, SpacingText, "Khoảng cách");
            AddIfNotNumber(errors, CellSizeText, "Cỡ ô");
            AddIfNotNumber(errors, TextHeightText, "Cao chữ");
            AddIfNotNumber(errors, OriginXText, "Tâm A1 — X");
            AddIfNotNumber(errors, OriginYText, "Tâm A1 — Y");
            return errors;
        }

        private static void AddIfNotNumber(List<string> errors, string text, string label)
        {
            if (!NumberText.TryParseDouble(text, out _))
                errors.Add($"{label}: nhập một số, phần thập phân dùng dấu chấm (ví dụ 2.5).");
        }

        // ─── Ô số: chuỗi ↔ số ─────────────────────────────────────────────────────
        // Người dùng gõ → đọc số: đọc được thì gán vào property số (GridSpec dùng nó); không đọc được thì giữ
        // số cũ và Revalidate báo lỗi của ô đó — nút Vẽ tắt. Code gán property số (test, thiết lập đã lưu…)
        // → ô hiển thị theo, trừ khi chuỗi đang có đã ghi đúng số đó (giữ nguyên "20.0" người dùng đang gõ).

        partial void OnRowsTextChanged(string value)
        {
            if (NumberText.TryParseInt(value, out int rows)) Rows = rows;
            Revalidate();
        }

        partial void OnColumnsTextChanged(string value)
        {
            if (NumberText.TryParseInt(value, out int columns)) Columns = columns;
            Revalidate();
        }

        partial void OnSpacingTextChanged(string value)
        {
            if (NumberText.TryParseDouble(value, out double spacing)) Spacing = spacing;
            Revalidate();
        }

        partial void OnCellSizeTextChanged(string value)
        {
            if (NumberText.TryParseDouble(value, out double cellSize)) CellSize = cellSize;
            Revalidate();
        }

        partial void OnTextHeightTextChanged(string value)
        {
            if (NumberText.TryParseDouble(value, out double textHeight)) TextHeight = textHeight;
            Revalidate();
        }

        partial void OnOriginXTextChanged(string value)
        {
            if (NumberText.TryParseDouble(value, out double originX)) OriginX = originX;
            Revalidate();
        }

        partial void OnOriginYTextChanged(string value)
        {
            if (NumberText.TryParseDouble(value, out double originY)) OriginY = originY;
            Revalidate();
        }

        partial void OnRowsChanged(int value) => RowsText = Shown(RowsText, value);
        partial void OnColumnsChanged(int value) => ColumnsText = Shown(ColumnsText, value);
        partial void OnSpacingChanged(double value) => SpacingText = Shown(SpacingText, value);
        partial void OnCellSizeChanged(double value) => CellSizeText = Shown(CellSizeText, value);
        partial void OnTextHeightChanged(double value) => TextHeightText = Shown(TextHeightText, value);
        partial void OnOriginXChanged(double value) => OriginXText = Shown(OriginXText, value);
        partial void OnOriginYChanged(double value) => OriginYText = Shown(OriginYText, value);

        private static string Shown(string text, int value)
            => NumberText.TryParseInt(text, out int shown) && shown == value ? text : NumberText.Format(value);

        private static string Shown(string text, double value)
            => NumberText.TryParseDouble(text, out double shown) && shown == value ? text : NumberText.Format(value);

        private void SetStatus(GridMarkerStatus status, string message)
        {
            Status = status;
            StatusMessage = message;
        }
    }
}
