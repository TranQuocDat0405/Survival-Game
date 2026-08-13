using System;
using UnityEngine;

namespace Survival.Skills
{
    /// <summary>
    /// Trạng thái lúc chạy game của một skill: đang hồi chiêu bao lâu, còn mấy charge...
    ///
    /// Vì sao tách ra khỏi <see cref="SkillDefinition"/>?
    /// SkillDefinition là ScriptableObject — nó là một FILE trên ổ đĩa, dùng chung cho mọi nhân vật.
    /// Nếu nhét biến đếm cooldown vào đó thì hai nhân vật cùng dùng một skill sẽ dùng chung
    /// bộ đếm, và tệ hơn: giá trị bị ghi thẳng vào file, Play xong vẫn còn nguyên.
    /// Nên: Definition giữ SỐ LIỆU (bất biến), Runtime giữ TRẠNG THÁI (mỗi nhân vật một bản).
    /// </summary>
    public abstract class SkillRuntime
    {
        protected readonly SkillDefinition Definition;
        protected readonly SkillContext Context;

        protected float CooldownTimer;

        /// <summary>Bắn ra khi skill khai hoả thành công. Animator, âm thanh, camera shake nghe sự kiện này.</summary>
        public event Action<SkillRuntime> OnUsed;

        protected SkillRuntime(SkillDefinition definition, SkillContext context)
        {
            Definition = definition;
            Context = context;
        }

        public SkillDefinition Def => Definition;

        /// <summary>Số giây còn lại của cooldown. UI hiển thị con số này trên nút.</summary>
        public virtual float CooldownRemaining => Mathf.Max(0f, CooldownTimer);

        /// <summary>
        /// 0 = vừa dùng xong, 1 = sẵn sàng. Nút skill dùng giá trị này cho ảnh dạng Filled
        /// (ảnh bị che một phần theo hình quạt tròn, giống đồng hồ đếm ngược).
        /// </summary>
        public virtual float CooldownNormalized
        {
            get
            {
                if (Definition.Cooldown <= 0f)
                    return 1f;
                return Mathf.Clamp01(1f - CooldownTimer / Definition.Cooldown);
            }
        }

        /// <summary>Số charge hiện có. Trả về -1 nghĩa là skill này không dùng cơ chế charge.</summary>
        public virtual int ChargeCount => -1;

        public virtual int MaxCharges => -1;

        public virtual bool CanUse => CooldownTimer <= 0f;

        /// <summary>Gọi mỗi khung hình để đếm lùi cooldown, hồi charge, chạy hiệu ứng đang diễn ra.</summary>
        public virtual void Tick(float deltaTime)
        {
            if (CooldownTimer > 0f)
                CooldownTimer -= deltaTime;
        }

        /// <summary>
        /// Thử dùng skill. Trả về true nếu đã thực sự khai hoả.
        /// Lớp con chỉ cần cài đặt <see cref="Execute"/>, phần kiểm tra điều kiện đã lo sẵn ở đây.
        /// </summary>
        public bool TryUse()
        {
            if (!CanUse)
                return false;

            Execute();
            RaiseUsed();
            return true;
        }

        protected abstract void Execute();

        protected void RaiseUsed() => OnUsed?.Invoke(this);

        protected void StartCooldown() => CooldownTimer = Definition.Cooldown;
    }
}
