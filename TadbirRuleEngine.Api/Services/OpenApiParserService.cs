using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TadbirRuleEngine.Api.DTOs;

namespace TadbirRuleEngine.Api.Services;

public class OpenApiParserService : IOpenApiParserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenApiParserService> _logger;

    public OpenApiParserService(HttpClient httpClient, ILogger<OpenApiParserService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<ApiDefinitionDto>> ParseSwaggerAsync(string swaggerUrl)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(swaggerUrl);
            var swaggerDoc = JsonConvert.DeserializeObject<JObject>(response);

            if (swaggerDoc == null)
                return Enumerable.Empty<ApiDefinitionDto>();

            var apis = new List<ApiDefinitionDto>();
            var paths = swaggerDoc["paths"] as JObject;
            
            if (paths == null)
                return apis;

            foreach (var pathProperty in paths.Properties())
            {
                var path = pathProperty.Name;
                var pathItem = pathProperty.Value as JObject;
                
                if (pathItem == null) continue;

                foreach (var methodProperty in pathItem.Properties())
                {
                    var method = methodProperty.Name.ToUpper();
                    var operation = methodProperty.Value as JObject;
                    
                    if (operation == null) continue;

                    // Check if operation has "RuleEngineEndpoint" tag
                    if (!HasRuleEngineTag(operation))
                        continue;

                    var operationId = operation["operationId"]?.ToString() ?? $"{method}_{path}";
                    
                    var api = new ApiDefinitionDto
                    {
                        Name = operation["summary"]?.ToString() ?? $"{method} {path}",
                        Path = path,
                        Method = method,
                        Description = operation["description"]?.ToString() ?? operation["summary"]?.ToString(),
                        RequestSchema = ExtractRequestSchema(operation, swaggerDoc),
                        ResponseSchema = ExtractResponseSchema(operation, swaggerDoc),
                        Parameters = ExtractParameters(operation),
                        RequiresAuth = HasAuthRequirement(operation)
                    };

                    apis.Add(api);
                }
            }

            return apis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing swagger from {Url}", swaggerUrl);
            return Enumerable.Empty<ApiDefinitionDto>();
        }
    }

    private bool HasRuleEngineTag(JObject operation)
    {
        try
        {
            var tags = operation["tags"] as JArray;
            if (tags == null) return false;

            foreach (var tag in tags)
            {
                if (tag.ToString().Equals("RuleEngineEndpoint", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private string? ExtractRequestSchema(JObject operation, JObject swaggerDoc)
    {
        try
        {
            var requestBody = operation["requestBody"];
            if (requestBody == null) return null;

            var content = requestBody["content"];
            if (content == null) return null;

            var jsonContent = content["application/json"] ?? content["text/json"] ?? content["application/*+json"];
            if (jsonContent == null) return null;

            var schema = jsonContent["schema"];
            if (schema == null) return null;

            // Resolve $ref if exists
            var resolvedSchema = ResolveSchema(schema, swaggerDoc);
            return JsonConvert.SerializeObject(resolvedSchema, Formatting.Indented);
        }
        catch
        {
            return null;
        }
    }

    private string? ExtractResponseSchema(JObject operation, JObject swaggerDoc)
    {
        try
        {
            var responses = operation["responses"];
            if (responses == null) return null;

            var successResponse = responses["200"] ?? responses["201"] ?? responses["202"];
            if (successResponse == null) return null;

            var content = successResponse["content"];
            if (content == null) return null;

            var jsonContent = content["application/json"] ?? content["text/json"];
            if (jsonContent == null) return null;

            var schema = jsonContent["schema"];
            if (schema == null) return null;

            // Resolve $ref if exists
            var resolvedSchema = ResolveSchema(schema, swaggerDoc);
            return JsonConvert.SerializeObject(resolvedSchema, Formatting.Indented);
        }
        catch
        {
            return null;
        }
    }

    private JToken ResolveSchema(JToken schema, JObject swaggerDoc)
    {
        try
        {
            var refValue = schema["$ref"]?.ToString();
            if (string.IsNullOrEmpty(refValue))
                return schema;

            // Parse reference like "#/components/schemas/CustomerSalesOrderAddDTO"
            var parts = refValue.Split('/');
            if (parts.Length < 2) return schema;

            JToken? current = swaggerDoc;
            foreach (var part in parts.Skip(1)) // Skip the # part
            {
                if (current == null) break;
                current = current[part];
            }

            return current ?? schema;
        }
        catch
        {
            return schema;
        }
    }

    private string? ExtractParameters(JObject operation)
    {
        try
        {
            var parameters = operation["parameters"];
            if (parameters == null) return null;

            return parameters.ToString();
        }
        catch
        {
            return null;
        }
    }

    private bool HasAuthRequirement(JObject operation)
    {
        try
        {
            var security = operation["security"];
            return security != null && security.HasValues;
        }
        catch
        {
            return false;
        }
    }
}