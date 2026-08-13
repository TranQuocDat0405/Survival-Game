namespace Survival.Enemies.States
{
    /// <summary>
    /// Giai đoạn 3 của chu kỳ trong spec: "đứng im 1 giây → lặp lại (tiếp cận tiếp)".
    ///
    /// Quãng nghỉ này không phải để trang trí — nó chính là khoảng thở của người chơi.
    /// Quái chạy 3 unit/giây còn player chỉ 2 unit/giây, nên nếu quái đuổi liên tục
    /// không nghỉ thì người chơi không bao giờ thoát ra được. Một giây đứng im
    /// là lúc người chơi lùi lại, bắn trả, hoặc đặt bom.
    /// </summary>
    public class EnemyIdleState : EnemyState
    {
        private float _timer;

        public EnemyIdleState(EnemyActor enemy) : base(enemy) { }

        public override void OnEnter()
        {
            _timer = 0f;
            Enemy.StopMoving();
        }

        public override void OnUpdate(float deltaTime)
        {
            _timer += deltaTime;

            if (_timer >= Enemy.Config.IdleAfterAttack)
                GoTo(EEnemyState.Approach);
        }
    }
}
