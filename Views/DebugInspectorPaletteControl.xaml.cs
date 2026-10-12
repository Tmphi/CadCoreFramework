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
            // Fail soft when the plugin is unloaded or DI not yet initialized —
            // palette just shows empty data, no exceptions bubble up to AutoCAD.
            DataContext = AppBootstrapper.ServiceProvider?.GetService<DebugInspectorViewModel>();
        }
    }
}
