// PDollar 원본이 참조하는 UnityEngine API를 콘솔 환경에서 대신 제공하는 최소 셔밍.
// 실제 사용처는 PointCloudRecognizer.cs의 Mathf.Max 한 곳뿐이다.
namespace UnityEngine
{
    internal static class Mathf
    {
        public static float Max(float a, float b) => a > b ? a : b;
    }
}
