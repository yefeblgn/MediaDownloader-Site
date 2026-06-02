"""
BLGNTube — tam sunucu kurulum betiği
Hedef: Ubuntu 22.04 | 191.44.68.43 | tube.yefeblgn.net
"""
import io, os, time, zipfile
import paramiko

# ── Ayarlar ────────────────────────────────────────────────────────────────
HOST        = "191.44.68.43"
USER        = "root"
PASSWD      = "T_t7Wd!Ver"
DOMAIN      = "tube.yefeblgn.net"
APP_DIR     = "/var/www/blgntube"
PUBLISH_DIR = r"C:\Users\ucark\Desktop\MediaDownloader-Site\publish"
ADMIN_EMAIL = "admin@tube.yefeblgn.net"
ADMIN_PASS  = "BLGNTube2024!"
# ───────────────────────────────────────────────────────────────────────────

import sys, io as _io
sys.stdout = _io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
sys.stderr = _io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", errors="replace")

BOLD = ""; RED = ""; GRN = ""; YEL = ""; RST = ""

def banner(msg): print(f"\n=== {msg} ===")
def ok(msg):     print(f"  [OK] {msg}")
def err(msg):    print(f"  [ERR] {msg}")

def run(ssh, cmd, timeout=120, check=True):
    chan = ssh.get_transport().open_session()
    chan.set_combine_stderr(False)
    chan.exec_command(cmd)
    out_b = b""; err_b = b""
    while True:
        if chan.recv_ready():   out_b += chan.recv(65536)
        if chan.recv_stderr_ready(): err_b += chan.recv_stderr(65536)
        if chan.exit_status_ready():
            while chan.recv_ready():       out_b += chan.recv(65536)
            while chan.recv_stderr_ready(): err_b += chan.recv_stderr(65536)
            break
        time.sleep(0.05)
    rc = chan.recv_exit_status()
    out = out_b.decode("utf-8", errors="replace").strip()
    er  = err_b.decode("utf-8", errors="replace").strip()
    short = cmd[:70].replace("\n", " ")
    if rc == 0:
        ok(f"[{rc}] {short}")
        if out: print(f"     {out[:200]}")
    else:
        if check:
            err(f"[{rc}] {short}")
            if out: print(f"     OUT: {out[:300]}")
            if er:  print(f"     ERR: {er[:300]}")
        else:
            print(f"  ~ [{rc}] {short}  (ignored)")
    return rc, out, er

def write_file(sftp, remote_path, content):
    with sftp.file(remote_path, "w") as f:
        f.write(content)
    ok(f"Yazıldı: {remote_path}")

# ── Bağlan ─────────────────────────────────────────────────────────────────
banner("Sunucuya bağlanılıyor")
ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(HOST, username=USER, password=PASSWD, timeout=20)
ok(f"Bağlantı kuruldu → {HOST}")

# ── 1. Sistem güncellemesi ─────────────────────────────────────────────────
banner("1/9 — Sistem güncelleniyor")
run(ssh, "apt-get update -qq", timeout=180)
run(ssh, "DEBIAN_FRONTEND=noninteractive apt-get upgrade -y -qq", timeout=300)

# ── 2. .NET 8 ──────────────────────────────────────────────────────────────
banner("2/9 — .NET 8 Runtime")
rc, out, _ = run(ssh, "dotnet --list-runtimes 2>/dev/null | grep 'Microsoft.AspNetCore.App 8'", check=False)
if rc == 0 and "8." in out:
    ok(".NET 8 zaten kurulu, atlanıyor")
else:
    run(ssh, "wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/ms.deb", timeout=60)
    run(ssh, "dpkg -i /tmp/ms.deb", timeout=30)
    run(ssh, "apt-get update -qq", timeout=120)
    run(ssh, "DEBIAN_FRONTEND=noninteractive apt-get install -y aspnetcore-runtime-8.0", timeout=300)
    run(ssh, "dotnet --list-runtimes | grep 8")

# ── 3. ffmpeg, nginx, certbot ──────────────────────────────────────────────
banner("3/9 — ffmpeg / nginx / certbot")
run(ssh, "DEBIAN_FRONTEND=noninteractive apt-get install -y ffmpeg nginx certbot python3-certbot-nginx unzip curl", timeout=300)
run(ssh, "ffmpeg -version 2>&1 | head -1")
run(ssh, "nginx -v 2>&1")

# ── 4. yt-dlp ──────────────────────────────────────────────────────────────
banner("4/9 — yt-dlp")
run(ssh, "curl -sSL https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp", timeout=60)
run(ssh, "chmod +x /usr/local/bin/yt-dlp")
run(ssh, "yt-dlp --version")

# ── 5. Uygulama dosyalarını yükle ─────────────────────────────────────────
banner("5/9 — Uygulama dosyaları yükleniyor")
run(ssh, f"mkdir -p {APP_DIR}/wwwroot/downloads")

# Zip oluştur
print("  Zip oluşturuluyor…")
zip_buf = io.BytesIO()
with zipfile.ZipFile(zip_buf, "w", zipfile.ZIP_DEFLATED) as zf:
    for root, _, files in os.walk(PUBLISH_DIR):
        for fname in files:
            fp = os.path.join(root, fname)
            arc = os.path.relpath(fp, PUBLISH_DIR).replace("\\", "/")
            zf.write(fp, arc)
zip_buf.seek(0)
size_mb = round(len(zip_buf.getvalue()) / 1_048_576, 1)
print(f"  Zip hazır: {size_mb} MB")

sftp = ssh.open_sftp()
print("  SFTP ile yükleniyor…")
sftp.putfo(zip_buf, "/tmp/blgntube.zip")
ok("Zip yüklendi")

run(ssh, f"unzip -o /tmp/blgntube.zip -d {APP_DIR}", timeout=60)
run(ssh, f"rm /tmp/blgntube.zip")

# ── 6. .env dosyası ────────────────────────────────────────────────────────
banner("6/9 — .env yapılandırma")
env_content = f"""ADMIN_EMAIL={ADMIN_EMAIL}
ADMIN_PASSWORD={ADMIN_PASS}
"""
write_file(sftp, f"{APP_DIR}/.env", env_content)

# ── 7. İzinler + servis kullanıcısı ───────────────────────────────────────
banner("7/9 — İzinler & systemd servisi")
run(ssh, "id blgntube 2>/dev/null || useradd -r -s /bin/false blgntube", check=False)
run(ssh, f"chown -R blgntube:blgntube {APP_DIR}")
run(ssh, f"chmod -R 755 {APP_DIR}")
run(ssh, f"chmod 775 {APP_DIR}/wwwroot/downloads")
run(ssh, f"chmod 600 {APP_DIR}/.env")

service = f"""[Unit]
Description=BLGNTube Media Downloader
After=network.target

[Service]
WorkingDirectory={APP_DIR}
ExecStart=/usr/bin/dotnet {APP_DIR}/BLGNTube.Web.dll
Restart=always
RestartSec=10
User=blgntube
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
KillSignal=SIGINT
TimeoutStopSec=30

[Install]
WantedBy=multi-user.target
"""
write_file(sftp, "/etc/systemd/system/blgntube.service", service)

run(ssh, "systemctl daemon-reload")
run(ssh, "systemctl enable blgntube")
run(ssh, "systemctl restart blgntube")
time.sleep(4)
run(ssh, "systemctl status blgntube --no-pager -l", check=False)

# ── 8. Nginx yapılandırması ────────────────────────────────────────────────
banner("8/9 — Nginx")
nginx_conf = f"""server {{
    listen 80;
    listen [::]:80;
    server_name {DOMAIN};

    # Büyük dosyalar için buffer kapatılır (streaming indirme)
    proxy_buffering off;

    location / {{
        proxy_pass          http://localhost:5000;
        proxy_http_version  1.1;
        proxy_set_header    Upgrade $http_upgrade;
        proxy_set_header    Connection keep-alive;
        proxy_set_header    Host $host;
        proxy_set_header    X-Real-IP $remote_addr;
        proxy_set_header    X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header    X-Forwarded-Proto $scheme;
        proxy_cache_bypass  $http_upgrade;
        proxy_read_timeout  600s;
        proxy_send_timeout  600s;
        client_max_body_size 0;
    }}
}}
"""
write_file(sftp, "/etc/nginx/sites-available/blgntube", nginx_conf)
run(ssh, "ln -sf /etc/nginx/sites-available/blgntube /etc/nginx/sites-enabled/blgntube")
run(ssh, "rm -f /etc/nginx/sites-enabled/default")
run(ssh, "nginx -t")
run(ssh, "systemctl reload nginx")

# ── 9. SSL (Let's Encrypt) ─────────────────────────────────────────────────
banner("9/9 — SSL (Let's Encrypt)")
rc, out, er = run(ssh,
    f"certbot --nginx -d {DOMAIN} --non-interactive --agree-tos "
    f"-m {ADMIN_EMAIL} --redirect 2>&1",
    timeout=120, check=False)
if rc == 0:
    ok("SSL sertifikası başarıyla alındı!")
else:
    print(f"  {YEL}⚠ SSL alınamadı (DNS henüz yönlendirilmemiş olabilir).{RST}")
    print(f"    Sonradan: certbot --nginx -d {DOMAIN} --non-interactive --agree-tos -m {ADMIN_EMAIL} --redirect")

sftp.close()

# ── Özet ───────────────────────────────────────────────────────────────────
banner("Kurulum tamamlandı")
run(ssh, "systemctl status blgntube --no-pager", check=False)
run(ssh, "systemctl status nginx --no-pager", check=False)

print(f"""
{BOLD}═══════════════════════════════════════════════
  Adres  : http://{DOMAIN}  (SSL varsa https)
  Admin  : {ADMIN_EMAIL}
  Şifre  : {ADMIN_PASS}
  App    : {APP_DIR}
  Loglar : journalctl -u blgntube -f
═══════════════════════════════════════════════{RST}
""")

ssh.close()
