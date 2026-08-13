namespace Survival.Stats
{
    /// <summary>
    /// Mọi chỉ số của nhân vật và kẻ địch đều được định danh bằng enum này.
    /// Thêm một chỉ số mới cho toàn game = thêm đúng một dòng ở đây, không phải sửa logic.
    ///
    /// Giá trị số được ghi tường minh để việc sắp xếp lại thứ tự trong tương lai
    /// không làm hỏng dữ liệu đã serialize trong các file config.
    /// </summary>
    public enum EStatType
    {
        /// <summary>Máu tối đa.</summary>
        MaxHealth = 0,

        /// <summary>Tốc độ di chuyển, đơn vị unit/giây.</summary>
        MoveSpeed = 1,

        /// <summary>Tốc độ xoay thân, đơn vị độ/giây.</summary>
        RotationSpeed = 2,

        /// <summary>Giáp. Trừ thẳng vào sát thương nhận được.</summary>
        Armor = 3,

        /// <summary>Hệ số nhân sát thương gây ra. 0 nghĩa là x1.0, 0.1 nghĩa là x1.1.</summary>
        DamageMultiplier = 4,
    }
}
