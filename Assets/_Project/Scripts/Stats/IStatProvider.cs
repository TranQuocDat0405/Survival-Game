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
    }
}
