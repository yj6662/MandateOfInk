using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MandateOfInk.EditorTools
{
    // M1 게이트용 Windows 빌드 (D28) — 외부 5인 테스트 배포본.
    // 메뉴 또는 배치 호출로 실행한다. 결과는 Builds/Gate_M1/ (gitignore 대상).
    public static class GateBuild
    {
        private const string OutputDir = "Builds/Gate_M1";
        private const string ExeName = "MandateOfInk_Gate.exe";

        [MenuItem("MandateOfInk/게이트 빌드 (Windows)")]
        public static void BuildGate()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Project/Scenes/InGame.unity" },
                locationPathName = $"{OutputDir}/{ExeName}",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"[GateBuild] 성공 — {summary.outputPath} " +
                    $"({summary.totalSize / (1024 * 1024)}MB, {summary.totalTime.TotalSeconds:F0}s, 경고 {summary.totalWarnings})");
            else
                Debug.LogError($"[GateBuild] 실패 — {summary.result}, 에러 {summary.totalErrors}건: " +
                    string.Join(" | ", report.steps.SelectMany(s => s.messages)
                        .Where(m => m.type == LogType.Error || m.type == LogType.Exception)
                        .Select(m => m.content).Take(5)));
        }
    }
}
