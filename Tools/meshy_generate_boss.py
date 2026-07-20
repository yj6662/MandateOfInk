# Meshy text-to-3d로 대필(붓) 프로토 모델 생성 — preview -> refine -> FBX/텍스처 다운로드
# 사용: python meshy_generate_brush.py  (Tools/.env 의 MESHY_API_KEY 사용)
import io
import json
import os
import sys
import time
import urllib.request

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

BASE = "https://api.meshy.ai/openapi/v2/text-to-3d"
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "MandateOfInk", "Assets", "_Project", "Art", "Models", "Boss")
PROMPT = ("Mechanical thousand-armed Guanyin statue, steampunk automaton boss, "
          "fan of many articulated brass and steel arms arrayed like a halo behind the torso, "
          "serene buddhist statue face contrasting with menacing industrial machinery body, "
          "standing full body, game boss character, clean silhouette")


def env_key():
    p = os.path.join(os.path.dirname(__file__), ".env")
    for line in open(p, encoding="utf-8-sig"):
        line = line.strip()
        if line.startswith("MESHY_API_KEY="):
            return line.split("=", 1)[1].strip()
    raise SystemExit("MESHY_API_KEY 없음")


KEY = env_key()


def call(method, url, payload=None):
    data = json.dumps(payload).encode() if payload is not None else None
    req = urllib.request.Request(url, data=data, method=method,
                                 headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.loads(r.read().decode())


def wait(task_id, label, timeout_s=900):
    start = time.time()
    while time.time() - start < timeout_s:
        t = call("GET", f"{BASE}/{task_id}")
        status = t.get("status")
        print(f"[{label}] {status} {t.get('progress', 0)}%", flush=True)
        if status == "SUCCEEDED":
            return t
        if status in ("FAILED", "CANCELED"):
            raise SystemExit(f"{label} 실패: {t.get('task_error')}")
        time.sleep(12)
    raise SystemExit(f"{label} 시간 초과")


def download(url, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    urllib.request.urlretrieve(url, path)
    print("다운로드:", path, os.path.getsize(path), "bytes", flush=True)


preview_id = call("POST", BASE, {
    "mode": "preview", "prompt": PROMPT, "art_style": "realistic",
    "target_polycount": 30000, "topology": "triangle", "should_remesh": True,
})["result"]
print("preview 태스크:", preview_id, flush=True)
wait(preview_id, "preview")

refine_id = call("POST", BASE, {"mode": "refine", "preview_task_id": preview_id, "enable_pbr": False})["result"]
print("refine 태스크:", refine_id, flush=True)
task = wait(refine_id, "refine")

fbx = task.get("model_urls", {}).get("fbx")
if not fbx:
    raise SystemExit("FBX URL 없음: " + json.dumps(task.get("model_urls", {})))
download(fbx, os.path.join(OUT_DIR, "SM_MechaGuanyin.fbx"))
for i, tex in enumerate(task.get("texture_urls", []) or []):
    for kind, url in tex.items():
        if url:
            download(url, os.path.join(OUT_DIR, f"T_MechaGuanyin_{kind}.png"))
print("완료", flush=True)
