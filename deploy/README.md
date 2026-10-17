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

## 0. Server alınan gün: qısa yol (təxminən 45 dəqiqə)

Aşağıdakı addımlar ilk dəfə sistemi serverdə qaldırmaq üçündür. Ətraflı izahı sonrakı bölmələrdədir.

1. **Serverə girin** (SSH): `ssh root@<serverin-IP>`. Provayderin verdiyi IP və parolu işlədin.
2. **Təhlükəsizlik divarı** (yalnız lazım olan portlar açıq olsun):
   ```bash
   apt update && apt install -y ufw git
   ufw allow 22/tcp && ufw allow 80/tcp && ufw allow 443/tcp && ufw allow 443/udp
   ufw --force enable
   ```
3. **Docker qurun** (rəsmi skript): `curl -fsSL https://get.docker.com | sh`. Sonra `systemctl enable docker` (server yenidən açılanda Docker özü qalxsın; konteynerlər `restart: unless-stopped` ilə özü qayıdır).
4. **Kodu yükləyin:**
   ```bash
   git clone https://github.com/ibadullah-18/RentoX.git
   cd RentoX/deploy
   cp .env.example .env
   ```
   Repo **gizlidirsə**, serverdə GitHub-a giriş lazımdır (Settings → Deploy keys və ya şəxsi token). Açıqdırsa, bu addım lazım deyil.
5. **`.env` faylını doldurun** (`nano .env`). Gizli açarlar üçün `openssl rand -base64 48`:
   - `POSTGRES_PASSWORD`, `JWT_SIGNING_KEY`, `OTP_HASHING_KEY` (üçü ayrı-ayrı, güclü, təsadüfi)
     (`openssl rand -base64 48` düzgün uzunluq verir. API bu açarlar çox qısa olsa **başlamır** və logda səbəbini yazır: JWT açarı ən azı 64, OTP açarı ən azı 32 simvol.)
   - `SUPER_ADMIN_PHONE=+994XXXXXXXXX` (sizin nömrəniz)
   - **Domen hazırdırsa:** `WEB_DOMAIN`, `API_DOMAIN` (DNS "A" qeydləri serverin IP-sinə baxmalıdır).
   - **Domen hələ yoxdursa (yalnız sınaq):** `WEB_DOMAIN=:80` və `API_DOMAIN=api.localhost`. Sayt `http://<serverin-IP>` ünvanında açılır, HTTPS və mobil tətbiq üçün isə domen lazımdır.
   - **ImageSharp açarı hələ gəlməyibsə (yalnız şəxsi sınaq günləri):** `BUILD_CONFIGURATION=Debug` sətrini açın. Açar gələndən sonra bu sətri silin, açarı `SIXLABORS_LICENSE_KEY`-ə yazın.
   - **SMS provayderi hələ yoxdursa:** `SMS_LOG_CODES=true`. Heç bir SMS getmir, giriş kodları API-nin loguna yazılır, yalnız siz oxuya bilirsiniz.
   - R2 yedək dəyərləri (bölmə 4). Bunları əvvəlcədən sınamısınız.
6. **Başladın:**
   ```bash
   docker compose build
   docker compose up -d
   docker compose ps          # hamısı "running"/"healthy", migrate "exited (0)"
   ```
   İlk build 5–15 dəqiqə çəkə bilər.
7. **İlk giriş və admin olmaq:**
   - Sayta girin, öz nömrənizlə qeydiyyatdan keçin. Kodu bu əmrlə oxuyun: `docker compose logs api | grep "OTP"` (axırıncı sətir).
   - Sonra `docker compose restart api` edin: sistem `SUPER_ADMIN_PHONE` nömrəsini SuperAdmin edir.
   - Admin panelinin ünvanı: `/RentoXAdminPanel/giris`.
8. **Yoxlayın:** `curl http://localhost:8080/health/ready` (API konteynerində) və ya brauzerdə sayt açılır, şəkil yüklənir, admin panelinə girilir.
9. **Yedəyi dərhal sınayın** (bölmə 4, 5-ci addım) və bərpanı bir dəfə yoxlayın.
10. Sınaq günləri bitəndən sonra: `SMS_LOG_CODES=false`, real SMS və ödənişi qoşun, ImageSharp açarını yazıb Release build edin, saytı açın.

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

## 4b. Şəkilləri R2-də saxlamaq (istəyə görə, tövsiyə olunur)

Standart halda şəkillər serverin diskindədir (`STORAGE_PROVIDER=Disk`) və gecə yedəyinə düşür. **R2 rejimində** şəkillər birbaşa Cloudflare R2-yə yazılır: server xarab olsa şəkillər itmir, disk dolmur.

1. Cloudflare-də **ayrıca** bucket yaradın: `rentox-images` (yedək bucket-i ilə qarışdırmayın).
2. Yalnız bu bucket-ə "Object Read & Write" icazəli **ayrıca token** yaradın.
3. `.env`-də doldurun:
   - `STORAGE_PROVIDER=R2`
   - `R2_IMAGES_ENDPOINT=https://<account id>.r2.cloudflarestorage.com`
   - `R2_IMAGES_BUCKET=rentox-images`
   - `R2_IMAGES_ACCESS_KEY_ID=`, `R2_IMAGES_SECRET_ACCESS_KEY=`
4. **Əvvəl** mövcud şəkilləri diskdən R2-yə köçürün (yeni serverdə şəkil yoxdursa bu addım lazım deyil). Açarlar yalnız bu əmrdə işlənir:

```bash
docker run --rm -v rentox_uploads:/data:ro   -e RCLONE_CONFIG_R2_TYPE=s3 -e RCLONE_CONFIG_R2_PROVIDER=Cloudflare   -e RCLONE_CONFIG_R2_ENDPOINT=https://<account id>.r2.cloudflarestorage.com   -e RCLONE_CONFIG_R2_ACCESS_KEY_ID=<açar> -e RCLONE_CONFIG_R2_SECRET_ACCESS_KEY=<gizli açar>   rclone/rclone copy /data r2:rentox-images --progress
```

   Fayl adları (`listing-images/....webp`) eynidir, ona görə bazada heç nə dəyişmir.
5. `docker compose up -d api worker`. Sayta girib şəkillərin göründüyünü və yeni şəkil yükləndiyini yoxlayın.
6. Köhnə şəkil qovluğunu (`rentox_uploads` volume) bir müddət saxlayın, hər şey işləyəndən sonra silin.

Elan silinəndə şəkli 30 gün sonra worker R2-dən də silir (səhvən silinəni qaytarmaq üçün gecikmə).
R2 rejimində **şəkillər gecə yedəyinə düşmür** (onlar artıq R2-dədir); baza yedəyi isə əvvəlki kimi gedir. Bir şeyi unutmayın: R2-dən səhvən silinən şəkli qaytarmaq olmur, ona görə 30 günlük gecikmə vacibdir.

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
