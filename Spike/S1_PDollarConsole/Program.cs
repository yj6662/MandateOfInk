// S1 스파이크: PDollar($P Point-Cloud Recognizer) 콘솔 구동 검증.
// 1) 동봉 제스처 세트(16종) 로드 → 자기 자신 분류(구동 확인)
// 2) 마우스 입력을 흉내 낸 교란(지터·크기·회전·점 솎아내기) 변형 분류 → 인식률·지연 측정
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using PDollarGestureRecognizer;

namespace MandateOfInk.Spike.S1
{
    internal static class Program
    {
        private const int VariantsPerClass = 100;

        private static int Main(string[] args)
        {
            string setDir = args.Length > 0
                ? args[0]
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "MandateOfInk", "Assets", "PDollar", "Resources", "GestureSet", "10-stylus-MEDIUM"));

            if (!Directory.Exists(setDir))
            {
                Console.Error.WriteLine($"제스처 세트 폴더를 찾을 수 없음: {setDir}");
                return 1;
            }

            // 원시 점 데이터를 직접 읽는다 (Gesture 생성자는 정규화를 해버리므로 교란은 원시 점에 가한다)
            var raw = new List<(string Name, Point[] Points)>();
            foreach (string file in Directory.GetFiles(setDir, "*.xml").OrderBy(f => f))
                raw.Add(ReadRawGesture(file));

            Console.WriteLine($"템플릿 로드: {raw.Count}종 ({setDir})");

            var trainingSet = raw.Select(g => new Gesture(g.Points, g.Name)).ToArray();

            // 1) 자기 자신 분류 — 전부 일치해야 정상
            int selfOk = 0;
            foreach (var g in raw)
            {
                Result r = PointCloudRecognizer.Classify(new Gesture(g.Points), trainingSet);
                if (r.GestureClass == g.Name) selfOk++;
                else Console.WriteLine($"  자기 분류 실패: {g.Name} -> {r.GestureClass}");
            }
            Console.WriteLine($"자기 자신 분류: {selfOk}/{raw.Count}");

            // 2) 교란 변형 분류
            var rnd = new Random(42);
            var latencies = new List<double>();
            int total = 0, correct = 0;
            var perClass = new Dictionary<string, (int ok, int n)>();

            foreach (var g in raw)
            {
                int ok = 0;
                for (int i = 0; i < VariantsPerClass; i++)
                {
                    Point[] variant = Perturb(g.Points, rnd);
                    var sw = Stopwatch.StartNew();
                    var candidate = new Gesture(variant); // 정규화 비용도 실사용 지연에 포함
                    Result r = PointCloudRecognizer.Classify(candidate, trainingSet);
                    sw.Stop();
                    latencies.Add(sw.Elapsed.TotalMilliseconds);
                    total++;
                    if (r.GestureClass == g.Name) { ok++; correct++; }
                }
                perClass[g.Name] = (ok, VariantsPerClass);
            }

            Console.WriteLine();
            Console.WriteLine($"교란 변형 인식률: {correct}/{total} ({100.0 * correct / total:F1}%)  — 클래스당 {VariantsPerClass}회");
            foreach (var kv in perClass.OrderBy(k => k.Value.ok))
                Console.WriteLine($"  {kv.Key,-20} {kv.Value.ok,3}/{kv.Value.n}");

            latencies.Sort();
            Console.WriteLine();
            Console.WriteLine($"분류 지연(정규화 포함, 템플릿 {trainingSet.Length}개 기준):");
            Console.WriteLine($"  평균 {latencies.Average():F3} ms / 중앙값 {latencies[latencies.Count / 2]:F3} ms / p95 {latencies[(int)(latencies.Count * 0.95)]:F3} ms / 최대 {latencies[^1]:F3} ms");
            return 0;
        }

        // 마우스 필기 편차를 흉내 낸 교란: 균등 크기 0.7~1.3배, 회전 ±10도, 가우시안 지터(바운딩박스의 2%), 점 15% 솎아내기
        private static Point[] Perturb(Point[] src, Random rnd)
        {
            float minx = src.Min(p => p.X), maxx = src.Max(p => p.X);
            float miny = src.Min(p => p.Y), maxy = src.Max(p => p.Y);
            float size = Math.Max(maxx - minx, maxy - miny);
            float cx = (minx + maxx) / 2f, cy = (miny + maxy) / 2f;

            float scale = 0.7f + (float)rnd.NextDouble() * 0.6f;
            double angle = (rnd.NextDouble() * 20.0 - 10.0) * Math.PI / 180.0;
            float cos = (float)Math.Cos(angle), sin = (float)Math.Sin(angle);
            float sigma = size * 0.02f;

            var result = new List<Point>(src.Length);
            foreach (var p in src)
            {
                if (rnd.NextDouble() < 0.15 && src.Length > 20) continue; // 점 솎아내기
                float dx = (p.X - cx) * scale, dy = (p.Y - cy) * scale;
                float rx = dx * cos - dy * sin + cx + NextGaussian(rnd) * sigma;
                float ry = dx * sin + dy * cos + cy + NextGaussian(rnd) * sigma;
                result.Add(new Point(rx, ry, p.StrokeID));
            }
            return result.ToArray();
        }

        private static float NextGaussian(Random rnd)
        {
            double u1 = 1.0 - rnd.NextDouble();
            double u2 = rnd.NextDouble();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
        }

        private static (string, Point[]) ReadRawGesture(string fileName)
        {
            var points = new List<Point>();
            string name = "";
            int stroke = -1;
            using var reader = new XmlTextReader(File.OpenText(fileName));
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element) continue;
                switch (reader.Name)
                {
                    case "Gesture":
                        name = reader["Name"];
                        if (name.Contains('~')) name = name.Substring(0, name.LastIndexOf('~'));
                        if (name.Contains('_')) name = name.Replace('_', ' ');
                        break;
                    case "Stroke":
                        stroke++;
                        break;
                    case "Point":
                        points.Add(new Point(
                            float.Parse(reader["X"].Replace(',', '.'), CultureInfo.InvariantCulture),
                            float.Parse(reader["Y"].Replace(',', '.'), CultureInfo.InvariantCulture),
                            stroke));
                        break;
                }
            }
            return (name, points.ToArray());
        }
    }
}
