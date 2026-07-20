using MandateOfInk.Data;
using MandateOfInk.Spellcraft;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 평타 — 대필 자루끝 타격. 딜링 수단이 아니라 「먹 버는 동작」(설계 원문).
    // 교전 모드에서 좌클릭: 전방 짧은 스피어캐스트, 적중 시 미미한 피해 + 먹 충전.
    public sealed class MeleeAttack : MonoBehaviour
    {
        [SerializeField] private CombatConfigSO _config;
        [SerializeField] private SpellcraftModeController _modeController;
        [SerializeField] private InkPool _inkPool;

        private float _cooldownRemaining;

        private void Update()
        {
            if (_config == null) return;
            _cooldownRemaining -= Time.deltaTime;

            // 작도 모드에서는 마우스가 붓 — 평타 금지
            if (_modeController != null && _modeController.Mode != SpellcraftMode.Combat) return;
            if (!Input.GetMouseButtonDown(0) || _cooldownRemaining > 0f) return;
            _cooldownRemaining = _config.MeleeCooldown;
            Swing();
        }

        private void Swing()
        {
            var cam = Camera.main;
            Vector3 origin = cam != null ? cam.transform.position : transform.position + Vector3.up * 1.4f;
            Vector3 dir = cam != null ? cam.transform.forward : transform.forward;

            if (Physics.SphereCast(origin, _config.MeleeRadius, dir, out RaycastHit hit, _config.MeleeRange))
            {
                var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
                if (enemy != null)
                {
                    var status = EnemyStatus.GetOrAdd(enemy);
                    enemy.TakeDamage(_config.MeleeDamage * status.GroggyDamageMultiplier);
                    status.AddPoise(_config.MeleePoiseDamage, _config); // 평타도 포이즈를 깎는다
                    if (_inkPool != null) _inkPool.Add(_config.MeleeInkRefund);
                    SpellVisuals.SpawnBurst(hit.point, new Color(0.15f, 0.14f, 0.13f, 0.6f), 0.5f, 0.2f); // 먹빛 타격감
                    Debug.Log($"[Melee] 자루끝 적중 — 피해 {_config.MeleeDamage}, 먹 +{_config.MeleeInkRefund}");
                    return;
                }
            }
            Debug.Log("[Melee] 허공");
        }
    }
}
