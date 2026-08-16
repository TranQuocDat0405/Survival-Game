using UnityEngine;

namespace Survival.Core
{
    /// <summary>
    /// Đặt vài thiết lập cấp ứng dụng ngay khi game khởi động, trước cả khi scene được nạp.
    ///
    /// VÌ SAO PHẢI CÓ, VÀ VÌ SAO KHÔNG ĐẶT TRONG PROJECT SETTINGS:
    /// Số khung hình mục tiêu KHÔNG có ô nào trong Project Settings để điền — nó chỉ đặt được
    /// bằng code lúc chạy. Mà nếu không đặt, Unity trên Android mặc định khoá 30 khung hình/giây.
    /// Với game bắn và né như thế này thì 30 khung hình là ì rõ rệt: cảm giác điều khiển nặng,
    /// và cú dash 0.5 giây chỉ còn 15 khung để người chơi đọc. Trong Editor lại luôn chạy 60+
    /// nên lỗi này KHÔNG BAO GIỜ lộ ra lúc phát triển — chỉ người cầm bản build Android mới thấy.
    ///
    /// Dùng <c>RuntimeInitializeOnLoadMethod</c> thay vì gắn một MonoBehaviour vào scene:
    /// không phải nhớ kéo object vào scene, không sợ ai xoá nhầm, và nó chạy cho MỌI scene —
    /// kể cả scene menu sau này. Đây là thứ của cả ứng dụng, không thuộc riêng màn chơi nào.
    /// </summary>
    public static class AppBootstrap
    {
        /// <summary>
        /// Mục tiêu 60 khung hình/giây. Không đặt cao hơn: màn hình điện thoại phổ thông là 60Hz,
        /// đẩy cao hơn chỉ tốn pin và làm máy nóng chứ người chơi không thấy mượt thêm.
        /// </summary>
        private const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Phải tắt đồng bộ dọc trước. Còn bật thì nó khoá nhịp theo tần số quét của màn hình
            // và targetFrameRate bị bỏ qua hoàn toàn.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;

            // Không cho màn hình tự tắt giữa lúc đang chơi. Người chơi có thể đứng yên vài giây
            // để chờ hồi charge hoặc chờ wave sau, và Android tính quãng đó là "không hoạt động"
            // vì không có thao tác chạm nào.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
