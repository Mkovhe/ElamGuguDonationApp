namespace ElamIguguLeli.App.Models;

// These mirror the DTOs in the website project: Api/ApiModels.cs.

public class ApiAuthResponse
{
    public bool RequiresTwoFactor { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class ApiNpoContact
{
    public string Name { get; set; } = string.Empty;
    public string Slogan { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string InstagramHandle { get; set; } = string.Empty;
    public string DropOffAddress { get; set; } = string.Empty;
    public List<string> PhoneNumbers { get; set; } = new();
    public string? Email { get; set; }
    public List<string> ItemTypes { get; set; } = new();
    public List<string> Cities { get; set; } = new();
    public string MoneyItemType { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
}

public class ApiWebinar
{
    public int Id { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }

    public string WhenText => StartTime.ToString("dddd, d MMMM yyyy 'at' HH:mm") + " (SAST)";
    public string SpeakerText => "Speaker: " + Speaker;
}

public class ApiIntent
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string? City { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Submitted";
    public DateTime DateSubmittedUtc { get; set; }

    // Display helpers for the "My donations" cards.
    public bool IsMoney => ItemType.StartsWith("Money", StringComparison.OrdinalIgnoreCase);
    public string SummaryText => IsMoney
        ? $"Money R{Amount:0}"
        : ItemType + (string.IsNullOrWhiteSpace(Description) ? "" : " - " + Description);
    public string DetailText => $"Ref {Reference}" + (IsMoney || string.IsNullOrWhiteSpace(City) ? "" : " \u00B7 " + City) + " \u00B7 " + DateSubmittedUtc.ToLocalTime().ToString("d MMM yyyy");
    public bool IsReceived => Status == "Received";
    public Color ChipBackground => IsReceived ? Color.FromArgb("#D7EDD8") : Color.FromArgb("#FFF1CC");
    public Color ChipText => IsReceived ? Color.FromArgb("#1D5A20") : Color.FromArgb("#7A5200");
}

public class MessageResponse
{
    public string? Message { get; set; }
}
