using System.Security.Claims;

namespace QLBH.Utils;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var rawValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawValue, out var userId) ? userId : null;
    }
}
