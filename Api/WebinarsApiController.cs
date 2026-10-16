using ElamIgugu.Web.Data;
using ElamIgugu.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ElamIgugu.Web.Api;

/// <summary>Public: anyone can see webinars and RSVP, no account needed (same as the website).</summary>
[Route("api/webinars")]
public class WebinarsApiController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<WebinarsApiController> _logger;

    public WebinarsApiController(ApplicationDbContext db, ILogger<WebinarsApiController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ApiWebinarResponse>>> GetUpcoming()
    {
        var items = await _db.Webinars.AsNoTracking()
            .Where(w => w.StartTime >= DateTime.Today)
            .OrderBy(w => w.StartTime)
            .ToListAsync();

        return items.Select(w => new ApiWebinarResponse { Id = w.Id, Topic = w.Topic, Speaker = w.Speaker, StartTime = w.StartTime }).ToList();
    }

    [HttpPost("{id:int}/rsvp"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Rsvp(int id, ApiRsvpRequest request)
    {
        if (!await _db.Webinars.AnyAsync(w => w.Id == id))
            return Problem(StatusCodes.Status404NotFound, "Webinar not found.");

        var number = request.ContactNumber.Replace(" ", string.Empty);

        if (await _db.WebinarRsvps.AnyAsync(r => r.WebinarId == id && r.ContactNumber == number))
            return Ok(new { message = "You are already registered for this webinar." });

        try
        {
            _db.WebinarRsvps.Add(new WebinarRsvp
            {
                WebinarId = id,
                AttendeeName = request.AttendeeName.Trim(),
                ContactNumber = number,
                RsvpDate = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return Ok(new { message = "You are registered. The NPO will be in touch using the number you gave." });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not save RSVP via API for webinar {WebinarId}", id);
            return Problem(StatusCodes.Status500InternalServerError, "We could not save your registration right now. Please try again.");
        }
    }
}
