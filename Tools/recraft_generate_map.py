# Recraft로 대동여지도풍 강토 지도 생성 — 지도 화면(디제틱 소품)용, 2안 생성 후 사람이 선택(2026-07-27).
# 사용: python recraft_generate_map.py  (Tools/.env 의 RECRAFT_API_KEY 사용)
import io, json, os, sys, urllib.request
from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
TOOLS = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(TOOLS, "generated", "map_raw")
OUT = os.path.join(TOOLS, "..", "MandateOfInk", "Assets", "_Project", "Art", "UI")
os.makedirs(RAW, exist_ok=True)
os.makedirs(OUT, exist_ok=True)

KEY = [l.split("=", 1)[1].strip() for l in open(os.path.join(TOOLS, ".env"), encoding="utf-8-sig")
       if l.strip().startswith("RECRAFT_API_KEY=")][0]

PROMPT = (
    "antique korean woodblock-printed map in the style of joseon dynasty daedongyeojido, "
    "korean peninsula coastline, mountain ranges drawn as connected dark sawtooth ridge chains, "
    "rivers as thin flowing double lines, small circular town markers connected by road lines, "
    "printed in black ink on aged hanji mulberry paper, warm ivory sepia tones, subtle paper grain "
    "and worn creases, viewed flat from above, no readable text, no compass rose, no borders decoration"
)

JOBS = [("T_UI_DaedongMap_A", PROMPT), ("T_UI_DaedongMap_B", PROMPT)]


def generate(prompt):
    body = json.dumps({"prompt": prompt, "style": "digital_illustration", "size": "1024x1820",
                       "n": 1, "response_format": "url"}).encode()
    req = urllib.request.Request("https://external.api.recraft.ai/v1/images/generations", data=body,
                                 headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=180) as r:
        return json.loads(r.read().decode())["data"][0]["url"]


def download(url, path):
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=180) as r, open(path, "wb") as f:
        f.write(r.read())


for name, prompt in JOBS:
    print("생성:", name, flush=True)
    url = generate(prompt)
    raw_path = os.path.join(RAW, name + ".png")
    download(url, raw_path)
    im = Image.open(raw_path)
    im.save(os.path.join(OUT, name + ".png"))
    print("저장:", name, im.size, flush=True)

print("전체 완료", flush=True)
