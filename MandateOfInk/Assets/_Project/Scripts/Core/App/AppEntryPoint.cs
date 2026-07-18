using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace MandateOfInk.Core
{
    // 앱 시작 흐름: 부트 씬에서 초기화를 마친 뒤 인게임 스텁 씬으로 전환한다.
    // VContainer가 컨테이너 구성 완료 후 StartAsync를 호출한다.
    public sealed class AppEntryPoint : IAsyncStartable
    {
        public const string InGameSceneName = "InGame";

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            Debug.Log("[Boot] 루트 컨테이너 구성 완료 — 인게임 스텁 씬 로드 시작");
            await SceneManager.LoadSceneAsync(InGameSceneName).ToUniTask(cancellationToken: cancellation);
            Debug.Log("[Boot] 인게임 스텁 씬 로드 완료");
        }
    }
}
