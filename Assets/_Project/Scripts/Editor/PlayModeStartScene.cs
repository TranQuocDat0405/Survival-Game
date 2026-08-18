using UnityEditor;
using UnityEditor.SceneManagement;

namespace Survival.EditorTools
{
    /// <summary>
    /// Bấm Play ở BẤT KỲ scene nào cũng luôn khởi động từ <c>Main</c>.
    ///
    /// VÌ SAO CẦN: sau khi tách kiến trúc, mọi manager sống suốt vòng đời ứng dụng
    /// (GameManager, UIManager, SaveManager, SoundManager) đều nằm trong Main, còn scene trận đấu
    /// được nạp chồng lên. Mở thẳng Game.unity rồi bấm Play thì không có manager nào tồn tại và
    /// game trông như bị hỏng — trong khi thực ra chỉ là vào sai cửa.
    ///
    /// Dòng dưới đây làm cho việc đó không xảy ra được: Unity luôn nạp Main trước, Main tự đưa
    /// người chơi đi tiếp. Người chấm mở scene nào rồi bấm Play cũng chơi được, và người phát
    /// triển không phải nhớ chuyển scene mỗi lần muốn test.
    ///
    /// Đây là script CHỈ CHẠY TRONG EDITOR, không đi vào bản build.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartScene
    {
        private const string MainScenePath = "Assets/_Project/Scenes/Main.unity";

        private const string MenuPath = "Survival/Luôn Play từ scene Main";

        /// <summary>
        /// Bật/tắt được qua menu <c>Survival</c>, và lựa chọn đó nhớ theo từng máy chứ không đi
        /// vào git — vì nó là thói quen làm việc cá nhân, không phải cấu hình của project.
        ///
        /// Có công tắc là cần thiết chứ không phải tiện tay: trong lúc chuyển từ kiến trúc cũ
        /// sang kiến trúc mới, hai bộ giao diện tồn tại song song và phải mở được scene cũ để
        /// đối chiếu. Không có công tắc thì mỗi lần muốn xem bản cũ lại phải sửa code rồi ngồi
        /// chờ Unity nạp lại toàn bộ assembly.
        /// </summary>
        private const string EnabledPrefKey = "Survival.PlayModeStartScene.Enabled";

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPrefKey, false);
            set => EditorPrefs.SetBool(EnabledPrefKey, value);
        }

        static PlayModeStartScene() => Apply();

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, isValidateFunction: true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void Apply()
        {
            if (!Enabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            var mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);

            // Chưa tạo Main (hoặc vừa đổi đường dẫn) thì để nguyên hành vi mặc định của Unity
            // thay vì ném lỗi — bấm Play vẫn phải chạy được cái gì đó.
            EditorSceneManager.playModeStartScene = mainScene;
        }
    }
}
