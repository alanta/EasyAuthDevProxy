using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DemoApp.Infrastructure;

// Support for EasyAuth authentication in Azure Container Apps
// source: https://johnnyreilly.com/azure-container-apps-easy-auth-and-dotnet-authentication
public static class EasyAuthAuthenticationBuilderExtensions
{
    public static AuthenticationBuilder AddAzureContainerAppsEasyAuth(
        this AuthenticationBuilder builder,
        Action<EasyAuthAuthenticationOptions>? configure = null)
    {
        if (configure == null) configure = o => { };

        return builder.AddScheme<EasyAuthAuthenticationOptions, EasyAuthAuthenticationHandler>(
            EasyAuth.AUTHSCHEMENAME,
            EasyAuth.AUTHSCHEMENAME,
            configure);
    }
}

public static class EasyAuth
{
    public const string AUTHSCHEMENAME = "EasyAuth";

    public static class Headers
    {
        public const string Principal = "X-MS-CLIENT-PRINCIPAL";
        public const string IdentityProvider = "X-MS-CLIENT-PRINCIPAL-IDP";
        public const string PrincipalId = "X-MS-CLIENT-PRINCIPAL-ID";
        public const string PrincipalName = "X-MS-CLIENT-PRINCIPAL-NAME";
    }
}

public class EasyAuthAuthenticationOptions : AuthenticationSchemeOptions
{
    public EasyAuthAuthenticationOptions()
    {
        Events = new object();
    }
}

public class EasyAuthAuthenticationHandler : AuthenticationHandler<EasyAuthAuthenticationOptions>
{
    public EasyAuthAuthenticationHandler(
        IOptionsMonitor<EasyAuthAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            var easyAuthProvider = Context.Request.Headers[EasyAuth.Headers.IdentityProvider].FirstOrDefault() ?? "aad";
            var msClientPrincipalEncoded = Context.Request.Headers[EasyAuth.Headers.Principal].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(msClientPrincipalEncoded))
                return AuthenticateResult.NoResult();

            var decodedBytes = Convert.FromBase64String(msClientPrincipalEncoded);
            using var memoryStream = new MemoryStream(decodedBytes);
            var clientPrincipal = await JsonSerializer.DeserializeAsync<MsClientPrincipal>(memoryStream);

            if (clientPrincipal == null || !clientPrincipal.Claims.Any())
                return AuthenticateResult.NoResult();

            var claims = clientPrincipal.Claims.Select(claim => new Claim(claim.Type, claim.Value));

            // remap "roles" claims from easy auth to the more standard ClaimTypes.Role / "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            var easyAuthRoleClaims = claims.Where(claim => claim.Type == "roles");
            var claimsAndRoles = claims.Concat(easyAuthRoleClaims.Select(role => new Claim(ClaimTypes.Role, role.Value)));

            var principal = new ClaimsPrincipal();
            principal.AddIdentity(new ClaimsIdentity(claimsAndRoles, clientPrincipal.AuthenticationType, clientPrincipal.NameType, ClaimTypes.Role));

            var ticket = new AuthenticationTicket(principal, easyAuthProvider);
            var success = AuthenticateResult.Success(ticket);
            Context.User = principal;

            return success;
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail(ex);
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