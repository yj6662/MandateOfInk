using UnityEngine;

namespace MandateOfInk.Core.Services
{
    // 사운드 서비스 스텁 — 실제 구현 전까지 로그만 남긴다.
    public sealed class SoundServiceStub : ISoundService
    {
        public void PlayBgm(string id) => Debug.Log($"[Sound] (스텁) BGM 재생: {id}");
        public void StopBgm() => Debug.Log("[Sound] (스텁) BGM 정지");
        public void PlaySfx(string id) => Debug.Log($"[Sound] (스텁) SFX 재생: {id}");
    }
}
