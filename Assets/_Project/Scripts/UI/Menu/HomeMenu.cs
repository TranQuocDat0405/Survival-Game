using NFramework;
using Survival.Data;
using Survival.Manager;
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
    ///
    /// PHẦN NỀN 3D KHÔNG NẰM Ở ĐÂY. Diorama, nhân vật và đèn là object ba chiều nên không nhét
    /// vào một prefab giao diện được; chúng sống trong scene <c>Main</c> và do
    /// <see cref="GameManager"/> bật tắt theo trạng thái.
    /// </summary>
    public class HomeMenu : BaseUIView
    {
        [Header("Nút chính")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _quitButton;

        [Header("Cài đặt")]
        [SerializeField, Tooltip("Nút bánh răng mở bảng cài đặt.")]
        private Button _settingsButton;

        [Header("Thành tích")]
        [SerializeField, Tooltip("Dòng chữ thành tích tốt nhất. Tự ẩn khi chưa chơi ván nào.")]
        private TextMeshProUGUI _bestRecordText;

        // Nối nút trong Awake chứ không phải OnOpen: Awake chạy đúng MỘT lần, còn OnOpen chạy lại
        // mỗi lần màn hình được mở. Nối trong OnOpen thì lần mở thứ hai sẽ có hai listener trên
        // cùng một nút, và bấm Chơi một cái sẽ gọi vào trận hai lần.
        private void Awake()
        {
            if (_playButton != null) _playButton.onClick.AddListener(HandlePlay);
            if (_quitButton != null) _quitButton.onClick.AddListener(HandleQuit);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OpenSettings);
        }

        private void OnDestroy()
        {
            if (_playButton != null) _playButton.onClick.RemoveListener(HandlePlay);
            if (_quitButton != null) _quitButton.onClick.RemoveListener(HandleQuit);
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(OpenSettings);
        }

        // Đọc lại thành tích MỖI LẦN mở, không phải một lần lúc khởi động: người chơi vừa xong
        // một ván và quay về đây thì con số phải là con số mới.
        public override void OnOpen()
        {
            base.OnOpen();
            ShowBestRecord();
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

            var record = UserData.I;
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
            GameManager.I.EnterInGame();
        }

        private void OpenSettings()
        {
            Audio.GameAudioService.PlayUiClick();
            UIManager.I.Open(Define.UIName.SETTINGS_POPUP);
        }

        private void HandleQuit()
        {
            Audio.GameAudioService.PlayUiClick();
            GameManager.I.QuitApplication();
        }
    }
}
