using System.Collections.Generic;

namespace Survival.Enemies.States
{
    /// <summary>Ba trạng thái của quái. Dùng enum thay vì chuỗi để không thể gõ sai tên.</summary>
    public enum EEnemyState
    {
        Approach = 0,
        Attack = 1,
        Idle = 2,
    }

    /// <summary>
    /// Máy trạng thái cho AI quái.
    ///
    /// VÌ SAO TỰ VIẾT THAY VÌ DÙNG <c>NFramework.StateMachine</c> CÓ SẴN:
    /// Bản của framework là MonoBehaviour và lưu các trạng thái xuống scene bằng
    /// <c>[SerializeReference]</c>. Cách đó hợp với những trạng thái chỉ chứa số liệu,
    /// nhưng trạng thái của quái ở đây được TẠO LÚC CHẠY và giữ một tham chiếu sống
    /// tới chính con quái. Unity không lưu được tham chiếu kiểu đó: nó ghi trạng thái
    /// xuống đĩa rồi dựng lại như một object rỗng, tham chiếu tới con quái thành null,
    /// và AI ném lỗi NullReferenceException ở mỗi khung hình.
    ///
    /// Trong project thật đã xảy ra đúng như vậy: trạng thái đang chạy bị thoái hoá thành
    /// <c>NFramework.State</c> (lớp cha rỗng) sau khi Unity nạp lại scene.
    ///
    /// Bản này là một class C# thuần, không phải MonoBehaviour, nên KHÔNG có gì để serialize.
    /// Nó được <c>EnemyActor</c> sở hữu và gọi Tick — quyền điều khiển nằm hoàn toàn ở code.
    ///
    /// Bài học: dùng lại thư viện có sẵn là tốt, nhưng chỉ khi cách nó hoạt động
    /// hợp với cách mình cần dùng. Ở đây "lưu mọi thứ xuống đĩa" xung khắc trực tiếp với
    /// "trạng thái chỉ tồn tại lúc chạy".
    /// </summary>
    public class EnemyStateMachine
    {
        private readonly Dictionary<EEnemyState, EnemyState> _states = new Dictionary<EEnemyState, EnemyState>();

        public EnemyState Current { get; private set; }

        public EEnemyState CurrentId { get; private set; }

        /// <summary>Tắt cờ này để dừng AI mà không phải gỡ trạng thái ra. Dùng khi quái chết.</summary>
        public bool IsRunning { get; set; } = true;

        public void Register(EEnemyState id, EnemyState state)
        {
            state.Attach(this);
            _states[id] = state;
        }

        public void SetState(EEnemyState id)
        {
            if (!_states.TryGetValue(id, out var next))
                return;

            Current?.OnExit();
            Current = next;
            CurrentId = id;
            Current.OnEnter();
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning || Current == null)
                return;

            Current.OnUpdate(deltaTime);
        }
    }

    /// <summary>Lớp cha cho ba trạng thái của quái.</summary>
    public abstract class EnemyState
    {
        protected readonly EnemyActor Enemy;

        private EnemyStateMachine _machine;

        protected EnemyState(EnemyActor enemy)
        {
            Enemy = enemy;
        }

        internal void Attach(EnemyStateMachine machine) => _machine = machine;

        public virtual void OnEnter() { }

        public virtual void OnExit() { }

        /// <summary>
        /// Nhận thẳng deltaTime từ bên gọi thay vì tự đọc <c>Time.deltaTime</c>.
        /// Nhờ vậy viết unit test cho AI được: truyền vào bước thời gian cố định
        /// mà không cần chạy game thật.
        /// </summary>
        public abstract void OnUpdate(float deltaTime);

        protected void GoTo(EEnemyState id) => _machine.SetState(id);
    }
}
