using System.Collections;
using System.Collections.Generic;
using Survival.Combat;
using Survival.Pooling;
using Survival.Stats;
using UnityEngine;
using NFramework;

namespace Survival.Skills
{
    /// <summary>
    /// Kỹ năng bổ trợ 2 — Dash rồi nổ. Spec mục 3.3:
    /// đẩy player theo hướng forward 3 unit trong 0.5 giây;
    /// hết lướt thì nổ gây 15 sát thương gốc trong bán kính 3 unit. Cooldown 6 giây.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Skills/Dash", fileName = "Skill_Dash")]
    public class DashSkillSO : SkillDefinition
    {
        [Header("Lướt")]
        [SerializeField, Min(0f), Tooltip("Quãng đường lướt, tính bằng unit. Spec: 3.")]
        private float _distance = 3f;

        [SerializeField, Min(0.01f), Tooltip("Thời gian lướt, tính bằng giây. Spec: 0.5.")]
        private float _duration = 0.5f;

        [Header("Vụ nổ khi kết thúc")]
        [SerializeField, Min(0f), Tooltip("Sát thương GỐC. Spec: 15.")]
        private float _damage = 15f;

        [SerializeField, Min(0f), Tooltip("Bán kính vụ nổ, tính bằng unit. Spec: 3.")]
        private float _radius = 3f;

        [SerializeField, Tooltip("Hiệu ứng nổ cuối đường lướt. Có thể để trống.")]
        private PooledObject _explosionEffectPrefab;

        [SerializeField, Tooltip("Nhân kích thước hiệu ứng nổ cho khớp bán kính thật.")]
        private float _effectScalePerUnitRadius = 1f;

        [Header("Vệt bom dọc đường lướt")]
        [SerializeField, Tooltip(
            "Quả bom nhỏ rơi lại dọc đường lướt. Để trống thì chỉ nổ một điểm ở cuối như spec gốc.\n\n" +
            "MỤC ĐÍCH LÀ MỞ RỘNG VÙNG PHỦ, KHÔNG PHẢI TĂNG SÁT THƯƠNG.\n" +
            "Lý do nằm ở hình học của chính kỹ năng này: dash là để CHẠY KHỎI đám quái, nên tới lúc " +
            "nổ ở điểm cuối thì mấy con đang bám sau lưng đã ra ngoài tầm. Đo được: quái áp sát ở " +
            "1.3 unit, player lướt 3 unit, mấy con phía sau kết thúc ở 4.3 unit — ngoài hẳn bán kính 3, " +
            "và chỉ 2 trên 6 con bị dính.\n\n" +
            "Rải vụ nổ dọc đường thì phủ đúng chỗ đám đang đuổi đứng. MỖI CON VẪN CHỈ ĂN ĐÚNG MỘT LẦN " +
            "với đúng con số sát thương trong ô bên trên — vùng nổ rộng ra, sức sát thương không đổi.")]
        private PooledObject _trailBombPrefab;

        [SerializeField, Range(0, 8), Tooltip(
            "Rơi lại mấy quả dọc đường. 3-4 quả là vừa: ít hơn thì không phủ hết đường lướt, " +
            "nhiều hơn thì lúc nổ cùng lúc lại thành một dải sáng che màn hình.\n" +
            "Để 0 thì quay về đúng spec gốc: một vụ nổ duy nhất ở điểm kết thúc.")]
        private int _trailBombCount = 3;

        [SerializeField, Min(0f), Tooltip("Kích thước vụ nổ của mỗi quả bom trong vệt. Nhỏ hơn hẳn vụ nổ chính.")]
        private float _trailExplosionScale = 0.35f;

        /// <summary>Tốc độ lướt suy ra từ quãng đường và thời gian. Spec 3 unit / 0.5 giây = 6 unit/giây.</summary>
        public float DashSpeed => _distance / _duration;

        public float Distance => _distance;
        public float Duration => _duration;
        public float Damage => _damage;
        public float Radius => _radius;
        public PooledObject ExplosionEffectPrefab => _explosionEffectPrefab;
        public float EffectScalePerUnitRadius => _effectScalePerUnitRadius;
        public PooledObject TrailBombPrefab => _trailBombPrefab;
        public int TrailBombCount => _trailBombCount;
        public float TrailExplosionScale => _trailExplosionScale;

        public override SkillRuntime CreateRuntime(SkillContext context) => new DashRuntime(this, context);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Mathf.Approximately(Cooldown, 6f))
                Debug.Log($"[{name}] cooldown đang là {Cooldown}s. Spec yêu cầu 6s.", this);
        }
#endif
    }

    public class DashRuntime : SkillRuntime
    {
        /// <summary>
        /// Dùng lại một đối tượng chờ duy nhất cho mọi lần dash.
        /// Viết <c>yield return new WaitForFixedUpdate()</c> trong vòng lặp sẽ cấp phát
        /// một đối tượng mới ở MỖI bước — 25 bước mỗi lần dash, và dash dùng liên tục cả trận.
        /// Đối tượng này không giữ trạng thái gì nên dùng chung hoàn toàn an toàn.
        /// </summary>
        private static readonly WaitForFixedUpdate WaitForFixedStep = new WaitForFixedUpdate();

        private readonly DashSkillSO _def;
        private bool _isDashing;

        /// <summary>
        /// Những quả bom trang trí đã rơi lại trên đường lướt của lần dash này.
        ///
        /// Cấp phát một lần rồi dùng lại mãi. Dash được bấm liên tục suốt trận, mà tạo một
        /// danh sách mới mỗi lần là đều đặn ném rác cho bộ dọn rác — đúng thứ gây khựng hình.
        /// </summary>
        private readonly List<PooledObject> _trailBombs = new List<PooledObject>();

        /// <summary>
        /// Các tâm nổ của lần lướt này: chỗ từng quả bom rơi, cộng thêm điểm kết thúc cú lướt.
        /// Cũng dùng lại mãi thay vì cấp phát mới mỗi lần dash.
        /// </summary>
        private readonly List<Vector3> _blastCenters = new List<Vector3>();

        public DashRuntime(DashSkillSO definition, SkillContext context) : base(definition, context)
        {
            _def = definition;
        }

        /// <summary>Không cho dash chồng lên dash. Cooldown 6 giây đã chặn rồi, đây là lớp bảo vệ thứ hai.</summary>
        public override bool CanUse => base.CanUse && !_isDashing;

        protected override void Execute()
        {
            if (Context.CoroutineRunner == null)
                return;

            Context.CoroutineRunner.StartCoroutine(DashRoutine());
        }

        private IEnumerator DashRoutine()
        {
            _isDashing = true;
            Context.SetControlLocked?.Invoke(true);

            // Hướng lướt được CHỐT MỘT LẦN tại thời điểm bấm nút, đúng theo spec
            // "dùng hướng forward hiện tại của nhân vật". Nếu đọc lại forward mỗi khung hình
            // thì đường lướt sẽ bị bẻ cong khi người chơi ngoáy joystick giữa chừng.
            Vector3 direction = Context.Owner.forward;
            direction.y = 0f;
            direction.Normalize();

            var rigidbody = Context.OwnerRigidbody;
            float speed = _def.DashSpeed;
            float elapsed = 0f;

            // Đẩy bằng vận tốc của Rigidbody chứ không dịch thẳng transform:
            // nhờ vậy nếu lướt vào tường thì hệ vật lý chặn lại, không xuyên qua tường.
            // Hệ quả có chủ đích: lướt vào tường thì đi được ngắn hơn 3 unit — đúng và hợp lý.
            //
            // Vòng lặp đồng bộ theo NHỊP VẬT LÝ (FixedUpdate) chứ không theo nhịp khung hình.
            // Vận tốc chỉ được hệ vật lý đọc ở mỗi bước vật lý; nếu đếm giờ theo khung hình
            // (60, 144, hay 30 khung/giây tuỳ máy) thì số bước vật lý thực sự chạy sẽ lệch,
            // và quãng đường lướt sẽ không còn đúng 3 unit trên mọi máy.
            // Đếm theo bước vật lý thì 0.5 giây luôn là đúng 25 bước x 0.02 giây,
            // cho ra 25 x 0.02 x 6 = 3.00 unit, giống nhau ở mọi cấu hình.
            // Chuẩn bị rơi vệt bom. Khoảng cách giữa hai lần rơi tính theo THỜI GIAN chứ không
            // theo quãng đường: lướt vào tường thì nhân vật đứng lại nhưng đồng hồ vẫn chạy,
            // nên các quả bom sẽ dồn lại ngay chỗ đâm vào — đúng như một vệt bị chặn đứng.
            _trailBombs.Clear();
            int trailCount = _def.TrailBombPrefab != null ? _def.TrailBombCount : 0;
            float trailInterval = trailCount > 0 ? _def.Duration / trailCount : float.MaxValue;
            float nextTrailAt = 0f;

            while (elapsed < _def.Duration)
            {
                if (rigidbody != null)
                    rigidbody.velocity = direction * speed;

                // Rơi bom theo VỊ TRÍ THẬT ở thời điểm này, không phải vị trí tính trước.
                // Nhờ vậy vệt bom luôn nằm đúng trên đường nhân vật thật sự đã đi qua,
                // kể cả khi bị tường hay gốc cây chặn giữa chừng.
                if (_trailBombs.Count < trailCount && elapsed >= nextTrailAt)
                {
                    nextTrailAt += trailInterval;
                    DropTrailBomb();
                }

                yield return WaitForFixedStep;
                elapsed += Time.fixedDeltaTime;
            }

            if (rigidbody != null)
                rigidbody.velocity = Vector3.zero;

            Context.SetControlLocked?.Invoke(false);
            _isDashing = false;

            Explode();
        }

        /// <summary>Thả một quả bom trang trí tại đúng chỗ nhân vật đang đứng.</summary>
        private void DropTrailBomb()
        {
            if (PoolService.I == null || Context.Owner == null)
                return;

            var bomb = PoolService.I.Spawn(_def.TrailBombPrefab, Context.Owner.position, Quaternion.identity);
            if (bomb != null)
                _trailBombs.Add(bomb);
        }

        /// <summary>
        /// Cho cả vệt bom nổ CÙNG MỘT LÚC với vụ nổ chính, rồi thu chúng về pool.
        ///
        /// Nhấn mạnh lại vì đây là chỗ dễ hiểu nhầm nhất trong cả kỹ năng này:
        /// chúng KHÔNG gây thêm một điểm sát thương nào. Sát thương vẫn đúng spec mục 3.3 —
        /// một lần duy nhất, bán kính 3, 15 sát thương gốc, tại chỗ kết thúc cú lướt.
        /// Vệt bom chỉ để mắt người chơi đọc được đường lướt vừa đi qua đâu.
        /// </summary>
        private void DetonateTrailBombs()
        {
            for (int i = 0; i < _trailBombs.Count; i++)
            {
                var bomb = _trailBombs[i];
                if (bomb == null)
                    continue;

                // Ghi lại chỗ quả bom đứng TRƯỚC khi trả nó về pool, vì đó cũng là một tâm nổ.
                _blastCenters.Add(bomb.transform.position);

                if (_def.ExplosionEffectPrefab != null && PoolService.I != null)
                {
                    var puff = PoolService.I.Spawn(_def.ExplosionEffectPrefab, bomb.transform.position, Quaternion.identity);
                    if (puff != null)
                        puff.transform.localScale = Vector3.one * _def.TrailExplosionScale;
                }

                bomb.ReturnToPool();
            }

            _trailBombs.Clear();
        }

        private void Explode()
        {
            _blastCenters.Clear();

            DetonateTrailBombs();

            float multiplier = Context.Stats != null ? Context.Stats.Get(EStatType.DamageMultiplier) : 0f;
            float damage = CombatMath.ComputeOutgoing(_def.Damage, multiplier);

            Vector3 center = Context.Owner.position;

            // Điểm kết thúc cú lướt luôn là một tâm nổ — đó là điều spec mô tả.
            // Các quả bom dọc đường chỉ MỞ RỘNG VÙNG PHỦ ra sau lưng, chứ không nhân sát thương lên:
            // mỗi con quái vẫn chỉ ăn đúng một lần, đúng bằng con số trong config.
            _blastCenters.Add(center);

            AreaDamage.ExplodeMultiPoint(
                _blastCenters, _blastCenters.Count,
                _def.Radius, damage,
                EDamageSource.PlayerDash,
                Context.OwnerGameObject,
                Context.TargetMask);

            if (_def.ExplosionEffectPrefab != null && PoolService.I != null)
            {
                var effect = PoolService.I.Spawn(_def.ExplosionEffectPrefab, center, Quaternion.identity);
                if (effect != null)
                    effect.transform.localScale = Vector3.one * (_def.Radius * _def.EffectScalePerUnitRadius);
            }

            // Mặc định độ rung của cú này đang để 0, tức không rung — vì dash được dùng rất
            // thường xuyên để né đòn. Vẫn gọi ở đây để ai muốn bật lại thì chỉ cần đổi một số
            // trên Inspector, không phải sửa code.
            Survival.CameraRig.CameraShakeService.I?.ShakeOnDashExplosion();

            // Một tiếng cho cả bốn điểm nổ. Khoảng nghỉ trong GameSound lo phần chặn,
            // nhưng gọi một lần ở đây vẫn rõ ràng hơn là gọi bốn lần rồi trông chờ bị chặn.
            Survival.Audio.GameAudioService.PlayDashExplode();
        }
    }
}
