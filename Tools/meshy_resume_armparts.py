# Meshy: 팔 부품 리파인 재개 — 끊긴 세션의 refine ID 폴링·다운로드
import io, json, os, sys, time, urllib.request
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
BASE = "https://api.meshy.ai/openapi/v2/text-to-3d"
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "MandateOfInk", "Assets", "_Project", "Art", "Models", "Boss")
KEY = [l.split("=",1)[1].strip() for l in open(os.path.join(os.path.dirname(__file__), ".env"), encoding="utf-8-sig") if l.startswith("MESHY_API_KEY=")][0]

REFINES = {
    "ArmPistonRod": "019f81db-bd63-7f86-941b-13c34bb5c14f",
    "ArmHand": "019f81db-c2ce-70fa-8ad0-9dafc417cd7d",
    "ArmUpper": "019f81db-c85c-75c4-aab3-f2e1f8268259",
    "ArmForearm": "019f81db-cdcb-70b6-828b-b4c0edf19289",
    "ArmElbow": "019f81db-d34e-7f8d-a84f-25b4b80952a7",
    "ArmWrist": "019f81db-d8c6-70fd-8a5e-3d05ca2eacaa",
}

def call(url):
    req = urllib.request.Request(url, headers={"Authorization": f"Bearer {KEY}"})
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.loads(r.read().decode())

def download(url, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=180) as r, open(path, "wb") as f:
        f.write(r.read())
    print("다운로드:", os.path.basename(path), os.path.getsize(path), flush=True)

tasks = dict(REFINES)
done = {}
while tasks:
    for name, tid in list(tasks.items()):
        try:
            t = call(f"{BASE}/{tid}")
        except Exception as e:
            print(name, "폴링 오류:", e, flush=True)
            continue
        st = t.get("status")
        if st == "SUCCEEDED":
            done[name] = t
            del tasks[name]
            print(f"{name} 완료 (잔여 {len(tasks)})", flush=True)
        elif st in ("FAILED", "CANCELED"):
            del tasks[name]
            print(f"{name} 실패: {t.get('task_error')}", flush=True)
    if tasks:
        time.sleep(15)

for name, task in done.items():
    download(task["model_urls"]["fbx"], os.path.join(OUT_DIR, f"SM_{name}.fbx"))
    for tex in task.get("texture_urls", []) or []:
        for kind, url in tex.items():
            if url and kind == "base_color":
                download(url, os.path.join(OUT_DIR, f"T_{name}_base_color.png"))
print("재개 완료 — 성공:", list(done.keys()), flush=True)
