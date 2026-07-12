using Microsoft.AspNetCore.Mvc;
using DomainError = EventHub.Domain.Common.Error;
using DomainErrorType = EventHub.Domain.Common.ErrorType;

namespace EventHub.Api.Extensions;

public static class ResultExtensions
{
    public static ActionResult ToErrorActionResult(this ControllerBase controller, DomainError error) => error.Type switch
    {
        DomainErrorType.NotFound => controller.NotFound(error.Message),
        DomainErrorType.Conflict => controller.Conflict(error.Message),
        DomainErrorType.Unprocessable => controller.UnprocessableEntity(error.Message),
        _ => controller.BadRequest(error.Message)
    };
}
