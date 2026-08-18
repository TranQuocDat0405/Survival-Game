using NFramework;
using Survival.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Giao diện trong trận: thanh máu, cấp, thanh kinh nghiệm, cụm nút skill, joystick,
    /// viền đỏ khi trúng đòn, và nút mở bảng tạm dừng.
    ///
    /// VÌ SAO JOYSTICK NẰM TRONG PREFAB NÀY CHỨ KHÔNG NẰM TRONG SCENE TRẬN ĐẤU:
    /// joystick là một phần của giao diện, và giao diện giờ sống ở scene Main. Nhưng player thì
    /// sống ở scene trận đấu, nên KHÔNG THỂ kéo dây trực tiếp giữa hai bên — prefab không lưu
    /// được tham chiếu tới object của một scene khác. Cách nối đúng là tiêm từ phía UI xuống,
    /// tận dụng hàm SetJoystick vốn đã có sẵn trong PlayerInputRouter.
    ///
    /// Tiêm trong OnOpen là an toàn: GameManager chỉ mở màn hình này SAU KHI scene trận đấu đã
    /// nạp xong và Unity đã chạy hết Awake/Start, nên PlayerActor.Current chắc chắn đã có.
    /// </summary>
    public class GamePlayMenu : BaseUIView
    {
        [SerializeField, Tooltip("Joystick ảo góc dưới trái. Được tiêm xuống PlayerInputRouter khi màn hình này mở.")]
        private Joystick _joystick;

        [SerializeField, Tooltip("Nút bánh răng mở bảng tạm dừng.")]
        private Button _pauseButton;

        public Joystick Joystick => _joystick;

        private void Awake()
        {
            if (_pauseButton != null)
                _pauseButton.onClick.AddListener(HandlePausePressed);
        }

        private void OnDestroy()
        {
            if (_pauseButton != null)
                _pauseButton.onClick.RemoveListener(HandlePausePressed);
        }

        public override void OnOpen()
        {
            base.OnOpen();

            BindJoystickToPlayer();

            // Nút tạm dừng phải hiện lại mỗi lần vào trận. View được UIManager giữ lại chứ không
            // huỷ, nên nếu ván trước kết thúc và nút bị ẩn đi thì ván sau nó vẫn còn ẩn.
            if (_pauseButton != null)
                _pauseButton.gameObject.SetActive(true);

            if (GameplayManager.I != null)
            {
                GameplayManager.I.OnGameOver  += HandleRunEnded;
                GameplayManager.I.OnVictory   += HandleRunEnded;
                GameplayManager.I.OnRestarted += HandleRestarted;
            }
        }

        public override void OnClose()
        {
            base.OnClose();

            if (GameplayManager.I != null)
            {
                GameplayManager.I.OnGameOver  -= HandleRunEnded;
                GameplayManager.I.OnVictory   -= HandleRunEnded;
                GameplayManager.I.OnRestarted -= HandleRestarted;
            }
        }

        /// <summary>Phím Esc / nút Back mở bảng tạm dừng, giống hệt bấm nút bánh răng.</summary>
        public override void HandleOnKeyBack() => HandlePausePressed();

        private void BindJoystickToPlayer()
        {
            if (_joystick == null)
                return;

            var player = Player.PlayerActor.Current;
            if (player == null)
            {
                Debug.LogError("[GamePlayMenu] Mở HUD mà chưa có PlayerActor — joystick sẽ không " +
                               "điều khiển được gì. Kiểm tra lại thứ tự trong GameManager.CREnterInGame.", this);
                return;
            }

            var router = player.GetComponent<Player.PlayerInputRouter>();
            if (router != null)
                router.SetJoystick(_joystick);
        }

        private void HandlePausePressed()
        {
            // Hết ván rồi thì không cho tạm dừng nữa: bảng tạm dừng chồng lên bảng kết thúc
            // sẽ ra hai lớp nút chồng nhau, và "Tiếp tục" lúc đó cũng chẳng có nghĩa gì.
            if (GameplayManager.I != null && GameplayManager.I.State != EGameplayState.Playing)
                return;

            UIManager.I.Open(Define.UIName.PAUSE_POPUP);
        }

        private void HandleRunEnded()
        {
            if (_pauseButton != null)
                _pauseButton.gameObject.SetActive(false);
        }

        private void HandleRestarted()
        {
            if (_pauseButton != null)
                _pauseButton.gameObject.SetActive(true);
        }
    }
}
