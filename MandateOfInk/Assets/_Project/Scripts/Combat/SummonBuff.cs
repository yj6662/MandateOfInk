using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 버프 소환수(ㅜ+ㄱ) — 플레이어를 추종하며, 범위 안에 있는 동안 버프(공격력·이동속도·방어력·회복)를 건다.
    // 사용자 결정(2026-07-23): 플레이어 추종형, 개수 제한 없음(먹 비용으로만 억제).
    public sealed class SummonBuff : MonoBehaviour
    {
        private SummonDefinitionSO _def;
        private Transform _player;
        private PlayerHealth _playerHealth;
        private PlayerCombatStats _stats;
        private float _followDistance;
        private float _buffRadius;
        private float _despawnAt;
        private bool _buffActive;

        public void Init(SummonDefinitionSO def, Transform player, float followDistance, float buffRadius)
        {
            _def = def;
            _player = player;
            _followDistance = followDistance;
            _buffRadius = buffRadius;
            _despawnAt = Time.time + def.LifetimeSeconds;
            _playerHealth = player != null ? player.GetComponentInParent<PlayerHealth>() : null;
            _stats = FindFirstObjectByType<PlayerCombatStats>();
        }

        private void Update()
        {
            if (Time.time >= _despawnAt) { Cleanup(); Destroy(gameObject); return; }
            if (_player == null) return;

            // 추종 — 플레이어 뒤쪽 followDistance만큼 거리를 유지하며 따라온다
            Vector3 toPlayer = _player.position - transform.position;
            float dist = toPlayer.magnitude;
            if (dist > _followDistance)
            {
                Vector3 dir = toPlayer.normalized;
                transform.position += dir * ((dist - _followDistance) * 4f * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
            }

            bool inRange = dist <= _buffRadius;
            if (inRange && !_buffActive)
            {
                _buffActive = true;
                if (_stats != null)
                    _stats.ApplyBuff(this, _def.DamageMultiplier, _def.MoveSpeedMultiplier,
                        _def.DefenseMultiplier, _def.PoiseDamageMultiplier);
            }
            else if (!inRange && _buffActive)
            {
                _buffActive = false;
                if (_stats != null) _stats.RemoveBuff(this);
            }

            if (inRange && _def.HealPerSecond > 0f && _playerHealth != null)
                _playerHealth.Heal(_def.HealPerSecond * Time.deltaTime);
        }

        private void Cleanup()
        {
            if (_buffActive && _stats != null) _stats.RemoveBuff(this);
        }

        private void OnDestroy() => Cleanup();
    }
}
