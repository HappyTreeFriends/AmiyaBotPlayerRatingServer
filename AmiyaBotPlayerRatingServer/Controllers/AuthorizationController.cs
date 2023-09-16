using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Validation.AspNetCore;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    public class AuthorizationController : Controller
    {
        private readonly IOpenIddictApplicationManager _applicationManager;

        public AuthorizationController(IOpenIddictApplicationManager applicationManager)
            => _applicationManager = applicationManager;

        [HttpGet("~/connect/authorize")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest();

            var allowToAuthorize = true;
            // 这里应验证传入的 request 参数（例如：client_id，redirect_uri 等）

            if (allowToAuthorize)
            {
                // 创建或获取用户的身份标识（ClaimsIdentity）
                var identity = new ClaimsIdentity(
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    Claims.Name, Claims.Role);

                //这里的User是授权者,也就是普通用户
                //而开发者需要通过ClientId来确认

                var subjectClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value??"";
                var claim = new Claim(Claims.Subject, subjectClaim);
                claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
                identity.AddClaim(claim);

                claim = new Claim(Claims.Subject, subjectClaim);

                var principal = new ClaimsPrincipal(identity);
                
                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else
            {
                // 用户拒绝授权，您应该重定向到一个错误页面或返回到第三方应用
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
        }

        [AllowAnonymous]
        [HttpPost("~/connect/token"), Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest();
            if (request.IsAuthorizationCodeGrantType())
            {
                var identity = new ClaimsIdentity(
                    TokenValidationParameters.DefaultAuthenticationType,
                    Claims.Name,
                    Claims.Role);

                // Get the principal associated with the authorization code
                var info = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                var userPrincipal = info?.Principal;

                if (userPrincipal == null)
                {
                    // Handle error: the authorization code is invalid or has expired
                    return BadRequest();
                }

                // Extract the user ID from the principal
                var userId = userPrincipal.FindFirst(Claims.Subject)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    // Handle error: the user ID is missing
                    return BadRequest();
                }

                // 这里添加相应的声明。
                identity.AddClaim(Claims.Subject, userId,
                    OpenIddictConstants.Destinations.AccessToken);

                var principal = new ClaimsPrincipal(identity);

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else if (request.IsClientCredentialsGrantType())
            {

                // Note: the client credentials are automatically validated by OpenIddict:
                // if client_id or client_secret are invalid, this action won't be invoked.

                var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                                  throw new InvalidOperationException("The application cannot be found.");

                // Create a new ClaimsIdentity containing the claims that
                // will be used to create an id_token, a token or a code.
                var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name,
                    Claims.Role);

                // Use the client_id as the subject identifier.
                identity.SetClaim(Claims.Subject, await _applicationManager.GetClientIdAsync(application));
                identity.SetClaim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application));

                identity.SetDestinations(static claim => claim.Type switch
                {
                    // Allow the "name" claim to be stored in both the access and identity tokens
                    // when the "profile" scope was granted (by calling principal.SetScopes(...)).
                    Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                        => new[] { Destinations.AccessToken, Destinations.IdentityToken },

                    // Otherwise, only store the claim in the access tokens.
                    _ => new[] { Destinations.AccessToken }
                });

                return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else
            {
                throw new NotImplementedException("The specified grant type is not implemented.");
            }
        }


        [HttpGet("~/connect/userinfo"), Produces("application/json")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public IActionResult Get()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userInfo = new Dictionary<string, object>
            {
                ["sub"] = User.FindFirst("sub")?.Value??"",
                ["uid"] = userId??"",
            };

            return Ok(userInfo);
        }
    }
}
