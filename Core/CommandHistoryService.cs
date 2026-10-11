using System;
using System.Collections.Generic;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Triển khai dịch vụ ICommandHistoryService lưu trên bộ nhớ (In-memory) và an toàn đa luồng (thread-safe).
    /// Giữ lại N bản ghi lệnh gần nhất (mặc định N = 50) theo cơ chế hàng đợi vòng tròn:
    /// bản ghi cũ nhất sẽ bị xóa bỏ khi bộ nhớ đạt sức chứa tối đa.
    /// </summary>
    public class CommandHistoryService : ICommandHistoryService
    {
        private readonly object _lock = new object();
        private readonly LinkedList<CommandRecord> _records = new LinkedList<CommandRecord>();

        /// <inheritdoc/>
        public int Capacity { get; }

        /// <inheritdoc/>
        public IReadOnlyList<CommandRecord> RecentCommands
        {
            get
            {
                lock (_lock)
                    return new List<CommandRecord>(_records);
            }
        }

        /// <summary>
        /// Khởi tạo dịch vụ lịch sử lệnh với sức chứa tối đa.
        /// </summary>
        /// <param name="capacity">
        /// Số lượng bản ghi lệnh tối đa được lưu trữ. Mặc định là 50.
        /// </param>
        public CommandHistoryService(int capacity = 50)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Sức chứa (Capacity) phải lớn hơn 0.");
            Capacity = capacity;
        }

        /// <inheritdoc/>
        public void Record(string commandName, TimeSpan duration, bool success)
        {
            if (string.IsNullOrWhiteSpace(commandName)) return;

            var record = new CommandRecord(commandName, DateTime.UtcNow, duration, success);

            lock (_lock)
            {
                // Chèn vào đầu danh sách để RecentCommands[0] luôn là bản ghi mới nhất.
                _records.AddFirst(record);

                // Loại bỏ bản ghi cũ nhất khi vượt quá sức chứa tối đa.
                if (_records.Count > Capacity)
                    _records.RemoveLast();
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            lock (_lock)
                _records.Clear();
        }
    }
}
