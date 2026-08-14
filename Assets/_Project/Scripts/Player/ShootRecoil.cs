using UnityEngine;

namespace Survival.Player
{
    /// <summary>
    /// Cú giật khi bắn nỏ: đẩy CẢ cây nỏ LẪN thân người lùi lại cùng một lúc.
    ///
    /// ==================== VÌ SAO GIẬT CẢ HAI ====================
    /// Chỉ giật mỗi cây nỏ thì trông như cây nỏ bị ai giật khỏi tay, còn người thì đứng trơ ra.
    /// Lực bắn thật truyền ngược vào vai và cả thân người, nên người cũng phải nhích lùi theo.
    /// Cho hai thứ chuyển động cùng nhịp thì mắt mới đọc ra là "cú bắn có lực".
    ///
    /// Cây nỏ lùi NHIỀU HƠN người: nó nhẹ và nằm ngay đầu nguồn lực, còn cả thân người thì nặng
    /// nên chỉ nhích một chút. Tỉ lệ đó là thứ tạo cảm giác về khối lượng.
    ///
    /// ==================== VÌ SAO GIẬT TỨC THÌ RỒI HỒI DẦN ====================
    /// Cú bắn thật là một xung lực: bật ra ngay lập tức rồi cơ thể từ từ ghì lại.
    /// Nếu cho giật ra từ từ rồi cũng hồi từ từ thì trông như cây nỏ đang rung chứ không phải
    /// bị đá ngược lại.
    ///
    /// ==================== CHỈ GIẬT KHI THẬT SỰ BẮN ====================
    /// Lớp này nghe sự kiện <c>OnSkillUsed</c> và CHỈ phản ứng với skill bắn. Đặt bom hay lướt
    /// thì cây nỏ không việc gì phải nảy. Ngoài lúc bắn ra, nó không đụng vào tư thế nhân vật
    /// một chút nào — hết cú giật là mọi thứ về đúng chỗ cũ.
    /// </summary>
    public class ShootRecoil : MonoBehaviour
    {
        [SerializeField, Tooltip("Nút treo vũ khí. Đây là thứ bị đẩy lùi nhiều nhất.")]
        private Transform _weaponHolder;

        [SerializeField, Tooltip("Nút chứa phần hình ảnh nhân vật. Bị đẩy lùi ít hơn, chỉ để thân người nhích theo.")]
        private Transform _visualRoot;

        [SerializeField] private PlayerActor _actor;

        [SerializeField, Tooltip("Tên trigger animation của skill bắn. Chỉ skill có trigger này mới làm nỏ giật.")]
        private string _shootTrigger = "Shoot";

        [Header("Độ mạnh")]
        [SerializeField, Min(0f), Tooltip("Cây nỏ lùi bao xa, tính bằng unit.")]
        private float _weaponKickBack = 0.34f;

        [SerializeField, Min(0f), Tooltip("Cây nỏ ngóc nòng lên bao nhiêu độ. Cú giật thật luôn hất nòng lên chứ không chỉ lùi thẳng.")]
        private float _weaponKickUp = 26f;

        [SerializeField, Min(0f), Tooltip("Thân người nhích lùi bao xa. Phải NHỎ HƠN hẳn cây nỏ, vì người nặng hơn nhiều.")]
        private float _bodyKickBack = 0.12f;

        [SerializeField, Min(0f), Tooltip(
            "Thân người NGẢ RA SAU bao nhiêu độ.\n\n" +
            "Đây là phần quan trọng nhất khi vừa chạy vừa bắn. Lúc đang chạy, nhân vật đã trôi đi " +
            "liên tục nên một cú nhích lùi vài phần trăm unit chìm nghỉm trong chuyển động đó — " +
            "người chơi không thấy gì cả. Còn góc ngả thì không phụ thuộc vào việc đang đứng hay " +
            "đang chạy, nên nó luôn đọc ra được.")]
        private float _bodyLeanBack = 9f;

        [SerializeField, Min(0.01f), Tooltip("Hồi hết cú giật mất bao lâu, tính bằng giây.")]
        private float _recoverDuration = 0.22f;

        /// <summary>Từ 1 (vừa bắn) về 0 (đã hồi xong).</summary>
        private float _kick;

        private Vector3 _weaponRest;
        private Vector3 _visualRest;
        private Quaternion _weaponRestRotation;
        private Quaternion _visualRestRotation;

        private void Awake()
        {
            if (_actor == null)
                _actor = GetComponentInParent<PlayerActor>();

            if (_weaponHolder != null)
            {
                _weaponRest = _weaponHolder.localPosition;
                _weaponRestRotation = _weaponHolder.localRotation;
            }
            if (_visualRoot != null)
            {
                _visualRest = _visualRoot.localPosition;
                _visualRestRotation = _visualRoot.localRotation;
            }
        }

        private void OnEnable()
        {
            if (_actor != null)
                _actor.OnSkillUsed += HandleSkillUsed;
        }

        private void OnDisable()
        {
            if (_actor != null)
                _actor.OnSkillUsed -= HandleSkillUsed;
        }

        private void HandleSkillUsed(Skills.SkillRuntime skill)
        {
            if (skill == null || skill.Def == null)
                return;

            if (skill.Def.AnimationTrigger != _shootTrigger)
                return;

            // Bật thẳng lên 1 chứ không tăng dần: cú bắn là một xung lực tức thì.
            _kick = 1f;
        }

        /// <summary>
        /// Chạy ở LateUpdate vì phải đè lên KẾT QUẢ của animation.
        /// Đặt ở Update thì Animator ghi tư thế sau đó và xoá sạch cú giật.
        /// </summary>
        private void LateUpdate()
        {
            if (_kick <= 0f)
                return;

            _kick = Mathf.MoveTowards(_kick, 0f, Time.deltaTime / _recoverDuration);

            if (_weaponHolder != null)
            {
                _weaponHolder.localPosition = _weaponRest + Vector3.back * (_weaponKickBack * _kick);
                _weaponHolder.localRotation = _weaponRestRotation * Quaternion.Euler(-_weaponKickUp * _kick, 0f, 0f);
            }

            if (_visualRoot != null)
            {
                _visualRoot.localPosition = _visualRest + Vector3.back * (_bodyKickBack * _kick);
                _visualRoot.localRotation = _visualRestRotation * Quaternion.Euler(-_bodyLeanBack * _kick, 0f, 0f);
            }

            // Hết cú giật thì trả mọi thứ về đúng vị trí gốc, tránh sai số cộng dồn
            // qua hàng nghìn phát bắn khiến vũ khí trôi dần khỏi chỗ cũ.
            if (_kick <= 0f)
            {
                if (_weaponHolder != null)
                {
                    _weaponHolder.localPosition = _weaponRest;
                    _weaponHolder.localRotation = _weaponRestRotation;
                }
                if (_visualRoot != null)
                {
                    _visualRoot.localPosition = _visualRest;
                    _visualRoot.localRotation = _visualRestRotation;
                }
            }
        }
    }
}
