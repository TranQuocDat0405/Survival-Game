using System;
using System.Collections.Generic;
using UnityEngine;

namespace Survival.Stats
{
    /// <summary>
    /// Kho chỉ số lúc chạy game của một nhân vật (player hoặc quái).
    ///
    /// Cách lưu: một mảng float, đánh chỉ số bằng chính giá trị của <see cref="EStatType"/>.
    /// Lý do không dùng Dictionary: khi dùng enum làm khoá của Dictionary, .NET phải "boxing"
    /// (đóng gói giá trị enum thành object trên heap) mỗi lần tra cứu, sinh rác cho bộ dọn rác GC.
    /// Mảng thì tra cứu bằng chỉ số nguyên — không cấp phát bộ nhớ, không sinh rác.
    /// Việc này quan trọng vì chỉ số được đọc mỗi khung hình (tốc độ di chuyển, tốc độ xoay).
    /// </summary>
    public class StatSet : IStatProvider
    {
        private static readonly int StatCount = Enum.GetValues(typeof(EStatType)).Length;

        private readonly float[] _values = new float[StatCount];

        /// <summary>Bắn ra mỗi khi một chỉ số đổi giá trị. UI dùng cái này thay vì đọc liên tục trong Update.</summary>
        public event Action<EStatType, float> OnStatChanged;

        public StatSet() { }

        public StatSet(IReadOnlyList<StatModifier> baseStats)
        {
            SetBase(baseStats);
        }

        /// <summary>Ghi đè toàn bộ chỉ số bằng bộ giá trị gốc. Dùng khi sinh ra hoặc khi lấy quái từ pool.</summary>
        public void SetBase(IReadOnlyList<StatModifier> baseStats)
        {
            Array.Clear(_values, 0, _values.Length);
            if (baseStats == null)
                return;

            for (int i = 0; i < baseStats.Count; i++)
            {
                var modifier = baseStats[i];
                _values[(int)modifier.Type] = modifier.Value;
            }

            for (int i = 0; i < _values.Length; i++)
                OnStatChanged?.Invoke((EStatType)i, _values[i]);
        }

        public float Get(EStatType type) => _values[(int)type];

        public void Set(EStatType type, float value)
        {
            int index = (int)type;
            if (Mathf.Approximately(_values[index], value))
                return;

            _values[index] = value;
            OnStatChanged?.Invoke(type, value);
        }

        public void Add(EStatType type, float delta)
        {
            if (Mathf.Approximately(delta, 0f))
                return;

            int index = (int)type;
            _values[index] += delta;
            OnStatChanged?.Invoke(type, _values[index]);
        }

        /// <summary>
        /// Cộng dồn một danh sách phần thưởng. Đây chính là thứ hệ thống lên cấp gọi:
        /// truyền vào danh sách [+40 MaxHealth, +2 Armor, +0.1 DamageMultiplier] lấy từ file config.
        /// Thêm loại phần thưởng mới cho mỗi cấp = sửa file config, không sửa code.
        /// </summary>
        public void ApplyAll(IReadOnlyList<StatModifier> modifiers)
        {
            if (modifiers == null)
                return;

            for (int i = 0; i < modifiers.Count; i++)
                Add(modifiers[i].Type, modifiers[i].Value);
        }
    }
}
