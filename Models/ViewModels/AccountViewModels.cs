using System.ComponentModel.DataAnnotations;

namespace ElamIgugu.Web.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your contact number.")]
    [RegularExpression(@"^\+?[0-9 ]{9,15}$", ErrorMessage = "Enter a valid contact number, e.g. 060 434 0442.")]
    [Display(Name = "Contact number")]
    public string ContactNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree before creating an account.")]
    [Display(Name = "I agree that Elam Igugu Leli may store my details to manage my donations (POPIA).")]
    public bool Consent { get; set; }
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class TwoFactorLoginViewModel
{
    [Required(ErrorMessage = "Please enter the 6-digit code from your authenticator app.")]
    [StringLength(10, MinimumLength = 6, ErrorMessage = "Enter the 6-digit code.")]
    [Display(Name = "6-digit code")]
    public string Code { get; set; } = string.Empty;
}

public class RecoveryLoginViewModel
{
    [Required(ErrorMessage = "Please enter one of your recovery codes.")]
    [StringLength(30, MinimumLength = 6)]
    [Display(Name = "Recovery code")]
    public string RecoveryCode { get; set; } = string.Empty;
}

public class SecurityIndexViewModel
{
    public bool TwoFactorEnabled { get; set; }
    public int RecoveryCodesLeft { get; set; }
}

public class EnableTwoFactorViewModel
{
    public string SharedKey { get; set; } = string.Empty;
    public string QrSvg { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the 6-digit code shown in your authenticator app.")]
    [StringLength(10, MinimumLength = 6, ErrorMessage = "Enter the 6-digit code.")]
    [Display(Name = "6-digit code")]
    public string Code { get; set; } = string.Empty;
}

public class RecoveryCodesViewModel
{
    public IReadOnlyList<string> Codes { get; set; } = Array.Empty<string>();
}

public class DisableTwoFactorViewModel
{
    [Required(ErrorMessage = "Enter your password to switch this off.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
