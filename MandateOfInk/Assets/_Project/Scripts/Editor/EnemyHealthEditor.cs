using MandateOfInk.Combat;
using UnityEditor;
using UnityEngine;

namespace MandateOfInk.EditorTools
{
    // EnemyHealth 인스펙터에 테스트 버튼을 붙인다 — 보스 2페이즈(HP 절반) 전환 확인용.
    // 에디터 계층 전용: 런타임 코드에는 어떤 의존도 추가하지 않는다.
    [CustomEditor(typeof(EnemyHealth))]
    public sealed class EnemyHealthEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var health = (EnemyHealth)target;
            EditorGUILayout.Space(6f);

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("현재 HP", $"{health.CurrentHp:F0} / {health.MaxHp:F0}");
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("체력 절반으로 (2페이즈 테스트)"))
                        health.DebugSetHpFraction(0.5f);
                    if (GUILayout.Button("체력 10%로"))
                        health.DebugSetHpFraction(0.1f);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("플레이 모드에서 체력 조작 버튼이 나타납니다.", MessageType.Info);
            }
        }
    }
}
