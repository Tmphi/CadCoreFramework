using System.Windows.Controls;
using CadCoreFramework.Core;
using CadCoreFramework.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.Views
{
    /// <summary>
    /// Code-behind for the Debug Inspector palette. Resolves its ViewModel
    /// from DI at construction time — the palette is created on demand
    /// (CORE_DEBUG command) and lives only until the user closes it.
    /// </summary>
    public partial class DebugInspectorPaletteControl : UserControl
    {
        public DebugInspectorPaletteControl()
        {
            InitializeComponent();
            // Fail loud: palette được tạo bên trong ExecuteCommand (CORE_DEBUG → TogglePalette),
            // nên exception được bắt, in ra và ghi log thay vì hiện một palette rỗng khó hiểu.
            DataContext = AppBootstrapper.ServiceProvider?.GetService<DebugInspectorViewModel>()
                ?? throw new System.InvalidOperationException(
                    "DebugInspectorViewModel chưa được đăng ký trong AppBootstrapper.ConfigureServices.");
        }
    }
}
