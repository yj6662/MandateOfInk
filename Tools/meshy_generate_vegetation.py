# Meshy text-to-3d로 식생 3종 생성 — 인왕제색도풍 환경 배치용(사용자 결정 2026-07-27).
# 소나무(비틀린 줄기·층진 수관)·버드나무(늘어진 가지)·풀덤불. preview -> refine -> FBX/텍스처 다운로드.
# 사용: python meshy_generate_vegetation.py  (Tools/.env 의 MESHY_API_KEY 사용)
import io
import json
import os
import sys
import time
import urllib.request

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

BASE = "https://api.meshy.ai/openapi/v2/text-to-3d"
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "MandateOfInk", "Assets", "_Project", "Art", "Models", "Vegetation")

PLANTS = [
    ("SM_Tree_Pine", "Tree_Pine", 8000,
     "Korean red pine tree, twisted gnarled trunk leaning slightly, layered flat clusters "
     "of dark needle canopy like traditional East Asian ink painting pine, bare lower trunk, "
     "natural, game environment asset, single tree"),
    ("SM_Tree_Willow", "Tree_Willow", 8000,
     "Weeping willow tree, slender curved trunk, long drooping branch curtains, "
     "natural, game environment asset, single tree"),
    ("SM_Grass_Tuft", "Grass_Tuft", 4000,
     "Clump of tall wild meadow grass, single grass tuft bush with arching blades, "
     "natural, game environment asset, low poly"),
]


def env_key():
    p = os.path.join(os.path.dirname(__file__), ".env")
    for line in open(p, encoding="utf-8-sig"):
        line = line.strip()
        if line.startswith("MESHY_API_KEY="):
            key = line.split("=", 1)[1].strip()
            if key:
                return key
    raise SystemExit("MESHY_API_KEY 없음 — Tools/.env 를 확인할 것")


KEY = env_key()


def call(method, url, payload=None):
    data = json.dumps(payload).encode() if payload is not None else None
    req = urllib.request.Request(url, data=data, method=method,
                                 headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.loads(r.read().decode())


def wait(task_id, label, timeout_s=1200):
    start = time.time()
    while time.time() - start < timeout_s:
        t = call("GET", f"{BASE}/{task_id}")
        status = t.get("status")
        print(f"[{label}] {status} {t.get('progress', 0)}%", flush=True)
        if status == "SUCCEEDED":
            return t
        if status in ("FAILED", "CANCELED"):
            raise SystemExit(f"{label} 실패: {t.get('task_error')}")
        time.sleep(15)
    raise SystemExit(f"{label} 시간 초과")


def download(url, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    urllib.request.urlretrieve(url, path)
    print("다운로드:", path, os.path.getsize(path), "bytes", flush=True)


for file_name, tex_prefix, polycount, prompt in PLANTS:
    print(f"=== {file_name} 생성 시작 ===", flush=True)
    preview_id = call("POST", BASE, {
        "mode": "preview", "prompt": prompt, "art_style": "realistic",
        "target_polycount": polycount, "topology": "triangle", "should_remesh": True,
    })["result"]
    print("preview 태스크:", preview_id, flush=True)
    wait(preview_id, f"{file_name} preview")

    refine_id = call("POST", BASE, {"mode": "refine", "preview_task_id": preview_id, "enable_pbr": False})["result"]
    print("refine 태스크:", refine_id, flush=True)
    task = wait(refine_id, f"{file_name} refine")

    fbx = task.get("model_urls", {}).get("fbx")
    if not fbx:
        raise SystemExit("FBX URL 없음: " + json.dumps(task.get("model_urls", {})))
    download(fbx, os.path.join(OUT_DIR, f"{file_name}.fbx"))
    for tex in task.get("texture_urls", []) or []:
        for kind, url in tex.items():
            if url:
                download(url, os.path.join(OUT_DIR, f"T_{tex_prefix}_{kind}.png"))

print("전체 완료", flush=True)
