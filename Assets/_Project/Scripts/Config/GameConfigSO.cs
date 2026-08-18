using UnityEngine;

namespace Survival.Config
{
    /// <summary>
    /// Vài con số điều phối luồng ứng dụng — KHÔNG phải số cân bằng gameplay.
    ///
    /// Số cân bằng (máu, sát thương, tốc độ, thành phần wave) vẫn nằm nguyên ở các file
    /// config cũ trong <c>Assets/_Project/Configs/</c>. Không gom chúng vào đây: đổi chỗ ở
    /// của dữ liệu đã chạy đúng là rủi ro thuần tuý, không đổi lại được lợi ích nào.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Game Config", fileName = "GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip(
            "Tấm màn chuyển cảnh phải hiện ít nhất bấy nhiêu giây, kể cả khi scene đã nạp xong.\n\n" +
            "Nghe ngược đời nhưng đây là kỹ thuật phổ biến: một tấm màn loè lên rồi tắt trong " +
            "0.2 giây gây cảm giác giật cục hơn hẳn chờ đủ hai giây, và nó làm tốc độ chuyển cảnh " +
            "GIỐNG NHAU trên mọi máy thay vì máy mạnh loè một cái còn máy yếu đứng hình.")]
        private float _minimumLoadingSeconds = 2f;

        [SerializeField, Min(1), Tooltip(
            "Số khung hình mục tiêu. Android mặc định khoá 30 nếu không đặt bằng code, " +
            "và lỗi đó KHÔNG BAO GIỜ lộ ra trong Editor.")]
        private int _targetFrameRate = 60;

        public float MinimumLoadingSeconds => _minimumLoadingSeconds;
        public int TargetFrameRate => _targetFrameRate;
    }
}
