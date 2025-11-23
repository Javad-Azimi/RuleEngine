namespace TadbirRuleEngine.Api.Services;

public interface IAuthTokenService
{
    Task<string?> GetTokenAsync(string tokenKey);
    Task SetTokenAsync(string tokenKey, string token);
    Task<Dictionary<string, string>> GetAuthHeadersAsync(string? tokenKey = null);
    Task<string?> AcquireTokenAsync(int authSettingId);
    Task<string?> AcquireTokenByNameAsync(string authSettingName);
}