using Survival.Core;
using Survival.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Màn hình hiện ra khi player chết, kèm nút Chơi lại.
    ///
    /// Không có gì phức tạp, nhưng thiếu nó thì cái chết của player trông y hệt
    /// game bị treo: không đi được, không bắn được, quái đứng im, và không có
    /// một dòng chữ nào giải thích.
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        [SerializeField, Tooltip("Nút gốc chứa toàn bộ màn hình thua. Bị tắt trong lúc đang chơi.")]
        private GameObject _panel;

        [SerializeField] private Button _restartButton;

        [SerializeField, Tooltip("Dòng chữ tổng kết, ví dụ 'Bạn đã sống tới wave 4 — Cấp 3'.")]
        private TextMeshProUGUI _summaryText;

        private GameSession _session;

        private void Start()
        {
            _session = GameSession.I;

            if (_panel != null)
                _panel.SetActive(false);

            if (_session != null)
            {
                _session.OnGameOver += HandleGameOver;
                _session.OnRestarted += HandleRestarted;
            }

            if (_restartButton != null)
                _restartButton.onClick.AddListener(HandleRestartPressed);
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.OnGameOver -= HandleGameOver;
                _session.OnRestarted -= HandleRestarted;
            }

            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(HandleRestartPressed);
        }

        private void HandleGameOver()
        {
            if (_panel != null)
                _panel.SetActive(true);

            if (_summaryText == null)
                return;

            int wave = Waves.WaveManager.I != null ? Waves.WaveManager.I.CurrentWave : 0;
            int level = ExperienceSystem.I != null ? ExperienceSystem.I.Level.Value : 1;

            _summaryText.text = $"Sống tới wave {wave}\nCấp {level}";
        }

        private void HandleRestarted()
        {
            if (_panel != null)
                _panel.SetActive(false);
        }

        private void HandleRestartPressed() => _session?.Restart();
    }
}
