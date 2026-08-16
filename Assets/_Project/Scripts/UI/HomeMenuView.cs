using Survival.Core;
using Survival.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Màn hình chính: tên game, nút chơi, cài đặt, thoát, và thành tích tốt nhất.
    ///
    /// BỐ CỤC THEO ĐÚNG THÓI QUEN CỦA THỂ LOẠI: một nút Chơi to đùng ở giữa, mọi thứ khác lùi
    /// ra rìa. Gần như toàn bộ lượt bấm ở màn hình này rơi vào đúng nút đó, nên nó phải là thứ
    /// duy nhất nổi bật; nút cài đặt và nút thoát chỉ cần tìm thấy được khi có nhu cầu.
    ///
    /// Nút Thoát cố tình KHÔNG hiện trên nền web hay trên điện thoại iOS — trên web thì
    /// <c>Application.Quit</c> không làm gì cả, còn hướng dẫn của Apple coi việc tự thoát ứng
    /// dụng là hành vi không nên có. Trên Windows và Android thì nó chạy đúng như mong đợi,
    /// và nó cũng là cách duy nhất để đóng bản build Windows chạy toàn màn hình.
    /// </summary>
    public class HomeMenuView : MonoBehaviour
    {
        [Header("Nút chính")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _quitButton;

        [Header("Cài đặt")]
        [SerializeField, Tooltip("Nút bánh răng mở bảng cài đặt.")]
        private Button _settingsButton;

        [SerializeField, Tooltip("Nút gốc của bảng cài đặt. Bị tắt lúc mới vào.")]
        private GameObject _settingsPanel;

        [SerializeField] private Button _settingsCloseButton;
        [SerializeField] private VolumeSettingsView _volumeSettings;

        [Header("Thành tích")]
        [SerializeField, Tooltip("Dòng chữ thành tích tốt nhất. Tự ẩn khi chưa chơi ván nào.")]
        private TextMeshProUGUI _bestRecordText;

        private void Start()
        {
            if (_settingsPanel != null) _settingsPanel.SetActive(false);

            if (_playButton != null) _playButton.onClick.AddListener(HandlePlay);
            if (_quitButton != null) _quitButton.onClick.AddListener(HandleQuit);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OpenSettings);
            if (_settingsCloseButton != null) _settingsCloseButton.onClick.AddListener(CloseSettings);

            // Về tới màn hình chính từ một ván đang tạm dừng thì nhịp thời gian có thể vẫn là 0.
            // SceneFlow đã trả lại rồi, nhưng đặt lại lần nữa ở đây cho chắc: một màn hình chính
            // bị đứng hình là lỗi không có đường nào thoát ra được.
            Time.timeScale = 1f;

            ShowBestRecord();
        }

        private void OnDestroy()
        {
            if (_playButton != null) _playButton.onClick.RemoveListener(HandlePlay);
            if (_quitButton != null) _quitButton.onClick.RemoveListener(HandleQuit);
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(OpenSettings);
            if (_settingsCloseButton != null) _settingsCloseButton.onClick.RemoveListener(CloseSettings);
        }

        /// <summary>
        /// Hiện thành tích tốt nhất, hoặc ẩn hẳn dòng đó nếu người chơi chưa chơi xong ván nào.
        ///
        /// Ẩn hẳn chứ không hiện "Tốt nhất: Wave 0": một con số 0 trông như game bị lỗi, còn
        /// không có dòng nào thì người mới chơi hiểu ngay là mình chưa có gì để khoe.
        /// </summary>
        private void ShowBestRecord()
        {
            if (_bestRecordText == null)
                return;

            var record = BestRecord.I;
            if (record == null || !record.HasRecord)
            {
                _bestRecordText.gameObject.SetActive(false);
                return;
            }

            int minutes = Mathf.FloorToInt(record.BestSeconds / 60f);
            int seconds = Mathf.FloorToInt(record.BestSeconds % 60f);

            _bestRecordText.gameObject.SetActive(true);
            _bestRecordText.text =
                $"Tốt nhất:  Wave {record.BestWave}   ·   hạ {record.BestKills} quái   ·   {minutes:0}:{seconds:00}";
        }

        private void HandlePlay()
        {
            Audio.GameAudioService.PlayUiClick();
            SceneFlow.I.GoToGame();
        }

        private void OpenSettings()
        {
            Audio.GameAudioService.PlayUiClick();

            if (_settingsPanel != null) _settingsPanel.SetActive(true);
            if (_volumeSettings != null) _volumeSettings.Refresh();
        }

        private void CloseSettings()
        {
            Audio.GameAudioService.PlayUiClick();

            if (_settingsPanel != null) _settingsPanel.SetActive(false);
        }

        private void HandleQuit()
        {
            Audio.GameAudioService.PlayUiClick();

#if UNITY_EDITOR
            // Trong Editor thì Application.Quit không làm gì cả, nên phải tắt play mode bằng tay.
            // Không có nhánh này thì bấm Thoát lúc đang test trông y như nút bị hỏng.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
