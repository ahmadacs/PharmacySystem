using Application.Features.Auth.Commands;
using Application.Features.Auth.Dtos;
using Application.Features.Auth.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiVersion("1.0")]
public sealed class AuthController(ISender sender, IWebHostEnvironment environment) : ApiControllerBase(sender)
{
    public const string RefreshCookieName = "RefreshToken";

    /// <summary>
    /// Authenticates a user and issues a JWT access token plus a refresh token cookie.
    /// </summary>
    /// <param name="request">The user credentials.</param>
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => AuthResponse(new LoginCommand(request), SetRefreshCookie, cancellationToken);

    /// <summary>
    /// Registers a new user account and issues authentication tokens.
    /// </summary>
    /// <param name="request">The new account details.</param>
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        => AuthResponse(new RegisterCommand(request), SetRefreshCookie, cancellationToken);

    /// <summary>
    /// Rotates the refresh token cookie and issues a new access token.
    /// </summary>
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        return AuthResponse(new RefreshTokenCommand(refreshToken), SetRefreshCookie, cancellationToken);
    }

    /// <summary>
    /// Revokes the current refresh token and clears the refresh cookie.
    /// </summary>
    [EnableRateLimiting("auth")]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        await Sender.Send(new LogoutCommand(refreshToken), cancellationToken);
        ClearRefreshCookie();
        return NoContent();
    }

    /// <summary>
    /// Gets the profile, roles, and permissions of the authenticated user.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Me(CancellationToken cancellationToken)
        => OkResponse(new GetCurrentUserQuery(), cancellationToken);

    /// <summary>
    /// Changes the password of the authenticated user.
    /// </summary>
    /// <param name="request">The current and new password.</param>
    [Authorize]
    [EnableRateLimiting("auth")]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        => NoContent(new ChangePasswordCommand(request), cancellationToken);

    /// <summary>
    /// Sends a password reset token to the user's email address. Always returns no content to avoid account enumeration.
    /// </summary>
    /// <param name="request">The email address of the account.</param>
    [EnableRateLimiting("auth")]
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        => NoContent(new ForgotPasswordCommand(request), cancellationToken);

    /// <summary>
    /// Resets a password using a valid reset token.
    /// </summary>
    /// <param name="request">The reset token and the new password.</param>
    [EnableRateLimiting("auth")]
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        => NoContent(new ResetPasswordCommand(request), cancellationToken);

    private void SetRefreshCookie(string token)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Path = "/api/v1/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private void ClearRefreshCookie()
        => Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Path = "/api/v1/auth"
        });
}