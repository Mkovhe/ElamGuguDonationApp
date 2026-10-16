using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ElamIgugu.Web.Api;

/// <summary>Base for the JSON API used by the Android app. Uses a login token, so the cookie anti-forgery check does not apply.</summary>
[ApiController]
[IgnoreAntiforgeryToken]
public abstract class ApiControllerBase : ControllerBase
{
    protected string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    protected ObjectResult Problem(int status, string title) =>
        base.Problem(statusCode: status, title: title);
}
