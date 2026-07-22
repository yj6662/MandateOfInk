# Recraft로 UI 스프라이트 생성 — 한지 패널·부적·엽전·먹 번짐·낙관
# 아이콘류는 흰 배경 생성 후 테두리 연결 백색 플러드필로 알파 변환.
import io, json, os, sys, urllib.request
from collections import deque
from PIL import Image

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
TOOLS = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(TOOLS, "generated", "ui_raw")
OUT = os.path.join(TOOLS, "..", "MandateOfInk", "Assets", "_Project", "Art", "UI")
os.makedirs(RAW, exist_ok=True)
os.makedirs(OUT, exist_ok=True)

KEY = [l.split("=", 1)[1].strip() for l in open(os.path.join(TOOLS, ".env"), encoding="utf-8-sig")
       if l.strip().startswith("RECRAFT_API_KEY=")][0]

JOBS = [
    # (파일명, 크기, 알파키잉 여부, 프롬프트)
    ("T_UI_HanjiPanel", "1820x1024", False,
     "traditional korean hanji mulberry paper texture, warm ivory cream color, subtle natural fibers and faint "
     "ink stains at edges, aged handmade paper, flat texture, no objects, no text"),
    ("T_UI_HanjiSmall", "1024x1024", False,
     "traditional korean handmade paper texture closeup, soft warm beige, visible fibers, slightly darker "
     "vignette corners, flat 2D texture, no objects, no text"),
    ("T_UI_Talisman", "1024x1024", True,
     "single korean taoist paper talisman bujeok, vertical yellow paper strip with red cinnabar abstract symbol "
     "painted on it, flat 2D game icon, slightly ragged paper edges, pure white background, no text, no other objects"),
    ("T_UI_Coin", "1024x1024", True,
     "single antique korean brass coin with square hole in center, joseon tongbo style, flat 2D game icon "
     "viewed straight on, aged bronze patina, pure white background, no text around it, no other objects"),
    ("T_UI_InkBlot", "1024x1024", True,
     "single expressive black ink splash blot mark, sumi ink splatter with organic edges and small droplets, "
     "flat 2D graphic element, pure white background, no brush, no objects, no text"),
    ("T_UI_Seal", "1024x1024", True,
     "single square red seal stamp imprint, east asian cinnabar name seal with abstract carved pattern, "
     "slightly distressed stamped texture, flat 2D graphic, pure white background, no text outside the seal, no objects"),
]


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
    # 테두리에 연결된 백색 영역만 투명화 — 아이콘 내부의 흰 하이라이트는 보존
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
    from PIL import ImageFilter
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


for name, size, keyed, prompt in JOBS:
    print("생성:", name, flush=True)
    url = generate(prompt, size)
    raw_path = os.path.join(RAW, name + ".png")
    download(url, raw_path)
    im = Image.open(raw_path)
    if keyed:
        im = white_flood_alpha(im)
        # 내용 바운드로 크롭 + 여백
        bbox = im.getchannel("A").getbbox()
        if bbox:
            pad = 12
            bbox = (max(0, bbox[0] - pad), max(0, bbox[1] - pad),
                    min(im.width, bbox[2] + pad), min(im.height, bbox[3] + pad))
            im = im.crop(bbox)
    im.save(os.path.join(OUT, name + ".png"))
    print("저장:", name, im.size, flush=True)

print("전체 완료", flush=True)
