using NFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Cụm chỉnh âm lượng: hai thanh trượt và hai công tắc bật/tắt.
    ///
    /// Đây là một CỤM DÙNG CHUNG, không phải một bảng riêng: nó được nhúng vào màn hình chính
    /// và nhúng vào bảng tạm dừng trong lúc chơi. Nhờ vậy hai nơi không thể lệch nhau, và sửa
    /// bố cục một lần là cả hai chỗ đổi theo.
    ///
    /// Lớp này KHÔNG tự lưu gì cả. Nó chỉ ghi vào <c>SoundManager</c>, còn phần lưu xuống đĩa
    /// do <c>SaveManager</c> lo — xem <see cref="Core.SaveBootstrap"/>.
    /// </summary>
    public class VolumeSettingsView : MonoBehaviour
    {
        [Header("Nhạc nền")]
        [SerializeField] private Slider _musicSlider;
        [SerializeField, Tooltip("Công tắc bật/tắt nhạc. Tắt thì thanh trượt bị làm mờ.")]
        private Toggle _musicToggle;
        [SerializeField, Tooltip("Ảnh biểu tượng loa của nhạc, đổi giữa bật và tắt.")]
        private Image _musicIcon;

        [Header("Hiệu ứng âm thanh")]
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Image _sfxIcon;

        [Header("Ảnh biểu tượng")]
        [SerializeField] private Sprite _onSprite;
        [SerializeField] private Sprite _offSprite;

        /// <summary>
        /// Cờ chặn vòng lặp vô hạn.
        ///
        /// Khi mở bảng ra, ta đặt giá trị cho thanh trượt theo dữ liệu đã lưu. Việc đặt đó lại
        /// kích hoạt sự kiện "người dùng vừa kéo thanh trượt", và nếu không chặn thì nó ghi
        /// ngược lại vào SoundManager — thường là vô hại, nhưng nó cũng đánh dấu dữ liệu đã đổi
        /// và phát ra tiếng bấm nút, dù người chơi chưa hề chạm vào gì.
        /// </summary>
        private bool _isApplyingValues;

        private void Awake()
        {
            if (_musicSlider != null) _musicSlider.onValueChanged.AddListener(HandleMusicVolume);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(HandleSfxVolume);
            if (_musicToggle != null) _musicToggle.onValueChanged.AddListener(HandleMusicToggle);
            if (_sfxToggle != null) _sfxToggle.onValueChanged.AddListener(HandleSfxToggle);
        }

        private void OnDestroy()
        {
            if (_musicSlider != null) _musicSlider.onValueChanged.RemoveListener(HandleMusicVolume);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveListener(HandleSfxVolume);
            if (_musicToggle != null) _musicToggle.onValueChanged.RemoveListener(HandleMusicToggle);
            if (_sfxToggle != null) _sfxToggle.onValueChanged.RemoveListener(HandleSfxToggle);
        }

        /// <summary>Mỗi lần cụm này hiện ra thì đọc lại trạng thái thật từ SoundManager.</summary>
        private void OnEnable() => Refresh();

        public void Refresh()
        {
            if (SoundManager.I == null)
                return;

            _isApplyingValues = true;

            if (_musicSlider != null) _musicSlider.value = SoundManager.I.MusicVolume;
            if (_sfxSlider != null) _sfxSlider.value = SoundManager.I.SFXVolume;
            if (_musicToggle != null) _musicToggle.isOn = SoundManager.I.MusicStatus;
            if (_sfxToggle != null) _sfxToggle.isOn = SoundManager.I.SFXStatus;

            _isApplyingValues = false;

            UpdateVisuals();
        }

        private void HandleMusicVolume(float value)
        {
            if (_isApplyingValues || SoundManager.I == null) return;
            SoundManager.I.MusicVolume = value;
        }

        private void HandleSfxVolume(float value)
        {
            if (_isApplyingValues || SoundManager.I == null) return;

            SoundManager.I.SFXVolume = value;

            // Phát một tiếng mẫu để người chơi NGHE THẤY mình vừa chỉnh tới đâu.
            // Không có nó thì kéo thanh hiệu ứng chẳng khác gì kéo một thanh vô hình.
            Audio.GameAudioService.PlayUiClick();
        }

        private void HandleMusicToggle(bool isOn)
        {
            if (_isApplyingValues || SoundManager.I == null) return;

            SoundManager.I.MusicStatus = isOn;
            UpdateVisuals();
            Audio.GameAudioService.PlayUiClick();
        }

        private void HandleSfxToggle(bool isOn)
        {
            if (_isApplyingValues || SoundManager.I == null) return;

            SoundManager.I.SFXStatus = isOn;
            UpdateVisuals();

            // Chỉ kêu khi VỪA BẬT lên. Bấm tắt mà vẫn kêu thì đúng là phản tác dụng.
            if (isOn)
                Audio.GameAudioService.PlayUiClick();
        }

        /// <summary>Đổi ảnh loa và làm mờ thanh trượt khi đang tắt.</summary>
        private void UpdateVisuals()
        {
            if (SoundManager.I == null)
                return;

            bool music = SoundManager.I.MusicStatus;
            bool sfx = SoundManager.I.SFXStatus;

            if (_musicIcon != null && _onSprite != null && _offSprite != null)
                _musicIcon.sprite = music ? _onSprite : _offSprite;

            if (_sfxIcon != null && _onSprite != null && _offSprite != null)
                _sfxIcon.sprite = sfx ? _onSprite : _offSprite;

            // Vẫn cho kéo khi đang tắt — kéo thanh lên rồi bật lại là nghe đúng mức vừa đặt.
            // Chỉ làm mờ để báo rằng hiện tại nó không có tác dụng gì.
            SetSliderDimmed(_musicSlider, !music);
            SetSliderDimmed(_sfxSlider, !sfx);
        }

        private static void SetSliderDimmed(Slider slider, bool dimmed)
        {
            if (slider == null)
                return;

            var group = slider.GetComponent<CanvasGroup>();
            if (group == null)
                group = slider.gameObject.AddComponent<CanvasGroup>();

            group.alpha = dimmed ? 0.4f : 1f;
        }
    }
}
