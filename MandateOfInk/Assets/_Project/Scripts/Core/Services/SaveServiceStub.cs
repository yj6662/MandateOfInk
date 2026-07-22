using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MandateOfInk.Core.Services
{
    // 세이브 서비스 스텁 — 실제 구현 전까지 로그만 남긴다.
    public sealed class SaveServiceStub : ISaveService
    {
        public UniTask SaveAsync(CancellationToken cancellation = default)
        {
            Debug.Log("[Save] (스텁) 저장 요청");
            return UniTask.CompletedTask;
        }

        public UniTask LoadAsync(CancellationToken cancellation = default)
        {
            Debug.Log("[Save] (스텁) 불러오기 요청");
            return UniTask.CompletedTask;
        }
    }
}
