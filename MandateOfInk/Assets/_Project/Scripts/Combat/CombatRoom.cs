using System.Collections.Generic;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 전투방 게이트 — 트리거 볼륨 안에 플레이어가 있어야만 배정된 적 AI가 깨어난다.
    // 적 AI들은 거리로만 교전을 판정하므로(벽 무시), 방 밖 교전을 막는 것은 이 게이트의 몫이다.
    // 적이 죽으면(파괴) 목록에서 자연히 null이 되므로 매번 걸러서 다룬다.
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CombatRoom : MonoBehaviour
    {
        [Tooltip("이 방에 배정된 적 AI 컴포넌트(EnemyAI/BossAI 등) — 입장 전에는 꺼져 있다")]
        [SerializeField] private List<Behaviour> _enemyBehaviours = new List<Behaviour>();

        private void Awake()
        {
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
            SetEnemiesActive(false);
        }

        public void AssignEnemies(IEnumerable<Behaviour> behaviours)
        {
            _enemyBehaviours.Clear();
            _enemyBehaviours.AddRange(behaviours);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<CharacterController>() == null) return;
            SetEnemiesActive(true);
            Debug.Log($"[Room] {name} 입장 — 적 기상");
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<CharacterController>() == null) return;
            SetEnemiesActive(false);
            Debug.Log($"[Room] {name} 퇴장 — 적 휴면");
        }

        private void SetEnemiesActive(bool active)
        {
            foreach (var b in _enemyBehaviours)
                if (b != null) b.enabled = active;
        }
    }
}
