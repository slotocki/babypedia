using Microsoft.AspNetCore.Http;

namespace BabyRadnik.Infrastructure.Common;

public static class EndpointStubs
{
    public static IResult NotImplemented(string branch) => Results.Problem(
        statusCode: StatusCodes.Status501NotImplemented,
        title: "Jeszcze nie zaimplementowano",
        detail: $"Ten endpoint powstaje na gałęzi {branch}.");
}
