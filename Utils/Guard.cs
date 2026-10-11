using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Lớp tiện ích kiểm tra điều kiện đầu vào tập trung (Precondition Checks).
    /// Giúp code gọn gàng hơn và thông báo lỗi rõ ràng khi vi phạm điều kiện.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Đảm bảo đối tượng không được null. Nếu null sẽ ném ArgumentNullException.
        /// </summary>
        public static T NotNull<T>(T value, string paramName) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
            return value;
        }

        /// <summary>
        /// Đảm bảo chuỗi không bị rỗng hoặc chỉ chứa khoảng trắng. Nếu có sẽ ném ArgumentException.
        /// </summary>
        public static string NotEmpty(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Giá trị không được rỗng hoặc chỉ chứa khoảng trắng.", paramName);
            }
            return value;
        }

        /// <summary>
        /// Đảm bảo ObjectId của AutoCAD hợp lệ (không phải ObjectId.Null).
        /// </summary>
        public static ObjectId NotNull(ObjectId id, string paramName)
        {
            if (id.IsNull)
            {
                throw new ArgumentException("ObjectId không hợp lệ (IsNull = true).", paramName);
            }
            return id;
        }

        /// <summary>
        /// Đảm bảo một điều kiện boolean là true.
        /// </summary>
        public static void IsTrue(bool condition, string paramName, string message)
        {
            if (!condition)
            {
                throw new ArgumentException(message, paramName);
            }
        }
    }
}
