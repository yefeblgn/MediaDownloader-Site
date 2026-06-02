using System.Globalization;
using BLGNTube.Web.Data;
using BLGNTube.Web.Models;
using BLGNTube.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

// --- .env dosyasını yükle (gizli anahtarlar, admin hesabı vb.) ---
// Çalışma dizininden başlayıp üst dizinlerde .env aranır.
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// --- Veritabanı (SQLite) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=blgntube.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// --- Kimlik (ASP.NET Core Identity) ---
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Ödev için makul, çok katı olmayan şifre kuralları.
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireDigit = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// --- Google ile giriş (yalnızca .env'de anahtarlar varsa etkin) ---
var googleClientId = builder.Configuration["GOOGLE_CLIENT_ID"];
var googleClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });
}

// --- Yerelleştirme (TR / EN) ---
builder.Services.AddSingleton<LocService>();

// --- Uygulama servisleri ---
builder.Services.Configure<DownloaderOptions>(
    builder.Configuration.GetSection(DownloaderOptions.SectionName));
builder.Services.AddScoped<QuotaService>();
// YtDlpService durumsuzdur (yalnızca options + logger), bu yüzden singleton
// DownloadJobManager tarafından güvenle tüketilebilir.
builder.Services.AddSingleton<YtDlpService>();
builder.Services.AddSingleton<DownloadJobManager>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- Veritabanını oluştur + admin hesabını ekle ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
    await IdentitySeeder.SeedAsync(scope.ServiceProvider, app.Configuration, logger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// --- İstek yerelleştirme (dil çerezine göre kültür ayarla) ---
var supportedCultures = new[] { new CultureInfo("tr"), new CultureInfo("en") };
var defaultCulture = builder.Configuration["DEFAULT_CULTURE"] is "en" ? "en" : "tr";
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
