using UnityEngine;

namespace Survival.Player
{
    /// <summary>
    /// Ép CẢ HAI BÀN TAY nắm vào cây nỏ, để nhân vật thật sự cầm nỏ bằng hai tay ở mọi tư thế.
    ///
    /// ==================== VÌ SAO PHẢI LÀM THẾ NÀY ====================
    /// Bộ animation KayKit không có clip nào cầm nỏ hai tay. Nhóm clip tên "Ranged_2H" nghe thì
    /// giống, nhưng đo ra mới biết đó là tư thế GIƯƠNG CUNG: tay trái đưa ra cầm cánh cung,
    /// tay phải kéo dây, nên hai bàn tay luôn cách nhau khoảng 0.48 unit và không bao giờ chụm lại.
    /// Đã quét thử cả chín clip ứng viên, không clip nào vừa chụm hai tay vừa chĩa nỏ ra trước.
    ///
    /// ==================== VÌ SAO KHÔNG GẮN NỎ VÀO XƯƠNG TAY ====================
    /// Cách làm hiển nhiên là gắn cây nỏ vào xương bàn tay phải. Đã thử và hỏng:
    /// bàn tay xoay liên tục theo animation, nên cây nỏ đu theo và chĩa lung tung —
    /// đo được trục nỏ lệch tới (-0.79, 0.01, 0.62) trong khi cần (0, 0, 1).
    ///
    /// Cũng đã thử ghi đè hướng xoay của nỏ mỗi khung hình để ép nó luôn chĩa thẳng.
    /// Cách đó làm đúng hướng nhưng cây nỏ trông như rời khỏi bàn tay, và vì nó chống lại
    /// animation nên sinh ra hiện tượng giật giật liên tục dù không hề bắn.
    ///
    /// CÁCH ĐÚNG, và cũng là cách các game bắn súng vẫn làm: ĐẢO NGƯỢC quan hệ phụ thuộc.
    /// Vũ khí KHÔNG treo vào xương tay nữa mà treo vào một điểm cố định trên thân, luôn chĩa
    /// thẳng ra trước. Rồi dùng IK kéo hai bàn tay về nắm vào vũ khí. Tức là TAY ĐI THEO NỎ,
    /// chứ không phải nỏ đi theo tay.
    ///
    /// Nhờ vậy hướng bắn nhìn thấy luôn khớp tuyệt đối với hướng đạn bay — điều này quan trọng
    /// hơn thẩm mỹ: nếu nỏ chĩa một đằng mà đạn bay một nẻo, người chơi ngắm theo nỏ rồi bắn
    /// trượt sẽ kết luận là game bắn không chính xác.
    ///
    /// ==================== HAI ĐIỀU KIỆN BẮT BUỘC ====================
    ///   1. Component phải nằm CÙNG GameObject với Animator, nếu không Unity không gọi OnAnimatorIK.
    ///   2. Layer của Animator Controller phải bật "IK Pass" (xem AnimatorBuilder).
    /// Thiếu một trong hai thì không có lỗi nào báo ra cả — tay chỉ đơn giản là không nhúc nhích.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class WeaponTwoHandIK : MonoBehaviour
    {
        [SerializeField, Tooltip("Điểm trên cây nỏ mà bàn tay TRÁI nắm vào — phần thân nỏ phía trước.")]
        private Transform _leftGrip;

        [SerializeField, Tooltip("Điểm trên cây nỏ mà bàn tay PHẢI nắm vào — phần báng nỏ phía sau.")]
        private Transform _rightGrip;

        [SerializeField, Tooltip("Nhân vật, dùng để biết còn sống hay đã chết.")]
        private PlayerActor _actor;

        [SerializeField, Range(0f, 1f), Tooltip(
            "Kéo bàn tay về điểm nắm mạnh tới mức nào.\n" +
            "1 là bám dính hoàn toàn. Giảm xuống thì tay chỉ bị kéo một phần, giữ lại chút dáng gốc " +
            "của animation — hữu ích nếu thấy vai bị vặn trông gượng.")]
        private float _positionWeight = 1f;

        [SerializeField, Range(0f, 1f), Tooltip(
            "Xoay cổ tay theo điểm nắm mạnh tới mức nào.\n" +
            "Thường để thấp hơn phần vị trí: bàn tay nằm ĐÚNG CHỖ quan trọng hơn nhiều so với " +
            "việc nó xoay đúng góc, mà ép xoay quá tay thì cổ tay dễ bị bẻ ngược trông rất kỳ.")]
        private float _rotationWeight = 0.7f;

        [SerializeField, Min(0.01f), Tooltip("Thời gian tắt/bật IK cho mượt, tính bằng giây. Tắt đột ngột thì tay giật một cái rất rõ.")]
        private float _blendDuration = 0.15f;

        private Animator _animator;

        /// <summary>Trọng số đang dùng thật sự, chạy dần về đích thay vì nhảy cóc.</summary>
        private float _currentWeight;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_actor == null)
                _actor = GetComponentInParent<PlayerActor>();
        }

        /// <summary>
        /// Unity gọi hàm này giữa lúc dựng tư thế, sau khi animation đã ghi xong và trước khi vẽ.
        /// Đây là chỗ DUY NHẤT đặt IK có tác dụng — đặt ở Update hay LateUpdate đều vô ích.
        /// </summary>
        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null)
                return;

            // Chết rồi thì buông IK ra, để animation gục xuống chạy trọn vẹn.
            // Không có bước này thì cái xác vẫn cố giơ tay giữ nỏ, trông rất kỳ.
            bool wantsGrip = _actor == null || _actor.Health == null || _actor.Health.IsAlive;
            float target = wantsGrip ? 1f : 0f;

            _currentWeight = Mathf.MoveTowards(_currentWeight, target, Time.deltaTime / _blendDuration);
            if (_currentWeight <= 0.001f)
                return;

            ApplyHand(AvatarIKGoal.LeftHand, _leftGrip);
            ApplyHand(AvatarIKGoal.RightHand, _rightGrip);
        }

        private void ApplyHand(AvatarIKGoal goal, Transform grip)
        {
            if (grip == null)
                return;

            _animator.SetIKPositionWeight(goal, _positionWeight * _currentWeight);
            _animator.SetIKPosition(goal, grip.position);

            _animator.SetIKRotationWeight(goal, _rotationWeight * _currentWeight);
            _animator.SetIKRotation(goal, grip.rotation);
        }
    }
}
