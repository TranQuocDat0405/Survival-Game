using UnityEngine;

namespace Survival.Combat
{
    /// <summary>
    /// Gây sát thương cho mọi mục tiêu trong một hình cầu.
    ///
    /// Dùng chung cho bom (bán kính 5) và cho vụ nổ cuối đường dash (bán kính 3).
    /// Gom vào một chỗ vì hai skill này chỉ khác nhau ở CON SỐ, không khác ở cách làm —
    /// viết hai lần thì sau này sửa một chỗ sẽ quên chỗ kia.
    ///
    /// Về hiệu năng: dùng bản <c>NonAlloc</c> ghi kết quả vào mảng cấp phát sẵn.
    /// Bản thường (<c>Physics.OverlapSphere</c>) tạo một mảng MỚI mỗi lần nổ,
    /// và mảng đó trở thành rác mà bộ dọn rác GC phải dừng game lại để thu gom.
    /// </summary>
    public static class AreaDamage
    {
        /// <summary>
        /// Đủ lớn cho mọi tình huống của game này (một wave nhiều nhất 6 con quái).
        /// Nếu có nhiều mục tiêu hơn sức chứa, những con dư sẽ không bị trúng —
        /// nên bộ đệm được để rộng rãi hơn nhiều so với nhu cầu thực tế.
        /// </summary>
        private static readonly Collider[] Buffer = new Collider[32];

        /// <summary>
        /// Nổ tại một điểm.
        /// </summary>
        /// <param name="rawDamage">Sát thương gốc, ĐÃ nhân DamageMultiplier nếu người gây là player.</param>
        /// <returns>Số mục tiêu thực sự trúng đòn. Dùng để quyết định có rung camera hay không.</returns>
        public static int Explode(
            Vector3 center,
            float radius,
            float rawDamage,
            EDamageSource source,
            GameObject instigator,
            LayerMask targetMask)
        {
            int count = Physics.OverlapSphereNonAlloc(
                center, radius, Buffer, targetMask, QueryTriggerInteraction.Collide);

            int hitCount = 0;

            for (int i = 0; i < count; i++)
            {
                var collider = Buffer[i];
                if (collider == null)
                    continue;

                // GetComponentInParent chứ không phải GetComponent: collider thường nằm trên
                // GameObject con (phần thân, phần hình ảnh), còn Health nằm ở gốc nhân vật.
                var damageable = collider.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                    continue;

                var info = new DamageInfo(rawDamage, source, instigator, damageable.Transform.position);
                damageable.TakeDamage(in info);
                hitCount++;
            }

            // Mỗi nhân vật chỉ có một collider nên không con nào bị tính hai lần.
            // Nếu sau này nhân vật có nhiều collider (đầu, thân, chân) thì phải thêm
            // danh sách chống trùng ở đây.
            return hitCount;
        }
    }
}
