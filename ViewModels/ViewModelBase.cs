using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CadCoreFramework.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CadCoreFramework.ViewModels
{
    /// <summary>
    /// Lớp cơ sở cho tất cả các ViewModels, kế thừa từ ObservableObject của MVVM Toolkit.
    /// Implement IBusyIndicator để tương thích với PaletteService.RunWithBusyAsync().
    /// Implement IDisposable để tự động giải phóng các subscription EventAggregator.
    ///
    /// CÁC TÍNH NĂNG SẴN CÓ:
    /// - IsBusy: Trạng thái chờ (hiển thị loading spinner trên UI).
    /// - Title: Tiêu đề của View/Panel.
    /// - ErrorMessage: Thông báo lỗi hiển thị trên UI.
    /// - RunSafeAsync(): Chạy code async với try-catch tự động, tự bật/tắt IsBusy.
    /// - TrackSubscription(): Đăng ký IDisposable sẽ được tự động Dispose khi VM bị hủy.
    ///
    /// QUAN TRỌNG (WPF + AutoCAD):
    /// - Tất cả các thay đổi property phải diễn ra trên UI thread.
    ///   Nếu gọi từ background thread, dùng Application.Current.Dispatcher.Invoke().
    /// - Tuyệt đối không gọi AutoCAD API từ ViewModel — mọi thao tác CAD phải thông qua
    ///   ICadContext được inject vào ViewModel.
    /// </summary>
    public abstract partial class ViewModelBase : ObservableObject, IBusyIndicator, IDisposable
    {
        /// <summary>
        /// Trạng thái đang bận/đang tải của ViewModel.
        /// </summary>
        [ObservableProperty]
        private bool _isBusy;

        /// <summary>
        /// Tiêu đề của View hoặc Panel liên kết với ViewModel.
        /// </summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>
        /// Thông điệp lỗi hiện hành để hiển thị trên giao diện người dùng.
        /// </summary>
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private bool _disposed;

        /// <summary>
        /// Logger nhận MỌI exception mà RunSafe/RunSafeAsync bắt được (null = chỉ hiện ErrorMessage).
        /// ErrorMessage chỉ có message — stack trace cần cho việc điều tra nằm trong log.
        /// </summary>
        protected ILoggerService? Logger { get; }

        /// <summary>
        /// Khởi tạo một phiên bản ViewModelBase.
        /// </summary>
        protected ViewModelBase() { }

        /// <summary>
        /// Khởi tạo ViewModelBase có logger. Lớp con nhận <c>ILoggerService? logger = null</c> trong
        /// constructor rồi chuyển vào đây — DI tự inject, unit test có thể bỏ qua.
        /// </summary>
        protected ViewModelBase(ILoggerService? logger) { Logger = logger; }

        /// <summary>
        /// Đăng ký một IDisposable (thường là EventAggregator subscription) để tự động
        /// Dispose khi ViewModel bị hủy, tránh memory leak do event không được unsubscribe.
        /// </summary>
        protected void TrackSubscription(IDisposable subscription)
        {
            if (subscription != null) _subscriptions.Add(subscription);
        }

        /// <summary>
        /// Giải phóng tất cả subscription đã đăng ký qua TrackSubscription().
        /// Override trong lớp con nếu cần giải phóng thêm tài nguyên — nhớ gọi base.Dispose().
        /// </summary>
        public virtual void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var s in _subscriptions)
                try { s.Dispose(); } catch { }
            _subscriptions.Clear();
        }

        /// <summary>
        /// Chạy một hàm async với bảo vệ try-catch và tự động quản lý IsBusy.
        /// Khi chạy, IsBusy = true (hiển thị spinner). Khi hoàn thành (dù lỗi hay không), IsBusy = false.
        /// Nếu có lỗi, ErrorMessage được cập nhật tự động.
        ///
        /// Cách dùng trong Command/RelayCommand của ViewModel:
        /// <code>
        /// [RelayCommand]
        /// private async Task LoadData()
        ///     => await RunSafeAsync(async () =>
        ///     {
        ///         var result = await _dataService.GetDataAsync();
        ///         MyItems = new ObservableCollection&lt;MyItem&gt;(result);
        ///     });
        /// </code>
        /// </summary>
        protected async Task RunSafeAsync(Func<Task> asyncAction, string? errorPrefix = null)
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            try
            {
                await asyncAction();
            }
            catch (Exception ex)
            {
                ReportError(ex, errorPrefix);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Biến thể đồng bộ của RunSafeAsync cho các thao tác không async.
        /// ⚠️ IsBusy bật rồi tắt trong CÙNG một lượt chạy của UI thread → WPF không kịp vẽ spinner.
        /// Cần hiển thị trạng thái bận cho việc lâu → dùng RunSafeAsync với phần nặng chạy ngoài UI thread.
        /// </summary>
        protected void RunSafe(Action action, string? errorPrefix = null)
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ReportError(ex, errorPrefix);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ReportError(Exception ex, string? errorPrefix)
        {
            ErrorMessage = string.IsNullOrEmpty(errorPrefix)
                ? ex.Message
                : $"{errorPrefix}: {ex.Message}";
            Logger?.Error($"{GetType().Name}: {(string.IsNullOrEmpty(errorPrefix) ? "lỗi trong RunSafe" : errorPrefix)}", ex);
        }
    }
}
