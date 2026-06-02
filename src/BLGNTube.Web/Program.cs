using BLGNTube.Web.Data;
using BLGNTube.Web.Models;
using BLGNTube.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

// --- Veritabanını oluştur (ödev için migration yerine EnsureCreated) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
