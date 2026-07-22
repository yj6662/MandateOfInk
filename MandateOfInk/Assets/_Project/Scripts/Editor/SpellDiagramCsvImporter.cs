using System.Collections.Generic;
using System.IO;
using MandateOfInk.Data;
using UnityEditor;
using UnityEngine;

namespace MandateOfInk.EditorTools
{
    // 글자 일람 CSV(단일 진실) -> SpellDiagramSO 에셋 일괄 생성/갱신.
    // 축 필드는 항상 CSV로 덮어쓰고, Effects는 비어 있을 때만 분류별 기본값을 넣는다
    // (수동 튜닝 보존). 재실행해도 안전한 멱등 임포터다.
    public static class SpellDiagramCsvImporter
    {
        private const string CsvPath = "Assets/Docs/오행부_작도글자일람_v1_2.csv";
        private const string OutputDir = "Assets/_Project/Data/Diagrams";

        [MenuItem("MandateOfInk/Import/글자 일람 CSV → 도면 SO")]
        public static void Import()
        {
            if (!File.Exists(CsvPath))
            {
                Debug.LogError($"[DiagramImport] CSV 없음: {CsvPath}");
                return;
            }
            if (!AssetDatabase.IsValidFolder(OutputDir))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Diagrams");

            string[] lines = File.ReadAllLines(CsvPath);
            int created = 0, updated = 0;
            for (int i = 1; i < lines.Length; i++) // 0행 = 헤더
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] c = lines[i].Split(','); // 본 CSV는 쉼표를 값에 쓰지 않는다
                string letter = c[0];
                string assetPath = $"{OutputDir}/SD_{letter}.asset";

                var so = AssetDatabase.LoadAssetAtPath<SpellDiagramSO>(assetPath);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<SpellDiagramSO>();
                    AssetDatabase.CreateAsset(so, assetPath);
                    created++;
                }
                else updated++;

                so.Letter = letter;
                so.Initial = c[1];
                so.Medial = c[2];
                so.Final = c[3];
                so.Element = (Element)System.Enum.Parse(typeof(Element), c[4]);
                so.Polarity = (Polarity)System.Enum.Parse(typeof(Polarity), c[5]);
                so.Scope = (Scope)System.Enum.Parse(typeof(Scope), c[6]);
                so.Modifier = (FinalModifier)System.Enum.Parse(typeof(FinalModifier), c[7]);
                so.Category = (DiagramCategory)System.Enum.Parse(typeof(DiagramCategory), c[8]);
                so.InkCost = float.Parse(c[9]);
                so.Description = c[10];

                if (so.Effects == null || so.Effects.Count == 0)
                {
                    so.Effects = new List<EffectParam>();
                    switch (so.Category) // [가정] 분류별 기본 효과 — M1 실측으로 확정
                    {
                        case DiagramCategory.Attack: so.Effects.Add(new DamageEffect()); break;
                        case DiagramCategory.DefenseControl: so.Effects.Add(new ShieldEffect()); break;
                        case DiagramCategory.TriggerInstall: so.Effects.Add(new DamageEffect()); break;
                    }
                }
                EditorUtility.SetDirty(so);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[DiagramImport] 완료 — 생성 {created} / 갱신 {updated}");
        }
    }
}
