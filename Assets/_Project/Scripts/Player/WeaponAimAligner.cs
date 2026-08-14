using UnityEngine;

namespace Survival.Player
{
    /// <summary>
    /// Giữ cây nỏ luôn chĩa đúng hướng nhân vật đang nhìn, và tạo cú giật lùi khi bắn.
    ///
    /// ==================== VÌ SAO CẦN GHIM HƯỚNG ====================
    /// Cây nỏ được gắn vào XƯƠNG BÀN TAY, nên nó chịu trọn vẹn chuyển động cánh tay của
    /// animation. Đo lúc chạy thật: trục dài của cây nỏ lệch 32.6 độ so với hướng nhân vật.
    ///
    /// Đây không chỉ là chuyện xấu đẹp. Mũi tên luôn bay theo <c>transform.forward</c> của
    /// nhân vật, nên nếu cây nỏ chĩa một đằng mà đạn bay một nẻo, người chơi sẽ ngắm theo
    /// cây nỏ rồi bắn trượt — và họ sẽ kết luận là game bắn không chính xác, chứ không ai
    /// nghĩ tới chuyện hình ảnh với luật chơi lệch nhau.
    ///
    /// Cách sửa KHÔNG phải là dò một góc xoay cố định cho vừa mắt ở tư thế đứng yên: mỗi tư thế
    /// bàn tay lại nằm một góc khác, chỉnh vừa lúc đứng thì lúc chạy lại lệch.
    /// Ở đây ghi đè thẳng hướng xoay sau khi Animator đã ghi xong tư thế xương, nên đúng
    /// trong mọi tư thế, mọi animation.
    ///
    /// ==================== VÌ SAO GIẬT LÙI BẰNG CODE ====================
    /// Bộ animation đang dùng không có clip riêng cho việc bắn nỏ. Làm cú giật bằng code thì
    /// vừa không phải dựng clip mới, vừa chỉnh được độ giật và độ nảy ngay trên Inspector.
    /// Đây cũng đúng là cách các game bắn súng làm: tư thế nền để cho tất định, còn hiệu ứng
    /// giật thì chồng lên trên.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponAimAligner : MonoBehaviour
    {
        [Header("Ghim hướng")]
        [SerializeField, Tooltip("Gốc để lấy hướng nhắm. Bỏ trống thì tự tìm ngược lên PlayerActor.")]
        private Transform _aimSource;

        [SerializeField, Tooltip(
            "Xoay bù để trục ngắm của model trùng với hướng nhân vật.\n\n" +
            "Trục dài của cây nỏ nằm dọc theo Y của chính nó, nên phải quay -90 độ quanh X " +
            "thì trục đó mới nằm ngang và chĩa ra trước. Model khác sẽ cần con số khác — " +
            "để ở đây thay vì viết cứng trong code là để đổi vũ khí thì chỉ chỉnh Inspector.")]
        private Vector3 _rotationOffset = new Vector3(-90f, 0f, 0f);

        [Header("Giật khi bắn")]
        [SerializeField, Min(0f), Tooltip("Giật lùi bao xa, tính bằng unit. 0 là tắt hẳn hiệu ứng giật.")]
        private float _recoilDistance = 0.22f;

        [SerializeField, Min(0f), Tooltip("Ngóc nòng lên bao nhiêu độ khi giật. Cú giật thật luôn hất nòng lên chứ không chỉ lùi thẳng.")]
        private float _recoilKickAngle = 18f;

        [SerializeField, Min(0.01f), Tooltip(
            "Hồi về vị trí cũ mất bao lâu.\n\n" +
            "Cú giật xảy ra TỨC THÌ rồi hồi dần trong khoảng thời gian này — đúng nhịp của một " +
            "cú bắn thật. Nếu cho giật ra từ từ rồi cũng hồi từ từ thì trông như cây nỏ bị rung, " +
            "chứ không ra được cảm giác bị đá ngược lại.")]
        private float _recoilReturnDuration = 0.22f;

        /// <summary>Vị trí gốc trên tay, đọc một lần lúc khởi động rồi giữ nguyên làm mốc để giật quanh đó.</summary>
        private Vector3 _restLocalPosition;

        /// <summary>Từ 1 (đang giật hết cỡ) về 0 (đã hồi xong).</summary>
        private float _recoil;

        private void Awake()
        {
            _restLocalPosition = transform.localPosition;

            if (_aimSource == null)
            {
                var actor = GetComponentInParent<PlayerActor>();
                if (actor != null)
                    _aimSource = actor.transform;
            }
        }

        private void OnEnable()
        {
            var actor = GetComponentInParent<PlayerActor>();
            if (actor != null)
                actor.OnSkillUsed += HandleSkillUsed;
        }

        private void OnDisable()
        {
            var actor = GetComponentInParent<PlayerActor>();
            if (actor != null)
                actor.OnSkillUsed -= HandleSkillUsed;
        }

        private void HandleSkillUsed(Skills.SkillRuntime skill)
        {
            // Chỉ giật khi bắn nỏ. Đặt bom hay lướt thì cây nỏ không việc gì phải nảy.
            if (_recoilDistance <= 0f && _recoilKickAngle <= 0f)
                return;

            if (skill == null || skill.Def == null || skill.Def.AnimationTrigger != "Shoot")
                return;

            _recoil = 1f;
        }

        /// <summary>
        /// Chạy trong LateUpdate là BẮT BUỘC, không phải tuỳ chọn.
        ///
        /// Animator ghi tư thế xương trong pha cập nhật thường. Nếu chỉnh hướng cây nỏ ở
        /// <c>Update</c> thì ngay sau đó Animator ghi đè lên và không thấy tác dụng gì cả —
        /// một lỗi rất khó đoán vì code trông hoàn toàn đúng.
        /// </summary>
        private void LateUpdate()
        {
            if (_aimSource == null)
                return;

            Vector3 forward = _aimSource.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return;

            // Hồi dần cú giật. Giật ra rất nhanh nhưng hồi về chậm hơn — đó là nhịp làm cho
            // cú bắn có sức nặng; hồi nhanh bằng lúc giật thì trông như cây nỏ bị rung chứ không phải giật.
            if (_recoil > 0f)
                _recoil = Mathf.MoveTowards(_recoil, 0f, Time.deltaTime / _recoilReturnDuration);

            var aim = Quaternion.LookRotation(forward.normalized, Vector3.up);

            // Ngóc nòng lên theo mức giật hiện tại, rồi mới áp phần xoay bù của model.
            transform.rotation = aim
                * Quaternion.Euler(-_recoilKickAngle * _recoil, 0f, 0f)
                * Quaternion.Euler(_rotationOffset);

            // Lùi về sau theo đúng hướng đang ngắm.
            transform.localPosition = _restLocalPosition;
            transform.position -= forward.normalized * (_recoilDistance * _recoil);
        }
    }
}
