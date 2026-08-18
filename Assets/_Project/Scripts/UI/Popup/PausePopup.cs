using Survival.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Bảng tạm dừng trong lúc chơi: Tiếp tục · Chơi lại · chỉnh âm lượng · Về màn hình chính.
    ///
    /// VÌ SAO PHẢI THẬT SỰ DỪNG GAME:
    /// Không dừng thì mở bảng ra chỉnh âm lượng là bị quái vây đánh chết. Bảng cài đặt mà giết
    /// người chơi thì thà không có còn hơn.
    ///
    /// DỪNG BẰNG <c>Time.timeScale = 0</c> LÀ CHƯA ĐỦ — và đây là chỗ dễ sai nhất:
    /// <c>timeScale = 0</c> chỉ làm <c>Time.deltaTime</c> bằng 0, nó KHÔNG chặn <c>Update</c>.
    /// Nghĩa là <c>Input.GetKeyDown</c> vẫn chạy bình thường, và người chơi bấm chuột trái trong
    /// lúc đang mở bảng thì nhân vật vẫn bắn ra một phát. Nên phải tắt hẳn phần nhận phím.
    /// Còn phía cảm ứng thì tấm nền của bảng đã chặn chạm xuyên xuống các nút bên dưới.
    /// </summary>
    public class PausePopup : Popup
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _homeButton;

        [SerializeField, Tooltip(
            "Mở bảng cài đặt âm lượng — CÙNG MỘT prefab mà màn hình chính dùng.\n\n" +
            "Trước đây bảng này nhúng thẳng một cụm chỉnh âm lượng thứ hai vào bên trong, tức là " +
            "cùng một giao diện được dựng hai lần ở hai nơi. Mở popup dùng chung thì sửa bố cục " +
            "một lần là cả hai chỗ đổi theo, và hai chỗ không thể lệch nhau được nữa.")]
        private Button _settingsButton;

        /// <summary>
        /// Phải tìm lại MỖI TRẬN chứ không nhớ vĩnh viễn.
        ///
        /// Bảng này sống trong scene Main và không bao giờ bị huỷ, còn player thì chết theo scene
        /// trận đấu. Nhớ một lần rồi dùng mãi thì từ trận thứ hai trở đi nó trỏ vào một object đã
        /// bị huỷ, và phần chặn phím lặng lẽ ngừng hoạt động — người chơi mở bảng tạm dừng ra rồi
        /// bấm chuột là vẫn bắn.
        /// </summary>
        private Player.KeyboardSkillInput _keyboardInput;

        protected override void Awake()
        {
            base.Awake();

            if (_resumeButton != null) _resumeButton.onClick.AddListener(HandleResume);
            if (_retryButton != null) _retryButton.onClick.AddListener(HandleRetry);
            if (_homeButton != null) _homeButton.onClick.AddListener(HandleHome);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(HandleSettings);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(HandleResume);
            if (_retryButton != null) _retryButton.onClick.RemoveListener(HandleRetry);
            if (_homeButton != null) _homeButton.onClick.RemoveListener(HandleHome);
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(HandleSettings);
        }

        public override void OnOpen()
        {
            base.OnOpen();

            Time.timeScale = 0f;
            SetGameplayInputEnabled(false);

            Audio.GameAudioService.PlayUiClick();
        }

        public override void OnClose()
        {
            base.OnClose();

            // Chỉ trả nhịp thời gian khi ván còn đang chơi. Đóng bảng này vì đang chuyển sang
            // màn hình chính hoặc vì ván vừa kết thúc thì quyền quyết định timeScale thuộc về
            // GameManager / GameplayManager, không phải popup.
            if (GameplayManager.I != null && GameplayManager.I.State == EGameplayState.Playing)
                Time.timeScale = 1f;

            SetGameplayInputEnabled(true);

            // Quên tham chiếu tới player của trận vừa rồi. Xem chú thích ở khai báo _keyboardInput.
            _keyboardInput = null;
        }

        /// <summary>Phím Esc / nút Back đóng bảng này, đúng như bấm "Tiếp tục".</summary>
        public override void HandleOnKeyBack() => HandleResume();

        private void HandleResume()
        {
            Audio.GameAudioService.PlayUiClick();
            CloseSelf();
        }

        private void HandleRetry()
        {
            Audio.GameAudioService.PlayUiClick();
            GameManager.I.EnterReset();   // EnterReset tự đóng mọi popup ở layer Popup
        }

        private void HandleHome()
        {
            Audio.GameAudioService.PlayUiClick();
            GameManager.I.EnterHome();
        }

        // Bảng cài đặt mở CHỒNG LÊN bảng này chứ không thay thế nó: cả hai cùng ở layer Popup,
        // UIManager xếp cái mở sau lên trên. Đóng bảng cài đặt là quay lại đúng bảng tạm dừng,
        // và game vẫn đang đứng yên suốt thời gian đó vì OnClose của bảng này chưa hề chạy.
        private void HandleSettings()
        {
            Audio.GameAudioService.PlayUiClick();
            NFramework.UIManager.I.Open(Define.UIName.SETTINGS_POPUP);
        }

        private void SetGameplayInputEnabled(bool value)
        {
            if (_keyboardInput == null)
            {
                var player = Player.PlayerActor.Current;
                if (player != null)
                    _keyboardInput = player.GetComponentInChildren<Player.KeyboardSkillInput>(true);
            }

            if (_keyboardInput != null)
                _keyboardInput.enabled = value;
        }
    }
}
