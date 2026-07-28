using UnityEngine;

namespace MandateOfInk.Combat
{
    // 먹 실체화 연출 — S_ToonLitTemp의 _MaterializeProgress를 0(먹물)에서 1(실체)로 굴린다.
    // 필드 설치물(돌다리·기둥 등)이 스폰될 때 붙여 Play()를 부른다(사용자 결정 2026-07-27).
    // MaterialPropertyBlock이라 공유 머티리얼은 건드리지 않는다. 판정(콜라이더)은 즉시 유효 — 연출 전용.
    public sealed class InkMaterializer : MonoBehaviour
    {
        private static readonly int ProgressId = Shader.PropertyToID("_MaterializeProgress");
        private static readonly int ErodeId = Shader.PropertyToID("_MaterializeErode");

        private enum Mode { Idle, Materialize, Failure }

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private Mode _mode = Mode.Idle;
        private float _duration = 1.2f;
        private float _holdSeconds;
        private float _elapsed;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _mpb = new MaterialPropertyBlock();
        }

        // 성공 — 먹물(0)에서 실체(1)로 굳는다
        public void Play(float duration)
        {
            _duration = Mathf.Max(duration, 0.05f);
            _elapsed = 0f;
            _mode = Mode.Materialize;
            Apply(0f, 0f);
        }

        // 실패 — 먹물 상태로 잠시 머물다 흩어져 사라진다(지정 위치 밖 시전, 사용자 결정 2026-07-27)
        public void PlayFailure(float holdSeconds, float dissolveSeconds)
        {
            _holdSeconds = Mathf.Max(holdSeconds, 0f);
            _duration = Mathf.Max(dissolveSeconds, 0.05f);
            _elapsed = 0f;
            _mode = Mode.Failure;
            Apply(0f, 0f);
        }

        private void Update()
        {
            if (_mode == Mode.Idle) return;
            _elapsed += Time.deltaTime;

            if (_mode == Mode.Materialize)
            {
                float t = Mathf.Clamp01(_elapsed / _duration);
                Apply(t, 0f);
                if (t >= 1f) _mode = Mode.Idle;
            }
            else // Failure
            {
                float t = Mathf.Clamp01((_elapsed - _holdSeconds) / _duration);
                Apply(0f, t);
                if (t >= 1f) _mode = Mode.Idle;
            }
        }

        private void Apply(float progress, float erode)
        {
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(ProgressId, progress);
                _mpb.SetFloat(ErodeId, erode);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
