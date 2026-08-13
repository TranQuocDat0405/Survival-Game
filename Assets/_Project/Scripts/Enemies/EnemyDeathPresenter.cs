using System.Collections;
using System.Collections.Generic;
using Survival.Combat;
using UnityEngine;

namespace Survival.Enemies
{
    /// <summary>
    /// Trình diễn cái chết của quái: chạy animation gục xuống, rồi tan biến bằng shader.
    ///
    /// Tách hẳn khỏi <see cref="EnemyActor"/> vì đây thuần tuý là phần NHÌN.
    /// Gỡ component này ra thì quái vẫn chết, vẫn cộng EXP, vẫn về pool đúng lúc —
    /// chỉ là biến mất đột ngột. Nhờ tách vậy mà phần trình diễn không bao giờ
    /// có thể làm sai luật chơi.
    ///
    /// Nhịp thời gian:
    ///   [--- animation gục xuống ---][--- tan biến ---] rồi trả về pool
    /// Tổng hai đoạn này phải khớp với <c>_despawnDelay</c> bên EnemyActor,
    /// nên giá trị đó được lấy trực tiếp từ đây thay vì đặt hai nơi rồi lệch nhau.
    /// </summary>
    public class EnemyDeathPresenter : MonoBehaviour
    {
        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

        [SerializeField] private EnemyActor _enemy;

        [SerializeField, Min(0f), Tooltip("Thời gian chạy animation gục xuống trước khi bắt đầu tan, tính bằng giây.")]
        private float _deathAnimationTime = 0.7f;

        [SerializeField, Min(0.05f), Tooltip("Thời gian tan biến, tính bằng giây.")]
        private float _dissolveTime = 0.5f;

        [SerializeField, Tooltip("Vật liệu tan biến. Được nhân bản riêng cho từng con quái lúc chạy.")]
        private Material _dissolveMaterial;

        /// <summary>Vật liệu gốc để khôi phục khi quái được tái sử dụng từ pool.</summary>
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<Material[]> _originalMaterials = new List<Material[]>();
        private readonly List<Material> _runtimeInstances = new List<Material>();

        private Coroutine _routine;

        /// <summary>Tổng thời gian trình diễn. EnemyActor đọc số này để biết khi nào trả về pool.</summary>
        public float TotalDuration => _deathAnimationTime + _dissolveTime;

        private void Awake()
        {
            if (_enemy == null)
                _enemy = GetComponentInParent<EnemyActor>();

            // Ghi nhớ vật liệu gốc MỘT lần. Quái được tái sử dụng nên phải khôi phục được
            // về hình dạng ban đầu, nếu không thì con quái sinh ra lần sau sẽ trong suốt sẵn.
            GetComponentsInChildren(true, _renderers);
            for (int i = 0; i < _renderers.Count; i++)
                _originalMaterials.Add(_renderers[i].sharedMaterials);

            if (_enemy != null && _enemy.Health != null)
                _enemy.Health.OnDied += HandleDied;
        }

        private void OnDestroy()
        {
            if (_enemy != null && _enemy.Health != null)
                _enemy.Health.OnDied -= HandleDied;

            // Vật liệu nhân bản lúc chạy không tự được dọn, phải huỷ tay để không rò bộ nhớ.
            for (int i = 0; i < _runtimeInstances.Count; i++)
            {
                if (_runtimeInstances[i] != null)
                    Destroy(_runtimeInstances[i]);
            }
        }

        /// <summary>Khôi phục hình dạng ban đầu mỗi khi quái được lấy ra từ pool.</summary>
        private void OnEnable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            RestoreOriginalMaterials();
        }

        private void HandleDied(Health health)
        {
            if (!isActiveAndEnabled)
                return;

            _routine = StartCoroutine(PlayDeath());
        }

        private IEnumerator PlayDeath()
        {
            // Giai đoạn 1: để animation gục xuống chạy, chưa đụng gì tới vật liệu.
            yield return new WaitForSeconds(_deathAnimationTime);

            if (_dissolveMaterial == null)
                yield break;

            SwapToDissolveMaterials();

            // Giai đoạn 2: đẩy dần mức tan biến từ 0 lên 1.
            float elapsed = 0f;
            while (elapsed < _dissolveTime)
            {
                elapsed += Time.deltaTime;
                float amount = Mathf.Clamp01(elapsed / _dissolveTime);

                for (int i = 0; i < _runtimeInstances.Count; i++)
                    _runtimeInstances[i].SetFloat(DissolveAmountId, amount);

                yield return null;
            }

            _routine = null;
        }

        private void SwapToDissolveMaterials()
        {
            _runtimeInstances.Clear();

            for (int i = 0; i < _renderers.Count; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;

                var source = _originalMaterials[i];
                var replacements = new Material[source.Length];

                for (int m = 0; m < source.Length; m++)
                {
                    // Mỗi con quái cần một bản vật liệu RIÊNG, vì mức tan biến của chúng
                    // khác nhau. Dùng chung một vật liệu thì cả đàn sẽ tan cùng lúc
                    // ngay khi con đầu tiên chết.
                    var instance = new Material(_dissolveMaterial);

                    if (source[m] != null && source[m].HasProperty("_MainTex"))
                        instance.SetTexture("_MainTex", source[m].GetTexture("_MainTex"));

                    if (source[m] != null && source[m].HasProperty("_Color"))
                        instance.SetColor("_Color", source[m].GetColor("_Color"));

                    instance.SetFloat(DissolveAmountId, 0f);
                    replacements[m] = instance;
                    _runtimeInstances.Add(instance);
                }

                renderer.sharedMaterials = replacements;
            }
        }

        private void RestoreOriginalMaterials()
        {
            for (int i = 0; i < _renderers.Count && i < _originalMaterials.Count; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].sharedMaterials = _originalMaterials[i];
            }

            for (int i = 0; i < _runtimeInstances.Count; i++)
            {
                if (_runtimeInstances[i] != null)
                    Destroy(_runtimeInstances[i]);
            }
            _runtimeInstances.Clear();
        }
    }
}
