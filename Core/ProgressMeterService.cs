using System;
using Autodesk.AutoCAD.Runtime;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Triển khai IProgressMeterService sử dụng AutoCAD ProgressMeter API.
    /// Thanh tiến trình này sẽ hiển thị ở góc dưới cùng bên phải màn hình AutoCAD (Status Bar).
    /// </summary>
    public class ProgressMeterService : IProgressMeterService, IDisposable
    {
        private ProgressMeter _progressMeter;
        private bool _isRunning = false;

        public void Start(string title, int totalLimit)
        {
            Stop(); // Đảm bảo meter cũ (nếu có) bị dừng
            
            _progressMeter = new ProgressMeter();
            _progressMeter.Start(title);
            _progressMeter.SetLimit(totalLimit);
            _isRunning = true;
        }

        public void SetLimit(int max)
        {
            if (_isRunning && _progressMeter != null)
            {
                _progressMeter.SetLimit(max);
            }
        }

        public void MeterProgress()
        {
            if (_isRunning && _progressMeter != null)
            {
                _progressMeter.MeterProgress();
                // System.Windows.Forms.Application.DoEvents() có thể dùng nếu muốn màn hình nhạy hơn
                // nhưng với AutoCAD 2015+ thì không khuyến khích (gây crash).
                // AutoCAD ProgressMeter tự lo việc update UI.
            }
        }

        public void Stop()
        {
            if (_isRunning && _progressMeter != null)
            {
                _progressMeter.Stop();
                _progressMeter.Dispose();
                _progressMeter = null;
                _isRunning = false;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
