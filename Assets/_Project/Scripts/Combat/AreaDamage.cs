using System.Collections.Generic;
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

        /// <summary>
        /// Danh sách những mục tiêu đã ăn đòn trong CÙNG MỘT vụ nổ nhiều điểm.
        /// Cấp phát sẵn một lần và dùng lại, vì dash được bấm liên tục suốt trận.
        /// </summary>
        private static readonly List<IDamageable> AlreadyHit = new List<IDamageable>(16);

        /// <summary>
        /// Nổ tại NHIỀU ĐIỂM cùng một lúc, nhưng mỗi mục tiêu chỉ ăn đòn ĐÚNG MỘT LẦN.
        ///
        /// Dùng cho cú lướt: vụ nổ được rải dọc đường vừa lướt qua thay vì dồn hết vào điểm
        /// kết thúc. Lý do là hình học của chính kỹ năng này — dash là để CHẠY KHỎI đám quái,
        /// nên tới lúc nổ ở điểm cuối thì mấy con đang bám sau lưng đã nằm ngoài tầm.
        /// Đo được: quái áp sát ở 1.3 unit, player lướt 3 unit, mấy con phía sau kết thúc ở
        /// 4.3 unit — ngoài hẳn bán kính 3, và chỉ 2 trên 6 con bị dính.
        ///
        /// CHỐNG TRÙNG LÀ BẮT BUỘC, không phải tuỳ chọn. Ba điểm nổ đặt cách nhau khoảng 1 unit
        /// mà bán kính mỗi điểm là 3, nên các vùng nổ chồng lên nhau rất nhiều. Không chống trùng
        /// thì một con đứng giữa sẽ ăn ba lần sát thương, và cú lướt bỗng mạnh gấp ba —
        /// vừa sai spec vừa phá vỡ cân bằng.
        /// </summary>
        /// <param name="centers">Các tâm nổ. Chỉ đọc <paramref name="centerCount"/> phần tử đầu.</param>
        /// <param name="radii">
        /// Bán kính RIÊNG cho từng tâm, cùng thứ tự với <paramref name="centers"/>.
        ///
        /// Để riêng chứ không dùng chung một con số, vì các tâm nổ không nhất thiết ngang nhau:
        /// ở cú lướt, điểm kết thúc mang đúng bán kính spec còn mấy quả rơi dọc đường thì nhỏ hơn.
        /// Dùng chung một bán kính sẽ khiến vùng gây sát thương phình ra xa hơn hẳn thứ người chơi
        /// nhìn thấy, và họ ăn đòn mà không hiểu vì sao.
        /// </param>
        /// <returns>Số mục tiêu KHÁC NHAU đã trúng đòn.</returns>
        public static int ExplodeMultiPoint(
            List<Vector3> centers,
            List<float> radii,
            int centerCount,
            float rawDamage,
            EDamageSource source,
            GameObject instigator,
            LayerMask targetMask)
        {
            AlreadyHit.Clear();

            for (int c = 0; c < centerCount && c < centers.Count && c < radii.Count; c++)
            {
                int count = Physics.OverlapSphereNonAlloc(
                    centers[c], radii[c], Buffer, targetMask, QueryTriggerInteraction.Collide);

                for (int i = 0; i < count; i++)
                {
                    var collider = Buffer[i];
                    if (collider == null)
                        continue;

                    var damageable = collider.GetComponentInParent<IDamageable>();
                    if (damageable == null || !damageable.IsAlive)
                        continue;

                    if (AlreadyHit.Contains(damageable))
                        continue;

                    AlreadyHit.Add(damageable);

                    var info = new DamageInfo(rawDamage, source, instigator, damageable.Transform.position);
                    damageable.TakeDamage(in info);
                }
            }

            return AlreadyHit.Count;
        }
    }
}
