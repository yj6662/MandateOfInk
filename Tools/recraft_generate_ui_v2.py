# Recraft UI 재생성 — 평면 실패분(한지 패널·소형, 낙관, 먹 번짐)
# 교훈: "texture"만으론 원근 사진이 나온다 — 프레임 가득·수직 부감·무그림자를 명시할 것.
import io, json, os, sys, urllib.request
from collections import deque
from PIL import Image, ImageFilter
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
TOOLS = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(TOOLS, "generated", "ui_raw")
OUT = os.path.join(TOOLS, "..", "MandateOfInk", "Assets", "_Project", "Art", "UI")
KEY = [l.split("=", 1)[1].strip() for l in open(os.path.join(TOOLS, ".env"), encoding="utf-8-sig")
       if l.strip().startswith("RECRAFT_API_KEY=")][0]


def generate(prompt, size):
    body = json.dumps({"prompt": prompt, "style": "digital_illustration", "size": size,
                       "n": 1, "response_format": "url"}).encode()
    req = urllib.request.Request("https://external.api.recraft.ai/v1/images/generations", data=body,
                                 headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=180) as r:
        return json.loads(r.read().decode())["data"][0]["url"]


def download(url, path):
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=180) as r, open(path, "wb") as f:
        f.write(r.read())


def white_flood_alpha(im):
    im = im.convert("RGB")
    w, h = im.size
    small = im.resize((w // 2, h // 2))
    sw, sh = small.size
    px = small.load()

    def is_white(p):
        return p[0] > 228 and p[1] > 228 and p[2] > 228

    visited = [[False] * sw for _ in range(sh)]
    q = deque()
    for x in range(sw):
        for y in (0, sh - 1):
            if is_white(px[x, y]) and not visited[y][x]:
                visited[y][x] = True
                q.append((x, y))
    for y in range(sh):
        for x in (0, sw - 1):
            if is_white(px[x, y]) and not visited[y][x]:
                visited[y][x] = True
                q.append((x, y))
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < sw and 0 <= ny < sh and not visited[ny][nx] and is_white(px[nx, ny]):
                visited[ny][nx] = True
                q.append((nx, ny))
    mask = Image.new("L", (sw, sh), 255)
    mp = mask.load()
    for y in range(sh):
        for x in range(sw):
            if visited[y][x]:
                mp[x, y] = 0
    mask = mask.resize((w, h)).filter(ImageFilter.GaussianBlur(1.5))
    rgba = im.convert("RGBA")
    rgba.putalpha(mask)
    return rgba

JOBS = [
    ("T_UI_HanjiPanel", "1820x1024", False,
     "flat seamless korean hanji paper texture fill, warm ivory cream, subtle fibers and faint blotches, "
     "the paper surface fills the entire frame edge to edge, perfectly flat top-down view, no perspective, "
     "no sheet edges, no shadows, no objects, no text, 2D game UI background texture"),
    ("T_UI_HanjiSmall", "1024x1024", False,
     "flat aged korean paper texture fill, soft warm beige with darker mottled corners, surface fills the whole "
     "frame edge to edge, straight top-down flat view, no perspective, no paper edges visible, no shadows, "
     "no objects, no text, 2D game UI panel texture"),
    ("T_UI_Seal", "1024x1024", True,
     "red ink seal stamp imprint printed flat on white paper, east asian square name-seal impression with abstract "
     "carved strokes, distressed uneven stamping, completely flat 2D graphic mark, straight top-down view, "
     "no stamp object, no 3D, no shadows, pure white background, no text outside the mark"),
    ("T_UI_InkBlot", "1024x1024", True,
     "black sumi ink stain soaked into paper, flat matte calligraphy ink wash blot with soft feathered bleeding "
     "edges and a few speckles, completely flat 2D mark, top-down view, no gloss, no 3D liquid, no shadows, "
     "pure white background, no brush, no text"),
]

for name, size, keyed, prompt in JOBS:
    print("생성:", name, flush=True)
    url = generate(prompt, size)
    raw_path = os.path.join(RAW, name + "_v2.png")
    download(url, raw_path)
    im = Image.open(raw_path)
    if keyed:
        im = white_flood_alpha(im)
        bbox = im.getchannel("A").getbbox()
        if bbox:
            pad = 12
            bbox = (max(0, bbox[0] - pad), max(0, bbox[1] - pad),
                    min(im.width, bbox[2] + pad), min(im.height, bbox[3] + pad))
            im = im.crop(bbox)
    im.save(os.path.join(OUT, name + ".png"))
    print("저장:", name, im.size, flush=True)
print("재생성 완료", flush=True)
