using System;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(CadCoreFramework.HeadlessLab.LabApp))]

namespace CadCoreFramework.HeadlessLab
{
    /// <summary>
    /// Ghi lại thread mà AutoCAD dùng để gọi Initialize() — kiểm chứng giả định của Core/CadThread
    /// (plugin ghi nhận main thread trong AppBootstrapper.Initialize) trên AutoCAD THẬT.
    /// </summary>
    public sealed class LabApp : IExtensionApplication
    {
        /// <summary>Thread id lúc AutoCAD gọi Initialize (0 = chưa gọi).</summary>
        internal static int InitializeThreadId { get; private set; }

        public void Initialize() => InitializeThreadId = Environment.CurrentManagedThreadId;

        public void Terminate() { }
    }
}
