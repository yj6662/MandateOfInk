# Meshy image-to-3d로 해례본 고서 모델 생성 — Recraft 아이콘(T_UI_HaeryeBook)과 룩 일치(2026-07-27).
# 사용: python meshy_generate_book.py  (Tools/.env 의 MESHY_API_KEY 사용)
import base64
import io
import json
import os
import sys
import time
import urllib.request

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

TOOLS = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(TOOLS, "generated", "codex_raw", "T_UI_HaeryeBook.png")
OUT_DIR = os.path.join(TOOLS, "..", "MandateOfInk", "Assets", "_Project", "Art", "Models", "Props")
BASE = "https://api.meshy.ai/openapi/v1/image-to-3d"


def env_key():
    for line in open(os.path.join(TOOLS, ".env"), encoding="utf-8-sig"):
        line = line.strip()
        if line.startswith("MESHY_API_KEY="):
            key = line.split("=", 1)[1].strip()
            if key:
                return key
    raise SystemExit("MESHY_API_KEY 없음")


KEY = env_key()


def call(method, url, payload=None):
    data = json.dumps(payload).encode() if payload is not None else None
    req = urllib.request.Request(url, data=data, method=method,
                                 headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:
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
        time.sleep(12)
    raise SystemExit(f"{label} 시간 초과")


def download(url, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    urllib.request.urlretrieve(url, path)
    print("다운로드:", path, os.path.getsize(path), "bytes", flush=True)


uri = "data:image/png;base64," + base64.b64encode(open(SRC, "rb").read()).decode()
tid = call("POST", BASE, {
    "image_url": uri, "should_remesh": True, "should_texture": True,
    "target_polycount": 6000, "topology": "triangle",
})["result"]
print("image-to-3d 태스크:", tid, flush=True)
task = wait(tid, "book")

fbx = task.get("model_urls", {}).get("fbx")
if not fbx:
    raise SystemExit("FBX URL 없음: " + json.dumps(task.get("model_urls", {})))
download(fbx, os.path.join(OUT_DIR, "SM_HaeryeBook.fbx"))
for tex in task.get("texture_urls", []) or []:
    for kind, url in tex.items():
        if url:
            download(url, os.path.join(OUT_DIR, f"T_HaeryeBook_{kind}.png"))
print("전체 완료", flush=True)
