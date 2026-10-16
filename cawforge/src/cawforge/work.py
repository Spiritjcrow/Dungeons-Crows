"""Real deterministic Dungeons & Crows procedural asset generation and validation."""
from collections import deque
import hashlib
import math

KIND = "dc.dungeon.map.v1"


def validate_params(p):
    if set(p) != {"seed", "width", "height", "rooms"} or any(type(v) is not int for v in p.values()):
        raise ValueError("invalid or non-integer dungeon parameters")
    w, h, n = p["width"], p["height"], p["rooms"]
    if not (16 <= w <= 64 and 12 <= h <= 48 and 3 <= n <= 12 and 0 <= p["seed"] < 2**32):
        raise ValueError("dungeon parameters outside policy")
    cols = min(4, max(2, math.ceil(math.sqrt(n * w / h))))
    rows = math.ceil(n / cols)
    if w // cols < 5 or h // rows < 5:
        raise ValueError("rooms do not fit grid")
    return cols, rows


def rand(seed, index, mod):
    digest = hashlib.sha256(f"cawforge/dc-map-v1/{seed}/{index}".encode()).digest()
    return int.from_bytes(digest[:8], "big") % mod


def generate(p):
    cols, rows = validate_params(p)
    w, h, n, seed = p["width"], p["height"], p["rooms"], p["seed"]
    board = [["#"] * w for _ in range(h)]
    centers = []
    chambers = []
    for i in range(n):
        c, r = i % cols, i // cols
        x0, y0 = c * (w // cols), r * (h // rows)
        cw = (w // cols) if c < cols - 1 else w - x0
        ch = (h // rows) if r < rows - 1 else h - y0
        rw, rh = min(7, cw - 2), min(6, ch - 2)
        x = x0 + 1 + rand(seed, i * 5 + 1, max(1, cw - rw - 1))
        y = y0 + 1 + rand(seed, i * 5 + 2, max(1, ch - rh - 1))
        for yy in range(y, y + rh):
            for xx in range(x, x + rw):
                board[yy][xx] = "."
        chambers.append({"x": x, "y": y, "width": rw, "height": rh})
        centers.append((x + rw // 2, y + rh // 2))

    def dig(x1, y1, x2, y2):
        if x1 == x2:
            for yy in range(min(y1, y2), max(y1, y2) + 1):
                board[yy][x1] = "."
        else:
            for xx in range(min(x1, x2), max(x1, x2) + 1):
                board[y1][xx] = "."

    for i in range(1, len(centers)):
        (ax, ay), (bx, by) = centers[i - 1], centers[i]
        if rand(seed, i * 5 + 3, 2) == 0:
            dig(ax, ay, bx, ay)
            dig(bx, ay, bx, by)
        else:
            dig(ax, ay, ax, by)
            dig(ax, by, bx, by)
    sx, sy = centers[0]
    ex, ey = centers[-1]
    board[sy][sx] = "S"
    board[ey][ex] = "E"
    return {"format": KIND, "seed": seed, "width": w, "height": h, "rooms": chambers,
            "tiles": ["".join(row) for row in board], "start": [sx, sy], "exit": [ex, ey]}


def quality_check(asset, p):
    if asset != generate(p):
        return False, "deterministic output mismatch"
    w, h = p["width"], p["height"]
    if len(asset["tiles"]) != h or any(len(row) != w for row in asset["tiles"]):
        return False, "wrong dimensions"
    seen = set()
    start = tuple(asset["start"])
    todo = deque([start])
    while todo:
        x, y = todo.popleft()
        if (x, y) in seen or x < 0 or x >= w or y < 0 or y >= h:
            continue
        if asset["tiles"][y][x] == "#":
            continue
        seen.add((x, y))
        todo.extend([(x-1,y),(x+1,y),(x,y-1),(x,y+1)])
    if tuple(asset["exit"]) not in seen:
        return False, "exit unreachable"
    for room in asset["rooms"]:
        if not (room["x"], room["y"]) in seen:
            return False, "room disconnected"
    return True, "deterministic, dimensionally valid, all rooms reachable"


def difficulty_units(p):
    validate_params(p)
    return math.ceil(p["width"] * p["height"] / 256) + math.ceil(p["rooms"] / 4)


def reward_micro(p, quality_bp=10000, utility_bp=10000):
    """25% of a CAWF per policy-defined unit, scaled by verified 0..10000 bp factors."""
    if not all(type(x) is int and 0 <= x <= 10000 for x in (quality_bp, utility_bp)):
        raise ValueError("bad quality/utility basis points")
    return min(5_000_000, 250_000 * difficulty_units(p) * quality_bp * utility_bp // 100_000_000)
