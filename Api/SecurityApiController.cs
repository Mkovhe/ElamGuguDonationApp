using System.ComponentModel.DataAnnotations;
using ElamIgugu.Web.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Encodings.Web;

namespace ElamIgugu.Web.Api;

public class ApiTwoFactorStatus
{
    public bool Enabled { get; set; }
    public int RecoveryCodesLeft { get; set; }
}

public class ApiTwoFactorSetup
{
    /// <summary>The secret, grouped in fours, to type into an authenticator app.</summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>otpauth:// link. Authenticator apps on the same phone can open it directly.</summary>
    public string AuthenticatorUri { get; set; } = string.Empty;
}

public class ApiEnableTwoFactorRequest
{
    [Required, StringLength(10, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

public class ApiRecoveryCodesResponse
{
    public List<string> RecoveryCodes { get; set; } = new();
}

public class ApiDisableTwoFactorRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Lets the signed-in user of the app switch two-factor sign-in on or off.
/// Same rules as the website's Security page. The user always comes from the login token.
/// </summary>
[Route("api/security")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SecurityApiController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UrlEncoder _urlEncoder;
    private readonly ILogger<SecurityApiController> _logger;

    public SecurityApiController(
        UserManager<ApplicationUser> userManager,
        UrlEncoder urlEncoder,
        ILogger<SecurityApiController> logger)
    {
        _userManager = userManager;
        _urlEncoder = urlEncoder;
        _logger = logger;
    }

    [HttpGet("two-factor")]
    public async Task<ActionResult<ApiTwoFactorStatus>> GetStatus()
    {
        var user = await CurrentUserAsync();
        if (user == null) return Unauthorized();

        return new ApiTwoFactorStatus
        {
            Enabled = user.TwoFactorEnabled,
            RecoveryCodesLeft = user.TwoFactorEnabled ? await _userManager.CountRecoveryCodesAsync(user) : 0
        };
    }

    [HttpPost("two-factor/setup"), EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiTwoFactorSetup>> Setup()
    {
        var user = await CurrentUserAsync();
        if (user == null) return Unauthorized();
        if (user.TwoFactorEnabled)
            return Problem(StatusCodes.Status400BadRequest, "Two-factor sign-in is already on.");

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = user.Email ?? user.UserName ?? "user";
        var uri = $"otpauth://totp/{_urlEncoder.Encode("Elam Igugu Leli")}:{_urlEncoder.Encode(email)}" +
                  $"?secret={key}&issuer={_urlEncoder.Encode("Elam Igugu Leli")}&digits=6";

        return new ApiTwoFactorSetup { SharedKey = FormatKey(key!), AuthenticatorUri = uri };
    }

    [HttpPost("two-factor/enable"), EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiRecoveryCodesResponse>> Enable([FromBody] ApiEnableTwoFactorRequest request)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Unauthorized();
        if (user.TwoFactorEnabled)
            return Problem(StatusCodes.Status400BadRequest, "Two-factor sign-in is already on.");

        var code = request.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var valid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);
        if (!valid)
            return Problem(StatusCodes.Status400BadRequest, "That code is not correct. Try the newest code in your authenticator app.");

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8);
        _logger.LogInformation("Two-factor authentication switched on (app) for {UserId}", user.Id);

        return new ApiRecoveryCodesResponse { RecoveryCodes = codes!.ToList() };
    }

    [HttpPost("two-factor/disable"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Disable([FromBody] ApiDisableTwoFactorRequest request)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Unauthorized();

        // Ask for the password again so a stolen, unlocked phone cannot switch the protection off
        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            return Problem(StatusCodes.Status400BadRequest, "That password is not correct.");

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        _logger.LogInformation("Two-factor authentication switched off (app) for {UserId}", user.Id);

        return Ok(new { message = "Two-factor sign-in is now off." });
    }

    private async Task<ApplicationUser?> CurrentUserAsync()
    {
        var id = CurrentUserId;
        return id == null ? null : await _userManager.FindByIdAsync(id);
    }

    private static string FormatKey(string key)
    {
        var parts = new List<string>();
        for (var i = 0; i < key.Length; i += 4)
            parts.Add(key.Substring(i, Math.Min(4, key.Length - i)));
        return string.Join(" ", parts).ToLowerInvariant();
    }
}
