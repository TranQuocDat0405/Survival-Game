namespace Survival.Enemies.States
{
    /// <summary>
    /// Giai đoạn 1 của chu kỳ trong spec: "di chuyển tiếp cận player".
    ///
    /// Vừa xoay về phía player vừa đi tới. Khi đã vào tầm đánh thì chuyển sang ra đòn.
    /// </summary>
    public class EnemyApproachState : EnemyState
    {
        public EnemyApproachState(EnemyActor enemy) : base(enemy) { }

        public override void OnUpdate(float deltaTime)
        {
            if (!Enemy.HasTarget)
            {
                Enemy.StopMoving();
                return;
            }

            Enemy.RotateTowardsTarget(deltaTime);
            Enemy.MoveTowardsTarget();

            // So sánh bình phương khoảng cách với bình phương tầm đánh.
            // Cách này tránh phải tính căn bậc hai (phép toán tương đối đắt) ở mỗi khung hình
            // của mỗi con quái — mà kết quả so sánh thì hoàn toàn tương đương.
            float range = Enemy.Config.AttackRange;
            if (Enemy.SqrDistanceToTarget() <= range * range)
                GoTo(EEnemyState.Attack);
        }

        public override void OnExit() => Enemy.StopMoving();
    }
}
