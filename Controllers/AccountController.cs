using ElamIgugu.Web.Models;
using ElamIgugu.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ElamIgugu.Web.Controllers;

/// <summary>Sign up, sign in, sign out. Public pages, so no [Authorize] on the class.</summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    // ---------- Register ----------

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,          // the email is the sign-in name
            Email = model.Email,
            PhoneNumber = model.ContactNumber.Trim(),
            FullName = model.FullName.Trim(),
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        // Self-registration ALWAYS creates a Donor. Admins are created by the team (see DbSeeder).
        await _userManager.AddToRoleAsync(user, AppRoles.Donor);
        _logger.LogInformation("New donor registered: {Email}", model.Email);

        TempData["Success"] = "Your account has been created. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- Login ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        // lockoutOnFailure: true counts wrong passwords and locks the account temporarily
        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Only follow a return address that belongs to this site (stops open-redirect attacks)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Dashboard", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Too many failed attempts. Please try again in a few minutes.");
        }
        else if (result.RequiresTwoFactor)
        {
            // Password was right and this account has two-factor on: ask for the code next.
            return RedirectToAction(nameof(LoginTwoFactor), new { returnUrl });
        }
        else
        {
            // Same message for "no such email" and "wrong password" so attackers learn nothing
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
        }

        return View(model);
    }

    // ---------- Second step: authenticator-app code ----------

    [HttpGet]
    public async Task<IActionResult> LoginTwoFactor(string? returnUrl = null)
    {
        // Only reachable straight after a correct password
        if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
            return RedirectToAction(nameof(Login));

        ViewData["ReturnUrl"] = returnUrl;
        return View(new TwoFactorLoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> LoginTwoFactor(TwoFactorLoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
            return RedirectToAction(nameof(Login));
        if (!ModelState.IsValid)
            return View(model);

        var code = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            code, isPersistent: false, rememberClient: false);

        if (result.Succeeded)
            return AfterSignIn(returnUrl);

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Too many failed attempts. Please try again in a few minutes."
            : "That code is not correct or has expired. Try the newest code.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> LoginRecoveryCode(string? returnUrl = null)
    {
        if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
            return RedirectToAction(nameof(Login));

        ViewData["ReturnUrl"] = returnUrl;
        return View(new RecoveryLoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> LoginRecoveryCode(RecoveryLoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
            return RedirectToAction(nameof(Login));
        if (!ModelState.IsValid)
            return View(model);

        var code = model.RecoveryCode.Replace(" ", string.Empty);
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);

        if (result.Succeeded)
            return AfterSignIn(returnUrl);

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Too many failed attempts. Please try again in a few minutes."
            : "That recovery code is not valid. Each code works only once.");
        return View(model);
    }

    private IActionResult AfterSignIn(string? returnUrl)
    {
        // Only follow a return address that belongs to this site (stops open-redirect attacks)
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction("Dashboard", "Home");
    }

    // ---------- Logout ----------

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ---------- Shown when a donor tries to open an admin page ----------

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
