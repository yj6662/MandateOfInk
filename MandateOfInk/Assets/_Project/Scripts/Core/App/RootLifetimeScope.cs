using MandateOfInk.Core;
using MandateOfInk.Core.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

// 앱 전체 수명을 담당하는 루트 DI 컨테이너. 부트 씬에 하나만 배치한다.
public class RootLifetimeScope : LifetimeScope
{
    protected override void Awake()
    {
        base.Awake();
        // 씬이 바뀌어도 루트 컨테이너는 유지
        DontDestroyOnLoad(gameObject);
    }

    protected override void Configure(IContainerBuilder builder)
    {
        // 핵심 서비스 — 아직 스텁인 것은 실제 구현으로 교체 시 이 줄만 바꾸면 된다
        builder.Register<ISceneLoadService, SceneLoadService>(Lifetime.Singleton);
        builder.Register<ISaveService, SaveServiceStub>(Lifetime.Singleton);
        builder.Register<ISoundService, SoundServiceStub>(Lifetime.Singleton);
        builder.Register<IPoolService, PoolServiceStub>(Lifetime.Singleton);

        // 앱 시작 흐름(부트 -> 인게임 스텁)
        builder.RegisterEntryPoint<AppEntryPoint>();
    }
}
