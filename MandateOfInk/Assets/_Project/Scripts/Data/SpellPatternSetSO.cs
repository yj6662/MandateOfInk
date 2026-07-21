using UnityEngine;

namespace MandateOfInk.Data
{
    // 오행 속성 → 문양 이펙트 프리팹 매핑(Korean Traditional Pattern Effect 에셋).
    // 하드코딩 금지 규칙에 따라 진(陣) 룩 배정을 데이터로 뺀다. 프리팹 참조는 에디터에서 배선한다.
    // 분류별로 바닥 진(Bottom)과 발사체(Fly)를 나눠 담는다.
    [CreateAssetMenu(menuName = "MandateOfInk/Spell Pattern Set", fileName = "SpellPatternSet")]
    public sealed class SpellPatternSetSO : ScriptableObject
    {
        [System.Serializable]
        public struct ElementPatterns
        {
            public Element Element;
            [Tooltip("바닥 진 — 광역 파동·돔·전방 막·격발 파동에 깔린다")]
            public GameObject GroundCircle;
            [Tooltip("발사체 — 단일 투사체에 부착된다")]
            public GameObject Projectile;
        }

        [SerializeField] private ElementPatterns[] _patterns;

        public GameObject GetGroundCircle(Element element)
        {
            foreach (var p in _patterns)
                if (p.Element == element) return p.GroundCircle;
            return _patterns.Length > 0 ? _patterns[0].GroundCircle : null;
        }

        public GameObject GetProjectile(Element element)
        {
            foreach (var p in _patterns)
                if (p.Element == element) return p.Projectile;
            return _patterns.Length > 0 ? _patterns[0].Projectile : null;
        }
    }
}
