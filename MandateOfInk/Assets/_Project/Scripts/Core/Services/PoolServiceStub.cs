using UnityEngine;

namespace MandateOfInk.Core.Services
{
    // 풀링 서비스 스텁 — 지금은 단순 생성/파괴로 동작을 대신한다. 실제 풀은 M0 D8 이후 구현.
    public sealed class PoolServiceStub : IPoolService
    {
        public GameObject Rent(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Object.Instantiate(prefab, position, rotation);
        }

        public void Return(GameObject instance)
        {
            Object.Destroy(instance);
        }
    }
}
