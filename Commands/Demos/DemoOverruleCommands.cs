using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.Commands.Demos
{
    /// <summary>
    /// Overrule tùy biến: Tự động vẽ thêm dấu thập tâm màu đỏ cho TẤT CẢ Circle trong bản vẽ
    /// mà không làm biến đổi dữ liệu thực thể trong Database.
    /// Kế thừa DrawableOverrule và implement ICadOverrule để OverruleManager tự động dọn dẹp khi thoát.
    /// </summary>
    public class DemoCircleCenterOverrule : DrawableOverrule, ICadOverrule
    {
        private static DemoCircleCenterOverrule _instance;
        public static DemoCircleCenterOverrule Instance => _instance ?? (_instance = new DemoCircleCenterOverrule());

        private bool _isRegistered = false;

        public void Register()
        {
            if (_isRegistered) return;
            Overrule.AddOverrule(RXObject.GetClass(typeof(Circle)), this, false);
            _isRegistered = true;
        }

        public void Unregister()
        {
            if (!_isRegistered) return;
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(Circle)), this);
            _isRegistered = false;
        }

        public override bool WorldDraw(Drawable drawable, WorldDraw wd)
        {
            // 1. Gọi vẽ đối tượng gốc
            base.WorldDraw(drawable, wd);

            // 2. Vẽ thêm hiệu ứng ảo (Dấu thập tâm màu đỏ)
            if (drawable is Circle circle)
            {
                wd.SubEntityTraits.Color = 1; // Màu đỏ ACI
                Point3d c = circle.Center;
                double crossSize = Math.Max(circle.Radius * 0.4, 2.0);

                wd.Geometry.WorldLine(new Point3d(c.X - crossSize, c.Y, c.Z), new Point3d(c.X + crossSize, c.Y, c.Z));
                wd.Geometry.WorldLine(new Point3d(c.X, c.Y - crossSize, c.Z), new Point3d(c.X, c.Y + crossSize, c.Z));
            }

            return true;
        }
    }

    /// <summary>
    /// Các lệnh minh họa hệ thống Overrule của AutoCAD (can thiệp hiển thị mà không sửa Database).
    /// </summary>
    public class DemoOverruleCommands : CadCommandBase
    {
        private INotificationService Notify
            => AppBootstrapper.ServiceProvider?.GetService<INotificationService>();

        private OverruleManager OverruleMgr
            => AppBootstrapper.ServiceProvider?.GetService<OverruleManager>();

        private static bool _isEnabled = false;

        /// <summary>
        /// DEMO_OVERRULE_TOGGLE — Bật/Tắt hiệu ứng Overrule hiển thị tâm đường tròn màu đỏ.
        /// Minh họa kiến trúc OverruleManager theo tiêu chuẩn an toàn của Autodesk.
        /// </summary>
        [CommandMethod("DEMO_OVERRULE_TOGGLE")]
        public void DemoOverruleToggle()
        {
            Notify?.Section("DEMO: OVERRULE MANAGER — CAN THIỆP HIỂN THỊ ĐỒ HỌA");

            ExecuteCommand(() =>
            {
                if (OverruleMgr == null)
                {
                    Notify?.Error("OverruleManager chưa được đăng ký trong DI.");
                    return;
                }

                if (!_isEnabled)
                {
                    var overrule = DemoCircleCenterOverrule.Instance;
                    OverruleMgr.AddAndRegister(overrule);
                    OverruleMgr.IsOverruling = true;
                    _isEnabled = true;

                    // Làm mới màn hình đồ họa để hiển thị hiệu ứng
                    Editor?.Regen();

                    Notify?.Info("✔ ĐÃ BẬT Overrule! Mọi đường tròn Circle trên bản vẽ đều xuất hiện tâm chữ thập màu đỏ.");
                    Notify?.Info("  (Database đối tượng hoàn toàn không bị sửa đổi).");
                }
                else
                {
                    OverruleMgr.UnregisterAll();
                    _isEnabled = false;

                    Editor?.Regen();

                    Notify?.Info("✘ ĐÃ TẮT Overrule! Các đường tròn trở về hiển thị nguyên bản.");
                }
            });
        }
    }
}
