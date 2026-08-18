namespace Survival
{
    /// <summary>
    /// Nơi duy nhất chứa các chuỗi định danh mà code phải gõ đúng từng ký tự.
    ///
    /// VÌ SAO GOM VÀO ĐÂY: tên scene và tên prefab UI được dùng dưới dạng CHUỖI lúc chạy,
    /// nên gõ sai một ký tự hoặc đổi tên file mà quên sửa code thì trình biên dịch KHÔNG
    /// báo gì cả — lỗi chỉ nổ ra lúc chạy, và thường là ở đúng lúc chuyển màn hình.
    /// Gom về một chỗ thì đổi tên chỉ phải sửa một dòng, và mọi nơi dùng đều có autocomplete.
    /// </summary>
    public static class Define
    {
        /// <summary>
        /// Định danh của các màn hình UI. Giá trị PHẢI TRÙNG TÊN FILE PREFAB
        /// trong <c>Assets/_Project/Resources/UI/</c> — UIManager nạp prefab theo chính chuỗi này.
        /// </summary>
        public static class UIName
        {
            public const string HOME_MENU      = "HomeMenu";
            public const string GAMEPLAY_MENU  = "GamePlayMenu";
            public const string LOADING_POPUP  = "LoadingPopup";
            public const string SETTINGS_POPUP = "SettingsPopup";
            public const string PAUSE_POPUP    = "PausePopup";
            public const string RESULT_POPUP   = "ResultPopup";
        }

        public static class SceneName
        {
            /// <summary>Scene khởi động, chứa mọi manager sống suốt vòng đời ứng dụng.</summary>
            public const string MAIN = "Main";

            /// <summary>Scene trận đấu, được nạp ADDITIVE chồng lên Main.</summary>
            public const string GAME = "Game";
        }
    }
}
