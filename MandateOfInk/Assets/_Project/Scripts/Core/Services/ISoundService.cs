namespace MandateOfInk.Core.Services
{
    // 사운드 서비스 계약. 사운드 id 체계는 Data 모듈 확정 시 교체 예정 — [가정] 임시 string id.
    public interface ISoundService
    {
        void PlayBgm(string id);
        void StopBgm();
        void PlaySfx(string id);
    }
}
