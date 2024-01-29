using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using EasyAuthDevProxy.Infrastructure;


namespace EasyAuthDevProxy.Pages
{
    public class IndexModel : PageModel
    {
        [FromRoute]
        public string? Idp { get; set; }

        [BindProperty, Required, StringLength(30)]
        public string? UserName { get; set; }

        [BindProperty, Required, StringLength(40)]
        public string? UserId { get; set; }

        [BindProperty, Required, StringLength(100)]
        public string? Roles { get; set; }
        public void OnGet()
        {
            if (Request.Cookies.TryGetValue(EasyAuth.CookieName, out var cookieValue))
            {
                var principal = EasyAuth.Decode(cookieValue);

                if (Idp == principal.AuthenticationType)
                {
                    Idp = principal.AuthenticationType;
                    UserName = principal.Claims.FirstOrDefault(c => c.Type == principal.NameType)?.Value ?? "";
                    UserId = principal.Claims.FirstOrDefault(c => c.Type == EasyAuth.Claims.ObjectId)?.Value ?? Guid.NewGuid().ToString();
                    Roles = string.Join("\n", principal.Claims.Where(r => r.Type == "roles").Select(r => r.Value));
                }
            }

            if (string.IsNullOrEmpty(UserId))
            {
                UserId = Guid.NewGuid().ToString();
            }
        }
        

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var principal = new MsClientPrincipal
            {
                AuthenticationType = Idp,
                Claims = [
                    new UserClaim { Type = "name", Value = UserName! },
                    .. Roles.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(r => new UserClaim { Type = "roles", Value = r }).ToArray(),
                    new UserClaim { Type = EasyAuth.Claims.ObjectId, Value = UserId! },
                ],
                NameType = "name",
                RoleType = "role"
            };
            var encoded = principal.Encode();
            Response.Cookies.Append("EasyAuthDev", encoded, new CookieOptions{ MaxAge = TimeSpan.FromHours(1), HttpOnly = true });

            return Redirect($"/");
        }
    }
}
