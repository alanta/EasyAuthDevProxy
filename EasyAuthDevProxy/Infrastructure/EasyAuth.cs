using System.Text;
using System.Text.Json.Serialization;
using Yarp.ReverseProxy.Transforms;

namespace EasyAuthDevProxy.Infrastructure;

public static class EasyAuth
{
    public const string AuthenticationType = "EasyAuth";
    public const string CookieName = "EasyAuthDev";
    public static class Headers
    {
        public const string ClientPrincipal = "X-MS-CLIENT-PRINCIPAL";
        public const string ClientPrincipalIdp = "X-MS-CLIENT-PRINCIPAL-IDP";
        public const string ClientPrincipalName = "X-MS-CLIENT-PRINCIPAL-NAME";
        public const string ClientPrincipalId = "X-MS-CLIENT-PRINCIPAL-ID";
    }

    public static class Claims
    {
        public const string Name = "name";
        public const string Role = "roles";
        public const string ObjectId = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        public const string PreferredUserName = "preferred_username";
    }

    public static string Encode(this MsClientPrincipal principal)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(principal);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
    public static MsClientPrincipal Decode(string encoded)
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        var principal = System.Text.Json.JsonSerializer.Deserialize<MsClientPrincipal>(json);
        return principal;
    }

    public static void Logout(HttpContext context)
    {
        context.Response.Cookies.Delete(CookieName);
        context.Response.Redirect(context.Request.Headers.Referer.FirstOrDefault() ?? "/");
    }
    public static async ValueTask EasyAuthTransform(RequestTransformContext transformContext)
    {
        var cookie = transformContext.HttpContext.Request.Cookies[CookieName];
        if (cookie is null)
        {
            return;
        }

        var principal = EasyAuth.Decode(cookie); // TODO: validate 

        transformContext.ProxyRequest.Headers.Add(Headers.ClientPrincipal, cookie);
        transformContext.ProxyRequest.Headers.Add(Headers.ClientPrincipalIdp, principal.AuthenticationType);

        if (principal.Claims.FirstOrDefault(c => c.Type == Claims.Name) is { } nameClaim)
        {
            transformContext.ProxyRequest.Headers.Add(Headers.ClientPrincipalName, nameClaim.Value);
        }

        if (principal.Claims.FirstOrDefault(c => c.Type == Claims.ObjectId) is { } idClaim)
        {
            transformContext.ProxyRequest.Headers.Add(Headers.ClientPrincipalId, idClaim.Value);
        }
    }
}
public class MsClientPrincipal
{
    [JsonPropertyName("auth_typ")]
    public string? AuthenticationType { get; set; }
    [JsonPropertyName("claims")]
    public IEnumerable<UserClaim> Claims { get; set; } = Array.Empty<UserClaim>();
    [JsonPropertyName("name_typ")]
    public string? NameType { get; set; }
    [JsonPropertyName("role_typ")]
    public string? RoleType { get; set; }
}

public class UserClaim
{
    [JsonPropertyName("typ")]
    public string Type { get; set; } = string.Empty;
    [JsonPropertyName("val")]
    public string Value { get; set; } = string.Empty;
}