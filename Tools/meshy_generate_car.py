# Meshy image-to-3d로 마석 자동차 생성 — 사용자 삼면도 컨셉아트 기반(2026-07-27).
# 삼면도(정면/측면/후면)를 잘라 multi-image-to-3d에 투입, 실패 시 측면 단일 image-to-3d 폴백.
# 사용: python meshy_generate_car.py  (Tools/.env 의 MESHY_API_KEY 사용)
import base64
import io
import json
import os
import sys
import time
import urllib.request

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

CONCEPT = r"C:\Users\yj666\Desktop\오행부 프로젝트\컨셉아트\마석 자동차.png"
CROP_DIR = os.path.join(os.path.dirname(__file__), "generated", "car_views")
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "MandateOfInk", "Assets", "_Project", "Art", "Models", "Vehicle")
MULTI_BASE = "https://api.meshy.ai/openapi/v1/multi-image-to-3d"
SINGLE_BASE = "https://api.meshy.ai/openapi/v1/image-to-3d"


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
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read().decode())


def wait(base, task_id, label, timeout_s=1800):
    start = time.time()
    while time.time() - start < timeout_s:
        t = call("GET", f"{base}/{task_id}")
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


def to_data_uri(img):
    buf = io.BytesIO()
    img.save(buf, format="PNG")
    return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode()


# 삼면도 분할 — 좌(정면)/중(측면)/우(후면), 여백 살짝 포함
im = Image.open(CONCEPT)
w, h = im.size
views = {
    "front": im.crop((0, 0, int(w * 0.295), h)),
    "side": im.crop((int(w * 0.285), 0, int(w * 0.735), h)),
    "back": im.crop((int(w * 0.725), 0, w, h)),
}
os.makedirs(CROP_DIR, exist_ok=True)
uris = []
for name, img in views.items():
    img.save(os.path.join(CROP_DIR, f"car_{name}.png"))
    uris.append(to_data_uri(img))
    print(f"분할 저장: car_{name}.png {img.size}", flush=True)

# 멀티 이미지 생성 시도 → 실패 시 측면 단일 폴백
task = None
try:
    tid = call("POST", MULTI_BASE, {
        "image_urls": uris, "should_remesh": True, "should_texture": True,
        "target_polycount": 30000, "topology": "triangle",
    })["result"]
    print("multi-image 태스크:", tid, flush=True)
    task = wait(MULTI_BASE, tid, "car multi")
except Exception as e:
    print("multi-image 실패, 측면 단일 폴백:", e, flush=True)
    tid = call("POST", SINGLE_BASE, {
        "image_url": uris[1], "should_remesh": True, "should_texture": True,
        "target_polycount": 30000, "topology": "triangle",
    })["result"]
    print("single-image 태스크:", tid, flush=True)
    task = wait(SINGLE_BASE, tid, "car single")

fbx = task.get("model_urls", {}).get("fbx")
if not fbx:
    raise SystemExit("FBX URL 없음: " + json.dumps(task.get("model_urls", {})))
download(fbx, os.path.join(OUT_DIR, "SM_MagicStoneCar.fbx"))
for tex in task.get("texture_urls", []) or []:
    for kind, url in tex.items():
        if url:
            download(url, os.path.join(OUT_DIR, f"T_MagicStoneCar_{kind}.png"))
print("전체 완료", flush=True)
