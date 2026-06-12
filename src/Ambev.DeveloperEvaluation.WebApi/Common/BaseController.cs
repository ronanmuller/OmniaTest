using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

[Route("api/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    protected Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim not found"));

    protected string GetCurrentUserEmail() =>
        User.FindFirst(ClaimTypes.Email)?.Value ?? throw new NullReferenceException();

    protected IActionResult Ok<T>(T data) =>
        base.Ok(new ApiResponseWithData<T> { Data = data, Success = true });

    protected IActionResult OkWithMessage(string message) =>
        base.Ok(new ApiResponse { Success = true, Message = message });

    protected IActionResult CreatedWithData<T>(T data, string message = "Created successfully") =>
        base.StatusCode(StatusCodes.Status201Created,
            new ApiResponseWithData<T> { Data = data, Success = true, Message = message });

    protected IActionResult BadRequest(string message) =>
        base.BadRequest(new ApiResponse { Message = message, Success = false });

    protected IActionResult NotFound(string message = "Resource not found") =>
        base.NotFound(new ApiResponse { Message = message, Success = false });

    protected IActionResult OkPaginated<T>(PaginatedList<T> pagedList) =>
        Ok(new PaginatedResponse<T>
        {
            Data = pagedList,
            CurrentPage = pagedList.CurrentPage,
            TotalPages = pagedList.TotalPages,
            TotalCount = pagedList.TotalCount,
            Success = true
        });
}
