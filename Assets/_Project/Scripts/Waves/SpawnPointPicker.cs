using UnityEngine;

namespace Survival.Waves
{
    /// <summary>
    /// Chọn chỗ sinh quái: NGẪU NHIÊN MỌI HƯỚNG, nhưng luôn nằm NGOÀI khung hình.
    ///
    /// VÌ SAO KHÔNG CHỈ DÙNG MỘT BÁN KÍNH CỐ ĐỊNH:
    /// Camera nhìn chếch từ trên xuống nên vùng nó thấy KHÔNG PHẢI hình tròn quanh player —
    /// nó là một hình thang trải rất xa về phía trước và rất hẹp về phía sau.
    /// Với góc camera của game này, phía trước player nhìn thấy được tới khoảng 18 unit,
    /// còn phía sau chỉ khoảng 6 unit. Một bán kính cố định 14 unit vì thế sẽ
    /// nằm ngoài màn hình khi ở phía sau, nhưng lại HIỆN NGAY GIỮA MÀN HÌNH khi ở phía trước.
    /// Đó chính là cảnh quái "mọc ra từ hư không" mà người chơi nhìn thấy.
    ///
    /// CÁCH LÀM: bốc một góc ngẫu nhiên trước, rồi mới dò ra xa dần theo góc đó cho tới khi
    /// điểm đó ra khỏi khung hình. Nhờ tách hai bước như vậy, MỌI HƯỚNG đều có cơ hội như nhau —
    /// nếu làm ngược lại (bốc cả góc lẫn bán kính rồi loại bỏ điểm nào lộ ra màn hình)
    /// thì hướng phía trước sẽ bị loại gần hết và quái hầu như chỉ xuất hiện từ sau lưng.
    /// </summary>
    public static class SpawnPointPicker
    {
        /// <summary>Bước dò ra xa mỗi lần, tính bằng unit. Nhỏ thì chính xác hơn nhưng tốn phép tính hơn.</summary>
        private const float SearchStep = 1.5f;

        /// <summary>
        /// Tìm một điểm sinh quái hợp lệ.
        /// </summary>
        /// <param name="center">Tâm, thường là vị trí player.</param>
        /// <param name="minRadius">Không bao giờ sinh gần hơn khoảng này, kể cả khi chỗ đó đã khuất camera.</param>
        /// <param name="maxRadius">Dò xa nhất tới đây rồi bỏ cuộc với góc đang xét.</param>
        /// <param name="arenaHalfExtent">Nửa cạnh sân đấu. Điểm sinh luôn bị kẹp vào trong để quái không kẹt ngoài tường.</param>
        /// <param name="camera">Camera dùng để kiểm tra khuất tầm nhìn. Null thì bỏ qua bước này.</param>
        /// <param name="viewportMargin">Nới thêm quanh mép màn hình, tính theo tỉ lệ. 0.1 nghĩa là phải ra ngoài mép thêm 10%.</param>
        /// <param name="checkHeight">Độ cao thân quái dùng khi kiểm tra, để phần đầu cũng không ló vào khung hình.</param>
        /// <param name="angleAttempts">Thử bao nhiêu góc khác nhau trước khi dùng phương án dự phòng.</param>
        /// <param name="blockMask">Layer của vật cản. Điểm sinh nằm đè lên chúng sẽ bị loại. Để trống thì bỏ qua bước này.</param>
        /// <param name="clearRadius">Bán kính khoảng trống cần có quanh điểm sinh.</param>
        public static Vector3 Pick(
            Vector3 center,
            float minRadius,
            float maxRadius,
            float arenaHalfExtent,
            Camera camera,
            float viewportMargin = 0.12f,
            float checkHeight = 1.8f,
            int angleAttempts = 12,
            LayerMask blockMask = default,
            float clearRadius = 0.6f)
        {
            // Bốc trước một góc lệch ngẫu nhiên rồi rải đều các lần thử quanh vòng tròn.
            // Cách này phủ đều mọi hướng tốt hơn là bốc ngẫu nhiên độc lập từng lần,
            // vốn có thể vô tình thử ba lần liền cùng một phía.
            float angleOffset = Random.Range(0f, Mathf.PI * 2f);

            for (int i = 0; i < angleAttempts; i++)
            {
                float angle = angleOffset + i * (Mathf.PI * 2f / angleAttempts);
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                for (float radius = minRadius; radius <= maxRadius; radius += SearchStep)
                {
                    Vector3 candidate = ClampToArena(center + direction * radius, arenaHalfExtent);

                    // Việc kẹp vào sân có thể kéo điểm ngược trở lại gần player,
                    // nên phải kiểm tra lại khoảng cách sau khi kẹp.
                    if ((candidate - center).sqrMagnitude < minRadius * minRadius)
                        continue;

                    if (IsVisible(candidate, camera, viewportMargin, checkHeight))
                        continue;

                    if (IsBlocked(candidate, blockMask, clearRadius))
                        continue;

                    return candidate;
                }
            }

            // Dự phòng: khi player đứng sát góc sân, có thể không hướng nào vừa khuất camera
            // vừa nằm trong sân. Khi đó sinh ở phía SAU camera — đó luôn là vùng khuất nhất.
            Vector3 behind = camera != null ? -camera.transform.forward : Vector3.back;
            behind.y = 0f;
            if (behind.sqrMagnitude < 0.0001f)
                behind = Vector3.back;
            behind.Normalize();

            // Ngay cả phương án dự phòng cũng phải tránh sinh quái vào trong gốc cây,
            // nên dò thêm vài nấc ra xa dần trước khi đành chấp nhận điểm cuối cùng.
            for (float radius = minRadius; radius <= maxRadius; radius += SearchStep)
            {
                Vector3 candidate = ClampToArena(center + behind * radius, arenaHalfExtent);
                if (!IsBlocked(candidate, blockMask, clearRadius))
                    return candidate;
            }

            return ClampToArena(center + behind * minRadius, arenaHalfExtent);
        }

        /// <summary>
        /// Chỗ này có vật cản chắn không.
        ///
        /// Kiểm tra ở NGANG THÂN quái chứ không phải dưới chân: hình cầu đặt sát mặt đất
        /// có thể chạm vào chính mặt nền, và khi đó mọi điểm trên sân đều bị coi là có vật cản.
        /// </summary>
        private static bool IsBlocked(Vector3 groundPosition, LayerMask blockMask, float clearRadius)
        {
            if (blockMask.value == 0)
                return false;

            return Physics.CheckSphere(
                groundPosition + Vector3.up * clearRadius,
                clearRadius,
                blockMask,
                QueryTriggerInteraction.Ignore);
        }

        private static Vector3 ClampToArena(Vector3 position, float halfExtent)
        {
            position.x = Mathf.Clamp(position.x, -halfExtent, halfExtent);
            position.z = Mathf.Clamp(position.z, -halfExtent, halfExtent);
            position.y = 0f;
            return position;
        }

        /// <summary>
        /// Điểm này có lọt vào khung hình không.
        ///
        /// Kiểm tra cả chân lẫn đỉnh đầu: quái cao gần 2 unit, nếu chỉ xét điểm dưới chân
        /// thì vẫn có trường hợp chân khuất mà cái đầu ló ra ở mép dưới màn hình.
        /// </summary>
        private static bool IsVisible(Vector3 groundPosition, Camera camera, float margin, float height)
        {
            if (camera == null)
                return false;

            return IsPointVisible(groundPosition, camera, margin)
                || IsPointVisible(groundPosition + Vector3.up * height, camera, margin);
        }

        private static bool IsPointVisible(Vector3 worldPoint, Camera camera, float margin)
        {
            Vector3 viewport = camera.WorldToViewportPoint(worldPoint);

            // z âm nghĩa là điểm nằm phía sau camera, chắc chắn không nhìn thấy.
            if (viewport.z <= 0f)
                return false;

            return viewport.x >= -margin && viewport.x <= 1f + margin
                && viewport.y >= -margin && viewport.y <= 1f + margin;
        }
    }
}
