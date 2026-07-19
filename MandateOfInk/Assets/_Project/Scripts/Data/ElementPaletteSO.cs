using UnityEngine;

namespace MandateOfInk.Data
{
    // 오행 속성별 연출 색. [가정] 전통 오방색(청적황백흑)을 게임 가독성에 맞게 조정한 값 — 룩 판정은 사람.
    [CreateAssetMenu(menuName = "MandateOfInk/Config/Element Palette", fileName = "ElementPalette")]
    public sealed class ElementPaletteSO : ScriptableObject
    {
        [Tooltip("목(木) — 오방색 청. [가정] 청록")] public Color Wood = new Color(0.20f, 0.85f, 0.45f);
        [Tooltip("화(火) — 오방색 적")] public Color Fire = new Color(1.00f, 0.35f, 0.15f);
        [Tooltip("토(土) — 오방색 황")] public Color Earth = new Color(0.95f, 0.72f, 0.20f);
        [Tooltip("금(金) — 오방색 백")] public Color Metal = new Color(0.92f, 0.93f, 1.00f);
        [Tooltip("수(水) — 오방색 흑. [가정] 가독성 위해 심청색")] public Color Water = new Color(0.25f, 0.50f, 1.00f);

        public Color GetColor(Element element)
        {
            switch (element)
            {
                case Element.Wood: return Wood;
                case Element.Fire: return Fire;
                case Element.Earth: return Earth;
                case Element.Metal: return Metal;
                case Element.Water: return Water;
                default: return Color.white;
            }
        }
    }
}
