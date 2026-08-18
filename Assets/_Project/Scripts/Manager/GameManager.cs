using System.Collections;
using NFramework;
using Survival.Config;
using Survival.Data;
using Survival.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Survival.Manager
{
    /// <summary>
    /// Trạng thái của ỨNG DỤNG — không phải trạng thái của một ván chơi.
    /// Trạng thái trong một ván (đang chơi / thua / thắng) là việc của <see cref="GameplayManager"/>,
    /// và enum của nó tên <see cref="EGameplayState"/> để hai thứ không bao giờ lẫn vào nhau.
    /// </summary>
    public enum EGameState
    {
        NONE = 0,
        LOADING = 1,
        HOME = 2,
        INGAME = 3,
    }

    /// <summary>
    /// Bộ điều phối luồng ứng dụng. Sống trong scene <c>Main</c> và không bao giờ bị huỷ.
    ///
    /// VÌ SAO GOM TẤT CẢ VÀO MỘT MÁY TRẠNG THÁI:
    /// Trước khi có lớp này, luồng game nằm rải ở bốn nơi — <c>SceneFlow</c> biết cách đổi scene,
    /// <c>GameSession</c> biết ván kết thúc, mỗi màn hình tự quyết định trong <c>Start</c> của nó,
    /// và tên scene thì gõ tay. Muốn trả lời câu "bấm Play xong thì chuyện gì xảy ra theo thứ tự
    /// nào" phải mở sáu file. Bây giờ đọc đúng <see cref="HandleGameStateChanged"/> là thấy hết,
    /// và log <c>GameState: ...</c> in ra đúng đường đi thật lúc chạy.
    ///
    /// VÌ SAO KHÔNG CẦN <c>DontDestroyOnLoad</c>:
    /// scene <c>Main</c> không bao giờ bị unload — scene trận đấu được nạp ADDITIVE chồng lên nó.
    /// Nhờ vậy mọi manager nhìn thấy được trong Hierarchy, không có object "mồ côi" lơ lửng,
    /// và không phải viết đoạn "bản thứ hai tự huỷ" như <c>SceneFlow</c> cũ phải làm.
    /// </summary>
    public class GameManager : SingletonMono<GameManager>
    {
        [SerializeField, Tooltip("Vài con số điều phối luồng. Thiếu thì dùng giá trị mặc định an toàn.")]
        private GameConfigSO _config;

        [SerializeField, Tooltip(
            "Cả cụm nền 3D của màn hình chính: diorama, nhân vật đứng tạo dáng, đèn hướng, và " +
            "camera riêng chiếu vào chúng.\n\n" +
            "Bật/tắt bằng đúng MỘT lần SetActive lên object cha, nên camera và đèn tắt theo luôn. " +
            "Đèn phải nằm TRONG cụm này: ánh sáng không thuộc scene nào cả, để nó ở ngoài là lúc " +
            "vào trận sân đấu có hai mặt trời chiếu từ hai hướng.")]
        private GameObject _homeBackdrop;

        private EGameState _state = EGameState.NONE;

        /// <summary>Đang chuyển cảnh hay không. Dùng để chặn bấm nút hai lần.</summary>
        private bool _isTransitioning;

        public EGameState GetGameState() => _state;
        public GameConfigSO GetGameConfig() => _config;

        private float MinimumLoadingSeconds => _config != null ? _config.MinimumLoadingSeconds : 2f;

        // Start chứ không phải Awake: SingletonMono gán tham chiếu tĩnh trong Awake, nên mọi
        // truy cập chéo giữa các manager (UIManager.I, SaveManager.I...) chỉ an toàn từ Start
        // trở đi. Unity chạy xong TOÀN BỘ Awake rồi mới tới Start đầu tiên.
        private void Start() => SetGameState(EGameState.LOADING);

        /// <summary>Cổng duy nhất để đổi trạng thái. Mọi thay đổi đều đi qua đây và đều được log.</summary>
        private void SetGameState(EGameState state)
        {
            if (_state == state)
                return;

            _state = state;
            HandleGameStateChanged(_state);
        }

        private void HandleGameStateChanged(EGameState state)
        {
            Debug.Log($"GameState: {state}");

            switch (state)
            {
                case EGameState.LOADING:
                    StartCoroutine(CRBoot());
                    break;

                case EGameState.HOME:
                    SetHomeBackdropVisible(true);
                    Audio.GameAudioService.PlayHomeMusic();
                    UIManager.I.Open(Define.UIName.HOME_MENU);
                    break;

                case EGameState.INGAME:
                    SetHomeBackdropVisible(false);
                    Audio.GameAudioService.PlayIngameMusic();
                    // GamePlayMenu chỉ được mở SAU KHI scene trận đấu đã nạp xong, vì nó đọc
                    // PlayerActor.Current và GameplayManager.I ngay trong OnOpen.
                    UIManager.I.Open(Define.UIName.GAMEPLAY_MENU);
                    break;
            }
        }

        private void SetHomeBackdropVisible(bool visible)
        {
            if (_homeBackdrop != null)
                _homeBackdrop.SetActive(visible);
        }

        #region Phím Back / Esc

        /// <summary>
        /// Nơi DUY NHẤT trong game đọc phím quay lại, rồi giao cho màn hình đang ở trên cùng tự xử.
        ///
        /// VÌ SAO PHẢI TỰ VIẾT DÙ NFRAMEWORK ĐÃ CÓ SẴN PHẦN NÀY:
        /// <c>UIManager.Update</c> có đúng đoạn code như dưới đây, nhưng nó bị khoá sau điều kiện
        /// <c>CanInteract</c>, mà thuộc tính đó lại được định nghĩa là
        /// <c>!_canvasGroup.blocksRaycasts</c> — tức là nó chỉ đúng khi giao diện đang bị KHOÁ.
        /// Vì <c>blocksRaycasts</c> mặc định là true, nhánh xử lý phím ở đó không bao giờ chạy.
        /// Đây là lỗi trong thư viện, và bên mình cố ý KHÔNG sửa file của bên thứ ba: một dòng sửa
        /// trong <c>ThirdParty</c> là một dòng sẽ biến mất lặng lẽ ở lần cập nhật thư viện sau.
        ///
        /// Viết ở đây thì vẫn dùng đúng hợp đồng có sẵn của framework
        /// (<c>BaseUIView.HandleOnKeyBack</c>), nên mỗi màn hình tự quyết định phím này nghĩa là gì
        /// và không màn hình nào phải tự đọc <c>Input</c>. Trên Android, nút Back của hệ điều hành
        /// đến với Unity dưới dạng đúng phím <c>Escape</c> này.
        /// </summary>
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (_isTransitioning || UIManager.I == null)
                return;

            UIManager.I.GetCurrentView()?.HandleOnKeyBack();
        }

        #endregion

        #region Boot

        private IEnumerator CRBoot()
        {
            ApplyApplicationSettings();

            var loading = UIManager.I.Open<LoadingPopup>(Define.UIName.LOADING_POPUP);
            loading.SetLabel("ĐANG TẢI...");
            yield return loading.FadeIn();

            float startTime = Time.unscaledTime;

            RegisterAndLoadSave();

            float remaining = MinimumLoadingSeconds - (Time.unscaledTime - startTime);
            if (remaining > 0f)
                yield return new WaitForSecondsRealtime(remaining);

            SetGameState(EGameState.HOME);

            yield return loading.FadeOut();
            UIManager.I.Close(loading);
        }

        /// <summary>
        /// Vài thiết lập cấp ứng dụng, đặt một lần lúc khởi động.
        ///
        /// SỐ KHUNG HÌNH KHÔNG CÓ Ô NÀO TRONG PROJECT SETTINGS để điền — nó chỉ đặt được bằng
        /// code lúc chạy. Mà nếu không đặt, Unity trên Android mặc định khoá 30 khung hình/giây.
        /// Với game bắn và né như thế này thì 30 khung hình là ì rõ rệt: cảm giác điều khiển nặng,
        /// và cú dash 0.5 giây chỉ còn 15 khung để người chơi đọc. Trong Editor lại luôn chạy 60+
        /// nên lỗi này KHÔNG BAO GIỜ lộ ra lúc phát triển — chỉ người cầm bản build Android mới thấy.
        /// </summary>
        private void ApplyApplicationSettings()
        {
            // Phải tắt đồng bộ dọc TRƯỚC. Còn bật thì nó khoá nhịp theo tần số quét của màn hình
            // và targetFrameRate bị bỏ qua hoàn toàn.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _config != null ? _config.TargetFrameRate : 60;

            // Không cho màn hình tự tắt giữa lúc đang chơi. Người chơi có thể đứng yên vài giây
            // để chờ hồi charge hoặc chờ wave sau, và Android tính quãng đó là "không hoạt động"
            // vì không có thao tác chạm nào.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        /// <summary>
        /// Nối mọi thứ cần lưu vào SaveManager rồi mới ra lệnh nạp.
        ///
        /// THỨ TỰ Ở ĐÂY LÀ BẮT BUỘC: nframework ghi rõ mọi ISaveable phải đăng ký TRƯỚC khi Load,
        /// và bản thân SaveManager không tự gọi Load ở Awake hay Start. Gom vào một hàm thì thứ tự
        /// nhìn thấy được ngay trên màn hình và không thể sai — thứ tự Awake giữa các object khác
        /// nhau thì Unity KHÔNG bảo đảm.
        ///
        /// Trước khi có scene Main, việc này nằm ở SaveBootstrap và chạy LẠI mỗi lần đổi scene.
        /// Bây giờ nó chạy đúng một lần trong cả vòng đời ứng dụng.
        /// </summary>
        private void RegisterAndLoadSave()
        {
            if (SaveManager.I == null)
            {
                Debug.LogError("[GameManager] Không có SaveManager trong scene Main. " +
                               "Âm lượng và thành tích tốt nhất sẽ không được lưu.", this);
                return;
            }

            if (SoundManager.I != null) SaveManager.I.RegisterSaveData(SoundManager.I);
            if (UserData.I != null)     SaveManager.I.RegisterSaveData(UserData.I);

            SaveManager.I.Load();
        }

        #endregion

        #region Chuyển màn hình

        /// <summary>Vào trận. Nạp scene Game additive rồi chuyển sang state INGAME.</summary>
        public void EnterInGame()
        {
            if (_isTransitioning || _state == EGameState.INGAME)
                return;

            StartCoroutine(CREnterInGame());
        }

        /// <summary>Về màn hình chính. Unload scene Game rồi chuyển sang state HOME.</summary>
        public void EnterHome()
        {
            if (_isTransitioning || _state == EGameState.HOME)
                return;

            StartCoroutine(CREnterHome());
        }

        /// <summary>
        /// Chơi lại trận hiện tại.
        ///
        /// CỐ TÌNH KHÔNG nạp lại scene — xem GameplayManager.Restart. State cấp ứng dụng KHÔNG đổi
        /// vì ứng dụng vẫn đang ở INGAME; chỉ có ván chơi bắt đầu lại. Đây là chỗ lệch có chủ ý so
        /// với template gốc, nơi RESET là một state riêng vì nó phải unload rồi load lại scene.
        /// </summary>
        public void EnterReset()
        {
            if (_isTransitioning)
                return;

            Time.timeScale = 1f;
            UIManager.I.CloseAllInLayer(EUILayer.Popup);
            GameplayManager.I?.Restart();
        }

        private IEnumerator CREnterInGame()
        {
            _isTransitioning = true;

            var loading = UIManager.I.Open<LoadingPopup>(Define.UIName.LOADING_POPUP);
            loading.SetLabel("ĐANG VÀO TRẬN...");
            yield return loading.FadeIn();

            // Trả nhịp thời gian về bình thường SAU KHI tấm màn đã che kín, để người chơi không
            // kịp thấy một nhịp game chạy tiếp trước lúc chuyển cảnh.
            Time.timeScale = 1f;

            // LoadingPopup nằm ở layer AlwaysOnTop nên không bị dòng dưới đóng nhầm.
            UIManager.I.CloseAllInLayer(EUILayer.Popup);
            UIManager.I.Close(Define.UIName.HOME_MENU);

            float startTime = Time.unscaledTime;
            yield return CRLoadSceneAdditive(Define.SceneName.GAME);

            float remaining = MinimumLoadingSeconds - (Time.unscaledTime - startTime);
            if (remaining > 0f)
                yield return new WaitForSecondsRealtime(remaining);

            // Chờ thêm đúng một khung hình để scene mới kịp chạy Awake và Start của mọi thứ.
            // Mở HUD ngay lập tức thì PlayerActor.Current có thể vẫn còn null.
            yield return null;

            SetGameState(EGameState.INGAME);

            yield return loading.FadeOut();
            UIManager.I.Close(loading);

            _isTransitioning = false;
        }

        private IEnumerator CREnterHome()
        {
            _isTransitioning = true;

            var loading = UIManager.I.Open<LoadingPopup>(Define.UIName.LOADING_POPUP);
            loading.SetLabel("ĐANG QUAY VỀ...");
            yield return loading.FadeIn();

            Time.timeScale = 1f;

            UIManager.I.CloseAllInLayer(EUILayer.Popup);
            UIManager.I.Close(Define.UIName.GAMEPLAY_MENU);

            float startTime = Time.unscaledTime;

            if (SceneManager.GetSceneByName(Define.SceneName.GAME).isLoaded)
                yield return SceneManager.UnloadSceneAsync(Define.SceneName.GAME, UnloadSceneOptions.None);

            // Dọn bộ nhớ của scene vừa unload. Bỏ qua bước này thì asset của trận cũ nằm lại
            // trong RAM cho tới lần GC không xác định — trên điện thoại tầm thấp đây là chênh
            // lệch giữa "vào ra trận mười lần vẫn mượt" và "lần thứ tư bị hệ điều hành giết".
            yield return Resources.UnloadUnusedAssets();

            float remaining = MinimumLoadingSeconds - (Time.unscaledTime - startTime);
            if (remaining > 0f)
                yield return new WaitForSecondsRealtime(remaining);

            SetGameState(EGameState.HOME);

            yield return loading.FadeOut();
            UIManager.I.Close(loading);

            _isTransitioning = false;
        }

        /// <summary>
        /// Nạp scene chồng lên Main rồi đặt nó làm scene hoạt động.
        ///
        /// SetActiveScene là bắt buộc, không phải tuỳ chọn: mọi Instantiate không chỉ định cha
        /// sẽ rơi vào scene hoạt động. Không đặt thì object sinh ra lúc chơi (đạn, hiệu ứng,
        /// quái của pool) sẽ rơi vào Main và SỐNG SÓT qua lần unload trận đấu.
        /// Nó cũng quyết định ánh sáng và skybox được lấy từ scene nào.
        /// </summary>
        private IEnumerator CRLoadSceneAdditive(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            // Chặn Unity tự bật scene mới lên ngay khi nạp xong, để mình còn giữ tấm màn.
            // Unity coi là "nạp xong" ở mốc 0.9 chứ không phải 1.0.
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
                yield return null;

            operation.allowSceneActivation = true;

            while (!operation.isDone)
                yield return null;

            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid())
                SceneManager.SetActiveScene(scene);
        }

        #endregion

        /// <summary>Thoát ứng dụng. Nút Thoát trên màn hình chính gọi hàm này.</summary>
        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
