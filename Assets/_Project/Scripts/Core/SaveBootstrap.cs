using NFramework;
using UnityEngine;

namespace Survival.Core
{
    /// <summary>
    /// Nối những thứ cần lưu vào <c>SaveManager</c> rồi ra lệnh nạp dữ liệu đã lưu.
    ///
    /// VÌ SAO PHẢI CÓ LỚP NÀY:
    /// nframework để việc đăng ký cho bên dùng tự lo, và chú thích ngay trong mã nguồn của nó
    /// ghi rõ *"All ISaveable must register to SaveManager before it Load"*. Bản thân
    /// <c>SaveManager</c> cũng KHÔNG tự gọi <c>Load</c> ở <c>Awake</c> hay <c>Start</c>.
    ///
    /// Hệ quả trước khi có lớp này: <c>SoundManager</c> tuy có sẵn phần lưu âm lượng nhưng
    /// chưa bao giờ được đăng ký, nên chỉnh âm lượng xong tắt game là mất sạch — mà không có
    /// một dòng lỗi nào báo ra.
    ///
    /// VÌ SAO GOM VÀO MỘT CHỖ thay vì để mỗi lớp tự đăng ký trong <c>Awake</c> của nó:
    /// thứ tự giữa "đăng ký" và "nạp" là thứ bắt buộc phải đúng, mà thứ tự <c>Awake</c> giữa các
    /// object khác nhau thì Unity KHÔNG bảo đảm. Gom vào một hàm thì thứ tự nhìn thấy được ngay
    /// trên màn hình và không thể sai.
    ///
    /// Đặt ở <c>Start</c> chứ không phải <c>Awake</c>: Unity chạy xong toàn bộ <c>Awake</c> rồi
    /// mới tới <c>Start</c>, nên tới đây chắc chắn mọi singleton đã tồn tại.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SaveBootstrap : MonoBehaviour
    {
        private void Start()
        {
            if (SaveManager.I == null)
            {
                Debug.LogError(
                    "[SaveBootstrap] Không có SaveManager trong scene. Cài đặt âm lượng và " +
                    "thành tích tốt nhất sẽ không được lưu lại.", this);
                return;
            }

            // Âm lượng nhạc và hiệu ứng, cùng hai công tắc bật/tắt.
            if (SoundManager.I != null)
                SaveManager.I.RegisterSaveData(SoundManager.I);

            // Thành tích tốt nhất hiển thị ở màn hình chính.
            if (Progression.BestRecord.I != null)
                SaveManager.I.RegisterSaveData(Progression.BestRecord.I);

            // Nạp SAU CÙNG, khi mọi thứ cần nạp đã đăng ký xong.
            SaveManager.I.Load();
        }
    }
}
