# Meshy: 적 카탈로그 프로토 모델 일괄 생성 (병렬 제출)
# 필드 7종 + 정예 3종. 취생(연기)·산적(인간형)·해태(보스, 컨셉 기반 후속)는 제외.
import io, json, os, sys, time, urllib.request
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
BASE = "https://api.meshy.ai/openapi/v2/text-to-3d"
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "MandateOfInk", "Assets", "_Project", "Art", "Models", "Mobs")
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

MOBS = [
    ("Dokkaebibul", 8000,
     "small korean will-o-wisp spirit flame, teardrop shaped ghostly fire wisp with a faint glowing face, "
     "floating ember spirit, stylized game asset, single object, plain background"),
    ("Anggaengi", 15000,
     "small mischievous korean goblin imp, child-sized hunched creature in ragged korean traditional cloth, "
     "small glowing lantern light on top of its head, big grabbing hands, stylized game asset, single character, plain background"),
    ("Gaekgwi", 15000,
     "korean wandering ghost in tattered white traditional hanbok robes, faceless drifting phantom with long sleeves, "
     "lower body fading into a mist wisp, somber, stylized game asset, single character, plain background"),
    ("Wagwi", 18000,
     "korean dokkaebi goblin made of dark grey roof tiles, one-legged hunched tile golem holding a wooden club, "
     "traditional giwa tile armor plates, stylized game asset, single character, plain background"),
    ("Geumdwaeji", 18000,
     "golden boar monster, large aggressive wild boar with golden bristle fur and big tusks, korean folklore beast, "
     "stylized game asset, single animal, plain background"),
    ("Mulgwisin", 15000,
     "korean drowned water ghost, gaunt dark slick figure with unnaturally long thin arms and grasping hands, "
     "dripping wet hair covering the face, hunched crawling posture, stylized game asset, single character, plain background"),
    ("Galbeom", 20000,
     "korean tiger, lean muscular tiger with dark charcoal and brown stripes, snarling prowling stance, "
     "stylized game asset, single animal, plain background"),
    ("SunkenHan", 18000,
     "amorphous black water elemental mass, dark liquid blob creature with several pale drowned faces surfacing "
     "from its body, dripping tendrils, ominous, stylized game asset, single creature, plain background"),
    ("MoltenFire", 20000,
     "molten metal slag creature, hulking humanoid mass of glowing orange molten iron with cooled black crust plates, "
     "dripping metal, furnace spirit, stylized game asset, single creature, plain background"),
    ("MonkAutomaton", 25000,
     "steampunk bronze buddha statue robot, serene buddha-faced automaton with riveted bronze body, exposed gears "
     "and steam pipes at the joints, monk robe shaped metal plates, standing pose, stylized game asset, single character, plain background"),
]

def poll_all(tasks, label):
    # tasks: {name: task_id} — 전부 끝날 때까지 순회 폴링
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

# 1) 프리뷰 전체 제출
previews = {}
for name, poly, prompt in MOBS:
    pid = call("POST", BASE, {"mode": "preview", "prompt": prompt, "art_style": "realistic",
                              "target_polycount": poly, "topology": "triangle", "should_remesh": True})["result"]
    previews[name] = pid
    print("preview 제출:", name, pid, flush=True)
    time.sleep(1)

done_p, failed_p = poll_all(dict(previews), "preview")

# 2) 리파인 전체 제출
refines = {}
for name in done_p:
    rid = call("POST", BASE, {"mode": "refine", "preview_task_id": previews[name], "enable_pbr": False})["result"]
    refines[name] = rid
    print("refine 제출:", name, rid, flush=True)
    time.sleep(1)

done_r, failed_r = poll_all(dict(refines), "refine")

# 3) 다운로드
for name, task in done_r.items():
    download(task["model_urls"]["fbx"], os.path.join(OUT_DIR, f"SM_{name}.fbx"))
    for tex in task.get("texture_urls", []) or []:
        for kind, url in tex.items():
            if url: download(url, os.path.join(OUT_DIR, f"T_{name}_{kind}.png"))

print("전체 완료 — 성공:", list(done_r.keys()), "실패:", {**failed_p, **failed_r}, flush=True)
