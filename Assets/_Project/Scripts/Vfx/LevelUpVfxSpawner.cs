using NFramework;
using Survival.Player;
using Survival.Pooling;
using Survival.Progression;
using UnityEngine;

namespace Survival.Vfx
{
    /// <summary>
    /// Nổ một vầng sáng quanh người chơi mỗi lần lên cấp. Bonus mục 8: "feedback rõ lúc lên cấp".
    ///
    /// Tách thành lớp riêng thay vì nhét vào <see cref="ExperienceSystem"/>, vì hệ thống kinh
    /// nghiệm là LUẬT CHƠI còn cái này là TRANG TRÍ. Gỡ hẳn component này ra thì việc lên cấp
    /// vẫn cộng máu cộng giáp đúng như cũ — đó là phép thử cho thấy hai thứ thật sự tách rời.
    /// Cùng nguyên tắc mà <c>PlayerAnimatorDriver</c> đang theo.
    ///
    /// Hiệu ứng ĐI THEO người chơi (đặt làm con của player) chứ không đứng yên tại chỗ vừa lên
    /// cấp: lên cấp giữa lúc đang chạy trốn là chuyện thường, mà vầng sáng nằm lại phía sau thì
    /// người chơi không hiểu nó là của mình.
    /// </summary>
    public class LevelUpVfxSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("Hiệu ứng nổ ra khi lên cấp. Để trống thì không có gì.")]
        private PooledObject _levelUpVfx;

        [SerializeField, Min(0f), Tooltip("Nâng hiệu ứng lên khỏi chân người chơi bao nhiêu unit.")]
        private float _height = 0.1f;

        private ExperienceSystem _experience;

        private void OnEnable() => TryBind();

        private void OnDisable()
        {
            if (_experience == null)
                return;

            _experience.OnLeveledUp -= HandleLeveledUp;
            _experience = null;
        }

        /// <summary>
        /// Hệ thống kinh nghiệm có thể chưa khởi tạo xong lúc lớp này bật lên, nên thử nối lại
        /// cho tới khi được. Rẻ hơn nhiều so với việc phải phụ thuộc vào thứ tự khởi tạo.
        /// </summary>
        private void TryBind()
        {
            if (_experience != null || ExperienceSystem.I == null)
                return;

            _experience = ExperienceSystem.I;
            _experience.OnLeveledUp += HandleLeveledUp;
        }

        private void Update()
        {
            if (_experience == null)
                TryBind();
        }

        private void HandleLeveledUp(int newLevel)
        {
            if (_levelUpVfx == null || PoolService.I == null)
                return;

            var player = PlayerActor.Current;
            if (player == null)
                return;

            var effect = PoolService.I.Spawn(
                _levelUpVfx,
                player.transform.position + Vector3.up * _height,
                Quaternion.identity);

            // Gắn làm con của player để vầng sáng chạy theo chứ không nằm lại phía sau.
            // worldPositionStays = false để nó bám đúng gốc chân, không bị lệch đi một đoạn.
            if (effect != null)
                effect.transform.SetParent(player.transform, worldPositionStays: false);
        }
    }
}
