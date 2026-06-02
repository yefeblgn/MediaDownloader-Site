using System.ComponentModel.DataAnnotations;

namespace BLGNTube.Web.Models;

/// <summary>İndirilecek medya türü.</summary>
public enum MediaFormat
{
    /// <summary>Video + ses (MP4).</summary>
    Mp4 = 0,
    /// <summary>Sadece ses (MP3).</summary>
    Mp3 = 1
}

/// <summary>
/// Tamamlanmış bir indirmenin kalıcı kaydı. Hem kullanıcı geçmişini
/// hem de günlük kota hesabını beslemek için kullanılır.
/// </summary>
public class DownloadRecord
{
    public int Id { get; set; }

    /// <summary>Üye indirmesi ise kullanıcı kimliği; anonim ise null.</summary>
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>Anonim kullanıcıları kota için ayırt etmek üzere kullanılan IP adresi.</summary>
    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [Required]
    [MaxLength(2048)]
    public string SourceUrl { get; set; } = string.Empty;

    [MaxLength(512)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? ThumbnailUrl { get; set; }

    /// <summary>Kaynak sitenin adı (youtube, tiktok, twitter vb.).</summary>
    [MaxLength(128)]
    public string? SiteName { get; set; }

    public MediaFormat Format { get; set; }

    /// <summary>Seçilen kalite etiketi (ör. "1080p", "720p", "320kbps").</summary>
    [MaxLength(32)]
    public string? Quality { get; set; }

    [MaxLength(512)]
    public string? FileName { get; set; }

    public long FileSizeBytes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
