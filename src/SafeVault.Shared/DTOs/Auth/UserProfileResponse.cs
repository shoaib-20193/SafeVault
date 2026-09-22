namespace SafeVault.Shared.DTOs.Auth;

public class UserProfileResponse
{
    public string UserId { get; set; } = string.Empty;

    public string? Email { get; set; }

    public List<string> Roles { get; set; } = [];
}