using System.Collections;
using NFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Survival.Core
{
    /// <summary>
    /// Nơi duy nhất biết cách đi từ scene này sang scene kia.
    ///
    /// VÌ SAO LÀ <c>LazyPersistentSingletonMono</c>:
    ///   - "Lazy" = tự sinh ra ở lần đầu có người gọi tới. Nhờ vậy KHÔNG phải đặt sẵn nó vào
    ///     scene nào cả. Người chấm mở thẳng scene Game rồi bấm Play thì lớp này chưa hề tồn tại,
    ///     và đó là chuyện bình thường — nó chỉ sinh ra khi người chơi thật sự bấm "Về Home".
    ///   - "Persistent" = sống xuyên qua việc đổi scene. Bắt buộc, vì chính nó là thứ đang chạy
    ///     dở quá trình chuyển cảnh; nếu nó chết theo scene cũ thì tấm màn che sẽ kẹt lại
    ///     giữa chừng và không bao giờ mờ ra.
    ///
    /// Cách đặt singleton của nframework cũng tránh luôn được chuyện trùng lặp: đặt sẵn object
    /// vào cả hai scene thì mỗi lần đổi scene sẽ có một bản thừa bị huỷ, và nframework in ra
    /// một dòng lỗi đỏ mỗi lần như thế. Sinh theo yêu cầu thì không bao giờ có bản thứ hai.
    /// </summary>
    public class SceneFlow : LazyPersistentSingletonMono<SceneFlow>
    {
        public const string HomeSceneName = "HomeMenu";
        public const string GameSceneName = "Game";

        /// <summary>Đường dẫn trong Resources tới tấm màn chuyển cảnh.</summary>
        private const string LoadingScreenResourcePath = "UI/LoadingScreen";

        /// <summary>
        /// Tấm màn phải hiện ÍT NHẤT ngần này giây, kể cả khi scene đã nạp xong từ lâu.
        ///
        /// Nghe ngược đời — cố tình bắt người chơi chờ — nhưng đây là kỹ thuật phổ biến:
        /// một tấm màn loè lên rồi tắt trong 0.2 giây gây cảm giác giật cục và khó chịu hơn hẳn
        /// so với chờ đủ hai giây. Nó cũng làm cho tốc độ chuyển cảnh GIỐNG NHAU trên mọi máy,
        /// thay vì máy mạnh thì loè một cái còn máy yếu thì đứng hình mấy giây.
        /// </summary>
        [SerializeField, Min(0f)]
        private float _minimumLoadingSeconds = 2f;

        private UI.LoadingScreenView _loadingScreen;
        private bool _isTransitioning;

        /// <summary>Đang chuyển cảnh hay không. Dùng để chặn bấm nút hai lần.</summary>
        public bool IsTransitioning => _isTransitioning;

        public void GoToGame() => Go(GameSceneName, "ĐANG VÀO TRẬN...");

        public void GoToHome() => Go(HomeSceneName, "ĐANG QUAY VỀ...");

        private void Go(string sceneName, string label)
        {
            // Bấm hai lần liên tiếp thì lần sau bị bỏ qua. Không có chốt này thì hai coroutine
            // cùng nạp một scene và tấm màn sẽ bị một cái bật lên trong khi cái kia đang tắt đi.
            if (_isTransitioning)
                return;

            StartCoroutine(TransitionRoutine(sceneName, label));
        }

        private IEnumerator TransitionRoutine(string sceneName, string label)
        {
            _isTransitioning = true;

            EnsureLoadingScreen();

            // Thiếu tấm màn thì vẫn PHẢI chuyển scene được. Mất tấm màn chỉ là xấu một nhịp,
            // còn ném lỗi ở đây thì người chơi kẹt vĩnh viễn trong màn đang chơi.
            if (_loadingScreen != null)
            {
                _loadingScreen.SetLabel(label);
                yield return _loadingScreen.FadeIn();
            }

            // Trả nhịp thời gian về bình thường SAU KHI tấm màn đã che kín.
            // Người chơi có thể bấm "Về Home" từ bảng tạm dừng, lúc đó timeScale đang là 0;
            // scene mới mà nhận nguyên trạng thái đó thì mọi thứ đứng hình ngay khi vừa mở ra.
            // Đặt lại ở đây chứ không đặt sớm hơn để người chơi không kịp thấy một nhịp
            // game chạy tiếp trước khi màn hình tối đi.
            Time.timeScale = 1f;

            float startTime = Time.unscaledTime;

            var operation = SceneManager.LoadSceneAsync(sceneName);

            // Chặn Unity tự động bật scene mới lên ngay khi nạp xong, để mình còn giữ tấm màn
            // đủ thời gian tối thiểu. Unity coi là "nạp xong" ở mốc 0.9 chứ không phải 1.0;
            // phần 0.1 còn lại chỉ chạy sau khi cho phép kích hoạt.
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
                yield return null;

            float remaining = _minimumLoadingSeconds - (Time.unscaledTime - startTime);
            if (remaining > 0f)
                yield return new WaitForSecondsRealtime(remaining);

            operation.allowSceneActivation = true;

            while (!operation.isDone)
                yield return null;

            // Chờ thêm đúng một khung hình để scene mới kịp chạy Awake và Start của mọi thứ.
            // Mờ tấm màn ra ngay lập tức thì người chơi sẽ thấy một khung hình dở dang:
            // giao diện chưa kịp cập nhật, quái chưa kịp vào vị trí.
            yield return null;

            if (_loadingScreen != null)
                yield return _loadingScreen.FadeOut();

            _isTransitioning = false;
        }

        /// <summary>
        /// Nạp tấm màn từ Resources ở lần dùng đầu tiên rồi giữ lại luôn.
        ///
        /// Dùng Resources chứ không phải một ô kéo thả trên Inspector, vì lớp này tự sinh ra
        /// bằng code nên không có ai ngồi kéo prefab vào ô cả.
        /// </summary>
        private void EnsureLoadingScreen()
        {
            if (_loadingScreen != null)
                return;

            var prefab = Resources.Load<UI.LoadingScreenView>(LoadingScreenResourcePath);
            if (prefab == null)
            {
                Debug.LogError(
                    $"[SceneFlow] Không tìm thấy tấm màn chuyển cảnh ở Resources/{LoadingScreenResourcePath}. " +
                    "Chuyển scene vẫn chạy nhưng sẽ không có gì che, người chơi sẽ thấy một nhịp giật.", this);
                return;
            }

            _loadingScreen = Instantiate(prefab, transform);
        }
    }
}
