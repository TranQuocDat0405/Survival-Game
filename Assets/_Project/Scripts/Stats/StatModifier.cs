using System;
using UnityEngine;

namespace Survival.Stats
{
    /// <summary>
    /// Một cặp (loại chỉ số, giá trị). Đây là đơn vị dữ liệu dùng ở khắp nơi:
    /// chỉ số khởi đầu của player, chỉ số của quái, phần thưởng mỗi lần lên cấp.
    ///
    /// Vì nó là <see cref="SerializableAttribute"/>, Unity vẽ được nó thành list
    /// trên Inspector -> người tune chỉ số không cần đụng vào code.
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        [Tooltip("Chỉ số nào được tác động")]
        public EStatType Type;

        [Tooltip("Giá trị. Với chỉ số khởi đầu đây là giá trị gốc; với phần thưởng lên cấp đây là lượng cộng thêm.")]
        public float Value;

        public StatModifier(EStatType type, float value)
        {
            Type = type;
            Value = value;
        }
    }
}
