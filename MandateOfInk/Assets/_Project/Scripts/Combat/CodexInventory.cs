using System;
using System.Collections.Generic;
using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 해례본 수집 상태 — 플레이어에 부착. UI(Presentation)는 OnChanged를 구독해 갱신한다.
    // 저장은 후속(ISaveService 연동 예정) — 현재는 세션 한정 [가정].
    public sealed class CodexInventory : MonoBehaviour
    {
        private readonly HashSet<HaeryePageSO> _collected = new HashSet<HaeryePageSO>();

        public event Action OnChanged;

        public int Count => _collected.Count;

        public bool Has(HaeryePageSO page) => page != null && _collected.Contains(page);

        public bool Collect(HaeryePageSO page)
        {
            if (page == null || !_collected.Add(page)) return false;
            Debug.Log($"[Codex] 해례본 습득 — 「{page.Title}」 ({Count}장)");
            OnChanged?.Invoke();
            return true;
        }
    }
}
