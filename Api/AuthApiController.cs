using ElamIgugu.Web.Models;
using ElamIgugu.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ElamIgugu.Web.Api;

[Route("api/auth")]
public class AuthApiController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthApiController> _logger;

    public AuthApiController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ILogger<AuthApiController> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>Sign up. Always creates a Donor, exactly like the website.</summary>
    [HttpPost("register"), EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiAuthResponse>> Register(ApiRegisterRequest request)
    {
        if (!request.Consent)
            return Problem(StatusCodes.Status400BadRequest, "You must agree before creating an account.");

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.ContactNumber.Trim(),
            FullName = request.FullName.Trim(),
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return Problem(StatusCodes.Status400BadRequest, string.Join(" ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, AppRoles.Donor);
        _logger.LogInformation("New donor registered via API: {Email}", user.Email);

        return Ok(await BuildAuthResponseAsync(user));
    }

    [HttpPost("login"), EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiAuthResponse>> Login(ApiLoginRequest request)
    {
        // Same message for unknown email and wrong password so attackers learn nothing.
        const string invalidMessage = "Invalid email or password.";

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user == null)
            return Problem(StatusCodes.Status401Unauthorized, invalidMessage);

        if (await _userManager.IsLockedOutAsync(user))
            return Problem(StatusCodes.Status423Locked, "Too many failed attempts. Please try again in a few minutes.");

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);   // 5 wrong passwords locks the account, like the website
            return Problem(StatusCodes.Status401Unauthorized, invalidMessage);
        }

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
                return Ok(new ApiAuthResponse { RequiresTwoFactor = true });

            var code = request.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
            var valid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);
            if (!valid)
            {
                await _userManager.AccessFailedAsync(user);
                return Problem(StatusCodes.Status401Unauthorized, "That authenticator code is not valid.");
            }
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return Ok(await BuildAuthResponseAsync(user));
    }

    private async Task<ApiAuthResponse> BuildAuthResponseAsync(ApplicationUser user)
    {
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var (token, expires) = _tokenService.CreateToken(user, roles);
        return new ApiAuthResponse
        {
            Token = token,
            ExpiresAtUtc = expires,
            FullName = user.FullName,
            Email = user.Email,
            Roles = roles
        };
    }
}
