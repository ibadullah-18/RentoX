"""Draws simple but varied pictures (cars, houses, interiors ...) as PNG with the standard library only."""
import random
import struct
import zlib

W, H = 800, 600


class Canvas:
    def __init__(self, top, bottom):
        self.rows = []
        for y in range(H):
            t = y / (H - 1)
            c = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
            self.rows.append(bytearray(bytes(c) * W))

    def rect(self, x0, y0, x1, y1, c):
        x0, x1 = max(0, int(x0)), min(W, int(x1))
        if x1 <= x0:
            return
        seg = bytes(c) * (x1 - x0)
        for y in range(max(0, int(y0)), min(H, int(y1))):
            self.rows[y][x0 * 3:x1 * 3] = seg

    def circle(self, cx, cy, r, c):
        for y in range(max(0, int(cy - r)), min(H, int(cy + r) + 1)):
            dy = y - cy
            d = r * r - dy * dy
            if d < 0:
                continue
            dx = int(d ** 0.5)
            x0, x1 = max(0, int(cx - dx)), min(W, int(cx + dx) + 1)
            if x1 > x0:
                self.rows[y][x0 * 3:x1 * 3] = bytes(c) * (x1 - x0)

    def ring(self, cx, cy, r, w, c):
        for y in range(max(0, int(cy - r)), min(H, int(cy + r) + 1)):
            dy = y - cy
            for rr, col in ((r, c),):
                d = rr * rr - dy * dy
                if d < 0:
                    continue
                dx = int(d ** 0.5)
                d2 = (r - w) ** 2 - dy * dy
                if d2 <= 0:
                    x0, x1 = max(0, int(cx - dx)), min(W, int(cx + dx) + 1)
                    self.rows[y][x0 * 3:x1 * 3] = bytes(col) * max(0, x1 - x0)
                else:
                    ix = int(d2 ** 0.5)
                    for a, b in ((cx - dx, cx - ix), (cx + ix, cx + dx)):
                        a, b = max(0, int(a)), min(W, int(b) + 1)
                        if b > a:
                            self.rows[y][a * 3:b * 3] = bytes(col) * (b - a)

    def tri(self, p1, p2, p3, c):
        ys = [p1[1], p2[1], p3[1]]
        for y in range(max(0, int(min(ys))), min(H, int(max(ys)) + 1)):
            xs = []
            for a, b in ((p1, p2), (p2, p3), (p3, p1)):
                if (a[1] <= y < b[1]) or (b[1] <= y < a[1]):
                    xs.append(a[0] + (y - a[1]) * (b[0] - a[0]) / (b[1] - a[1]))
            if len(xs) >= 2:
                x0, x1 = max(0, int(min(xs))), min(W, int(max(xs)) + 1)
                if x1 > x0:
                    self.rows[y][x0 * 3:x1 * 3] = bytes(c) * (x1 - x0)

    def quad(self, p1, p2, p3, p4, c):
        self.tri(p1, p2, p3, c)
        self.tri(p1, p3, p4, c)

    def png(self):
        raw = b"".join(b"\x00" + bytes(r) for r in self.rows)

        def chunk(tag, data):
            body = tag + data
            return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

        return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", W, H, 8, 2, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(raw, 6)) + chunk(b"IEND", b""))


SKIES = [((120, 180, 240), (210, 232, 250)), ((250, 160, 110), (253, 215, 160)), ((150, 160, 175), (205, 210, 218)),
         ((70, 120, 200), (170, 205, 240)), ((255, 190, 140), (255, 235, 200))]
GROUNDS = [(96, 140, 84), (120, 120, 124), (170, 150, 110), (84, 90, 96)]
CAR_COLORS = [(200, 30, 40), (30, 60, 140), (240, 240, 244), (30, 30, 34), (130, 134, 140), (20, 110, 80), (230, 150, 20),
              (110, 40, 130)]
WALLS = [(244, 232, 210), (232, 216, 196), (250, 250, 246), (210, 190, 160), (226, 200, 190), (200, 214, 226)]
ROOFS = [(150, 60, 50), (90, 80, 78), (60, 80, 110), (120, 80, 50)]
GLASS = (170, 205, 230)


def darker(c, k=0.7):
    return tuple(int(v * k) for v in c)


def scene(rng, light=None):
    top, bottom = rng.choice(SKIES)
    cv = Canvas(top, bottom)
    ground = rng.choice(GROUNDS)
    horizon = int(H * rng.uniform(0.60, 0.68))
    cv.rect(0, horizon, W, H, ground)
    for _ in range(rng.randint(2, 4)):  # clouds
        x, y = rng.randint(40, W - 160), rng.randint(30, 170)
        for dx, dy, r in ((0, 0, 30), (34, 6, 26), (-32, 8, 24), (14, -14, 24)):
            cv.circle(x + dx, y + dy, r, (252, 252, 254))
    return cv, horizon


def tree(cv, x, base, s, rng):
    cv.rect(x - 5 * s, base - 50 * s, x + 5 * s, base, (110, 78, 50))
    for dx, dy, r in ((0, -80, 38), (-26, -58, 30), (26, -58, 30)):
        cv.circle(x + dx * s, base + dy * s, r * s, (50 + rng.randint(0, 30), 120 + rng.randint(0, 40), 60))


def wheel(cv, x, y, r):
    cv.circle(x, y, r, (24, 24, 26))
    cv.circle(x, y, r * 0.55, (190, 192, 198))
    cv.circle(x, y, r * 0.2, (70, 70, 74))


def car(cv, horizon, rng, color, x=150, s=1.0, bus=False, suv=True):
    base = horizon + 150
    cv.rect(0, base + 10, W, base + 14, (60, 60, 64))
    length = (470 if not bus else 560) * s
    body_h = (70 if suv else 56) * s
    x1 = x + length
    # shadow
    cv.rect(x - 10, base - 4, x1 + 10, base + 6, darker(GROUNDS[3], 0.8))
    # lower body
    cv.rect(x, base - body_h - 18 * s, x1, base - 18 * s, color)
    cv.rect(x + 8 * s, base - 22 * s, x1 - 8 * s, base - 10 * s, darker(color))
    if bus:
        cv.rect(x, base - body_h - 18 * s - 70 * s, x1, base - body_h - 18 * s, color)
        wx = x + 20 * s
        while wx + 70 * s < x1 - 20 * s:
            cv.rect(wx, base - body_h - 18 * s - 58 * s, wx + 62 * s, base - body_h - 18 * s - 10 * s, GLASS)
            wx += 78 * s
    else:
        top_y = base - body_h - 18 * s
        cabin_h = 60 * s
        cv.quad((x + 110 * s, top_y), (x + 160 * s, top_y - cabin_h), (x1 - 120 * s, top_y - cabin_h), (x1 - 60 * s, top_y), color)
        cv.quad((x + 128 * s, top_y - 4), (x + 168 * s, top_y - cabin_h + 8), (x + 250 * s, top_y - cabin_h + 8), (x + 250 * s, top_y - 4), GLASS)
        cv.quad((x + 262 * s, top_y - 4), (x + 262 * s, top_y - cabin_h + 8), (x1 - 128 * s, top_y - cabin_h + 8), (x1 - 78 * s, top_y - 4), GLASS)
    cv.rect(x1 - 10 * s, base - body_h - 4 * s, x1 + 2 * s, base - body_h + 14 * s, (255, 240, 170))
    cv.rect(x - 2 * s, base - body_h - 4 * s, x + 10 * s, base - body_h + 14 * s, (210, 40, 40))
    wheel(cv, x + 100 * s, base - 14 * s, 38 * s)
    wheel(cv, x1 - 100 * s, base - 14 * s, 38 * s)


def moto(cv, horizon, rng, color, x=210, s=1.0, scooter=False):
    base = horizon + 150
    cv.rect(0, base + 14, W, base + 18, (60, 60, 64))
    r = 58 * s
    w1, w2 = x + 90 * s, x + 340 * s
    cv.ring(w1, base - r + 14, r, 12 * s, (24, 24, 26))
    cv.ring(w2, base - r + 14, r, 12 * s, (24, 24, 26))
    cv.circle(w1, base - r + 14, 10 * s, (150, 150, 156))
    cv.circle(w2, base - r + 14, 10 * s, (150, 150, 156))
    cv.quad((w1 + 30 * s, base - 110 * s), (w2 - 40 * s, base - 120 * s), (w2 - 70 * s, base - 70 * s), (w1 + 70 * s, base - 62 * s), color)
    cv.rect(w1 + 100 * s, base - 134 * s, w2 - 60 * s, base - 116 * s, (30, 30, 34))
    cv.quad((w1 - 6 * s, base - 150 * s), (w1 + 8 * s, base - 160 * s), (w1 + 40 * s, base - 70 * s), (w1 + 22 * s, base - 66 * s), (120, 122, 128))
    cv.rect(w1 - 20 * s, base - 164 * s, w1 + 24 * s, base - 154 * s, (40, 40, 44))
    cv.circle(w1 - 8 * s, base - 136 * s, 14 * s, (255, 240, 170))


def bicycle(cv, horizon, rng, color, x=220, s=1.0):
    base = horizon + 150
    r = 70 * s
    w1, w2 = x + 90 * s, x + 380 * s
    cv.ring(w1, base - r + 10, r, 6 * s, (30, 30, 34))
    cv.ring(w2, base - r + 10, r, 6 * s, (30, 30, 34))
    y = base - r + 10
    cx = (w1 + w2) / 2

    def line(a, b, t=8 * s, c=color):
        n = int(max(abs(b[0] - a[0]), abs(b[1] - a[1])))
        for i in range(max(n, 1)):
            px = a[0] + (b[0] - a[0]) * i / max(n, 1)
            py = a[1] + (b[1] - a[1]) * i / max(n, 1)
            cv.circle(px, py, t / 2, c)

    line((w1, y), (cx - 10 * s, y - 70 * s))
    line((cx - 10 * s, y - 70 * s), (w2 - 40 * s, y - 70 * s))
    line((w2 - 40 * s, y - 70 * s), (w2, y))
    line((cx - 10 * s, y - 70 * s), (cx + 20 * s, y))
    line((cx + 20 * s, y), (w1, y))
    line((w2 - 40 * s, y - 70 * s), (w2 - 50 * s, y - 100 * s), 6 * s, (40, 40, 44))
    cv.rect(w2 - 76 * s, y - 106 * s, w2 - 34 * s, y - 98 * s, (40, 40, 44))
    cv.rect(cx - 50 * s, y - 84 * s, cx - 10 * s, y - 74 * s, (40, 40, 44))


def house(cv, horizon, rng, wall, roof, x=130, s=1.0, floors=2, pool=False, small=False):
    base = horizon + 120
    w = (440 if not small else 330) * s
    fh = 120 * s
    top = base - floors * fh
    cv.rect(x, top, x + w, base, wall)
    cv.tri((x - 24 * s, top), (x + w / 2, top - 110 * s), (x + w + 24 * s, top), roof)
    for f in range(floors):
        y0 = base - (f + 1) * fh + 30 * s
        n = 3 if not small else 2
        for i in range(n):
            wx = x + 40 * s + i * (w - 80 * s) / n
            if f == 0 and i == n // 2:
                cv.rect(wx + 10 * s, base - 100 * s, wx + 70 * s, base, (110, 70, 44))
                cv.circle(wx + 60 * s, base - 50 * s, 4 * s, (230, 200, 120))
            else:
                cv.rect(wx, y0, wx + 64 * s, y0 + 66 * s, (90, 80, 76))
                cv.rect(wx + 4 * s, y0 + 4 * s, wx + 60 * s, y0 + 62 * s, GLASS)
                cv.rect(wx + 30 * s, y0 + 4 * s, wx + 34 * s, y0 + 62 * s, (90, 80, 76))
    cv.rect(x - 10 * s, base, x + w + 10 * s, base + 14 * s, (150, 150, 152))
    if pool:
        cv.rect(x + w + 40 * s, base + 20 * s, x + w + 260 * s, base + 90 * s, (60, 160, 215))
        cv.rect(x + w + 40 * s, base + 20 * s, x + w + 260 * s, base + 26 * s, (220, 235, 245))
    tree(cv, x - 70 * s, base, 1.1 * s, rng)
    if not pool:
        tree(cv, x + w + 80 * s, base, 0.9 * s, rng)


def tower(cv, horizon, rng, wall, x=260, floors=7, s=1.0):
    base = horizon + 140
    w = 290 * s
    fh = 52 * s
    top = base - floors * fh
    cv.rect(x, top, x + w, base, wall)
    cv.rect(x, top - 12, x + w, top, darker(wall, 0.8))
    for f in range(floors):
        for i in range(5):
            wx = x + 16 * s + i * 54 * s
            cv.rect(wx, top + f * fh + 12 * s, wx + 38 * s, top + f * fh + 40 * s, GLASS if (f + i) % 5 else (250, 230, 150))
    cv.rect(x + w / 2 - 30 * s, base - 54 * s, x + w / 2 + 30 * s, base, (80, 90, 110))
    cv.rect(x - 80 * s, base, x + w + 80 * s, base + 10, (150, 150, 152))
    tree(cv, x - 60 * s, base, 0.9, rng)
    tree(cv, x + w + 70 * s, base, 0.9, rng)


def room(rng, kind):
    wall = rng.choice(WALLS)
    cv = Canvas(wall, darker(wall, 0.93))
    floor_c = rng.choice([(176, 138, 98), (150, 118, 88), (200, 188, 170), (120, 120, 124)])
    cv.rect(0, 430, W, H, floor_c)
    cv.rect(0, 424, W, 432, darker(wall, 0.8))
    # window
    cv.rect(520, 120, 740, 330, (230, 230, 232))
    cv.rect(530, 130, 730, 320, rng.choice([(150, 200, 235), (220, 190, 150), (170, 190, 215)]))
    cv.rect(626, 130, 634, 320, (230, 230, 232))
    c = rng.choice([(120, 110, 150), (60, 90, 130), (150, 80, 70), (90, 120, 100), (200, 170, 120)])
    if kind == "living":
        cv.rect(60, 330, 430, 440, c)
        cv.rect(60, 290, 430, 340, darker(c, 0.85))
        cv.rect(40, 310, 80, 440, darker(c, 0.8))
        cv.rect(410, 310, 450, 440, darker(c, 0.8))
        cv.rect(180, 470, 420, 530, (110, 76, 50))
        cv.rect(480, 360, 560, 440, (90, 70, 56))
    elif kind == "bedroom":
        cv.rect(120, 340, 480, 450, (250, 250, 250))
        cv.rect(120, 380, 480, 450, c)
        cv.rect(100, 290, 130, 460, (110, 76, 50))
        cv.rect(150, 340, 250, 370, (240, 240, 244))
        cv.rect(560, 380, 700, 460, (110, 76, 50))
    elif kind == "kitchen":
        cv.rect(0, 360, W, 440, (226, 226, 228))
        cv.rect(0, 440, W, 520, c)
        cv.rect(40, 130, 480, 230, darker(c, 0.9))
        cv.rect(120, 300, 230, 360, (60, 60, 64))
        cv.rect(300, 330, 420, 360, (180, 190, 200))
    elif kind == "office":
        cv.rect(120, 340, 520, 380, (150, 110, 80))
        cv.rect(150, 380, 170, 460, (120, 88, 62))
        cv.rect(470, 380, 490, 460, (120, 88, 62))
        cv.rect(250, 270, 390, 340, (40, 44, 52))
        cv.rect(310, 340, 330, 350, (60, 60, 64))
        cv.rect(560, 350, 700, 470, (60, 70, 90))
    else:  # bath / hall
        cv.rect(100, 300, 380, 450, (240, 242, 244))
        cv.rect(480, 330, 600, 450, (200, 205, 212))
        cv.circle(540, 300, 40, (180, 200, 215))
    return cv


def cockpit(rng):
    cv = Canvas((20, 22, 28), (50, 52, 58))
    cv.rect(0, 360, W, H, (40, 42, 46))
    cv.quad((0, 300), (W, 300), (W, 380), (0, 380), (28, 28, 32))
    cv.circle(240, 430, 120, (20, 20, 22))
    cv.circle(240, 430, 88, (50, 50, 56))
    cv.circle(240, 430, 24, (130, 130, 138))
    cv.rect(430, 330, 700, 470, (24, 30, 44))
    cv.rect(440, 340, 690, 460, (44, 90, 140))
    for i in range(3):
        cv.circle(120 + i * 70, 320, 24, (200, 200, 210))
    cv.rect(0, 520, W, H, (36, 36, 40))
    return cv


def make(kind, seed, view):
    """kind: car, bus, moto, bike, flat, villa, dacha, office. view: 0..4"""
    rng = random.Random(seed)
    color = rng.choice(CAR_COLORS)
    wall, roof = rng.choice(WALLS), rng.choice(ROOFS)
    rng = random.Random(seed * 31 + view * 7 + 1)
    interiors = ["living", "bedroom", "kitchen", "bath"]
    if kind in ("car", "bus") and view == 3:
        return cockpit(rng).png()
    if kind == "flat" and view >= 1:
        return room(rng, interiors[(view - 1) % 4]).png()
    if kind in ("villa", "dacha") and view >= 2:
        return room(rng, interiors[(view - 2) % 4]).png()
    if kind == "office" and view >= 1:
        return room(rng, "office" if view % 2 else "living").png()
    cv, horizon = scene(rng)
    s = rng.choice([1.0, 0.9, 1.1]) if view else 1.0
    x = rng.randint(80, 180)
    if kind == "car":
        car(cv, horizon, rng, color, x=x, s=s)
    elif kind == "bus":
        car(cv, horizon, rng, color, x=x - 40, s=s * 0.9, bus=True)
    elif kind == "moto":
        moto(cv, horizon, rng, color, x=x + 60, s=s)
    elif kind == "bike":
        bicycle(cv, horizon, rng, color, x=x + 40, s=s)
    elif kind == "flat":
        tower(cv, horizon, rng, wall, x=x + 100, floors=rng.randint(5, 9))
    elif kind == "villa":
        house(cv, horizon, rng, wall, roof, x=x - 20, s=s, floors=2, pool=True)
    elif kind == "dacha":
        house(cv, horizon, rng, wall, roof, x=x + 40, s=s, floors=1, small=True)
    else:
        tower(cv, horizon, rng, (180, 190, 205), x=x + 100, floors=rng.randint(4, 8))
    return cv.png()


if __name__ == "__main__":
    import sys
    for i, (k, v) in enumerate([("car", 0), ("car", 3), ("bus", 0), ("moto", 0), ("bike", 0), ("flat", 0), ("flat", 1),
                                ("villa", 0), ("villa", 2), ("dacha", 0), ("office", 0)]):
        open(f"{sys.argv[1]}/t{i}_{k}{v}.png", "wb").write(make(k, 7 + i, v))
