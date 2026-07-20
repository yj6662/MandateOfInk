# Meshy: 스팀펑크 메카천수관음 몸통 + 기계 팔 단품 생성
import io, json, os, sys, time, urllib.request
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
BASE = "https://api.meshy.ai/openapi/v2/text-to-3d"
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "MandateOfInk", "Assets", "_Project", "Art", "Models", "Boss")
KEY = [l.split("=",1)[1].strip() for l in open(os.path.join(os.path.dirname(__file__), ".env"), encoding="utf-8-sig") if l.startswith("MESHY_API_KEY=")][0]

def call(method, url, payload=None):
    data = json.dumps(payload).encode() if payload is not None else None
    req = urllib.request.Request(url, data=data, method=method, headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.loads(r.read().decode())

def wait(task_id, label, timeout_s=1500):
    start = time.time()
    while time.time() - start < timeout_s:
        t = call("GET", f"{BASE}/{task_id}")
        print(f"[{label}] {t.get('status')} {t.get('progress', 0)}%", flush=True)
        if t.get("status") == "SUCCEEDED": return t
        if t.get("status") in ("FAILED", "CANCELED"): raise SystemExit(f"{label} 실패: {t.get('task_error')}")
        time.sleep(15)
    raise SystemExit(f"{label} 시간 초과")

def download(url, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=180) as r, open(path, "wb") as f:
        f.write(r.read())
    print("다운로드:", path, os.path.getsize(path), flush=True)

def generate(name, prompt, polycount):
    pid = call("POST", BASE, {"mode": "preview", "prompt": prompt, "art_style": "realistic",
                              "target_polycount": polycount, "topology": "triangle", "should_remesh": True})["result"]
    print(name, "preview:", pid, flush=True)
    wait(pid, name + "-preview")
    rid = call("POST", BASE, {"mode": "refine", "preview_task_id": pid, "enable_pbr": False})["result"]
    task = wait(rid, name + "-refine")
    download(task["model_urls"]["fbx"], os.path.join(OUT_DIR, f"SM_{name}.fbx"))
    for tex in task.get("texture_urls", []) or []:
        for kind, url in tex.items():
            if url: download(url, os.path.join(OUT_DIR, f"T_{name}_{kind}.png"))

generate("SteamGuanyinBody",
    "Massive steampunk mechanical Guanyin statue boss, brass and dark iron body with rivets, exposed gears, "
    "steam pipes and pressure valves, serene bronze buddha face with cracked patina, large clockwork gear halo "
    "behind head, torso with folded primary hands, NO extra side arms, standing full body on ornate pedestal, "
    "highly detailed, game boss", 100000)
generate("SteamArm",
    "Single mechanical steampunk arm, articulated brass and steel construction with pistons, gears, riveted "
    "plates and hydraulic joints, shoulder mount at one end, large grasping metal hand at the other end, "
    "straight reaching pose, highly detailed, game asset, isolated single arm only", 30000)
print("전체 완료", flush=True)
