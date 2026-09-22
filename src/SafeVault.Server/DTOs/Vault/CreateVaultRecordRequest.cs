using System.ComponentModel.DataAnnotations;

namespace SafeVault.Shared.DTOs.Vault;

public class CreateVaultRecordRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Institution { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string AccountType { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{4}$")]
    public string LastFourDigits { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;
}