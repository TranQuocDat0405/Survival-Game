using System;

namespace Survival.Stats
{
    /// <summary>
    /// Hợp đồng tối thiểu để "đọc được chỉ số".
    ///
    /// Nhờ interface này mà <c>Health</c> không cần biết nó đang gắn trên player hay trên quái —
    /// nó chỉ cần một thứ gì đó trả lời được câu hỏi "giáp của mày bằng bao nhiêu".
    /// Đây là cách tránh việc phải viết hai bản Health riêng cho hai phe.
    /// </summary>
    public interface IStatProvider
    {
        float Get(EStatType type);

        /// <summary>
        /// Bắn ra mỗi khi một chỉ số đổi giá trị.
        ///
        /// Vì sao interface cần sự kiện này: khi player lên cấp, máu tối đa tăng từ 500 lên 540.
        /// Máu hiện tại KHÔNG đổi ở thời điểm đó, nên nếu thanh máu chỉ nghe "máu hiện tại đổi"
        /// thì nó sẽ vẫn vẽ theo tỉ lệ cũ (500/500 = đầy) trong khi thực tế đang là 540/540.
        /// Có sự kiện này thì <c>Health</c> biết mà báo lại cho UI vẽ đúng ngay lập tức.
        /// </summary>
        event Action<EStatType, float> OnStatChanged;
    }
}
