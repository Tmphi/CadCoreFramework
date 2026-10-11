using System;
using System.Text;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

// XDataHelper là một lớp tĩnh thuần C#: không gọi trực tiếp API AutoCAD.
// Chúng ta có thể kiểm thử logic chia nhỏ (chunking) và tuần tự hóa JSON mà không cần cài đặt AutoCAD.

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Các bài kiểm thử đơn vị cho logic xử lý dữ liệu và chia nhỏ chunk của XDataHelper.
    /// </summary>
    public class XDataHelperTests
    {
        // ─── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Mô phỏng logic chia nhỏ chunk của XDataHelper.SetMetadata một cách độc lập.
        /// Tái hiện quy trình mã nguồn thực tế mà không cần đụng tới các kiểu dữ liệu của AutoCAD.
        /// </summary>
        private static string[] ChunkJson<T>(T data, int chunkSize = 250)
        {
            string json = JsonConvert.SerializeObject(data,
                new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii });

            var chunks = new System.Collections.Generic.List<string>();
            for (int i = 0; i < json.Length; i += chunkSize)
                chunks.Add(json.Substring(i, Math.Min(chunkSize, json.Length - i)));

            return chunks.ToArray();
        }

        /// <summary>
        /// Mô phỏng logic ghép lại các chunk của XDataHelper.GetMetadata.
        /// </summary>
        private static T ReassembleChunks<T>(string[] chunks)
        {
            var sb = new StringBuilder();
            foreach (var chunk in chunks)
                sb.Append(chunk);

            return JsonConvert.DeserializeObject<T>(sb.ToString())!;
        }

        // ─── Chunking Tests ───────────────────────────────────────────────────────

        [Fact]
        public void ChunkJson_ShortPayload_ProducesSingleChunk()
        {
            var data = new { Name = "Motor", Power = 7.5 };

            var chunks = ChunkJson(data);

            chunks.Should().HaveCount(1);
            chunks[0].Length.Should().BeLessOrEqualTo(250);
        }

        [Fact]
        public void ChunkJson_LongPayload_ProducesMultipleChunks()
        {
            // Tạo gói dữ liệu vượt quá 250 ký tự khi được tuần tự hóa.
            var data = new
            {
                Name = new string('A', 100),
                Description = new string('B', 100),
                Location = new string('C', 100)
            };

            var chunks = ChunkJson(data);

            chunks.Should().HaveCountGreaterThan(1,
                because: "gói dữ liệu dài hơn 250 ký tự phải được chia thành nhiều phần");
            foreach (var chunk in chunks)
                chunk.Length.Should().BeLessOrEqualTo(250,
                    because: "mỗi chunk phải vừa vặn trong giới hạn chuỗi XData của AutoCAD");
        }

        [Fact]
        public void ChunkJson_AllChunksCombined_ReconstitutesOriginalData()
        {
            var original = new { Name = "Panel LV-01", Voltage = 400, Current = 630.0 };

            var chunks = ChunkJson(original);
            var restored = ReassembleChunks<dynamic>(chunks);

            ((string)restored.Name).Should().Be(original.Name);
            ((int)restored.Voltage).Should().Be(original.Voltage);
        }

        // ─── Unicode / Vietnamese Tests ───────────────────────────────────────────

        [Fact]
        public void ChunkJson_UnicodeContent_EscapedToAsciiSafely()
        {
            var data = new { Label = "Tủ điện hạ thế" }; // Tiếng Việt

            var chunks = ChunkJson(data);

            // Mã nguồn thực tế dùng EscapeNonAscii → không có ký tự đa byte thô trong chunk.
            foreach (var chunk in chunks)
                foreach (char c in chunk)
                    ((int)c).Should().BeLessOrEqualTo(127,
                        because: "EscapeNonAscii đảm bảo mọi chunk đều là ký tự ASCII thuần");
        }

        [Fact]
        public void ChunkJson_UnicodeRoundtrip_ReconstitutesCorrectly()
        {
            var original = new { Label = "Máy biến áp 400kVA" };

            var chunks = ChunkJson(original);
            var restored = ReassembleChunks<dynamic>(chunks);

            ((string)restored.Label).Should().Be(original.Label,
                because: "chuỗi JSON escape dạng \\uXXXX phải giải mã lại đúng chuỗi Unicode ban đầu");
        }

        // ─── Edge Cases ───────────────────────────────────────────────────────────

        [Fact]
        public void ChunkJson_EmptyObject_ProducesSingleSmallChunk()
        {
            var data = new { };

            var chunks = ChunkJson(data);

            chunks.Should().HaveCount(1);
            chunks[0].Should().Be("{}");
        }

        [Fact]
        public void ChunkJson_ExactlyAtBoundary_ProducesTwoChunks()
        {
            // Tạo chuỗi tuần tự hóa chính xác 251 ký tự → phải chia thành 2 chunk.
            // "{\"V\":\"" = 6 ký tự; đóng "\"}" = 2 ký tự → cần 243 ký tự giá trị.
            string value = new string('X', 243);
            var data = new { V = value };
            string json = JsonConvert.SerializeObject(data);
            json.Length.Should().BeGreaterThan(250, because: "kiểm tra tính hợp lệ: gói dữ liệu phải vượt quá giới hạn");

            var chunks = ChunkJson(data);

            chunks.Should().HaveCountGreaterThanOrEqualTo(2);
        }
    }
}
