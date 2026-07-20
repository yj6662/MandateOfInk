using UnityEngine;

namespace MandateOfInk.Spellcraft
{
    // 디제틱 먹 잔량 1차 — 붓 모델이 먹을 머금은 정도로 잔량을 보여준다.
    // 잔량이 임계 아래로 내려가면 붓이 눈에 띄게 바랜다. (정식 표현은 찰랑이는 벼루 아이콘 — 사용자 결정)
    public sealed class BrushInkTint : MonoBehaviour
    {
        [SerializeField] private InkPool _inkPool;
        [SerializeField] private Renderer _brushRenderer;

        [Header("[가정]")]
        [SerializeField] private Color _fullTint = Color.white;
        [SerializeField] private Color _emptyTint = new Color(0.55f, 0.53f, 0.5f);
        [Tooltip("이 잔량 비율부터 바래기 시작")]
        [SerializeField, Range(0.05f, 1f)] private float _fadeStart = 0.3f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (_inkPool == null || _brushRenderer == null) return;
            float t = Mathf.InverseLerp(0f, _fadeStart, _inkPool.Normalized); // 임계 위=1
            _mpb.SetColor(BaseColorId, Color.Lerp(_emptyTint, _fullTint, t));
            _brushRenderer.SetPropertyBlock(_mpb);
        }
    }
}
