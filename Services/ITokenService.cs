using ElamIgugu.Web.Models;

namespace ElamIgugu.Web.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(ApplicationUser user, IEnumerable<string> roles);
}
