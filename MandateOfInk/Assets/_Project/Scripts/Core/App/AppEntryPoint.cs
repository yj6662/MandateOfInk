using System.Threading;
using Cysharp.Threading.Tasks;
using MandateOfInk.Core.Services;
using UnityEngine;
using VContainer.Unity;

namespace MandateOfInk.Core
{
    // 앱 시작 흐름: 부트 씬에서 초기화를 마친 뒤 메인 메뉴로 전환한다(메뉴에서 인게임 진입).
    // 씬 로딩을 직접 하지 않고 주입받은 ISceneLoadService를 쓴다 — DI 그래프 검증을 겸한다.
    public sealed class AppEntryPoint : IAsyncStartable
    {
        public const string FirstSceneName = "MainMenu";

        private readonly ISceneLoadService _sceneLoad;

        public AppEntryPoint(ISceneLoadService sceneLoad)
        {
            _sceneLoad = sceneLoad;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            Debug.Log("[Boot] 루트 컨테이너 구성 완료 — 인게임 스텁 씬 로드 시작");
            await _sceneLoad.LoadAsync(FirstSceneName, cancellation);
            Debug.Log("[Boot] 인게임 스텁 씬 로드 완료");
        }
    }
}
