using ElamIgugu.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ElamIgugu.Web.Helpers;

/// <summary>
/// Administrators must have two-factor sign-in switched on before they can use the admin pages.
/// An admin without it is sent to the Security page to set it up. Put it on the admin controller.
/// </summary>
public class RequireAdminTwoFactorAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var principal = context.HttpContext.User;

        if (principal.Identity?.IsAuthenticated == true && principal.IsInRole(AppRoles.Admin))
        {
            var userManager = context.HttpContext.RequestServices
                .GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.GetUserAsync(principal);

            if (user is not null && !user.TwoFactorEnabled)
            {
                if (context.Controller is Controller controller)
                {
                    controller.TempData["Warning"] =
                        "Administrators must switch on two-factor sign-in before using the admin pages. Follow the steps below.";
                }
                context.Result = new RedirectToActionResult("Enable", "Security", null);
                return;
            }
        }

        await next();
    }
}
