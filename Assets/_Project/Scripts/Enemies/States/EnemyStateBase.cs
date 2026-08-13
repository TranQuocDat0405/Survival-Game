using NFramework;

namespace Survival.Enemies.States
{
    /// <summary>Tên định danh của ba trạng thái. Gom vào hằng số để không gõ sai chuỗi.</summary>
    public static class EnemyStateIds
    {
        public const string Approach = "Approach";
        public const string Attack = "Attack";
        public const string Idle = "Idle";
    }

    /// <summary>
    /// Lớp cha cho các trạng thái của quái, chỉ để giữ sẵn tham chiếu tới con quái
    /// và một hàm chuyển trạng thái cho gọn.
    ///
    /// Kế thừa <see cref="NFramework.State"/> có sẵn trong framework: nó đã lo phần
    /// gọi OnEnter / OnUpdate / OnExit đúng lúc, mình không cần viết lại máy trạng thái.
    /// </summary>
    public abstract class EnemyStateBase : State
    {
        protected readonly EnemyActor Enemy;

        protected EnemyStateBase(string id, EnemyActor enemy) : base(id)
        {
            Enemy = enemy;
        }

        protected void GoTo(string stateId) => StateMachine.SetState(stateId);
    }
}
