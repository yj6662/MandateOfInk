using UnityEditor;
using UnityEngine;

namespace MandateOfInk.EditorTools
{
    // 환경 머티리얼에 잉크 코팅(S_ToonLitTemp의 _InkCoat* 프로퍼티)을 일괄 적용/해제한다.
    // 셰이더 기본값이 전부 0(효과 없음)이므로, 환경으로 지정할 폴더의 머티리얼에만 이 메뉴로 값을 넣는다.
    // 같은 셰이더를 쓰는 캐릭터/적 머티리얼(_Project/Art의 M_Enemy_* 등)은 건드리지 않는다.
    public static class InkCoatApplier
    {
        // 잉크 코팅을 적용할 환경 폴더 — 확장 시 여기에 추가
        private static readonly string[] EnvironmentFolders =
        {
            "Assets/House Of Changwon",
        };

        // [가정] 잉크 코팅 기본 세팅 — 손맛 튜닝으로 확정
        private const float CoatBase = 0.35f;
        private const float CoatTop = 0.7f;
        private const float DripAmount = 0.45f;
        private const float DripScale = 1.5f;
        private const float GlossStrength = 0.6f;
        private const float GlossSharpness = 0.6f;

        [MenuItem("MandateOfInk/잉크 코팅 적용 (환경 머티리얼)")]
        private static void Apply()
        {
            int count = ForEachEnvironmentMaterial(mat =>
            {
                mat.SetFloat("_InkCoatBase", CoatBase);
                mat.SetFloat("_InkCoatTop", CoatTop);
                mat.SetFloat("_InkDripAmount", DripAmount);
                mat.SetFloat("_InkDripScale", DripScale);
                mat.SetFloat("_InkGlossStrength", GlossStrength);
                mat.SetFloat("_InkGlossSharpness", GlossSharpness);
            });
            Debug.Log($"[InkCoat] 잉크 코팅 적용 완료: {count}개 머티리얼");
        }

        [MenuItem("MandateOfInk/잉크 코팅 해제 (환경 머티리얼)")]
        private static void Remove()
        {
            int count = ForEachEnvironmentMaterial(mat =>
            {
                mat.SetFloat("_InkCoatBase", 0f);
                mat.SetFloat("_InkCoatTop", 0f);
                mat.SetFloat("_InkDripAmount", 0f);
                mat.SetFloat("_InkGlossStrength", 0f);
            });
            Debug.Log($"[InkCoat] 잉크 코팅 해제 완료: {count}개 머티리얼");
        }

        private static int ForEachEnvironmentMaterial(System.Action<Material> action)
        {
            var toonShader = Shader.Find("MandateOfInk/Temp/ToonLit");
            if (toonShader == null)
            {
                Debug.LogError("[InkCoat] MandateOfInk/Temp/ToonLit 셰이더를 찾지 못했습니다");
                return 0;
            }

            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", EnvironmentFolders))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader != toonShader) continue;
                action(mat);
                EditorUtility.SetDirty(mat);
                count++;
            }
            AssetDatabase.SaveAssets();
            return count;
        }
    }
}
