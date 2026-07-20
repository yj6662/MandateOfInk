using System.Collections.Generic;
using UnityEngine;

namespace MandateOfInk.Presentation
{
    /// <summary>
    /// 옷자락·불길 절차 흔들림. 모델의 본 이름 접두사(b_robe/b_flame)로 체인을 자동 수집해
    /// 끝점(tip)을 버렛(Verlet) 스프링으로 시뮬레이션하고 본 회전으로 환산한다.
    /// 옷은 중력에 끌리고, 불길은 부력으로 떠오르며 바람 노이즈에 하늘거린다. 수치는 전부 [가정].
    /// </summary>
    public sealed class SpringBoneSway : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Profile
        {
            public string Prefix = "b_robe";
            [Tooltip("복원력 — 클수록 빳빳함")] public float Stiffness = 55f;
            [Range(0f, 1f), Tooltip("감쇠 — 클수록 빨리 가라앉음")] public float Damping = 0.18f;
            [Tooltip("월드 Y 중력. 음수=처짐, 양수=부력(불길)")] public float Gravity = -5f;
            [Tooltip("바람 노이즈 세기")] public float WindStrength = 0.6f;
            [Tooltip("바람 노이즈 속도")] public float WindFrequency = 1.6f;
        }

        [SerializeField]
        private Profile[] _profiles =
        {
            new Profile { Prefix = "b_robe", Stiffness = 55f, Damping = 0.18f, Gravity = -5f, WindStrength = 0.5f, WindFrequency = 1.4f },
            new Profile { Prefix = "b_flame", Stiffness = 22f, Damping = 0.10f, Gravity = 3.5f, WindStrength = 1.6f, WindFrequency = 2.8f },
        };

        private sealed class Node
        {
            public Transform Bone;
            public Profile Profile;
            public Quaternion RestLocalRotation;
            public Vector3 TipLocal;   // 본 로컬 공간의 끝점 오프셋
            public float TipLength;
            public Vector3 TipWorld;
            public Vector3 PrevTipWorld;
            public float WindPhase;
        }

        private readonly List<Node> _nodes = new List<Node>();

        private void Awake()
        {
            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                Profile profile = null;
                foreach (var p in _profiles)
                {
                    if (bone.name.StartsWith(p.Prefix)) { profile = p; break; }
                }
                if (profile == null) continue;

                // 끝점: 자식 본이 있으면 그 위치, 말단 본이면 부모→자신 방향으로 같은 길이만큼 연장
                Vector3 tipLocal;
                if (bone.childCount > 0)
                {
                    tipLocal = bone.GetChild(0).localPosition;
                }
                else
                {
                    var dirWorld = bone.position - bone.parent.position;
                    tipLocal = bone.InverseTransformDirection(dirWorld.normalized) * Mathf.Max(dirWorld.magnitude, 0.05f);
                }
                if (tipLocal.sqrMagnitude < 1e-6f) continue;

                _nodes.Add(new Node
                {
                    Bone = bone,
                    Profile = profile,
                    RestLocalRotation = bone.localRotation,
                    TipLocal = tipLocal,
                    TipLength = tipLocal.magnitude,
                    TipWorld = bone.TransformPoint(tipLocal),
                    PrevTipWorld = bone.TransformPoint(tipLocal),
                    WindPhase = Random.value * 100f,
                });
            }
        }

        private void LateUpdate()
        {
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            if (dt <= 0f || _nodes.Count == 0) return;
            float time = Time.time;

            // GetComponentsInChildren 순서는 부모 우선이라 상위 본부터 갱신된다
            foreach (var n in _nodes)
            {
                var p = n.Profile;
                n.Bone.localRotation = n.RestLocalRotation;
                Vector3 bonePos = n.Bone.position;
                Vector3 restTip = n.Bone.TransformPoint(n.TipLocal);

                // 순간이동(부활·스폰) 가드 — 끝점이 본 길이의 3배 이상 벌어지면 리셋
                if ((n.TipWorld - bonePos).sqrMagnitude > n.TipLength * n.TipLength * 9f)
                {
                    n.TipWorld = restTip;
                    n.PrevTipWorld = restTip;
                }

                Vector3 wind = new Vector3(
                    Mathf.PerlinNoise(time * p.WindFrequency, n.WindPhase) - 0.5f,
                    0f,
                    Mathf.PerlinNoise(n.WindPhase, time * p.WindFrequency) - 0.5f) * (2f * p.WindStrength);
                Vector3 force = (restTip - n.TipWorld) * p.Stiffness + Vector3.up * p.Gravity + wind;

                Vector3 velocity = (n.TipWorld - n.PrevTipWorld) * (1f - p.Damping);
                Vector3 next = n.TipWorld + velocity + force * (dt * dt);
                next = bonePos + (next - bonePos).normalized * (n.TipLength * n.Bone.lossyScale.y);

                n.PrevTipWorld = n.TipWorld;
                n.TipWorld = next;
                n.Bone.rotation = Quaternion.FromToRotation(restTip - bonePos, next - bonePos) * n.Bone.rotation;
            }
        }
    }
}
