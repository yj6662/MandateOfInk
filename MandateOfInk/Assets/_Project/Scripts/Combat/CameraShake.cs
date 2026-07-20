using UnityEngine;

namespace MandateOfInk.Combat
{
    // 화면 흔들림 헬퍼 — Cinemachine이 카메라를 배치한 뒤(onBeforeRender)에 오프셋을 더하고,
    // 다음 프레임에 브레인이 다시 배치하므로 자동으로 원위치되는 비파괴 방식이다.
    // Time.time 기반이라 작도 중 시간 감속이면 흔들림도 같이 느려진다.
    public static class CameraShake
    {
        private static float _strength;
        private static float _duration;
        private static float _endTime;
        private static bool _hooked;

        public static void Shake(float strength, float duration)
        {
            if (strength <= 0f || duration <= 0f) return;
            if (!_hooked) { Application.onBeforeRender += Apply; _hooked = true; }
            // 더 센 흔들림이 오면 교체, 기존 흔들림이 끝났으면 새로 시작
            if (strength >= _strength || Time.time >= _endTime)
            {
                _strength = strength;
                _duration = duration;
                _endTime = Time.time + duration;
            }
        }

        private static void Apply()
        {
            float remain = _endTime - Time.time;
            if (remain <= 0f) return;
            var cam = Camera.main;
            if (cam == null) return;
            float k = remain / _duration; // 감쇠
            float t = Time.time * 34f;
            var offset = new Vector3(
                Mathf.PerlinNoise(t, 0.31f) - 0.5f,
                Mathf.PerlinNoise(0.73f, t) - 0.5f,
                0f) * (2f * _strength * k * k);
            cam.transform.position += cam.transform.rotation * offset;
        }
    }
}
