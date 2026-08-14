using UnityEngine;
using UnityEngine.AI;

namespace Survival.Waves
{
    /// <summary>
    /// Chọn chỗ sinh quái: NGẪU NHIÊN MỌI HƯỚNG, nằm NGOÀI khung hình, và ĐI TỚI ĐƯỢC PLAYER.
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
    ///
    /// VÌ SAO PHẢI HỎI THÊM NAVMESH:
    /// Sân đấu có rừng dày, và trong rừng có những hốc bị cây vây kín tứ phía. Một điểm sinh
    /// rơi vào đó thì trống trải, khuất camera, đủ xa — qua hết mọi bài kiểm tra hình học —
    /// nhưng con quái sinh ra ở đấy sẽ húc đầu vào thân cây tới hết ván mà không bao giờ
    /// gặp được người chơi. Đo thực tế trước khi sửa: 35% số điểm sinh bị như vậy, và khi
    /// player đứng sâu trong rừng thì lên tới 78%.
    /// Hình học chỉ trả lời được "chỗ này có trống không". Chỉ NavMesh mới trả lời được
    /// "từ chỗ này có ĐƯỜNG ĐI tới người chơi không" — nên đó là bài kiểm tra cuối cùng.
    /// </summary>
    public static class SpawnPointPicker
    {
        /// <summary>Bước dò ra xa mỗi lần, tính bằng unit. Nhỏ thì chính xác hơn nhưng tốn phép tính hơn.</summary>
        private const float SearchStep = 1.5f;

        /// <summary>Dò xa nhất bao nhiêu để tìm mặt lưới đi được dưới chân người chơi.</summary>
        private const float TargetSampleRadius = 3f;

        /// <summary>
        /// Tối đa bao nhiêu lần hỏi đường trên MỘT hướng trước khi bỏ hướng đó mà đổi hướng khác.
        ///
        /// Các điểm trên cùng một hướng nằm sát nhau nên gần như luôn cùng nằm trong một hốc kín:
        /// hỏi mãi một hướng chỉ tốn phép tính mà vẫn ra cùng một câu trả lời. Đổi hướng thì
        /// khả năng thoát khỏi hốc cao hơn hẳn.
        /// </summary>
        private const int MaxPathChecksPerAngle = 3;

        /// <summary>
        /// Dùng lại một đối tượng đường đi duy nhất cho mọi lần sinh quái.
        ///
        /// <see cref="NavMeshPath"/> là class, cấp phát mới mỗi lần sinh quái sẽ đều đặn
        /// ném rác cho bộ dọn rác — đúng thứ gây khựng hình giữa lúc đánh nhau.
        /// Hàm này chỉ chạy trên luồng chính nên dùng chung một đối tượng là an toàn.
        /// </summary>
        private static readonly NavMeshPath ReusablePath = new NavMeshPath();

        /// <summary>
        /// Tìm một điểm sinh quái hợp lệ.
        /// </summary>
        /// <param name="center">Tâm, thường là vị trí player.</param>
        /// <param name="minRadius">Không bao giờ sinh gần hơn khoảng này, kể cả khi chỗ đó đã khuất camera.</param>
        /// <param name="maxRadius">Dò xa nhất tới đây rồi bỏ cuộc với góc đang xét.</param>
        /// <param name="arenaExtent">Nửa cạnh sân đấu vuông. Điểm sinh luôn bị kéo vào trong để quái không rơi ra ngoài tường.</param>
        /// <param name="camera">Camera dùng để kiểm tra khuất tầm nhìn. Null thì bỏ qua bước này.</param>
        /// <param name="viewportMargin">Nới thêm quanh mép màn hình, tính theo tỉ lệ. 0.1 nghĩa là phải ra ngoài mép thêm 10%.</param>
        /// <param name="checkHeight">Độ cao thân quái dùng khi kiểm tra, để phần đầu cũng không ló vào khung hình.</param>
        /// <param name="angleAttempts">Thử bao nhiêu góc khác nhau trước khi dùng phương án dự phòng.</param>
        /// <param name="blockMask">Layer của vật cản. Điểm sinh nằm đè lên chúng sẽ bị loại. Để trống thì bỏ qua bước này.</param>
        /// <param name="clearRadius">Bán kính khoảng trống cần có quanh điểm sinh.</param>
        /// <param name="requireReachable">Bắt buộc điểm sinh phải có đường đi thông tới tâm. Tắt đi khi sân chưa bake NavMesh.</param>
        /// <param name="navSampleRadius">Điểm sinh được phép bị kéo về mặt lưới đi được xa nhất bao nhiêu.</param>
        /// <param name="maxPathChecks">Tổng số lần hỏi đường tối đa cho một lần sinh, để không tốn phép tính vô hạn.</param>
        public static Vector3 Pick(
            Vector3 center,
            float minRadius,
            float maxRadius,
            float arenaExtent,
            Camera camera,
            float viewportMargin = 0.12f,
            float checkHeight = 1.8f,
            int angleAttempts = 12,
            LayerMask blockMask = default,
            float clearRadius = 0.6f,
            bool requireReachable = true,
            float navSampleRadius = 1f,
            int maxPathChecks = 12)
        {
            // Chỉ đòi hỏi "đi tới được" khi CHÍNH NGƯỜI CHƠI đang đứng trên lưới đi được.
            // Nếu player lọt ra ngoài lưới thì mọi câu hỏi đường đều trả lời "không" và
            // vòng lặp sẽ loại sạch mọi điểm — thà quay về cách chọn cũ còn hơn không sinh nổi quái.
            bool centerOnMesh = NavMesh.SamplePosition(center, out NavMeshHit centerHit, TargetSampleRadius, NavMesh.AllAreas);
            bool checkReachable = requireReachable && centerOnMesh;
            Vector3 target = checkReachable ? centerHit.position : center;

            int pathChecks = 0;

            // Hai phương án lùi dần, ghi lại trong lúc dò để khỏi phải dò lại từ đầu:
            // ưu tiên điểm ít nhất còn nằm trên lưới, sau đó mới tới điểm chỉ đạt yêu cầu hình học.
            bool haveOnMesh = false;
            Vector3 onMeshFallback = Vector3.zero;
            bool haveOpen = false;
            Vector3 openFallback = Vector3.zero;

            // Bốc trước một góc lệch ngẫu nhiên rồi rải đều các lần thử quanh vòng tròn.
            // Cách này phủ đều mọi hướng tốt hơn là bốc ngẫu nhiên độc lập từng lần,
            // vốn có thể vô tình thử ba lần liền cùng một phía.
            float angleOffset = Random.Range(0f, Mathf.PI * 2f);

            for (int i = 0; i < angleAttempts; i++)
            {
                float angle = angleOffset + i * (Mathf.PI * 2f / angleAttempts);
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                int checksThisAngle = 0;

                for (float radius = minRadius; radius <= maxRadius; radius += SearchStep)
                {
                    Vector3 candidate = ClampToArena(center + direction * radius, arenaExtent);

                    // Việc kẹp vào sân có thể kéo điểm ngược trở lại gần player,
                    // nên phải kiểm tra lại khoảng cách sau khi kẹp.
                    if ((candidate - center).sqrMagnitude < minRadius * minRadius)
                        continue;

                    if (IsVisible(candidate, camera, viewportMargin, checkHeight))
                        continue;

                    if (IsBlocked(candidate, blockMask, clearRadius))
                        continue;

                    if (!haveOpen)
                    {
                        haveOpen = true;
                        openFallback = candidate;
                    }

                    if (!checkReachable)
                        return candidate;

                    // Kéo điểm về mặt lưới đi được gần nhất. Làm vậy con quái bắt đầu ván
                    // ở đúng chỗ nó hỏi đường được, thay vì đứng chênh vênh cạnh mép lưới.
                    if (!NavMesh.SamplePosition(candidate, out NavMeshHit spawnHit, navSampleRadius, NavMesh.AllAreas))
                        continue;

                    Vector3 snapped = spawnHit.position;
                    if (!haveOnMesh)
                    {
                        haveOnMesh = true;
                        onMeshFallback = snapped;
                    }

                    // Hết ngân sách hỏi đường thì chấp nhận điểm đang có còn hơn dò mãi.
                    // Sinh quái xảy ra thành từng đợt vài con một lúc, không được phép
                    // biến thành một cú khựng hình.
                    if (pathChecks >= maxPathChecks)
                        return snapped;

                    pathChecks++;
                    checksThisAngle++;

                    if (NavMesh.CalculatePath(snapped, target, NavMesh.AllAreas, ReusablePath)
                        && ReusablePath.status == NavMeshPathStatus.PathComplete)
                        return snapped;

                    // Hướng này đã hỏng mấy lần liền, gần như chắc chắn cả hướng nằm trong
                    // một hốc kín. Bỏ sang hướng khác thay vì dò tiếp ra xa.
                    if (checksThisAngle >= MaxPathChecksPerAngle)
                        break;
                }
            }

            if (haveOnMesh)
                return onMeshFallback;

            if (haveOpen)
                return openFallback;

            // Dự phòng cuối: khi player đứng sát góc sân, có thể không hướng nào vừa khuất camera
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
                Vector3 candidate = ClampToArena(center + behind * radius, arenaExtent);
                if (!IsBlocked(candidate, blockMask, clearRadius))
                    return SnapToNavMesh(candidate, navSampleRadius);
            }

            return SnapToNavMesh(ClampToArena(center + behind * minRadius, arenaExtent), navSampleRadius);
        }

        /// <summary>Kéo điểm về mặt lưới đi được gần nhất; không có lưới quanh đó thì giữ nguyên.</summary>
        private static Vector3 SnapToNavMesh(Vector3 position, float sampleRadius)
        {
            return NavMesh.SamplePosition(position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas)
                ? hit.position
                : position;
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

        /// <summary>
        /// Kéo điểm sinh vào trong sân.
        ///
        /// Kẹp theo TỪNG TRỤC vì sân đấu là hình vuông. Hình dạng ở đây phải khớp đúng với
        /// hình dạng tường vô hình: kẹp theo bán kính trong một sân vuông sẽ chừa bốn góc sân
        /// không bao giờ có quái, còn kẹp theo trục trong một sân tròn thì bốn góc lại nhô
        /// ra ngoài tường và quái sinh ra bên ngoài rồi không vào được.
        /// </summary>
        private static Vector3 ClampToArena(Vector3 position, float arenaExtent)
        {
            position.x = Mathf.Clamp(position.x, -arenaExtent, arenaExtent);
            position.z = Mathf.Clamp(position.z, -arenaExtent, arenaExtent);
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
