using System;
using UnityEngine;

namespace Survival.Combat.StatusEffects
{
    /// <summary>
    /// Mô tả một hiệu ứng kéo dài bám lên mục tiêu (độc, làm chậm, choáng...).
    ///
    /// Là class thường có <c>[Serializable]</c> chứ không phải ScriptableObject, để nhúng
    /// thẳng vào <c>EnemyConfigSO</c> bằng <c>[SerializeReference]</c> — nhờ vậy chọn được
    /// loại hiệu ứng qua danh sách xổ xuống ngay trên Inspector.
    ///
    /// Hiện chỉ có <see cref="PoisonEffect"/>. Thêm hiệu ứng mới = viết thêm một lớp con;
    /// đạn, quái và <see cref="StatusEffectHandler"/> không phải sửa gì.
    /// </summary>
    [Serializable]
    public abstract class StatusEffectDefinition
    {
        [SerializeField, Tooltip(
            "Hiệu ứng BÁM TRÊN NGƯỜI suốt thời gian còn dính, ví dụ làn khói độc.\n\n" +
            "Nó được gắn làm con của mục tiêu nên đi theo mục tiêu, và bị gỡ đúng lúc hiệu ứng " +
            "hết hạn. Đây là cách người chơi biết mình ĐANG dính độc và còn dính bao lâu — " +
            "khác hẳn với vụ nổ lúc đạn chạm, vốn chỉ báo 'vừa bị trúng' rồi thôi.\n\n" +
            "Prefab dùng ở đây phải TẮT ô tự trả về pool trong PooledVfx, vì chỉ hệ thống " +
            "hiệu ứng trạng thái mới biết khi nào nên gỡ nó ra.")]
        private NFramework.PooledObject _activeVfx;

        [SerializeField, Min(0f), Tooltip("Nâng hiệu ứng lên khỏi chân mục tiêu bao nhiêu unit, để nó quấn quanh thân chứ không nằm dưới đất.")]
        private float _activeVfxHeight = 0.7f;

        public NFramework.PooledObject ActiveVfx => _activeVfx;
        public float ActiveVfxHeight => _activeVfxHeight;

        /// <summary>
        /// Khoá định danh loại hiệu ứng. Hai lần dính CÙNG một khoá thì được gộp làm một
        /// (làm mới thời gian) thay vì chạy song song — đây chính là luật "không stack" của spec.
        /// </summary>
        public abstract string Id { get; }

        /// <summary>Tạo bản trạng thái riêng cho một mục tiêu cụ thể.</summary>
        public abstract StatusEffectRuntime CreateRuntime(StatusEffectContext context);
    }

    /// <summary>Thông tin cần để một hiệu ứng hoạt động trên một mục tiêu.</summary>
    public class StatusEffectContext
    {
        public IDamageable Target;
        public GameObject Instigator;
    }

    /// <summary>Trạng thái lúc chạy của một hiệu ứng đang bám trên mục tiêu.</summary>
    public abstract class StatusEffectRuntime
    {
        protected readonly StatusEffectContext Context;

        protected StatusEffectRuntime(StatusEffectContext context)
        {
            Context = context;
        }

        /// <summary>Hiệu ứng đã hết hạn và có thể gỡ bỏ.</summary>
        public abstract bool IsFinished { get; }

        /// <summary>Gọi ngay khi hiệu ứng vừa bám vào, hoặc khi bị đánh trúng lại.</summary>
        public abstract void OnApplied(bool isRefresh);

        public abstract void Tick(float deltaTime);
    }
}
