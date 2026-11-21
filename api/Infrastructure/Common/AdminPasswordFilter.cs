using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace BabyRadnik.Infrastructure.Common;

/// <summary>Panel /admin: hasło z ADMIN_PASSWORD, przesyłane w nagłówku X-Admin-Password.</summary>
public sealed class AdminPasswordFilter(IConfiguration configuration) : IEndpointFilter
{
    public const string HeaderName = "X-Admin-Password";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = configuration["ADMIN_PASSWORD"];
        if (string.IsNullOrEmpty(expected))
        {
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Panel administratora wyłączony", detail: "Nie ustawiono zmiennej ADMIN_PASSWORD.");
        }

        var given = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected)))
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Nieprawidłowe hasło administratora");
        }

        return await next(context);
    }
}
