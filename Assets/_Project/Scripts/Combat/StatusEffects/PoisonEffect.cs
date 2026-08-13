using System;
using UnityEngine;

namespace Survival.Combat.StatusEffects
{
    /// <summary>
    /// Hiệu ứng độc của quái đánh xa. Spec mục 4.2:
    ///
    ///   "30 sát thương gốc / giây; tick ngay lúc trúng; kéo dài 3 giây"
    ///   "Số tick: Tổng 4 tick (lúc trúng + mỗi giây trong 3 giây)"
    ///   "Refresh: Dính độc khi đang độc: reset thời gian độc, không stack sát thương"
    ///
    /// Nghĩa là mốc gây sát thương rơi vào giây thứ 0, 1, 2, 3 — đúng 4 lần, mỗi lần 30 gốc.
    /// Mỗi tick đi qua <see cref="Health.TakeDamage"/> nên GIÁP ĐƯỢC TRỪ cho từng tick,
    /// đúng như spec mục 2.2 nói rõ giáp áp dụng cho cả độc.
    /// </summary>
    [Serializable]
    public class PoisonEffect : StatusEffectDefinition
    {
        [SerializeField, Min(0f), Tooltip("Sát thương GỐC mỗi tick, trước khi trừ giáp. Spec: 30.")]
        private float _damagePerTick = 30f;

        [SerializeField, Min(0.01f), Tooltip("Khoảng cách giữa hai tick, tính bằng giây. Spec: 1.")]
        private float _tickInterval = 1f;

        [SerializeField, Min(1), Tooltip("Tổng số tick, tính cả tick ngay lúc trúng. Spec: 4.")]
        private int _totalTicks = 4;

        [SerializeField, Tooltip(
            "Khi dính độc lần nữa lúc đang còn độc thì có gây sát thương ngay không.\n\n" +
            "BẬT (mặc định) bám sát chữ 'tick ngay lúc trúng' của spec — mỗi lần trúng đạn " +
            "đều là một lần 'lúc trúng'.\n" +
            "Dù bật hay tắt thì vẫn CHỈ CÓ MỘT hiệu ứng độc chạy, không bao giờ có hai luồng " +
            "sát thương song song — đó là ý của chữ 'không stack sát thương'.")]
        private bool _tickOnRefresh = true;

        public override string Id => "Poison";

        public float DamagePerTick => _damagePerTick;
        public float TickInterval => _tickInterval;
        public int TotalTicks => _totalTicks;
        public bool TickOnRefresh => _tickOnRefresh;

        public override StatusEffectRuntime CreateRuntime(StatusEffectContext context)
            => new PoisonRuntime(this, context);
    }

    public class PoisonRuntime : StatusEffectRuntime
    {
        private readonly PoisonEffect _def;

        private int _ticksRemaining;
        private float _timer;

        public PoisonRuntime(PoisonEffect definition, StatusEffectContext context) : base(context)
        {
            _def = definition;
        }

        public override bool IsFinished => _ticksRemaining <= 0;

        /// <summary>
        /// Bám vào lần đầu, hoặc bị đánh trúng lại khi đang còn độc.
        ///
        /// Cả hai trường hợp đều nạp lại đủ số tick — đó chính là "reset thời gian độc".
        /// Vì vẫn chỉ có MỘT đối tượng runtime cho mỗi mục tiêu, không thể có hai luồng
        /// sát thương chạy song song — đó là "không stack sát thương".
        /// </summary>
        public override void OnApplied(bool isRefresh)
        {
            _ticksRemaining = _def.TotalTicks;
            _timer = 0f;

            if (!isRefresh || _def.TickOnRefresh)
                ApplyTick();
        }

        public override void Tick(float deltaTime)
        {
            if (IsFinished)
                return;

            _timer += deltaTime;

            // Dùng while chứ không phải if: nếu một khung hình bị kéo dài bất thường
            // (máy khựng, hoặc chuyển cảnh) thì vẫn trả đủ số tick đáng lẽ phải xảy ra,
            // thay vì âm thầm nuốt mất một nhịp sát thương.
            while (_timer >= _def.TickInterval && !IsFinished)
            {
                _timer -= _def.TickInterval;
                ApplyTick();
            }
        }

        private void ApplyTick()
        {
            if (_ticksRemaining <= 0)
                return;

            _ticksRemaining--;

            var target = Context.Target;
            if (target == null || !target.IsAlive)
            {
                _ticksRemaining = 0;
                return;
            }

            // Truyền sát thương GỐC. Giáp do phía nhận tự trừ trong Health,
            // nên độc và đòn chém đi qua đúng cùng một công thức.
            var info = new DamageInfo(
                _def.DamagePerTick,
                EDamageSource.Poison,
                Context.Instigator,
                target.Transform.position);

            target.TakeDamage(in info);
        }
    }
}
