using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TadbirRuleEngine.Api.Data;

namespace TadbirRuleEngine.Api.Services;

public class AuthTokenService : IAuthTokenService
{
    private readonly Dictionary<string, (string Token, DateTime ExpiresAt)> _tokens = new();
    private readonly ILogger<AuthTokenService> _logger;
    private readonly TadbirDbContext _context;
    private readonly HttpClient _httpClient;

    public AuthTokenService(
        ILogger<AuthTokenService> logger,
        TadbirDbContext context,
        HttpClient httpClient)
    {
        _logger = logger;
        _context = context;
        _httpClient = httpClient;
    }

    public async Task<string?> GetTokenAsync(string tokenKey)
    {
        await Task.CompletedTask;
        
        if (_tokens.ContainsKey(tokenKey))
        {
            var (token, expiresAt) = _tokens[tokenKey];
            if (expiresAt > DateTime.UtcNow.AddMinutes(5)) // Token still valid for at least 5 minutes
            {
                return token;
            }
            _tokens.Remove(tokenKey);
        }
        
        return null;
    }

    public async Task SetTokenAsync(string tokenKey, string token)
    {
        await SetTokenAsync(tokenKey, token, 3600);
    }

    public async Task SetTokenAsync(string tokenKey, string token, int expiresInSeconds)
    {
        await Task.CompletedTask;
        var expiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
        _tokens[tokenKey] = (token, expiresAt);
        _logger.LogInformation("Token set for key: {TokenKey}, expires at: {ExpiresAt}", tokenKey, expiresAt);
    }

    public async Task<Dictionary<string, string>> GetAuthHeadersAsync(string? tokenKey = null)
    {
        await Task.CompletedTask;
        var headers = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(tokenKey))
        {
            var token = await GetTokenAsync(tokenKey);
            if (!string.IsNullOrEmpty(token))
            {
                headers["Authorization"] = $"Bearer {token}";
            }
        }

        return headers;
    }

    public async Task<string?> AcquireTokenAsync(int authSettingId)
    {
        try
        {
            var authSetting = await _context.AuthenticationSettings.FindAsync(authSettingId);
            if (authSetting == null || !authSetting.IsActive)
            {
                _logger.LogWarning("Authentication setting {Id} not found or inactive", authSettingId);
                return null;
            }

            return await AcquireTokenFromSettingAsync(authSetting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring token for auth setting {Id}", authSettingId);
            return null;
        }
    }

    public async Task<string?> AcquireTokenByNameAsync(string authSettingName)
    {
        try
        {
            var authSetting = await _context.AuthenticationSettings
                .FirstOrDefaultAsync(a => a.Name == authSettingName && a.IsActive);
            
            if (authSetting == null)
            {
                _logger.LogWarning("Authentication setting '{Name}' not found or inactive", authSettingName);
                return null;
            }

            return await AcquireTokenFromSettingAsync(authSetting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring token for auth setting '{Name}'", authSettingName);
            return null;
        }
    }

    private async Task<string?> AcquireTokenFromSettingAsync(Models.AuthenticationSetting authSetting)
    {
        try
        {
            // Check if we already have a valid token
            var existingToken = await GetTokenAsync(authSetting.Name);
            if (!string.IsNullOrEmpty(existingToken))
            {
                return existingToken;
            }

            _logger.LogInformation("Acquiring new token from {Endpoint}", authSetting.TokenEndpoint);

            // Prepare token request
            var requestData = new Dictionary<string, string>
            {
                ["grant_type"] = authSetting.GrantType ?? "password",
                ["username"] = authSetting.Username,
                ["password"] = authSetting.Password
            };

            if (!string.IsNullOrEmpty(authSetting.ClientId))
                requestData["client_id"] = authSetting.ClientId;

            if (!string.IsNullOrEmpty(authSetting.ClientSecret))
                requestData["client_secret"] = authSetting.ClientSecret;

            if (!string.IsNullOrEmpty(authSetting.Scope))
                requestData["scope"] = authSetting.Scope;

            // Add additional parameters if any
            if (!string.IsNullOrEmpty(authSetting.AdditionalParameters))
            {
                try
                {
                    var additionalParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(authSetting.AdditionalParameters);
                    if (additionalParams != null)
                    {
                        foreach (var param in additionalParams)
                        {
                            requestData[param.Key] = param.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse additional parameters");
                }
            }

            var content = new FormUrlEncodedContent(requestData);
            var response = await _httpClient.PostAsync(authSetting.TokenEndpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token acquisition failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JObject.Parse(responseContent);
            
            var accessToken = tokenResponse["access_token"]?.ToString();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("No access_token in response");
                return null;
            }

            // Get expiration time
            var expiresIn = tokenResponse["expires_in"]?.Value<int>() ?? 3600;
            
            // Store token
            await SetTokenAsync(authSetting.Name, accessToken, expiresIn);

            // Update last used time
            authSetting.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully acquired token for {Name}", authSetting.Name);
            return accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring token from {Endpoint}", authSetting.TokenEndpoint);
            return null;
        }
    }
}