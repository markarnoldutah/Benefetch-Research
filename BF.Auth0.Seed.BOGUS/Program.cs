using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions;
using BF.Auth0.Seed;    

public class Program
{
    static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var domain = config["Auth0:Domain"]!;
        var clientId = config["Auth0:ClientId"]!;
        var clientSecret = config["Auth0:ClientSecret"]!;
        var apiId = config["EcApi:Identifier"]!;
        var apiName = config["EcApi:Name"] ?? "EC Eligibility Checker API";

        var mgmtAudience = $"https://{domain}/api/v2/";

        using var http = new HttpClient
        {
            BaseAddress = new Uri($"https://{domain}/")
        };

        // 1) Get Management API token
        var token = await GetManagementTokenAsync(http, clientId, clientSecret, mgmtAudience);
        Console.WriteLine("Got management token.");

        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 2) Ensure EC API (resource server) exists and has scopes
        var resourceServerId = await EnsureApiAsync(http, apiId, apiName);
        Console.WriteLine($"API ready. Resource server id: {resourceServerId}");

        // await EnsureApiScopesAsync(http, resourceServerId, EcSeedData.Permissions);
        Console.WriteLine("SKIPPED:  API scopes synced.");

        // 3) Create roles and assign permissions
        foreach (var role in EcSeedData.Roles)
        {
            var roleId = await EnsureRoleAsync(http, role.Name, role.Description);
            Console.WriteLine($"Role '{role.Name}' ready (id: {roleId}).");

            await EnsureRolePermissionsAsync(http, roleId, apiId, role.Permissions);
            Console.WriteLine($"  Permissions synced for role '{role.Name}'.");
        }

        Console.WriteLine("Done.");
    }

    private static async Task<string> GetManagementTokenAsync(
        HttpClient http, string clientId, string clientSecret, string audience)
    {
        var payload = new
        {
            client_id = clientId,
            client_secret = clientSecret,
            audience = audience,
            grant_type = "client_credentials"
        };

        var resp = await http.PostAsJsonAsync("oauth/token", payload);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<TokenResponse>();
        return json!.AccessToken;
    }

    private static async Task<string> EnsureApiAsync(
        HttpClient http, string apiIdentifier, string apiName)
    {
        // Try to get existing resource server by identifier
        var resp = await http.GetAsync(
            $"api/v2/resource-servers?identifier={Uri.EscapeDataString(apiIdentifier)}");

        resp.EnsureSuccessStatusCode();
        var existing = await resp.Content.ReadFromJsonAsync<List<ResourceServer>>();

        if (existing != null && existing.Count > 0)
        {
            return existing[0].Id!;
        }

        // Create new resource server
        var createPayload = new
        {
            name = apiName,
            identifier = apiIdentifier,
            signing_alg = "RS256",
            token_lifetime = 7200,
            enforce_policies = true,
            skip_consent_for_verifiable_first_party_clients = true,
            token_dialect = "access_token_authz"
        };

        var createResp = await http.PostAsJsonAsync("api/v2/resource-servers", createPayload);
        createResp.EnsureSuccessStatusCode();

        var created = await createResp.Content.ReadFromJsonAsync<ResourceServer>();
        return created!.Id!;
    }

    private static async Task EnsureApiScopesAsync(
        HttpClient http,
        string resourceServerId,
        IEnumerable<EcPermission> desiredPermissions)
    {
        // Get current resource server
        var resp = await http.GetAsync($"api/v2/resource-servers/{resourceServerId}");
        resp.EnsureSuccessStatusCode();

        var rs = await resp.Content.ReadFromJsonAsync<ResourceServer>();
        var currentScopes = rs!.Scopes ?? new List<Auth0Scope>();

        var desiredScopes = desiredPermissions
            .Select(p => new Auth0Scope { Value = p.Name, Description = p.Description })
            .ToList();

        // Merge: union by Value
        var merged = currentScopes
            .Concat(desiredScopes)
            .GroupBy(s => s.Value)
            .Select(g => g.First())
            .ToList();

        rs.Scopes = merged;

        // Patch / Put resource server
        var updateResp = await http.PatchAsJsonAsync(
            $"api/v2/resource-servers/{resourceServerId}",
            new { scopes = rs.Scopes });

        if (!updateResp.IsSuccessStatusCode)
        {
            // Some tenants require PUT instead of PATCH
            var putResp = await http.PutAsJsonAsync(
                $"api/v2/resource-servers/{resourceServerId}",
                new
                {
                    name = rs.Name,
                    identifier = rs.Identifier,
                    signing_alg = rs.SigningAlgorithm,
                    scopes = rs.Scopes
                });

            putResp.EnsureSuccessStatusCode();
        }
    }

    private static async Task<string> EnsureRoleAsync(
        HttpClient http,
        string roleName,
        string description)
    {
        // Query for existing role
        var resp = await http.GetAsync(
            $"api/v2/roles?name_filter={Uri.EscapeDataString(roleName)}");
        resp.EnsureSuccessStatusCode();

        var roles = await resp.Content.ReadFromJsonAsync<List<Auth0Role>>();

        var existing = roles?.FirstOrDefault(r => r.Name == roleName);
        if (existing != null)
        {
            // Optionally update description
            if (!string.Equals(existing.Description, description, StringComparison.Ordinal))
            {
                var patchResp = await http.PatchAsJsonAsync(
                    $"api/v2/roles/{existing.Id}",
                    new { description = description });

                patchResp.EnsureSuccessStatusCode();
            }

            return existing.Id!;
        }

        // Create role
        var createResp = await http.PostAsJsonAsync(
            "api/v2/roles",
            new { name = roleName, description = description });

        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<Auth0Role>();
        return created!.Id!;
    }

    private static async Task EnsureRolePermissionsAsync(
        HttpClient http,
        string roleId,
        string apiIdentifier,
        IEnumerable<string> desiredPermissionNames)
    {
        // Get current role permissions
        var resp = await http.GetAsync(
            $"api/v2/roles/{roleId}/permissions");
        resp.EnsureSuccessStatusCode();

        var current = await resp.Content.ReadFromJsonAsync<RolePermissionsResponse>();
        var currentPerms = current?.Permissions ?? new List<Auth0Permission>();

        var currentNames = new HashSet<string>(
            currentPerms
                .Where(p => p.ResourceServerIdentifier == apiIdentifier)
                .Select(p => p.PermissionName));

        var missing = desiredPermissionNames
            .Where(p => !currentNames.Contains(p))
            .Distinct()
            .ToList();

        if (!missing.Any())
            return;

        var payload = new
        {
            permissions = missing.Select(m => new
            {
                permission_name = m,
                resource_server_identifier = apiIdentifier
            }).ToArray()
        };

        var addResp = await http.PostAsJsonAsync(
            $"api/v2/roles/{roleId}/permissions",
            payload);

        addResp.EnsureSuccessStatusCode();
    }

    // DTOs for Auth0 responses
    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = default!;
    }

    private class ResourceServer
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }

        [JsonPropertyName("signing_alg")]
        public string? SigningAlgorithm { get; set; }

        [JsonPropertyName("scopes")]
        public List<Auth0Scope>? Scopes { get; set; }
    }

    private class Auth0Scope
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = default!;

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    private class Auth0Role
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    private class RolePermissionsResponse
    {
        [JsonPropertyName("permissions")]
        public List<Auth0Permission>? Permissions { get; set; }
    }

    private class Auth0Permission
    {
        [JsonPropertyName("permission_name")]
        public string PermissionName { get; set; } = default!;

        [JsonPropertyName("resource_server_identifier")]
        public string ResourceServerIdentifier { get; set; } = default!;
    }
}

