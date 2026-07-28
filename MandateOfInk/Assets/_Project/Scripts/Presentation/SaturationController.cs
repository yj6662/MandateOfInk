using System.Collections.Generic;
using UnityEngine;

namespace MandateOfInk.Presentation
{
    // S_ToonLitTemp(_Saturation)를 시간에 걸쳐 목표값으로 보간한다. 환경 오브젝트를 이벤트/구역
    // 트리거로 흑백<->채색 전환할 때 사용(사용자 결정 2026-07-24). MaterialPropertyBlock으로
    // 렌더러 인스턴스별로만 값을 바꾸므로 같은 머티리얼을 공유하는 다른 오브젝트에 영향이 없다.
    public sealed class SaturationController : MonoBehaviour
    {
        [Tooltip("대상 렌더러 — 비우면 이 오브젝트와 자식의 모든 Renderer를 자동 수집")]
        [SerializeField] private Renderer[] _renderers;
        [SerializeField, Range(0f, 1f)] private float _initialSaturation = 0f;

        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private readonly List<MaterialPropertyBlock> _blocks = new List<MaterialPropertyBlock>();
        private float _current;
        private float _target;
        private float _velocity; // SmoothDamp용, 초 단위 전환 시간과 무관하게 부드럽게

        private void Awake()
        {
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in _renderers) _blocks.Add(new MaterialPropertyBlock());
            _current = _target = _initialSaturation;
            ApplyImmediate(_current);
        }

        // 목표 채도로 fadeSeconds에 걸쳐 부드럽게 전환한다(0=흑백, 1=원색).
        public void SetTarget(float saturation, float fadeSeconds)
        {
            _target = Mathf.Clamp01(saturation);
            _fadeSeconds = Mathf.Max(fadeSeconds, 0.01f);
        }

        private float _fadeSeconds = 1f;

        private void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;
            _current = Mathf.SmoothDamp(_current, _target, ref _velocity, _fadeSeconds);
            if (Mathf.Abs(_current - _target) < 0.001f) _current = _target;
            ApplyImmediate(_current);
        }

        private void ApplyImmediate(float saturation)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                _renderers[i].GetPropertyBlock(_blocks[i]);
                _blocks[i].SetFloat(SaturationId, saturation);
                _renderers[i].SetPropertyBlock(_blocks[i]);
            }
        }
    }
}
