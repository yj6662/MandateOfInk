# Recraft로 붓 스트로크 텍스처 생성 -> 알파 변환 -> 커버리지 정렬 아틀라스 굽기
# 산출물: MandateOfInk/Assets/_Project/Art/Ink/T_InkStrokeAtlas.png (행당 512x128, 위=촉촉 아래=갈필 순서 아님 — 행0=촉촉)
import io
import json
import os
import sys
import time
import urllib.request

from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

TOOLS = os.path.dirname(os.path.abspath(__file__))
RAW_DIR = os.path.join(TOOLS, "generated", "ink_raw_v2")
OUT_PATH = os.path.join(TOOLS, "..", "MandateOfInk", "Assets", "_Project", "Art", "Ink", "T_InkStrokeAtlas.png")
ROW_W, ROW_H = 512, 128

PROMPTS = [
    ("wet", "isolated single horizontal black ink brushstroke smear mark, flat 2D graphic texture asset, "
            "thick wet solid ink, plain white background, only the paint mark itself, no paintbrush, no objects, no text"),
    ("wet", "one isolated bold horizontal black brushstroke mark, flat graphic design element, dense solid ink body "
            "with pressed start and tapered end, pure white background, no brush tool visible, no text"),
    ("mid", "isolated single horizontal black ink brushstroke mark, flat 2D texture, semi-dry with visible bristle "
            "streak lines running along the stroke, white background, only the ink mark, no paintbrush, no text"),
    ("mid", "one isolated horizontal black paint stroke smear, graphic design element, medium dry with ragged "
            "edges and thin streaks, plain white background, no objects, no text"),
    ("mid", "isolated expressive horizontal black ink stroke mark, flat texture asset, slightly dry brush texture, "
            "white background only, nothing else, no brush, no text"),
    ("dry", "isolated single horizontal dry-brush black ink stroke mark, flat 2D graphic, heavy flying white effect "
            "with scratchy bristle gaps, plain white background, only the ink mark, no paintbrush, no text"),
    ("dry", "one isolated very dry horizontal black brushstroke mark broken into thin scratchy bristle lines, "
            "flat graphic texture, white background, nothing else, no brush, no text"),
    ("dry", "isolated rough dry-brush horizontal black stroke mark, sparse scratchy ink texture, flat 2D asset, "
            "pure white background, no objects, no text"),
]


def env_key():
    for line in open(os.path.join(TOOLS, ".env"), encoding="utf-8-sig"):
        line = line.strip()
        if line.startswith("RECRAFT_API_KEY="):
            return line.split("=", 1)[1].strip()
    raise SystemExit("RECRAFT_API_KEY 없음")


KEY = env_key()


def generate(prompt):
    body = json.dumps({
        "prompt": prompt, "style": "digital_illustration", "size": "1820x1024",
        "n": 1, "response_format": "url",
    }).encode()
    req = urllib.request.Request("https://external.api.recraft.ai/v1/images/generations", data=body,
                                 headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read().decode())["data"][0]["url"]


def to_alpha_strip(path):
    # 검은 획 -> 알파(불투명), 배경 -> 투명. 획 영역만 잘라 ROW_W x ROW_H로 정규화.
    img = Image.open(path).convert("L")
    px = img.load()
    w, h = img.size
    alpha = Image.new("L", (w, h))
    ap = alpha.load()
    for y in range(h):
        for x in range(w):
            a = 1.0 - px[x, y] / 255.0
            a = max(0.0, min(1.0, (a - 0.06) * 1.18))  # 배경 노이즈 컷 + 대비
            ap[x, y] = int(a * 255)
    bbox = alpha.getbbox()
    if bbox is None:
        return None, 0.0
    alpha = alpha.crop(bbox).resize((ROW_W, ROW_H), Image.LANCZOS)
    coverage = sum(alpha.getdata()) / (255.0 * ROW_W * ROW_H)
    return alpha, coverage


os.makedirs(RAW_DIR, exist_ok=True)
strips = []
for i, (tier, prompt) in enumerate(PROMPTS):
    raw = os.path.join(RAW_DIR, f"stroke_{i:02d}_{tier}.png")
    if not os.path.exists(raw):
        print(f"[{i+1}/{len(PROMPTS)}] 생성 중 ({tier})...", flush=True)
        url = generate(prompt)
        dl = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
        with urllib.request.urlopen(dl, timeout=120) as resp, open(raw, "wb") as f:
            f.write(resp.read())
        time.sleep(1)
    strip, cov = to_alpha_strip(raw)
    if strip is not None and cov > 0.02:
        strips.append((cov, strip))
        print(f"  커버리지 {cov:.3f}", flush=True)
    else:
        print("  획 추출 실패 — 제외", flush=True)

if len(strips) < 3:
    raise SystemExit("사용 가능한 스트로크가 너무 적음")

strips.sort(key=lambda s: -s[0])  # 행0 = 커버리지 최대(촉촉) ... 마지막 = 갈필
atlas = Image.new("RGBA", (ROW_W, ROW_H * len(strips)), (255, 255, 255, 0))
for row, (cov, strip) in enumerate(strips):
    rgba = Image.merge("RGBA", (Image.new("L", strip.size, 255),) * 3 + (strip,))
    # 행0이 텍스처 아래(V=0)가 되도록 위에서부터 역순 배치
    atlas.paste(rgba, (0, ROW_H * (len(strips) - 1 - row)))

os.makedirs(os.path.dirname(OUT_PATH), exist_ok=True)
atlas.save(OUT_PATH)
print(f"아틀라스 저장: {os.path.abspath(OUT_PATH)} (행 {len(strips)}개)", flush=True)
