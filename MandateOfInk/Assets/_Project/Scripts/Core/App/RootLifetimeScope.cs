using MandateOfInk.Core;
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
        // 앱 시작 흐름(부트 -> 인게임 스텁). D3에서 핵심 서비스 등록 추가 예정.
        builder.RegisterEntryPoint<AppEntryPoint>();
    }
}
