using Newtonsoft.Json;
using System.Text;
using TadbirRuleEngine.Web.Models;

namespace TadbirRuleEngine.Web.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;

    public ApiService(IHttpClientFactory httpClientFactory, ILogger<ApiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("TadbirApi");
        _logger = logger;
    }

    // Swagger Sources
    public async Task<List<SwaggerSourceDto>> GetSwaggerSourcesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("swaggersources");
            return JsonConvert.DeserializeObject<List<SwaggerSourceDto>>(response) ?? new List<SwaggerSourceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting swagger sources");
            return new List<SwaggerSourceDto>();
        }
    }

    public async Task<SwaggerSourceDto?> CreateSwaggerSourceAsync(CreateSwaggerSourceDto dto)
    {
        try
        {
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("swaggersources", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<SwaggerSourceDto>(responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating swagger source");
        }
        return null;
    }

    public async Task<SwaggerSourceDto?> UpdateSwaggerSourceAsync(int id, UpdateSwaggerSourceDto dto)
    {
        try
        {
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"swaggersources/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<SwaggerSourceDto>(responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating swagger source {Id}", id);
        }
        return null;
    }

    public async Task<bool> DeleteSwaggerSourceAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"swaggersources/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting swagger source {Id}", id);
            return false;
        }
    }

    public async Task<bool> SyncSwaggerSourceAsync(int id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"swaggersources/{id}/sync", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing swagger source {Id}", id);
            return false;
        }
    }

    // API Catalog
    public async Task<List<ApiDefinitionDto>> GetApiCatalogAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("apicatalog");
            return JsonConvert.DeserializeObject<List<ApiDefinitionDto>>(response) ?? new List<ApiDefinitionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API catalog");
            return new List<ApiDefinitionDto>();
        }
    }

    // Policies
    public async Task<List<PolicyDto>> GetPoliciesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("policies");
            return JsonConvert.DeserializeObject<List<PolicyDto>>(response) ?? new List<PolicyDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting policies");
            return new List<PolicyDto>();
        }
    }

    public async Task<PolicyDto?> CreatePolicyAsync(CreatePolicyDto dto)
    {
        try
        {
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("policies", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PolicyDto>(responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating policy");
        }
        return null;
    }

    public async Task<object?> ExecutePolicyAsync(int policyId, Dictionary<string, object?>? context = null)
    {
        try
        {
            var json = JsonConvert.SerializeObject(context ?? new Dictionary<string, object?>());
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"policies/{policyId}/execute", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject(responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing policy {PolicyId}", policyId);
        }
        return null;
    }

    // Rules
    public async Task<List<RuleDto>> GetRulesByPolicyIdAsync(int policyId)
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"rules/by-policy/{policyId}");
            return JsonConvert.DeserializeObject<List<RuleDto>>(response) ?? new List<RuleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rules for policy {PolicyId}", policyId);
            return new List<RuleDto>();
        }
    }

    public async Task<RuleDto?> CreateRuleAsync(CreateRuleDto dto)
    {
        try
        {
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("rules", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<RuleDto>(responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rule");
        }
        return null;
    }

    public async Task<PolicyDto?> GetPolicyByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"policies/{id}");
            return JsonConvert.DeserializeObject<PolicyDto>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting policy {Id}", id);
            return null;
        }
    }

    public async Task<PolicyDto?> UpdatePolicyAsync(int id, UpdatePolicyDto dto)
    {
        try
        {
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"policies/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PolicyDto>(responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating policy {Id}", id);
        }
        return null;
    }

    public async Task<bool> DeletePolicyAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"policies/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting policy {Id}", id);
            return false;
        }
    }

    public async Task<RuleDto?> UpdateRuleAsync(int id, UpdateRuleDto dto)
    {
        try
        {
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"rules/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<RuleDto>(responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rule {Id}", id);
        }
        return null;
    }

    public async Task<bool> DeleteRuleAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"rules/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rule {Id}", id);
            return false;
        }
    }

    // Authentication Settings
    public async Task<List<AuthenticationSettingDto>> GetAuthenticationSettingsAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("authenticationsettings");
            return JsonConvert.DeserializeObject<List<AuthenticationSettingDto>>(response) ?? new List<AuthenticationSettingDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication settings");
            return new List<AuthenticationSettingDto>();
        }
    }

    public async Task<AuthenticationSettingDto?> CreateAuthenticationSettingAsync(CreateAuthenticationSettingDto dto)
    {
        try
        {
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("authenticationsettings", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<AuthenticationSettingDto>(responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating authentication setting");
        }
        return null;
    }

    public async Task<AuthenticationSettingDto?> UpdateAuthenticationSettingAsync(int id, UpdateAuthenticationSettingDto dto)
    {
        try
        {
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"authenticationsettings/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<AuthenticationSettingDto>(responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating authentication setting {Id}", id);
        }
        return null;
    }

    public async Task<bool> DeleteAuthenticationSettingAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"authenticationsettings/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting authentication setting {Id}", id);
            return false;
        }
    }

    // Execution Logs
    public async Task<List<ExecutionLogDto>> GetExecutionLogsAsync(int? policyId = null, int skip = 0, int take = 50)
    {
        try
        {
            var url = $"executionlogs?skip={skip}&take={take}";
            if (policyId.HasValue)
                url += $"&policyId={policyId}";

            var response = await _httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<List<ExecutionLogDto>>(response) ?? new List<ExecutionLogDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution logs");
            return new List<ExecutionLogDto>();
        }
    }


    // Test API Call
    public async Task<object?> TestApiCallAsync(object testRequest)
    {
        try
        {
            var json = JsonConvert.SerializeObject(testRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("rules/test", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                // Deserialize to JObject to preserve structure
                var result = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(responseContent);
                _logger.LogInformation("Test API call successful, result: {Result}", result?.ToString());
                return result;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"API test failed: {response.StatusCode} - {errorContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing API call");
            throw;
        }
    }

    // Generic POST method
    public async Task<T?> PostAsync<T>(string endpoint, object requestData) where T : class
    {
        try
        {
            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseContent);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("POST {Endpoint} failed: {StatusCode} - {Error}", endpoint, response.StatusCode, errorContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in POST {Endpoint}", endpoint);
            return null;
        }
    }
}