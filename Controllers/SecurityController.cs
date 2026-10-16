using ElamIgugu.Web.Models;
using ElamIgugu.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Globalization;
using System.Text.Encodings.Web;

namespace ElamIgugu.Web.Controllers;

/// <summary>
/// Lets a signed-in donor or admin switch two-factor authentication (authenticator app) on or off.
/// Works with Google Authenticator, Microsoft Authenticator, Authy and similar apps.
/// </summary>
[Authorize]
public class SecurityController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UrlEncoder _urlEncoder;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        UrlEncoder urlEncoder,
        ILogger<SecurityController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _urlEncoder = urlEncoder;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");

        return View(new SecurityIndexViewModel
        {
            TwoFactorEnabled = user.TwoFactorEnabled,
            RecoveryCodesLeft = user.TwoFactorEnabled ? await _userManager.CountRecoveryCodesAsync(user) : 0
        });
    }

    // ---------- Switch on ----------

    [HttpGet]
    public async Task<IActionResult> Enable()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");
        if (user.TwoFactorEnabled) return RedirectToAction(nameof(Index));

        return View(await BuildSetupAsync(user, new EnableTwoFactorViewModel()));
    }

    [HttpPost]
    public async Task<IActionResult> Enable(EnableTwoFactorViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");
        if (user.TwoFactorEnabled) return RedirectToAction(nameof(Index));

        if (!ModelState.IsValid)
            return View(await BuildSetupAsync(user, model));

        var code = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var valid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!valid)
        {
            ModelState.AddModelError(nameof(model.Code), "That code is not correct. Check the app and try the newest code.");
            return View(await BuildSetupAsync(user, model));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8);
        _logger.LogInformation("Two-factor authentication switched on for {UserId}", user.Id);

        // Show the codes once, on this response only (they are not stored in readable form)
        return View("RecoveryCodes", new RecoveryCodesViewModel { Codes = codes!.ToList() });
    }

    // ---------- Switch off ----------

    [HttpGet]
    public IActionResult Disable() => View(new DisableTwoFactorViewModel());

    [HttpPost]
    public async Task<IActionResult> Disable(DisableTwoFactorViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View(model);

        // Ask for the password again so someone at an unlocked computer cannot switch it off
        if (!await _userManager.CheckPasswordAsync(user, model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "That password is not correct.");
            return View(model);
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("Two-factor authentication switched off for {UserId}", user.Id);

        TempData["Success"] = "Two-factor sign-in is now off.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Helpers ----------

    private async Task<EnableTwoFactorViewModel> BuildSetupAsync(ApplicationUser user, EnableTwoFactorViewModel model)
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = await _userManager.GetEmailAsync(user) ?? user.UserName ?? "user";
        var uri = string.Format(CultureInfo.InvariantCulture,
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            _urlEncoder.Encode("Elam Igugu Leli"), _urlEncoder.Encode(email), key);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(data).GetGraphic(5);

        model.SharedKey = FormatKey(key!);
        model.QrSvg = svg;
        model.Code = string.Empty;
        return model;
    }

    // Groups the key in fours so it is easier to type: "abcd efgh ..."
    private static string FormatKey(string key)
    {
        var parts = new List<string>();
        for (var i = 0; i < key.Length; i += 4)
            parts.Add(key.Substring(i, Math.Min(4, key.Length - i)));
        return string.Join(" ", parts).ToLowerInvariant();
    }
}
