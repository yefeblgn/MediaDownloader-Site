namespace BLGNTube.Web.Services;

/// <summary>
/// Basit bir ".env" dosyası okuyucusu. KEY=VALUE satırlarını okuyup, henüz
/// tanımlı değilse süreç ortam değişkeni olarak ayarlar. Böylece bu değerler
/// ASP.NET Core yapılandırmasından (Configuration[...]) da okunabilir.
/// </summary>
public static class DotEnv
{
    /// <summary>
    /// .env dosyasını yükler. Belirtilen yol yoksa, çalışma dizininden başlayıp
    /// üst dizinlere doğru bir ".env" arar (böylece proje dizininden de, depo
    /// kökünden de çalıştırıldığında bulunur).
    /// </summary>
    public static void Load(string? path = null)
    {
        if (path is null || !File.Exists(path))
            path = FindUpwards(".env");

        if (path is null || !File.Exists(path)) return;

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var idx = line.IndexOf('=');
            if (idx <= 0) continue;

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();

            // Tırnak içindeyse tırnakları kaldır.
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            // Zaten ortamda tanımlıysa üzerine yazma (gerçek ortam değişkenleri önceliklidir).
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    /// <summary>Çalışma dizininden başlayarak üst dizinlerde dosyayı arar.</summary>
    private static string? FindUpwards(string fileName)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < 6 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir.FullName, fileName);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }
}
