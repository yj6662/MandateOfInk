using System.Threading;
using Cysharp.Threading.Tasks;

namespace MandateOfInk.Core.Services
{
    // 세이브/로드 서비스 계약. 세이브 데이터 스키마는 Data 모듈에서 정의 예정.
    public interface ISaveService
    {
        UniTask SaveAsync(CancellationToken cancellation = default);
        UniTask LoadAsync(CancellationToken cancellation = default);
    }
}
