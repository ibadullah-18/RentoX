"""Makes 10 DEMO listings on the real site, signed in as YOU.

You type your own phone number and the SMS code (read from the server log) into this window: nobody else ever sees
them. Every listing is marked "[DEMO]" in its title and description, gets 3 drawn pictures (cars, houses ...) and is
sent for review; you approve them in the admin panel. Nothing is stored on this computer.

Run:  python make_demo_listings.py            (Python 3.9+, nothing to install)
      python make_demo_listings.py --api https://api.rentox.az
"""
import argparse
import json
import sys
import time
import urllib.error
import urllib.request
import uuid

import pics

# kind (a picture drawn by pics.py), root category name, title, description, price, rental period (2 day, 4 month)
DEMO = [
    ("car", "Avtomobillər", "Toyota Camry 2021, sərnişin avtomobili", "Təmiz, rahat sedan. Şəhər daxili və region səfərləri üçün.", 70, 2),
    ("car", "Avtomobillər", "Hyundai Tucson 2022, ailə üçün SUV", "Geniş salon, avtomat qutu. Yanacaq xərci kirayəçiyə aiddir.", 90, 2),
    ("bus", "Avtomobillər", "Mercedes Sprinter 18 yerlik mikroavtobus", "Qrup və tədbirlər üçün sürücü ilə.", 220, 2),
    ("moto", "Motosiklet və velosiped", "Yamaha skuter 150cc", "Şəhərdə rahat hərəkət üçün, dəbilqə daxildir.", 25, 2),
    ("bike", "Motosiklet və velosiped", "Dağ velosipedi, 21 sürət", "Gəzinti və turlar üçün, ölçü M.", 12, 2),
    ("flat", "Mənzillər", "2 otaqlı mənzil, mərkəzdə", "Təmirli, əşyalı, metroya 5 dəqiqə.", 650, 4),
    ("flat", "Mənzillər", "3 otaqlı mənzil, yeni tikili", "Panoramik pəncərələr, bağlı həyət, dayanacaq.", 900, 4),
    ("villa", "Ev və villalar", "Hovuzlu villa, tətil üçün", "4 yataq otağı, mətbəx, hovuz və mangal sahəsi.", 350, 2),
    ("dacha", "Ev və villalar", "Bağ evi, sakit yerdə", "Meyvə bağı, mətbəx, 2 yataq otağı.", 120, 2),
    ("office", "Ofislər", "Ofis, 60 kv.m, iş mərkəzində", "Qaynar internet, iclas otağı, qəbul.", 800, 4),
]

# Used only when the site has no categories yet (you are asked first): Azerbaijani, slug, Russian, English.
ROOTS = [
    ("Avtomobillər", "avtomobiller", "Автомобили", "Cars"),
    ("Motosiklet və velosiped", "motosiklet-velosiped", "Мотоциклы и велосипеды", "Motorbikes and bicycles"),
    ("Mənzillər", "menziller", "Квартиры", "Apartments"),
    ("Ev və villalar", "ev-villalar", "Дома и виллы", "Houses and villas"),
    ("Ofislər", "ofisler", "Офисы", "Offices"),
]


class Api:
    def __init__(self, base):
        self.base = base.rstrip("/")
        self.token = None

    def call(self, method, path, body=None, multipart=None, retry=True):
        headers = {"Accept": "application/json", "User-Agent": "rentox-demo-script"}
        data = None
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        if multipart:
            boundary = uuid.uuid4().hex
            name, filename, content, ctype = multipart
            data = (
                f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"; filename="{filename}"\r\n'
                f"Content-Type: {ctype}\r\n\r\n"
            ).encode() + content + f"\r\n--{boundary}--\r\n".encode()
            headers["Content-Type"] = f"multipart/form-data; boundary={boundary}"
        elif body is not None:
            data = json.dumps(body).encode()
            headers["Content-Type"] = "application/json"
        elif method == "POST":
            data = b""
        req = urllib.request.Request(self.base + path, data=data, method=method, headers=headers)
        try:
            with urllib.request.urlopen(req, timeout=60) as res:
                raw = res.read()
                return res.status, (json.loads(raw) if raw else None)
        except urllib.error.HTTPError as e:
            raw = e.read()
            if e.code == 429 and retry:
                wait = int(e.headers.get("Retry-After", "20"))
                print(f"   ... çox tez-tez: {wait} san. gözləyirəm")
                time.sleep(min(wait, 120))
                return self.call(method, path, body, multipart, retry=False)
            try:
                return e.code, json.loads(raw)
            except ValueError:
                return e.code, {"detail": raw.decode("utf-8", "replace")[:300]}


def fail(message):
    print("\nXƏTA:", message)
    sys.exit(1)


def sign_in(api):
    raw = input("1) Telefon nömrən (məs. 055 924 48 40). Kodu yox, NÖMRƏNİ yaz: ")
    digits = "".join(ch for ch in raw if ch.isdigit())
    if digits.startswith("994"):
        digits = digits[3:]
    digits = digits.lstrip("0")
    if len(digits) != 9:
        fail(f"Nömrə 9 rəqəm olmalıdır (məs. 55 924 48 40), sən {len(digits)} rəqəm yazdın. Kodu yox, telefon nömrəsini yaz.")
    phone = "+994" + digits
    status, challenge = api.call("POST", "/api/auth/login/otp", {"phoneNumber": phone})
    if status >= 300:
        fail(f"Kod göndərilmədi ({status}): {challenge}")
    print('Kod istənildi. Serverdən oxu:  docker logs rentox-api-1 2>&1 | grep "DEVELOPMENT OTP"')
    code = "".join(ch for ch in input("2) Serverdən oxuduğun 6 rəqəmli KOD: ") if ch.isdigit())
    status, tokens = api.call("POST", "/api/auth/login/complete", {"challengeId": challenge["challengeId"], "code": code})
    if status >= 300:
        fail(f"Giriş alınmadı ({status}): {tokens}")
    api.token = tokens["accessToken"]
    print("Daxil oldun.\n")


def flatten(tree):
    for c in tree:
        yield c
        yield from flatten(c.get("children") or [])


def ensure_categories(api):
    status, tree = api.call("GET", "/api/categories?language=az")
    if status >= 300:
        fail(f"Kateqoriyalar oxunmadı ({status})")
    if tree:
        return tree
    answer = input("Saytda hələ kateqoriya yoxdur. Demo üçün 5 əsas kateqoriya yaradım? (b/x): ").strip().lower()
    if answer not in ("b", "bəli", "beli", "y", "yes"):
        fail("Kateqoriyasız demo elan yaratmaq olmur. Əvvəl admin paneldə kateqoriya yarat.")
    for i, (az, slug, ru, en) in enumerate(ROOTS, 1):
        status, res = api.call("POST", "/api/admin/categories", {
            "parentId": None, "slug": slug, "iconUrl": None, "displayOrder": i,
            "translations": [{"language": 1, "name": az}, {"language": 2, "name": ru}, {"language": 3, "name": en}],
        })
        if status >= 300:
            fail(f"Kateqoriya yaradılmadı ({status}): {res}. (Bu hesab SuperAdmin və ya kataloq icazəli Admin olmalıdır.)")
        print("   kateqoriya:", az)
    status, tree = api.call("GET", "/api/categories?language=az")
    return tree


def needs_fields(api, category_id):
    status, fields = api.call("GET", f"/api/categories/{category_id}/fields?language=az")
    return status < 300 and any(f.get("isRequired") for f in (fields or []))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--api", default="https://api.rentox.az")
    args = parser.parse_args()
    api = Api(args.api)

    sign_in(api)
    tree = ensure_categories(api)
    by_name = {c["name"]: c for c in flatten(tree)}
    leaves = [c for c in flatten(tree) if not c.get("children")]

    status, cities = api.call("GET", "/api/locations/cities?language=az")
    if status >= 300 or not cities:
        fail("Şəhərlər oxunmadı.")
    city = next((c for c in cities if c["name"] == "Bakı"), cities[0])
    district = None
    if city.get("hasDistricts"):
        status, districts = api.call("GET", f"/api/locations/cities/{city['id']}/districts?language=az")
        if status < 300 and districts:
            district = districts[0]["id"]

    made, paid_needed = [], []
    for n, (kind, root, title, text, price, unit) in enumerate(DEMO, 1):
        category = by_name.get(root) or leaves[(n - 1) % len(leaves)]
        if needs_fields(api, category["id"]):
            print(f"{n}. KEÇİLDİ: «{category['name']}» kateqoriyasında məcburi sahələr var (script onları doldurmur).")
            continue
        body = {
            "categoryId": category["id"], "title": f"[DEMO] {title}",
            "description": f"{text} Bu elan demo/test məqsədli yaradılıb, real təklif deyil.",
            "price": price, "currency": "AZN", "rentalPeriodUnit": unit, "fields": [],
            "cityId": city["id"], "districtId": district,
        }
        status, res = api.call("POST", "/api/listings", body)
        if status >= 300:
            print(f"{n}. Elan yaradılmadı ({status}): {res}")
            continue
        listing = res["id"]
        for view in range(3):
            png = pics.make(kind, 100 + n, view)
            status, up = api.call("POST", f"/api/listings/{listing}/images",
                                  multipart=("file", f"demo-{n}-{view}.png", png, "image/png"))
            if status >= 300:
                print(f"   şəkil {view + 1} yüklənmədi ({status}): {up}")
            time.sleep(0.5)
        status, sub = api.call("POST", f"/api/listings/{listing}/submit")
        if status >= 300:
            print(f"{n}. Göndərilmədi ({status}): {sub}")
            continue
        state = sub.get("status")
        if state == 8:  # payment required
            status, paid = api.call("POST", f"/api/listings/{listing}/pay-and-activate")
            if status >= 300:
                paid_needed.append(title)
                print(f"{n}. «{title}» ödəniş gözləyir (pul kisəsində balans yoxdur).")
                continue
            state = "ödənildi"
        made.append(title)
        print(f"{n}. Hazırdır: {title}  (status: {state})")
        time.sleep(1)

    print(f"\n{len(made)} demo elan göndərildi. Admin paneldə «Elanlar» bölməsindən qəbul et.")
    if paid_needed:
        print(f"{len(paid_needed)} elan aktivləşmə haqqı gözləyir: admin paneldən pul kisənə balans əlavə et, "
              "sonra saytda «Elanlarım»-dan aktivləşdir.")


if __name__ == "__main__":
    main()
