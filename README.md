# BLGNTube 🎬🎵

YouTube, TikTok, Twitter/X, Reddit ve **yüzlerce siteden** medyayı **MP3** veya
**MP4** olarak yüksek kalitede indirebileceğin, ASP.NET Core ile yazılmış modern
bir medya indirme sitesi. (Eğitim/ödev amaçlı.)

> İndirme motoru olarak açık kaynaklı [**yt-dlp**](https://github.com/yt-dlp/yt-dlp)
> kullanılır — tıpkı [MeTube](https://github.com/alexta69/metube) gibi projeler gibi.

## ✨ Özellikler

- 🔗 Tek bir bağlantı ile yüzlerce site desteği (yt-dlp tabanlı)
- 🎞️ MP4 video (4K'ya kadar, kalite seçimi) ve 🎵 MP3 ses (320 kbps)
- 📊 Canlı ilerleme çubuğu (arka plan job + durum polling)
- 👤 Kayıt / giriş (ASP.NET Core Identity)
- ⛔ Günlük indirme limitleri:
  - Üye **olmayanlar**: günde **10** indirme (IP bazlı)
  - **Üyeler**: günde **100** indirme
- 🗂️ Profil sayfası: indirme geçmişi, kalan hak ve istatistikler
- 📱 Tam responsive, koyu temalı, animasyonlu modern arayüz (Tailwind + cam efektli kartlar)

## 🧱 Teknolojiler

| Katman | Teknoloji |
|--------|-----------|
| Backend & UI | ASP.NET Core 8 (MVC + Razor) |
| Veritabanı | SQLite + Entity Framework Core |
| Kimlik | ASP.NET Core Identity |
| İndirme motoru | yt-dlp + ffmpeg |
| Arayüz | Tailwind CSS (CDN), vanilla JS |

## ✅ Gereksinimler

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. [**yt-dlp**](https://github.com/yt-dlp/yt-dlp#installation) (PATH'te olmalı)
3. [**ffmpeg**](https://ffmpeg.org/download.html) (ses dönüştürme ve video birleştirme için)

### yt-dlp & ffmpeg kurulumu

**Windows (winget):**
```powershell
winget install yt-dlp.yt-dlp
winget install Gyan.FFmpeg
```

**macOS (Homebrew):**
```bash
brew install yt-dlp ffmpeg
```

**Linux (Debian/Ubuntu):**
```bash
sudo apt install ffmpeg
python3 -m pip install -U yt-dlp     # ya da: sudo apt install yt-dlp
```

Kurulumu doğrula:
```bash
yt-dlp --version
ffmpeg -version
```

## 🚀 Çalıştırma

```bash
# Depoyu klonla
git clone <repo-url>
cd MediaDownloader-Site

# Projeyi geri yükle ve çalıştır
dotnet restore
dotnet run --project src/BLGNTube.Web
```

Tarayıcıda aç: **http://localhost:5080** (veya **https://localhost:7080**)

Veritabanı (`blgntube.db`) ilk çalıştırmada otomatik oluşturulur.

## ⚙️ Yapılandırma

`src/BLGNTube.Web/appsettings.json` içinden ayarlanabilir:

```jsonc
"Downloader": {
  "YtDlpPath": "yt-dlp",        // yt-dlp tam yolu (PATH'te değilse)
  "FfmpegPath": "ffmpeg",       // ffmpeg tam yolu/dizini
  "OutputDirectory": "wwwroot/downloads",
  "TimeoutSeconds": 600,        // tek indirme için zaman aşımı
  "MaxDurationSeconds": 0       // 0 = sınırsız süre
}
```

Limitler kod tarafında `Services/QuotaService.cs` içindedir
(`AnonymousDailyLimit = 10`, `AuthenticatedDailyLimit = 100`).

## 📁 Proje yapısı

```
BLGNTube.sln
src/BLGNTube.Web/
├── Controllers/   # Home, Download (API), Account, Profile
├── Data/          # ApplicationDbContext (EF Core / SQLite)
├── Models/        # ApplicationUser, DownloadRecord, MediaInfo, DownloadJob, ViewModels
├── Services/      # YtDlpService, DownloadJobManager, QuotaService
├── Views/         # Razor görünümleri (Home, Account, Profile, Shared)
├── wwwroot/       # css/js, indirilen geçici dosyalar
├── Program.cs     # uygulama yapılandırması & DI
└── appsettings.json
```

## 🔄 İndirme akışı

1. Kullanıcı bağlantıyı yapıştırır → `POST /api/download/info` ile önizleme alınır.
2. Format/kalite seçilir → `POST /api/download/start` kotayı kontrol eder ve arka plan job'u başlatır.
3. Arayüz `GET /api/download/status/{id}` ile ilerlemeyi canlı izler.
4. Tamamlanınca `GET /api/download/file/{id}` ile dosya kullanıcıya teslim edilir.
5. İndirme kaydı veritabanına yazılır (geçmiş + kota). Geçici dosyalar 30 dk sonra silinir.

## ⚠️ Yasal uyarı

Bu proje yalnızca **eğitim amaçlıdır**. Yalnızca yasal olarak indirme hakkına
sahip olduğunuz veya açık izinli içerikleri indirin. Telif hakkı ihlalinden
kullanıcı sorumludur.
