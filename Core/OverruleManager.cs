using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Giao diện cho mọi Custom Overrule (DrawOverrule, TransformOverrule, GripOverrule...).
    /// Đảm bảo tính nhất quán khi đăng ký và gỡ bỏ Overrule theo chuẩn Autodesk.
    /// Dev sẽ tạo class kế thừa từ DrawOverrule (hoặc khác) và implement interface này.
    /// </summary>
    public interface ICadOverrule
    {
        /// <summary>Đăng ký Overrule với hệ thống AutoCAD.</summary>
        void Register();

        /// <summary>Hủy đăng ký Overrule khỏi hệ thống AutoCAD.</summary>
        void Unregister();
    }

    /// <summary>
    /// Trình quản lý toàn cầu cho Overrule, đảm bảo an toàn bộ nhớ.
    /// Khi Plugin Unload hoặc tắt ứng dụng, bắt buộc phải Remove toàn bộ Overrule
    /// nếu không AutoCAD sẽ bị Fatal Error.
    /// </summary>
    public class OverruleManager : IDisposable
    {
        private readonly List<ICadOverrule> _activeOverrules = new List<ICadOverrule>();
        private readonly ILoggerService _logger;

        public OverruleManager(ILoggerService logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Bật/Tắt chế độ chạy Overrule toàn cầu của AutoCAD.
        /// </summary>
        public bool IsOverruling
        {
            get => Overrule.Overruling;
            set => Overrule.Overruling = value;
        }

        /// <summary>
        /// Thêm và đăng ký một Overrule mới vào AutoCAD.
        /// </summary>
        public void AddAndRegister(ICadOverrule overrule)
        {
            if (overrule == null) return;
            overrule.Register();
            _activeOverrules.Add(overrule);
            Overrule.Overruling = true; // Đảm bảo Engine Overrule được kích hoạt
        }

        /// <summary>
        /// Hủy đăng ký toàn bộ Overrule đang chạy (Dùng trong IEventService.Shutdown).
        /// </summary>
        public void UnregisterAll()
        {
            foreach (var overrule in _activeOverrules)
            {
                try
                {
                    overrule.Unregister();
                }
                catch (System.Exception ex)
                {
                    // Không ném ra trong lúc Shutdown — nhưng ghi lại để biết Overrule nào chưa gỡ được
                    _logger?.Error($"Không gỡ được Overrule {overrule.GetType().Name}.", ex);
                }
            }
            _activeOverrules.Clear();
        }

        /// <summary>
        /// Giải phóng toàn bộ Overrule đang đăng ký.
        /// </summary>
        public void Dispose()
        {
            UnregisterAll();
        }
    }
}
