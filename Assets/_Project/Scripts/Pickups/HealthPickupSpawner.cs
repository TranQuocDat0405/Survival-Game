using System.Collections.Generic;
using NFramework;
using Survival.Config;
using Survival.Player;
using Survival.Pooling;
using UnityEngine;

namespace Survival.Pickups
{
    /// <summary>
    /// Định kỳ thả bình hồi máu quanh người chơi.
    ///
    /// CHỖ SINH PHẢI NHÌN THẤY ĐƯỢC — đây là điểm ngược hoàn toàn với <c>SpawnPointPicker</c>.
    /// Quái bắt buộc phải sinh NGOÀI khung hình, nếu không người chơi thấy chúng mọc ra từ hư
    /// không. Vật phẩm thì ngược lại: rơi ngoài khung hình nghĩa là người chơi không biết nó
    /// tồn tại, và nó không tạo ra quyết định nào cả — trong khi cả điểm hay của vật phẩm là
    /// buộc người chơi cân nhắc "có đáng rời chỗ an toàn để chạy ra nhặt không".
    ///
    /// Hai lớp làm hai việc ngược nhau nên cố tình KHÔNG dùng chung một hàm chọn điểm: nhét cả
    /// hai vào một chỗ sẽ đẻ ra một tham số kiểu "phải thấy hay phải khuất" và người đọc sau này
    /// phải lần ngược mới hiểu mỗi nhánh phục vụ ai.
    /// </summary>
    public class HealthPickupSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("File cấu hình mọi con số. Để trống thì không sinh gì cả.")]
        private PickupConfigSO _config;

        [SerializeField, Tooltip("Camera dùng để kiểm tra chỗ sinh có nằm trong khung hình không. Để trống thì tự lấy Camera.main.")]
        private Camera _camera;

        /// <summary>Những bình đang nằm trên sân. Giữ danh sách để biết khi nào đã đủ số tối đa.</summary>
        private readonly List<PooledObject> _alive = new List<PooledObject>();

        private float _timer;

        private void Update()
        {
            if (_config == null || _config.HealthPickupPrefab == null)
                return;

            PruneCollected();

            _timer += Time.deltaTime;
            if (_timer < _config.SpawnInterval)
                return;

            _timer = 0f;
            TrySpawn();
        }

        /// <summary>
        /// Bỏ khỏi danh sách những bình đã được nhặt (chúng tự trả về pool nên bị tắt đi).
        ///
        /// Duyệt ngược để xoá phần tử giữa chừng mà không làm lệch chỉ số của những phần tử
        /// chưa xét — duyệt xuôi rồi xoá sẽ nhảy cóc qua phần tử ngay sau chỗ vừa xoá.
        /// </summary>
        private void PruneCollected()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                var pickup = _alive[i];
                if (pickup == null || !pickup.gameObject.activeInHierarchy)
                    _alive.RemoveAt(i);
            }
        }

        private void TrySpawn()
        {
            if (_alive.Count >= _config.MaxAlive || PoolService.I == null)
                return;

            var player = PlayerActor.Current;
            if (player == null || player.Health == null || !player.Health.IsAlive)
                return;

            // Không rải bình khi người chơi đang đầy máu — chúng sẽ chỉ nằm đó làm nền.
            if (_config.OnlyWhenHurt && player.Health.Current.Value >= player.Health.Max)
                return;

            if (!TryFindSpot(player.transform.position, out Vector3 spot))
                return;

            var spawned = PoolService.I.Spawn(_config.HealthPickupPrefab, spot, Quaternion.identity);
            if (spawned == null)
                return;

            var pickup = spawned.GetComponent<HealthPickup>();
            if (pickup != null)
                pickup.Setup(_config.HealAmount, _config.PickupRadius);

            _alive.Add(spawned);
        }

        /// <summary>
        /// Tìm một chỗ vừa trống, vừa đang nằm trong khung hình, vừa không quá sát người chơi.
        /// </summary>
        private bool TryFindSpot(Vector3 center, out Vector3 spot)
        {
            spot = center;

            if (_camera == null)
                _camera = Camera.main;

            // Bốc một góc lệch ngẫu nhiên rồi rải đều các lần thử quanh vòng tròn, thay vì bốc
            // độc lập từng lần — cách này phủ đều mọi hướng thay vì vô tình dồn về một phía.
            float angleOffset = Random.Range(0f, Mathf.PI * 2f);
            int attempts = Mathf.Max(1, _config.PlacementAttempts);

            for (int i = 0; i < attempts; i++)
            {
                float angle = angleOffset + i * (Mathf.PI * 2f / attempts);
                float radius = Random.Range(_config.MinSpawnRadius, _config.MaxSpawnRadius);

                var candidate = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                candidate.y = 0f;

                if (IsBlocked(candidate))
                    continue;

                if (_config.RequireVisible && !IsOnScreen(candidate))
                    continue;

                spot = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Chỗ này có vướng cây đá không.
        ///
        /// Kiểm tra ở NGANG THÂN chứ không sát mặt đất: hình cầu đặt sát đất có thể chạm vào
        /// chính mặt nền, và khi đó mọi chỗ trên sân đều bị coi là vướng.
        /// </summary>
        private bool IsBlocked(Vector3 groundPosition)
        {
            if (_config.BlockMask.value == 0)
                return false;

            return Physics.CheckSphere(
                groundPosition + Vector3.up * _config.ClearRadius,
                _config.ClearRadius,
                _config.BlockMask,
                QueryTriggerInteraction.Ignore);
        }

        /// <summary>Chỗ này có đang nằm trong khung hình không, và cách mép đủ xa chưa.</summary>
        private bool IsOnScreen(Vector3 worldPoint)
        {
            if (_camera == null)
                return true;

            Vector3 viewport = _camera.WorldToViewportPoint(worldPoint);

            // z âm nghĩa là điểm nằm phía sau camera — chiếu lên màn hình vẫn ra toạ độ hợp lệ
            // nhưng đó là ảnh lật ngược, không phải thứ người chơi nhìn thấy.
            if (viewport.z <= 0f)
                return false;

            float margin = _config.ViewportMargin;
            return viewport.x >= margin && viewport.x <= 1f - margin
                && viewport.y >= margin && viewport.y <= 1f - margin;
        }
    }
}
