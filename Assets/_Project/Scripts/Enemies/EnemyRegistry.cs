using System;
using System.Collections.Generic;
using NFramework;
using UnityEngine;

namespace Survival.Enemies
{
    /// <summary>
    /// Danh bạ những con quái đang sống trong màn.
    ///
    /// VÌ SAO CẦN:
    /// Hệ thống wave phải biết "đã diệt hết quái chưa" để quyết định có spawn wave kế hay không.
    /// Cách ngây thơ là gọi <c>FindObjectsOfType&lt;Enemy&gt;()</c> mỗi khung hình —
    /// hàm đó quét TOÀN BỘ scene và trả về một MẢNG MỚI mỗi lần gọi.
    /// Chạy 60 lần mỗi giây thì vừa tốn thời gian máy, vừa liên tục sinh rác cho bộ dọn rác.
    ///
    /// Ở đây quái tự báo danh lúc sinh ra và tự xoá tên lúc chết. Muốn biết còn bao nhiêu con
    /// thì chỉ cần đọc <see cref="ActiveCount"/> — một phép đọc biến, gần như miễn phí.
    ///
    /// Quan trọng hơn: có sự kiện <see cref="OnEnemyDied"/> nên hệ thống wave và hệ thống EXP
    /// KHÔNG phải kiểm tra gì mỗi khung hình cả — chúng chỉ ngồi im chờ được gọi.
    /// </summary>
    public class EnemyRegistry : SingletonMono<EnemyRegistry>
    {
        private readonly List<EnemyActor> _active = new List<EnemyActor>();

        /// <summary>Bắn ra khi một con quái chết. Tham số là con quái vừa chết.</summary>
        public event Action<EnemyActor> OnEnemyDied;

        /// <summary>Bắn ra khi số quái còn sống về 0. Hệ thống wave nghe cái này.</summary>
        public event Action OnAllEnemiesCleared;

        public int ActiveCount => _active.Count;

        public IReadOnlyList<EnemyActor> Active => _active;

        public void Register(EnemyActor enemy)
        {
            if (enemy == null || _active.Contains(enemy))
                return;

            _active.Add(enemy);
        }

        public void NotifyDied(EnemyActor enemy)
        {
            if (enemy == null)
                return;

            if (!_active.Remove(enemy))
                return;   // đã bị xoá rồi, tránh bắn sự kiện hai lần

            OnEnemyDied?.Invoke(enemy);

            if (_active.Count == 0)
                OnAllEnemiesCleared?.Invoke();
        }

        /// <summary>Gỡ tên mà KHÔNG tính là chết. Dùng khi dọn màn để chơi lại.</summary>
        public void Unregister(EnemyActor enemy)
        {
            if (enemy != null)
                _active.Remove(enemy);
        }

        /// <summary>Giết sạch quái đang sống. Dùng cho nút debug và cho lúc chơi lại.</summary>
        public void KillAll()
        {
            // Duyệt ngược vì mỗi lần giết sẽ làm con quái tự gỡ tên khỏi danh sách,
            // duyệt xuôi sẽ bị nhảy cóc qua phần tử.
            for (int i = _active.Count - 1; i >= 0; i--)
                _active[i].Kill();
        }
    }
}
