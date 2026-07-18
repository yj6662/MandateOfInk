using System.Threading;
using Cysharp.Threading.Tasks;

namespace MandateOfInk.Core.Services
{
    // 씬 로딩 서비스 계약. 씬 이름은 호출부의 상수/데이터로 관리한다.
    public interface ISceneLoadService
    {
        UniTask LoadAsync(string sceneName, CancellationToken cancellation = default);
    }
}
