using System.ComponentModel.DataAnnotations;

namespace BLGNTube.Web.Models;

public enum MediaFormat
{
    Mp4 = 0,
    Mp3 = 1
}

public class DownloadRecord
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [Required]
    [MaxLength(2048)]
    public string SourceUrl { get; set; } = string.Empty;

    [MaxLength(512)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(128)]
    public string? SiteName { get; set; }

    public MediaFormat Format { get; set; }

    [MaxLength(32)]
    public string? Quality { get; set; }

    [MaxLength(512)]
    public string? FileName { get; set; }

    public long FileSizeBytes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
