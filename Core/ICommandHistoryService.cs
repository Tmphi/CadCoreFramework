using System;
using System.Collections.Generic;

namespace CadCoreFramework.Core
{
    // ─── Model ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Đại diện cho thông tin một lần thực thi lệnh AutoCAD được ghi nhận.
    /// </summary>
    public class CommandRecord
    {
        /// <summary>Tên lệnh AutoCAD được đăng ký với thuộc tính [CommandMethod].</summary>
        public string CommandName { get; }

        /// <summary>Thời điểm bắt đầu thực thi lệnh (theo chuẩn UTC).</summary>
        public DateTime StartedAtUtc { get; }

        /// <summary>Thời gian chạy để hoàn thành lệnh.</summary>
        public TimeSpan Duration { get; }

        /// <summary>True nếu lệnh hoàn thành thành công mà không ném ra ngoại lệ.</summary>
        public bool Success { get; }

        public CommandRecord(string commandName, DateTime startedAtUtc, TimeSpan duration, bool success)
        {
            CommandName  = commandName;
            StartedAtUtc = startedAtUtc;
            Duration     = duration;
            Success      = success;
        }

        public override string ToString()
            => $"[{(Success ? "OK" : "FAIL")}] {CommandName} ({Duration.TotalMilliseconds:F0} ms) @ {StartedAtUtc:HH:mm:ss}";
    }

    // ─── Interface ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Theo dõi lịch sử thực thi các lệnh AutoCAD được gọi qua plugin này.
    /// Hữu ích cho việc gỡ lỗi ("lệnh nào vừa chạy bị crash?") và
    /// hiển thị danh sách các lệnh được sử dụng gần đây (MRU) trên giao diện.
    /// </summary>
    public interface ICommandHistoryService
    {
        /// <summary>
        /// Danh sách các lệnh được ghi nhận gần đây nhất, lệnh mới nhất xếp đầu tiên.
        /// Giới hạn tối đa theo thuộc tính <see cref="Capacity"/>.
        /// </summary>
        IReadOnlyList<CommandRecord> RecentCommands { get; }

        /// <summary>Số lượng bản ghi tối đa được lưu trữ trong bộ nhớ. Mặc định: 50.</summary>
        int Capacity { get; }

        /// <summary>
        /// Ghi nhận kết quả của một lệnh vừa chạy xong.
        /// </summary>
        /// <param name="commandName">Tên lệnh AutoCAD.</param>
        /// <param name="duration">Thời lượng thực thi lệnh.</param>
        /// <param name="success">False nếu lệnh kết thúc bằng một ngoại lệ không được xử lý.</param>
        void Record(string? commandName, TimeSpan duration, bool success);

        /// <summary>Xóa toàn bộ lịch sử lệnh đã lưu.</summary>
        void Clear();
    }
}
