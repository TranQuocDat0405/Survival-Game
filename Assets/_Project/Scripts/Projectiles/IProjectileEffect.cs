using Survival.Combat;
using UnityEngine;

namespace Survival.Projectiles
{
    /// <summary>
    /// Hiệu ứng phụ mà một viên đạn gây ra khi trúng, ngoài phần sát thương tức thời.
    ///
    /// Hiện tại chỉ có một cài đặt: đạn độc của quái đánh xa gây hiệu ứng độc.
    /// Nhưng nhờ tách thành interface, sau này thêm đạn làm chậm, đạn gây choáng, đạn hút máu...
    /// thì <see cref="ProjectileBase"/> không phải sửa một dòng nào —
    /// nó chỉ gọi <see cref="ApplyTo"/> mà không cần biết hiệu ứng đó là gì.
    /// </summary>
    public interface IProjectileEffect
    {
        void ApplyTo(IDamageable target, GameObject instigator);
    }
}
