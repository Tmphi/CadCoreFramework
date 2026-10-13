using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using CadCoreFramework.Core;

namespace CadCoreFramework.HeadlessLab
{
    /// <summary>Logger cho lab: giữ lại lỗi để kiểm thử in vào lý do FAIL.</summary>
    internal sealed class LabLogger : ILoggerService
    {
        public List<string> Errors { get; } = new List<string>();

        public void Debug(string message) { }
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception ex = null)
            => Errors.Add(ex == null ? message : $"{message} [{ex.GetType().Name}: {ex.Message} @ {TopFrames(ex, 3)}]");

        private static string TopFrames(Exception ex, int count)
        {
            string[] lines = (ex.StackTrace ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" <- ", lines, 0, Math.Min(count, lines.Length)).Replace("   at ", "");
        }
        public void Performance(string operationName, TimeSpan elapsed) { }
    }

    /// <summary>Notification cho lab: ghi ra command line của bản vẽ (accoreconsole in ra stdout).</summary>
    internal sealed class LabNotification : INotificationService
    {
        private readonly Document _doc;
        public LabNotification(Document doc) { _doc = doc; }

        public void Info(string message) => _doc.Editor.WriteMessage("\n[lab] " + message);
        public void Warn(string message) => _doc.Editor.WriteMessage("\n[lab] WARN " + message);
        public void Error(string message, Exception ex = null) => _doc.Editor.WriteMessage("\n[lab] ERROR " + message);
        public bool Confirm(string message, string title = "Xác nhận") => true;
        public void Section(string title) { }
    }
}
