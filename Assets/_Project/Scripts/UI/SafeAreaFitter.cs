using UnityEngine;

namespace Survival.UI
{
    /// <summary>
    /// Co vùng UI vào "vùng an toàn" của màn hình — phần không bị tai thỏ, camera đục lỗ,
    /// hay thanh gạt home của điện thoại che mất.
    ///
    /// VÌ SAO TỰ VIẾT THAY VÌ DÙNG <c>NFramework.SafeArea</c> CÓ SẴN:
    /// Bản của framework tin tưởng <c>Screen.safeArea</c> một cách vô điều kiện.
    /// Nhưng giá trị đó do hệ điều hành cung cấp, và nó CÓ THỂ SAI.
    /// Thực tế gặp phải trong chính project này: Unity Editor báo
    /// <c>safeArea = (x:136, width:2204)</c> trong khi màn hình chỉ rộng 1920 —
    /// tức là vùng an toàn nằm LỌT RA NGOÀI màn hình. Hậu quả: toàn bộ cụm nút kỹ năng
    /// bị đẩy ra ngoài mép phải và biến mất.
    ///
    /// Vấn đề này không chỉ có ở Editor. Android có hàng nghìn model máy, và việc một hãng
    /// báo sai vùng an toàn là chuyện đã từng xảy ra. Nếu không phòng, người chơi trên đúng
    /// dòng máy đó sẽ mở game lên và không thấy nút bấm đâu cả — lỗi rất khó lần ra.
    ///
    /// Nên bản này luôn KẸP vùng an toàn vào trong phạm vi màn hình, và nếu con số nhận được
    /// vô lý tới mức không cứu được thì quay về dùng nguyên màn hình.
    /// Mất thêm bốn dòng, đổi lại loại bỏ hẳn một kiểu lỗi "UI biến mất" trên thiết bị thật.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeAreaFitter : MonoBehaviour
    {
        /// <summary>Vùng an toàn giả lập, chỉ dùng trong Editor để kiểm tra bố cục.</summary>
        public enum ESimulatedDevice
        {
            None = 0,
            IPhoneNotchLandscape = 1,
            AndroidPunchHoleLandscape = 2,
        }

        [SerializeField, Tooltip(
            "Giả lập tai thỏ ngay trong Editor để kiểm tra bố cục mà không cần build ra máy thật. " +
            "Luôn bị bỏ qua khi chạy trên thiết bị.")]
        private ESimulatedDevice _editorSimulation = ESimulatedDevice.None;

        [SerializeField, Tooltip("Co theo chiều ngang. Tắt nếu muốn nền trải hết bề ngang.")]
        private bool _conformX = true;

        [SerializeField, Tooltip("Co theo chiều dọc.")]
        private bool _conformY = true;

        private RectTransform _rectTransform;
        private Rect _appliedSafeArea;
        private Vector2Int _appliedScreenSize;
        private ESimulatedDevice _appliedSimulation = (ESimulatedDevice)(-1);

        private void Awake() => _rectTransform = GetComponent<RectTransform>();

        private void OnEnable() => Apply();

        /// <summary>
        /// Kiểm tra mỗi khung hình nhưng chỉ ghi lại khi có gì đó thực sự đổi.
        /// Người chơi có thể xoay máy hoặc mở chế độ chia đôi màn hình giữa lúc chơi,
        /// nên không thể chỉ tính đúng một lần lúc khởi động.
        /// So sánh vài con số mỗi khung hình gần như không tốn gì.
        /// </summary>
        private void Update() => Apply();

        private void Apply()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            if (screenWidth <= 0 || screenHeight <= 0)
                return;

            Rect safeArea = ResolveSafeArea(screenWidth, screenHeight);

            if (safeArea == _appliedSafeArea
                && _appliedScreenSize.x == screenWidth
                && _appliedScreenSize.y == screenHeight
                && _appliedSimulation == _editorSimulation)
            {
                return;
            }

            _appliedSafeArea = safeArea;
            _appliedScreenSize = new Vector2Int(screenWidth, screenHeight);
            _appliedSimulation = _editorSimulation;

            Vector2 anchorMin = new Vector2(safeArea.xMin / screenWidth, safeArea.yMin / screenHeight);
            Vector2 anchorMax = new Vector2(safeArea.xMax / screenWidth, safeArea.yMax / screenHeight);

            if (!_conformX) { anchorMin.x = 0f; anchorMax.x = 1f; }
            if (!_conformY) { anchorMin.y = 0f; anchorMax.y = 1f; }

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }

        private Rect ResolveSafeArea(int screenWidth, int screenHeight)
        {
#if UNITY_EDITOR
            if (_editorSimulation != ESimulatedDevice.None)
                return SimulateSafeArea(_editorSimulation, screenWidth, screenHeight);
#endif
            Rect reported = Screen.safeArea;

            // Đây là phần bảo vệ. Kẹp mọi cạnh vào trong phạm vi màn hình,
            // để một giá trị sai từ hệ điều hành không thể đẩy UI ra ngoài tầm nhìn.
            float xMin = Mathf.Clamp(reported.xMin, 0f, screenWidth);
            float xMax = Mathf.Clamp(reported.xMax, 0f, screenWidth);
            float yMin = Mathf.Clamp(reported.yMin, 0f, screenHeight);
            float yMax = Mathf.Clamp(reported.yMax, 0f, screenHeight);

            // Nếu sau khi kẹp mà vùng còn lại quá nhỏ thì con số nhận được là rác —
            // thà dùng nguyên màn hình còn hơn hiện ra một dải UI méo mó.
            if (xMax - xMin < screenWidth * 0.5f || yMax - yMin < screenHeight * 0.5f)
                return new Rect(0f, 0f, screenWidth, screenHeight);

            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

#if UNITY_EDITOR
        /// <summary>Tỉ lệ lấy theo thông số thật của máy, quy về dạng phần trăm màn hình.</summary>
        private static Rect SimulateSafeArea(ESimulatedDevice device, int screenWidth, int screenHeight)
        {
            switch (device)
            {
                // iPhone X ngang: khuyết 132/2436 mỗi bên, thanh home chiếm 63/1125 phía dưới.
                case ESimulatedDevice.IPhoneNotchLandscape:
                    return new Rect(
                        screenWidth * (132f / 2436f),
                        screenHeight * (63f / 1125f),
                        screenWidth * (2172f / 2436f),
                        screenHeight * (1062f / 1125f));

                // Máy Android đục lỗ nằm ngang: khuyết 5.5% một bên, thanh điều hướng 3% phía dưới.
                case ESimulatedDevice.AndroidPunchHoleLandscape:
                    return new Rect(
                        screenWidth * 0.055f,
                        screenHeight * 0.03f,
                        screenWidth * 0.945f,
                        screenHeight * 0.97f);

                default:
                    return new Rect(0f, 0f, screenWidth, screenHeight);
            }
        }
#endif
    }
}
