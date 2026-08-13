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
