using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AutoMapper;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth.AuthenticateUserFeature;
using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.WebApi.Services;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Auth;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IAuthThrottleService _authThrottle;

    /// <summary>
    /// Initializes a new instance of AuthController
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="authThrottle">Authentication lockout guard</param>
    public AuthController(IMediator mediator, IMapper mapper, IAuthThrottleService authThrottle)
    {
        _mediator = mediator;
        _mapper = mapper;
        _authThrottle = authThrottle;
    }

    /// <summary>
    /// Authenticates a user with their credentials
    /// </summary>
    /// <param name="request">The authentication request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication token if successful</returns>
    [HttpPost]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponseWithData<AuthenticateUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AuthenticateUser([FromBody] AuthenticateUserRequest request, CancellationToken cancellationToken)
    {
        if (_authThrottle.IsLockedOut(request.Email, out _))
        {
            // Keep the same generic response to avoid user enumeration and account-state disclosure.
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var command = _mapper.Map<AuthenticateUserCommand>(request);

        try
        {
            var response = await _mediator.Send(command, cancellationToken);
            _authThrottle.RecordSuccess(request.Email);

            return Ok(_mapper.Map<AuthenticateUserResponse>(response));
        }
        catch (UnauthorizedAccessException)
        {
            _authThrottle.RecordFailure(request.Email);
            throw;
        }
    }
}
