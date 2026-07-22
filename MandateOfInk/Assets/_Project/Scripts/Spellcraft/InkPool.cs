using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Spellcraft
{
    // 단일 먹 풀 — 작도의 연료. 트리클로 아주 느리게 차고, 주 수급은 교전(평타·환급)이다.
    // 잔량 표시는 임시 HUD(허용 항목: 잉크 미터) — 디제틱(자루 마석 빛)은 M1 W2에서.
    public sealed class InkPool : MonoBehaviour
    {
        [SerializeField] private CombatConfigSO _config;

        public float Current { get; private set; }
        public float Max => _config != null ? _config.MaxInk : 100f;
        public float Normalized => Max > 0f ? Current / Max : 0f; // 0=마름, 1=가득

        private void Awake()
        {
            Current = Max; // 도착하면 풀 — 시작은 가득
        }

        private void Update()
        {
            if (_config == null) return;
            Add(_config.TricklePerSecond * Time.deltaTime);
        }

        public void Add(float amount)
        {
            Current = Mathf.Clamp(Current + amount, 0f, Max);
        }

        // 작도 중 실시간 소모(획 길이 비례). 실제로 빠진 양을 돌려준다 — 바닥나면 0.
        public float ConsumeDrawing(float amount)
        {
            float consumed = Mathf.Min(amount, Current);
            Current -= consumed;
            return consumed;
        }

        // 시전 소모. 잔량이 비용 이상이면 정상(1) — 부족하면 「쥐어짜기」:
        // 잔량/비용 비율(하한 있음)의 위력 배율을 돌려주고 잔량을 전부 소모한다. 불발은 없다.
        public float TrySpendForCast(float cost)
        {
            if (_config == null || cost <= 0f) return 1f;
            if (Current >= cost)
            {
                Current -= cost;
                return 1f;
            }
            float ratio = Mathf.Max(Current / cost, _config.SqueezeMinPower);
            Debug.Log($"[Ink] 쥐어짜기 — 잔량 {Current:F0}/{cost:F0}, 위력 {ratio:P0}");
            Current = 0f;
            return ratio;
        }

        // 먹 미터 표시는 HudController(캔버스 먹획 게이지)가 담당한다
    }
}
