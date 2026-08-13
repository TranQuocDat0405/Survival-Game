using Survival.Stats;
using UnityEngine;

namespace Survival.Skills
{
    /// <summary>
    /// Tất cả những gì một skill cần biết về "người đang dùng nó".
    ///
    /// Nhờ gói lại thành một object thế này, bản thân skill không cần giữ tham chiếu tới
    /// <c>PlayerActor</c>. Hệ quả: sau này muốn cho quái dùng chung hệ thống skill
    /// thì chỉ cần dựng một SkillContext khác, không phải sửa dòng nào trong skill.
    /// </summary>
    public class SkillContext
    {
        /// <summary>Transform của nhân vật. Hướng bắn luôn lấy từ <c>Owner.forward</c> tại thời điểm khai hoả.</summary>
        public Transform Owner;

        /// <summary>Điểm sinh ra đạn (thường đặt ở đầu nòng súng). Nếu null thì dùng tạm Owner.</summary>
        public Transform Muzzle;

        /// <summary>GameObject của nhân vật, ghi vào DamageInfo để biết ai là người gây sát thương.</summary>
        public GameObject OwnerGameObject;

        /// <summary>Nguồn đọc chỉ số — skill dùng nó để lấy DamageMultiplier khi tính sát thương gây ra.</summary>
        public IStatProvider Stats;

        /// <summary>Những layer được coi là mục tiêu hợp lệ của skill này.</summary>
        public LayerMask TargetMask;

        /// <summary>Dùng để chạy coroutine (dash cần di chuyển liên tục trong 0.5 giây).</summary>
        public MonoBehaviour CoroutineRunner;

        /// <summary>Rigidbody của nhân vật. Dash cần nó để đẩy người đi mà vẫn va chạm được với tường.</summary>
        public Rigidbody OwnerRigidbody;

        /// <summary>
        /// Khoá / mở quyền điều khiển di chuyển.
        ///
        /// Dash cần khoá điều khiển trong 0.5 giây lướt, nếu không thì joystick sẽ vừa đẩy
        /// theo hướng người chơi giữ, vừa bị dash đẩy theo hướng forward — hai lực đánh nhau,
        /// quãng đường lướt sẽ không còn đúng 3 unit như spec.
        ///
        /// Dùng một hàm uỷ nhiệm (delegate) thay vì tham chiếu thẳng tới PlayerMotor,
        /// để skill không bị trói vào riêng player — quái cũng có thể dùng dash sau này.
        /// </summary>
        public System.Action<bool> SetControlLocked;

        public Vector3 SpawnPosition => Muzzle != null ? Muzzle.position : Owner.position;

        public Vector3 Forward => Owner.forward;
    }
}
