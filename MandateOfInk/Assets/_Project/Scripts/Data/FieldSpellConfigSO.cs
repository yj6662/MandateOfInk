using UnityEngine;

namespace MandateOfInk.Data
{
    // 필드/퍼즐 술식 수치 — 전부 [가정], 손맛 튜닝으로 확정.
    // 구현된 4종: 뭄(돌다리)·굼(도약 기둥)·언(낙사 방어)·넌(차량 가속). 나머지 41자는 예약(미구현).
    [CreateAssetMenu(menuName = "MandateOfInk/Config/Field Spell Config", fileName = "FieldSpellConfig")]
    public sealed class FieldSpellConfigSO : ScriptableObject
    {
        [Header("먹 실체화 연출 (설치형 공통) [가정]")]
        [Tooltip("먹물이 실체로 굳는 시간(초) — 판정은 즉시, 연출만 지연")]
        public float MaterializeSeconds = 1.2f;

        [Header("지정 위치 밖 시전 실패 (설치형 공통) [가정]")]
        [Tooltip("실패 시 먹물 상태로 머무는 시간(초)")]
        public float FailHoldSeconds = 0.5f;
        [Tooltip("먹이 흩어져 사라지는 시간(초)")]
        public float FailDissolveSeconds = 0.7f;
        [Tooltip("실패 시 환급되는 먹 비율")]
        [Range(0f, 1f)] public float FailInkRefundFraction = 0.5f;

        [Header("설치물 모델 — 비우면 프리미티브 폴백. KCISA 우선, 없으면 Meshy 생성(사용자 결정)")]
        public GameObject BridgePrefab;
        public GameObject PillarPrefab;

        [Header("뭄 — 돌다리 설치 (토+ㅜ+ㅁ) [가정]")]
        [Tooltip("전방으로 뻗는 다리 길이(m)")]
        public float BridgeLength = 9f;
        public float BridgeWidth = 2.6f;
        public float BridgeThickness = 0.35f;
        [Tooltip("다리 유지 시간(초) — 0 이하면 영구")]
        public float BridgeSeconds = 25f;

        [Header("굼 — 도약 기둥 (목+ㅜ+ㅁ) [가정]")]
        [Tooltip("도약 초속(m/s) — 약 v^2/(2g) 높이만큼 오른다(12면 약 7m)")]
        public float LeapVelocity = 12f;
        [Tooltip("발밑에서 솟는 기둥 연출 높이(m)")]
        public float PillarHeight = 3f;
        public float PillarSeconds = 2.5f;

        [Header("언 — 낙사 방어 수막 (수+ㅓ+ㄴ) [가정]")]
        [Tooltip("낙사 무효 지속(초) — 낙하 중에도 시전 가능")]
        public float FallGuardSeconds = 12f;

        [Header("넌 — 차량 가속 (화+ㄴ+ㅓ) [가정]")]
        [Tooltip("이 거리(m) 안의 가장 가까운 마석 자동차에 건다")]
        public float BoostCastRange = 12f;
        public float BoostMultiplier = 1.8f;
        public float BoostSeconds = 10f;

        [Header("낙사 피해 (필드 시스템 공통) [가정]")]
        [Tooltip("이 낙하 속도(m/s)까지는 무해")]
        public float SafeFallSpeed = 12f;
        [Tooltip("안전 속도 초과분 1m/s당 피해")]
        public float DamagePerExcessSpeed = 6f;
    }
}
