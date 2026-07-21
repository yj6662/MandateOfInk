using MandateOfInk.Combat;
using MandateOfInk.Spellcraft;
using UnityEngine;
using UnityEngine.UI;

namespace MandateOfInk.Presentation
{
    // 인게임 최소 HUD — 허용 항목만: HP(먹획 게이지), 먹 미터, 부적 수, 화물 상태, 통보(변동 팝).
    // 게이지는 먹획 스프라이트를 RectMask2D로 오른쪽부터 지워 "마르는" 표현. 배선은 에디터 스크립트가 한다.
    public sealed class HudController : MonoBehaviour
    {
        [Header("게이지 (마스크 폭 = 가득 폭 × 비율)")]
        [SerializeField] private RectTransform _hpMask;
        [SerializeField] private RectTransform _inkMask;
        [SerializeField] private float _gaugeFullWidth = 260f;

        [Header("텍스트·패널")]
        [SerializeField] private Text _talismanText;
        [SerializeField] private GameObject _cargoPanel;
        [SerializeField] private Text _cargoText;
        [SerializeField] private Text _coinText;
        [SerializeField] private RectTransform _coinRoot;

        [Header("[가정] 통보 팽창 연출")]
        [SerializeField] private float _coinPunchScale = 1.6f;
        [SerializeField] private float _coinPunchSeconds = 0.9f;

        private PlayerHealth _health;
        private InkPool _ink;
        private TalismanController _talismans;
        private PlayerCargo _cargo;
        private PlayerWallet _wallet;
        private float _coinPunchTimer;

        private void Start()
        {
            GameSettings.ApplyAll();
            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null)
            {
                _health = cc.GetComponent<PlayerHealth>();
                _cargo = cc.GetComponent<PlayerCargo>();
                _wallet = cc.GetComponent<PlayerWallet>();
            }
            if (_wallet == null) _wallet = FindFirstObjectByType<PlayerWallet>();
            _ink = FindFirstObjectByType<InkPool>();
            _talismans = FindFirstObjectByType<TalismanController>();
            if (_wallet != null) _wallet.OnChanged += OnCoinsChanged;
        }

        private void OnDestroy()
        {
            if (_wallet != null) _wallet.OnChanged -= OnCoinsChanged;
        }

        private void OnCoinsChanged(int delta) => _coinPunchTimer = _coinPunchSeconds;

        private void Update()
        {
            if (_health != null && _hpMask != null)
                SetGauge(_hpMask, _health.MaxHpValue > 0f ? _health.CurrentHp / _health.MaxHpValue : 0f);
            if (_ink != null && _inkMask != null)
                SetGauge(_inkMask, _ink.Normalized);

            if (_talismans != null && _talismanText != null)
            {
                var sb = new System.Text.StringBuilder();
                var slots = _talismans.Slots;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i].Diagram == null) continue;
                    if (sb.Length > 0) sb.Append("   ");
                    sb.Append(slots[i].Diagram.Letter).Append(' ').Append('x').Append(slots[i].Count);
                }
                _talismanText.text = sb.Length > 0 ? sb.ToString() : "-";
            }

            if (_cargoPanel != null)
            {
                bool carrying = _cargo != null && _cargo.IsCarrying;
                if (_cargoPanel.activeSelf != carrying) _cargoPanel.SetActive(carrying);
                if (carrying && _cargoText != null)
                    _cargoText.text = $"{_cargo.ActiveMission.DisplayName}  {_cargo.Condition:F0}%";
            }

            if (_wallet != null && _coinText != null)
            {
                _coinText.text = _wallet.Coins.ToString();
                if (_coinRoot != null)
                {
                    _coinPunchTimer = Mathf.Max(0f, _coinPunchTimer - Time.unscaledDeltaTime);
                    float t = _coinPunchTimer / Mathf.Max(_coinPunchSeconds, 0.01f);
                    float scale = 1f + (_coinPunchScale - 1f) * Mathf.SmoothStep(0f, 1f, t);
                    _coinRoot.localScale = Vector3.one * scale;
                }
            }
        }

        private void SetGauge(RectTransform mask, float fraction)
        {
            var size = mask.sizeDelta;
            size.x = _gaugeFullWidth * Mathf.Clamp01(fraction);
            mask.sizeDelta = size;
        }
    }
}
