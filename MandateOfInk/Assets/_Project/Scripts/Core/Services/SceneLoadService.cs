using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MandateOfInk.Core.Services
{
    // 기본 씬 로딩 구현. 이후 로딩 화면·페이드 연출이 붙으면 여기서 확장한다.
    public sealed class SceneLoadService : ISceneLoadService
    {
        public async UniTask LoadAsync(string sceneName, CancellationToken cancellation = default)
        {
            Debug.Log($"[SceneLoad] '{sceneName}' 로드 시작");
            await SceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: cancellation);
            Debug.Log($"[SceneLoad] '{sceneName}' 로드 완료");
        }
    }
}
