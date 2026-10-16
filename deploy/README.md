# RentoX: quraşdırma, yedəkləmə, bərpa

Bu qovluq bütün sistemi (sayt, API, işçi proqram, baza, HTTPS, gecə yedəyi) **bir serverdə** Docker ilə qaldırır.

| Xidmət | Nə edir |
|---|---|
| `caddy` | Giriş qapısı: HTTPS sertifikatlarını özü alır və yeniləyir, iki domeni yönləndirir, təhlükəsizlik başlıqlarını qoyur |
| `web` | Sayt |
| `api` | API (mobil tətbiq birbaşa buna qoşulur, `API_DOMAIN` üzərindən) |
| `worker` | Push bildirişlər və elan təmizliyi |
| `migrate` | Hər başlanğıcda bazanı yeniləyir və dayanır |
| `postgres`, `redis` | Baza və canlı bağlantılar |
| `backup` | Hər gecə bazanı və şəkilləri şifrələyib Cloudflare R2-yə göndərir |

## 1. Serverə hazırlıq

1. Ubuntu Server 24.04 qurun. Serverə SSH ilə girin (root və ya sudo hüququ lazımdır).
2. Docker qurun: <https://docs.docker.com/engine/install/ubuntu/> ("apt repository" yolu).
3. Firewall-da yalnız **22, 80, 443** (və 443/udp) açıq olsun. Baza və Redis çölə açılmır, heç bir `ports:` yoxdur.
4. **Domen:** iki ad alın, məsələn `rentox.az` (sayt) və `api.rentox.az` (mobil üçün). Hər ikisinin DNS "A" qeydi serverin IP ünvanına baxmalıdır. Caddy sertifikatı yalnız DNS düzgün olanda ala bilir.

## 2. Kodu və ayarları qoyun

```bash
git clone https://github.com/ibadullah-18/RentoX.git
cd RentoX/deploy
cp .env.example .env
nano .env          # hər sətri doldurun
```

`.env` içindəkilər **gizlidir**: GitHub-a qoymayın, heç kimə göndərməyin, bir nüsxəsini parol menecerində saxlayın.
Gizli açarlar üçün: `openssl rand -base64 48`.

Doldurmalı olduqlarınız:

- `WEB_DOMAIN`, `API_DOMAIN`
- `POSTGRES_PASSWORD`, `JWT_SIGNING_KEY`, `OTP_HASHING_KEY`, `SUPER_ADMIN_PHONE`
- `SIXLABORS_LICENSE_KEY`: şəkil kitabxanasının (ImageSharp) lisenziya açarı. **Bunsuz Release build alınmır.** Pulsuz "Community" açarını <https://sixlabors.com/pricing/> səhifəsindən müraciətlə alırsınız (illik gəliri 1 milyon dollardan az olan şirkətlər üçün ödənişli lisenziya tələb olunmur, amma açar yenə lazımdır). Açar yalnız build zamanı işlənir, hazır imajların içində yoxdur.

Push bildirişlər üçün Firebase servis hesabı faylını `deploy/secrets/firebase.json` kimi qoyun və `.env`-də `PUSH_ENABLED=true` yazın. Bu fayl da gizlidir (`deploy/secrets/` Git-ə düşmür).

## 3. İlk başlatma

```bash
docker compose build
docker compose up -d
docker compose ps          # hamısı "running" / "healthy" olmalıdır, migrate "exited (0)"
curl https://api.rentox.az/health/ready     # {"status":"Ready",...}
```

Yeniləmə (yeni kod gələndə):

```bash
git pull
docker compose build
docker compose up -d       # migrasiya avtomatik işləyir
```

Qısa dayanma (bir neçə saniyə) olur. Yeniləməni az yükün olduğu saatda edin.

## 4. Yedəkləmə (Cloudflare R2)

1. Cloudflare-də R2-ni aktivləşdirin, `rentox-backup` adlı bucket yaradın.
2. R2 → "Manage API tokens" → **yalnız bu bucket-ə** "Object Read & Write" icazəli token yaradın.
3. `.env` içinə yazın:
   - `RESTIC_REPOSITORY=s3:https://<hesab-id>.r2.cloudflarestorage.com/rentox-backup`
   - `RESTIC_PASSWORD=` uzun təsadüfi parol. **Bu parolsuz yedəklər açılmır. Onu serverdən kənarda (parol meneceri və kağız) saxlayın.**
   - `R2_ACCESS_KEY_ID`, `R2_SECRET_ACCESS_KEY` (tokendən)
4. `docker compose up -d backup`
5. **Dərhal əl ilə bir dəfə sınayın:**

```bash
docker compose exec backup /scripts-default/backup.sh
docker compose exec backup restic snapshots       # iki növ görməlisiniz: tag db və tag files
```

Bundan sonra hər gecə `BACKUP_HOUR` (standart 03:00, Bakı vaxtı) avtomatik işləyir.
Saxlama qaydası: baza nüsxələrindən 30 gündəlik, 8 həftəlik, 12 aylıq; şəkillər artımlı göndərilir (yalnız yeniləri).
`BACKUP_HEALTHCHECK_URL` doldurun (məsələn <https://healthchecks.io> pulsuz hesabı): yedək dayansa e-poçt gəlir.

## 5. Bərpa (server xarab olub, ya köçürürsünüz)

Yeni serverdə 1–2-ci addımları edin. `.env`-də **əvvəlki ilə eyni** `JWT_SIGNING_KEY`, `OTP_HASHING_KEY`, `POSTGRES_PASSWORD` və R2/restic dəyərlərini yazın (yoxsa istifadəçilər çıxış edər və ya yedək açılmaz). Sonra:

```bash
docker compose up -d postgres redis backup
docker compose exec backup restic snapshots                      # nə olduğunu görün
docker compose exec backup /scripts-default/restore.sh db        # bazanı qaytarır (son nüsxə)
docker compose exec backup /scripts-default/restore.sh files     # şəkilləri qaytarır
docker compose up -d                                             # hamısını qaldırın
```

Köhnə nüsxə lazımdırsa: `restore.sh db <snapshot-id>`.

**Köçürmə (serverdən serverə) üçün qayda:** köhnə serveri dərhal silməyin. Əvvəlcə yeni serverdə bərpanı yoxlayın (sayt açılır, şəkillər görünür, admin girişi işləyir), sonra DNS-i yeni IP-yə yönəldin, köhnəni bir həftə saxlayın.

> **Bərpanı əvvəlcədən sınayın.** Heç vaxt sınanmamış yedək, yedək sayılmaz. Birinci yedəkdən sonra boş bir test serverində (və ya kompüterdə) 5-ci bölməni bir dəfə işlədin.

## 6. Gündəlik baxış

```bash
docker compose ps                 # vəziyyət
docker compose logs -f api        # API logları (web, worker, caddy də eyni)
docker compose logs --since 1h web | grep '"level":"error"'   # saytın səhv sətirləri
df -h                             # disk dolmasın
```

Xəta izləmə (istəyə görə): <https://sentry.io> və ya özünüz qura biləcəyiniz GlitchTip-də layihə açıb DSN-i `.env`-də `SENTRY_DSN` kimi yazın, `docker compose up -d api`.

## 7. Hələ görülməyənlər (buraxılışdan əvvəl)

- **SMS sağlayıcısı:** giriş kodu SMS ilə göndərilir. Kodda hazırda yalnız sınaq göndəricisi var, ona görə real serverdə **heç kim giriş edə bilməz**, bir SMS sağlayıcısı qoşulmalıdır.
- **Real ödəniş (bank):** balans artırma yalnız sınaq rejimində var.
- Mobil tətbiqin Android/iOS buraxılış build-ləri.
