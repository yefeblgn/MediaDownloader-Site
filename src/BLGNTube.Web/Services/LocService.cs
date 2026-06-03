using System.Globalization;
using System.Text.Json;

namespace BLGNTube.Web.Services;

public class LocService
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new();
    private const string Fallback = "tr";

    public LocService(IWebHostEnvironment env, ILogger<LocService> logger)
    {
        var dir = Path.Combine(env.ContentRootPath, "Resources");
        foreach (var culture in new[] { "tr", "en" })
        {
            var file = Path.Combine(dir, $"{culture}.json");
            try
            {
                if (File.Exists(file))
                {
                    var json = File.ReadAllText(file);
                    _translations[culture] =
                        JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                }
                else
                {
                    _translations[culture] = new();
                    logger.LogWarning("Çeviri dosyası bulunamadı: {File}", file);
                }
            }
            catch (Exception ex)
            {
                _translations[culture] = new();
                logger.LogError(ex, "Çeviri dosyası okunamadı: {File}", file);
            }
        }
    }

    public string CurrentCulture => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    public string this[string key]
    {
        get
        {
            var culture = CurrentCulture;
            if (_translations.TryGetValue(culture, out var dict) && dict.TryGetValue(key, out var value))
                return value;
            if (_translations.TryGetValue(Fallback, out var fb) && fb.TryGetValue(key, out var fbValue))
                return fbValue;
            return key;
        }
    }
}
