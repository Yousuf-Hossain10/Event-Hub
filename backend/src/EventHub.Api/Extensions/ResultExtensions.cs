using EventHub.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Extensions;

public static class ResultExtensions
{
    public static ActionResult ToErrorActionResult(this ControllerBase controller, Error error) => error.Type switch
    {
        ErrorType.NotFound => controller.NotFound(error.Message),
        ErrorType.Conflict => controller.Conflict(error.Message),
        ErrorType.Unprocessable => controller.UnprocessableEntity(error.Message),
        _ => controller.BadRequest(error.Message)
    };
}
