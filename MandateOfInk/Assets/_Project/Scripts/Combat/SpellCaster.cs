using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 시전기 — 작도 인식 성공 채널(EC_DiagramDrawn)을 구독해 도면(진)을 발동한다.
    // 발사 방향은 이 컴포넌트가 붙은 카메라의 정면. [가정] 수치는 인스펙터 조정.
    public sealed class SpellCaster : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private ElementRelationTableSO _relationTable;
        [SerializeField] private DiagramEventChannelSO _diagramDrawn;

        [Header("연출")]
        [SerializeField] private GameObject _projectileVfxPrefab;
        [SerializeField] private GameObject _hitVfxPrefab;

        [Header("[가정] 투사체 파라미터")]
        [SerializeField] private float _projectileSpeed = 25f;
        [SerializeField] private float _projectileLifetime = 5f;

        private void OnEnable()
        {
            if (_diagramDrawn != null) _diagramDrawn.OnRaised += Cast;
        }

        private void OnDisable()
        {
            if (_diagramDrawn != null) _diagramDrawn.OnRaised -= Cast;
        }

        private void Cast(SpellDiagramSO diagram)
        {
            if (diagram == null) return;

            float damage = 0f;
            foreach (var effect in diagram.Effects)
                if (effect is DamageEffect dmg) { damage = dmg.Damage; break; }

            var go = new GameObject($"Projectile_{diagram.Letter}");
            go.transform.SetPositionAndRotation(transform.position + transform.forward * 0.8f, transform.rotation);

            var collider = go.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.25f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; // 트리거 판정용 — 물리 낙하 없음

            var projectile = go.AddComponent<SpellProjectile>();
            projectile.Init(_projectileSpeed, damage, diagram.Element,
                _relationTable, _hitVfxPrefab, _projectileLifetime);

            if (_projectileVfxPrefab != null)
                Instantiate(_projectileVfxPrefab, go.transform.position, go.transform.rotation, go.transform);

            Debug.Log($"[Spell] 「{diagram.Letter}」 시전 — {diagram.Element}, 피해 {damage}");
        }
    }
}
