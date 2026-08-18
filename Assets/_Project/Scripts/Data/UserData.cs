using System;
using NFramework;
using UnityEngine;

namespace Survival.Data
{
    /// <summary>
    /// Ghi nhớ thành tích tốt nhất người chơi từng đạt, và giữ lại sau khi tắt game.
    ///
    /// VÌ SAO CẦN: một màn hình chính chỉ có mỗi nút Play thì không nói lên điều gì. Có dòng
    /// "Tốt nhất: Wave 4 · hạ 27 quái" thì người chơi lập tức có một cái mốc để vượt qua,
    /// và đó là lý do họ bấm Play lần thứ hai.
    ///
    /// So sánh theo THỨ TỰ ƯU TIÊN chứ không gộp thành một điểm số: wave cao hơn luôn thắng,
    /// wave bằng nhau thì mới xét số quái đã hạ. Gộp thành điểm thì phải bịa ra tỉ giá quy đổi
    /// giữa "một wave" và "một con quái" — con số đó không có căn cứ nào cả.
    ///
    /// Việc đăng ký với <c>SaveManager</c> KHÔNG nằm ở đây mà nằm ở nơi khởi động ứng dụng,
    /// vì thứ tự đăng ký so với lúc nạp dữ liệu là quan trọng và nên nhìn thấy được ở một chỗ.
    /// </summary>
    public class UserData : SingletonMono<UserData>, ISaveable
    {
        [Serializable]
        public class SaveData
        {
            public int bestWave;
            public int bestKills;
            public float bestSeconds;
        }

        private SaveData _data = new SaveData();

        public int BestWave => _data.bestWave;
        public int BestKills => _data.bestKills;
        public float BestSeconds => _data.bestSeconds;

        /// <summary>Đã chơi xong ván nào chưa. Chưa thì màn hình chính ẩn dòng thành tích đi.</summary>
        public bool HasRecord => _data.bestWave > 0;

        /// <summary>
        /// Báo kết quả một ván vừa kết thúc. Chỉ ghi đè khi thật sự tốt hơn.
        /// </summary>
        public void Submit(int wave, int kills, float seconds)
        {
            bool better = wave > _data.bestWave
                          || (wave == _data.bestWave && kills > _data.bestKills);

            if (!better)
                return;

            _data.bestWave = wave;
            _data.bestKills = kills;
            _data.bestSeconds = seconds;

            // Không báo cờ này thì SaveManager coi như không có gì đổi và bỏ qua lúc tới kỳ lưu.
            DataChanged = true;
        }

        #region ISaveable

        /// <summary>
        /// ⚠️ CỐ TÌNH KHÔNG TRÙNG TÊN LỚP. Lớp này trước đây tên là <c>BestRecord</c>, và chuỗi
        /// dưới đây chính là khoá mà dữ liệu đã lưu trên máy người chơi đang dùng.
        /// Sửa nó cho "khớp tên lớp" là làm mất sạch thành tích của mọi bản cài sẵn có —
        /// và hỏng luôn phép thử "thành tích còn nguyên sau khi tắt app".
        /// </summary>
        public string SaveKey => "BestRecord";

        public bool DataChanged { get; set; }

        public object GetData() => _data;

        public void SetData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                _data = new SaveData();
                return;
            }

            var loaded = JsonUtility.FromJson<SaveData>(data);
            _data = loaded ?? new SaveData();
        }

        public void OnAllDataLoaded() { }

        #endregion
    }
}
