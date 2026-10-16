using System.ComponentModel.DataAnnotations;

namespace ElamIgugu.Web.Api;

// The JSON shapes used by the Android app. Field names follow the website's own Donation / Webinar classes.

public class ApiRegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\+?[0-9 ]{9,15}$", ErrorMessage = "Enter a valid contact number, e.g. 060 434 0442.")]
    public string ContactNumber { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>POPIA consent, same rule as the website's sign-up form.</summary>
    public bool Consent { get; set; }
}

public class ApiLoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>Only used if two-factor sign-in is switched on for the account later.</summary>
    public string? TwoFactorCode { get; set; }
}

public class ApiAuthResponse
{
    public bool RequiresTwoFactor { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
}

/// <summary>Sent as multipart/form-data so a money donation can carry its proof-of-payment PDF.</summary>
public class ApiDonationRequest
{
    /// <summary>"Clothes", "Non-perishable food" or "Money (EFT)".</summary>
    [Required]
    public string ItemType { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    /// <summary>Whole rand, money only.</summary>
    public int? Amount { get; set; }

    /// <summary>Clothes and food only: "PE", "Alice", "EL" or "Joburg".</summary>
    public string? City { get; set; }

    /// <summary>Money only: PDF, 2 MB or smaller.</summary>
    public IFormFile? Proof { get; set; }
}

public class ApiDonationResponse
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public string City { get; set; } = string.Empty;

    /// <summary>"Submitted" or "Received".</summary>
    public string Status { get; set; } = "Submitted";
    public DateTime DateSubmittedUtc { get; set; }
}

public class ApiWebinarResponse
{
    public int Id { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
}

public class ApiRsvpRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string AttendeeName { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\+?[0-9 ]{9,15}$")]
    public string ContactNumber { get; set; } = string.Empty;
}

public class ApiNpoResponse
{
    public string Name { get; set; } = "Elam Igugu Leli";
    public string Slogan { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string InstagramHandle { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> PhoneNumbers { get; set; } = new();
    public string DropOffAddress { get; set; } = "12 Susan Road, Amalinda, Haven Hills";
    public string BankName { get; set; } = string.Empty;
    public string BankAccountName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public List<string> ItemTypes { get; set; } = new();
    public List<string> Cities { get; set; } = new();
    public string MoneyItemType { get; set; } = string.Empty;
}
