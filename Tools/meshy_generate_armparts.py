# Meshy: 메카천수관음 팔 부품 6종 병렬 생성 — 부품 분해·리지드 조립용
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

def download(url, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=180) as r, open(path, "wb") as f:
        f.write(r.read())
    print("다운로드:", os.path.basename(path), os.path.getsize(path), flush=True)

STYLE = "antique dark bronze with brass accents, riveted plates, weathered buddhist temple machinery, stylized game asset, single object, plain background"
PARTS = [
    ("ArmUpper", 12000,
     f"steampunk mechanical upper arm housing, straight thick cylindrical bronze casing segment with exposed gear ring at the shoulder end and hinge fork at the elbow end, {STYLE}"),
    ("ArmElbow", 9000,
     f"steampunk mechanical elbow joint block, compact brass hinge module with one large toothed gear wheel on the side and rivet details, {STYLE}"),
    ("ArmForearm", 12000,
     f"steampunk mechanical forearm, straight tapered bronze sleeve segment with two open piston sleeve channels along its length and riveted plating, {STYLE}"),
    ("ArmPistonRod", 6000,
     f"polished brass hydraulic piston with cylinder sleeve and protruding rod, straight machine actuator part, {STYLE}"),
    ("ArmWrist", 7000,
     f"small steampunk wrist joint ring module, brass ball joint collar with thin gear ring and bolts, {STYLE}"),
    ("ArmHand", 22000,
     f"steampunk mechanical open hand, elegant bronze and steel hand with all five articulated fingers extended and palm plate exposed and detailed, buddhist statue hand proportions, {STYLE}"),
]

def poll_all(tasks, label):
    done, failed = {}, {}
    while tasks:
        for name, tid in list(tasks.items()):
            t = call("GET", f"{BASE}/{tid}")
            st = t.get("status")
            if st == "SUCCEEDED":
                done[name] = t
                del tasks[name]
                print(f"[{label}] {name} 완료 (잔여 {len(tasks)})", flush=True)
            elif st in ("FAILED", "CANCELED"):
                failed[name] = t.get("task_error")
                del tasks[name]
                print(f"[{label}] {name} 실패: {t.get('task_error')}", flush=True)
        if tasks:
            time.sleep(20)
    return done, failed

previews = {}
for name, poly, prompt in PARTS:
    pid = call("POST", BASE, {"mode": "preview", "prompt": prompt, "art_style": "realistic",
                              "target_polycount": poly, "topology": "triangle", "should_remesh": True})["result"]
    previews[name] = pid
    print("preview 제출:", name, pid, flush=True)
    time.sleep(1)

done_p, failed_p = poll_all(dict(previews), "preview")

refines = {}
for name in done_p:
    rid = call("POST", BASE, {"mode": "refine", "preview_task_id": previews[name], "enable_pbr": False})["result"]
    refines[name] = rid
    print("refine 제출:", name, rid, flush=True)
    time.sleep(1)

done_r, failed_r = poll_all(dict(refines), "refine")

for name, task in done_r.items():
    download(task["model_urls"]["fbx"], os.path.join(OUT_DIR, f"SM_{name}.fbx"))
    for tex in task.get("texture_urls", []) or []:
        for kind, url in tex.items():
            if url and kind == "base_color":
                download(url, os.path.join(OUT_DIR, f"T_{name}_{kind}.png"))

print("전체 완료 — 성공:", list(done_r.keys()), "실패:", {**failed_p, **failed_r}, flush=True)
