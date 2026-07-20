using System.Collections.Generic;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 보스의 방사 배열 기계 팔 묶음 — 무작위 유휴 팔을 골라 내려찍기를 시킨다.
    public sealed class BossArmRig : MonoBehaviour
    {
        [SerializeField] private List<BossArm> _arms = new List<BossArm>();

        [Header("[가정]")]
        [SerializeField] private float _slamDamage = 25f;
        [SerializeField] private float _impactRadius = 3f;

        [Header("[가정] 산탄 조준 — 무작위처럼 보이되 절대 겹치지 않게")]
        [SerializeField] private float _minSeparation = 6.5f;      // 타격 지점 최소 이격
        [SerializeField] private float _scatterRadius = 6f;        // 중심 주변 흩뿌리기 반경
        [SerializeField] private float _pointMemorySeconds = 2.5f; // 이 시간 안의 지점과는 겹치지 않는다
        [SerializeField] private float _volleyStaggerSeconds = 0.09f; // 볼리 팔 간 시차

        private readonly List<Vector3> _recentPoints = new List<Vector3>();
        private readonly List<float> _recentTimes = new List<float>();

        public void RegisterArms(IEnumerable<BossArm> arms)
        {
            _arms.Clear();
            _arms.AddRange(arms);
        }

        // 현재 타격 중인 팔들의 최대 기울임 가중치(0~1) — BossAI가 몸통 숙임에 쓴다
        public float MaxLeanWeight
        {
            get
            {
                float max = 0f;
                foreach (var arm in _arms)
                    if (arm != null && arm.LeanWeight > max) max = arm.LeanWeight;
                return max;
            }
        }

        public bool HasIdleArm
        {
            get
            {
                foreach (var arm in _arms) if (arm != null && !arm.IsBusy) return true;
                return false;
            }
        }

        // 무작위 유휴 팔로 대상 지점 내려찍기. 시작했으면 true.
        // 최근 타격 지점과 겹치면 근처의 빈 자리로 밀려난다 — 연속 콤보도 자동으로 안 겹친다.
        public bool SlamRandomArm(Vector3 targetPoint, bool frantic = false)
        {
            var idle = CollectIdle();
            if (idle.Count == 0) return false;
            var chosen = idle[Random.Range(0, idle.Count)];
            chosen.SlamAt(PickNonOverlapping(targetPoint), _slamDamage, _impactRadius, 0f, frantic);
            Debug.Log($"[Boss] 팔 내려찍기 — {chosen.name}");
            return true;
        }

        // 여러 유휴 팔의 난타 — 첫 팔은 중심(플레이어 지점) 정조준, 나머지는 주변에 흩뿌리되
        // 서로 절대 겹치지 않는다. 팔마다 시차를 둬서 과부하 드럼롤처럼 떨어진다.
        public int SlamVolley(Vector3 centerPoint, int count, float spreadRadius)
        {
            var idle = CollectIdle();
            int started = 0;
            for (int i = 0; i < count && idle.Count > 0; i++)
            {
                var chosen = idle[Random.Range(0, idle.Count)];
                idle.Remove(chosen);
                Vector2 off = i == 0 ? Vector2.zero : Random.insideUnitCircle * spreadRadius;
                Vector3 target = PickNonOverlapping(centerPoint + new Vector3(off.x, 0f, off.y));
                float delay = i * _volleyStaggerSeconds * Random.Range(0.7f, 1.4f);
                chosen.SlamAt(target, _slamDamage, _impactRadius, delay, frantic: true);
                started++;
            }
            if (started > 0) Debug.Log($"[Boss] 다중 내려찍기 x{started}");
            return started;
        }

        private List<BossArm> CollectIdle()
        {
            var idle = new List<BossArm>();
            foreach (var arm in _arms) if (arm != null && !arm.IsBusy) idle.Add(arm);
            return idle;
        }

        // 원하는 지점이 최근 타격 지점과 최소 이격 미만이면, 주변에서 빈 자리를 다시 뽑는다.
        // "무작위처럼" 보이지만 실제로는 절대 겹치지 않는 산탄이 된다.
        private Vector3 PickNonOverlapping(Vector3 desired)
        {
            // 기억 만료 정리
            for (int i = _recentTimes.Count - 1; i >= 0; i--)
                if (Time.time - _recentTimes[i] > _pointMemorySeconds)
                { _recentTimes.RemoveAt(i); _recentPoints.RemoveAt(i); }

            Vector3 candidate = desired;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                if (IsFarEnough(candidate)) { Register(candidate); return candidate; }
                Vector2 off = Random.insideUnitCircle * _scatterRadius;
                candidate = desired + new Vector3(off.x, 0f, off.y);
            }
            // 폴백: 마지막 타격 지점의 반대편으로 최소 이격만큼 밀어낸다
            Vector3 away = desired - _recentPoints[_recentPoints.Count - 1];
            away.y = 0f;
            away = away.sqrMagnitude < 0.01f
                ? new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f).normalized
                : away.normalized;
            candidate = desired + away * _minSeparation;
            Register(candidate);
            return candidate;
        }

        private bool IsFarEnough(Vector3 p)
        {
            foreach (var q in _recentPoints)
            {
                Vector3 d = p - q;
                d.y = 0f;
                if (d.sqrMagnitude < _minSeparation * _minSeparation) return false;
            }
            return true;
        }

        private void Register(Vector3 p)
        {
            _recentPoints.Add(p);
            _recentTimes.Add(Time.time);
        }
    }
}
