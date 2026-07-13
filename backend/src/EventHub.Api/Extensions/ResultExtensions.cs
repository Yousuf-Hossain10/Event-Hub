using Microsoft.AspNetCore.Mvc;
using DomainError = EventHub.Domain.Common.Error;
using DomainErrorType = EventHub.Domain.Common.ErrorType;

namespace EventHub.Api.Extensions;

public record ErrorResponse(string Code, string Message);

public static class ResultExtensions
{
    public static ActionResult ToErrorActionResult(this ControllerBase controller, DomainError error)
    {
        var body = new ErrorResponse(error.Code, error.Message);

        return error.Type switch
        {
            DomainErrorType.NotFound => controller.NotFound(body),
            DomainErrorType.Conflict => controller.Conflict(body),
            DomainErrorType.Unprocessable => controller.UnprocessableEntity(body),
            _ => controller.BadRequest(body)
        };
    }
}
