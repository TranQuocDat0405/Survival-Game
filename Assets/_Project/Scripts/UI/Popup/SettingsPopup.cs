using UnityEngine;

namespace Survival.UI
{
    /// <summary>
    /// Bảng cài đặt âm lượng — MỘT bảng dùng chung cho cả màn hình chính lẫn lúc đang chơi.
    ///
    /// ĐÂY LÀ LỢI ÍCH RÕ NHẤT CỦA VIỆC ĐƯA UI VỀ UIManager.
    /// Trước đây cụm chỉnh âm lượng bị dựng HAI lần: một bản nhúng trong màn hình chính, một bản
    /// nhúng trong bảng tạm dừng. Hai bản đó phải tự giống nhau bằng kỷ luật của người dựng, nên
    /// đổi bố cục là phải nhớ sửa cả hai chỗ, và sớm muộn chúng sẽ lệch nhau. Giờ chỉ còn một
    /// prefab, mở từ đâu cũng là chính nó.
    /// </summary>
    public class SettingsPopup : Popup
    {
        [SerializeField, Tooltip("Cụm hai thanh trượt và hai công tắc âm lượng.")]
        private VolumeSettingsView _volumeSettings;

        public override void OnOpen()
        {
            base.OnOpen();

            // Đọc lại trạng thái thật từ SoundManager mỗi lần mở, thay vì tin vào giá trị còn sót
            // từ lần mở trước — UIManager giữ view lại trong bộ nhớ đệm chứ không huỷ đi, nên các
            // thanh trượt vẫn đang ở đúng vị trí của lần mở gần nhất.
            if (_volumeSettings != null)
                _volumeSettings.Refresh();
        }
    }
}
