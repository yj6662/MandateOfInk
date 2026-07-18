using UnityEngine;

namespace MandateOfInk.Core.Services
{
    // 오브젝트 풀링/스폰 서비스 계약. VFX·투사체·적 스폰에 사용 예정.
    public interface IPoolService
    {
        GameObject Rent(GameObject prefab, Vector3 position, Quaternion rotation);
        void Return(GameObject instance);
    }
}
