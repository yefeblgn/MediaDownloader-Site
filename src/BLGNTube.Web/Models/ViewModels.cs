using System.ComponentModel.DataAnnotations;

namespace BLGNTube.Web.Models;

/// <summary>Kayıt formu.</summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "Görünen ad gerekli.")]
    [Display(Name = "Görünen Ad")]
    [StringLength(60, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta gerekli.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gerekli.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Şifre (Tekrar)")]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Giriş formu.</summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta gerekli.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gerekli.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }
}

/// <summary>Profil sayfasının görüntü modeli.</summary>
public class ProfileViewModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime MemberSince { get; set; }

    public int UsedToday { get; set; }
    public int DailyLimit { get; set; }
    public int Remaining => Math.Max(0, DailyLimit - UsedToday);

    public int TotalDownloads { get; set; }

    public List<DownloadRecord> History { get; set; } = new();
}
