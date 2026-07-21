using System.Collections.Generic;
using MandateOfInk.Data;
using MandateOfInk.Spellcraft;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 부적 — 미리 그려둔 진을 즉발로 터뜨리는 비상 패. 먹 무소모·제한 수량(설계 원문).
    // 교전 모드에서 숫자 키(1~슬롯 수)로 사용. 제작/충전 UX는 미결 — 프로토는 시작 슬롯 고정 [가정].
    public sealed class TalismanController : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Slot
        {
            public SpellDiagramSO Diagram;
            public int Count;
        }

        [SerializeField] private DiagramEventChannelSO _castChannel;
        [SerializeField] private SpellcraftModeController _modeController;
        [SerializeField] private List<Slot> _slots = new List<Slot>();

        public IReadOnlyList<Slot> Slots => _slots; // HUD 표시용

        private void Update()
        {
            // 작도 중엔 숫자 키가 등록 모드와 겹치므로 교전 모드 한정 [가정]
            if (_modeController != null && _modeController.Mode != SpellcraftMode.Combat) return;

            for (int i = 0; i < _slots.Count && i < 5; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;
                var slot = _slots[i];
                if (slot.Diagram == null || slot.Count <= 0)
                {
                    Debug.Log($"[Talisman] {i + 1}번 슬롯 비어 있음");
                    continue;
                }
                slot.Count--;
                // 부적은 먹 무소모·정발동 — 채널로 곧장 시전
                _castChannel?.Raise(new DiagramCastRequest
                {
                    Diagram = slot.Diagram,
                    IsWeak = false,
                    PowerMultiplier = 1f,
                });
                Debug.Log($"[Talisman] 「{slot.Diagram.Letter}」 부적 사용 — 남은 수 {slot.Count}");
            }
        }

        // 부적 수 표시는 HudController(캔버스)가 담당한다
    }
}
