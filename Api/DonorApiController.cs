using ElamIgugu.Web.Data;
using ElamIgugu.Web.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElamIgugu.Web.Api;

/// <summary>Donor-only endpoints for the app. The donor's identity always comes from the login token, never from the request.</summary>
[Route("api/donor")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Donor)]
public class DonorApiController : ApiControllerBase
{
    private const int MaxProofBytes = 2 * 1024 * 1024;

    private readonly ApplicationDbContext _db;
    private readonly ILogger<DonorApiController> _logger;

    public DonorApiController(ApplicationDbContext db, ILogger<DonorApiController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>"My donations": only this donor's own donations.</summary>
    [HttpGet("donations")]
    public async Task<ActionResult<List<ApiDonationResponse>>> GetMine()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();

        var items = await _db.Donations.AsNoTracking()
            .Where(d => d.DonorId == userId)
            .OrderByDescending(d => d.DateSubmitted)
            .Take(100)
            .ToListAsync();

        return items.Select(ToResponse).ToList();
    }

    /// <summary>Same rules as the website's Donate / Drop Off pop-up.</summary>
    [HttpPost("donations")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<ApiDonationResponse>> Create([FromForm] ApiDonationRequest request)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized();

        if (!DonationOptions.ItemTypes.Contains(request.ItemType))
            return Problem(StatusCodes.Status400BadRequest, "Please choose what you are donating.");

        var isMoney = request.ItemType == DonationOptions.Money;
        decimal? amount = null;
        string city;
        DonationProof? proof = null;

        if (isMoney)
        {
            if (request.Amount is null or < 1 or > 1_000_000)
                return Problem(StatusCodes.Status400BadRequest, "Please enter the amount you plan to pay, in whole rand (R1 to R1,000,000).");
            amount = request.Amount.Value;

            var file = request.Proof;
            if (file is null || file.Length == 0)
                return Problem(StatusCodes.Status400BadRequest, "Please upload your proof of payment (a PDF).");
            if (file.Length > MaxProofBytes)
                return Problem(StatusCodes.Status400BadRequest, "The proof of payment is too big. Please keep it under 2 MB.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            // Must end in .pdf AND really start like a PDF file ("%PDF")
            var looksLikePdf = bytes.Length > 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46;
            if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase) || !looksLikePdf)
                return Problem(StatusCodes.Status400BadRequest, "The proof of payment must be a PDF file.");

            var raw = file.FileName ?? "proof.pdf";
            var safeName = raw[(raw.LastIndexOfAny(new[] { '/', '\\' }) + 1)..];
            if (safeName.Length > 100) safeName = safeName[^100..];
            proof = new DonationProof { FileName = safeName, Data = bytes };
            city = "N/A";   // a bank payment has no collection city
        }
        else
        {
            if (request.City is null || !DonationOptions.Cities.Contains(request.City))
                return Problem(StatusCodes.Status400BadRequest, "Please choose your city.");
            city = request.City;
        }

        var donation = new Donation
        {
            DonorId = userId,
            ItemType = request.ItemType,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Amount = amount,
            ProofFileName = proof?.FileName,
            Proof = proof,
            City = city,
            DateSubmitted = DateTime.UtcNow,
            Status = DonationStatus.Submitted
        };

        try
        {
            _db.Donations.Add(donation);
            await _db.SaveChangesAsync();   // the Id (and so the Reference) exists after this
            return Ok(ToResponse(donation));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not save donation via API for {UserId}", userId);
            return Problem(StatusCodes.Status500InternalServerError, "We could not save that right now. Please try again.");
        }
    }

    private static ApiDonationResponse ToResponse(Donation d) => new()
    {
        Id = d.Id,
        Reference = d.Reference,
        ItemType = d.ItemType,
        Description = d.Description,
        Amount = d.Amount,
        City = d.City,
        Status = d.Status.ToString(),
        DateSubmittedUtc = d.DateSubmitted
    };
}
