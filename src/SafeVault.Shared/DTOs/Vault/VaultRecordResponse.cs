namespace SafeVault.Shared.DTOs.Vault;

public class VaultRecordResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Institution { get; set; } = string.Empty;

    public string AccountType { get; set; } = string.Empty;

    public string LastFourDigits { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}