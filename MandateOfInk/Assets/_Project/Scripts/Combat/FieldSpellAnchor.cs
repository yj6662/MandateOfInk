using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 필드 설치 앵커 — 설치형 필드 술식(뭄·굼 등)이 작동하는 지정 위치(사용자 결정 2026-07-27).
    // 이 트랜스폼이 곧 설치물의 스폰 위치·방향이다(레벨 디자이너가 정확한 자리를 저작 — 다리가 틈에 딱 걸침).
    // 시전자가 AcceptRange 안에 있고 (오행, 받침)이 맞으면 성공, 아니면 먹물이 흩어지는 실패 연출.
    public sealed class FieldSpellAnchor : MonoBehaviour
    {
        [SerializeField] private Element _element;
        [SerializeField] private FinalModifier _modifier;
        [Header("[가정]")]
        [Tooltip("시전자가 이 거리(m) 안에 있어야 이 앵커가 술식을 받는다")]
        [SerializeField] private float _acceptRange = 8f;

        public Element Element => _element;
        public FinalModifier Modifier => _modifier;
        public float AcceptRange => _acceptRange;

        public bool Accepts(Element element, FinalModifier modifier, Vector3 casterPosition)
        {
            return _element == element && _modifier == modifier &&
                   (transform.position - casterPosition).sqrMagnitude <= _acceptRange * _acceptRange;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _acceptRange);
            Gizmos.color = new Color(0.9f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }
    }
}
