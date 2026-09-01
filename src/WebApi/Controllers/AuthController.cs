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

/// <summary>
/// Authentication endpoints: login, self-registration, token refresh, logout and
/// password management. The refresh token travels as an httpOnly cookie scoped to
/// /api/v1/auth; sensitive endpoints are rate-limited (10 requests/minute).
/// </summary>
[ApiVersion("1.0")]
public sealed class AuthController(ISender sender, IWebHostEnvironment environment) : ApiControllerBase(sender)
{
    public const string RefreshCookieName = "RefreshToken";

    private const string AccessTokenStorageNote =
        "Access tokens are kept in browser memory by the client; refresh tokens travel only in this httpOnly cookie.";

    /// <summary>Authenticates a user and returns a short-lived access token plus a refresh-token cookie.</summary>
    /// <param name="request">Email and password.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => AuthResponse(new LoginCommand(request), SetRefreshCookie, cancellationToken);

    /// <summary>Registers a new account and returns the same token pair as login.</summary>
    /// <param name="request">Names, email, password and role.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        => AuthResponse(new RegisterCommand(request), SetRefreshCookie, cancellationToken);

    /// <summary>Rotates the refresh token: the old token is revoked and a new pair is issued.</summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        return AuthResponse(new RefreshTokenCommand(refreshToken), SetRefreshCookie, cancellationToken);
    }

    /// <summary>Revokes the refresh token and clears the httpOnly cookie.</summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
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

    /// <summary>Returns the current authenticated user with role and permissions.</summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> Me(CancellationToken cancellationToken)
        => OkResponse(new GetCurrentUserQuery(), cancellationToken);

    /// <summary>Changes the current user's password.</summary>
    /// <param name="request">Current password and new password.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [Authorize]
    [EnableRateLimiting("auth")]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        => NoContent(new ChangePasswordCommand(request), cancellationToken);

    /// <summary>Requests a password reset; the reset link/token is delivered by email (mocked to a log/file).</summary>
    /// <param name="request">Email address.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [EnableRateLimiting("auth")]
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        => NoContent(new ForgotPasswordCommand(request), cancellationToken);

    /// <summary>Sets a new password using the reset token from the forgot-password flow.</summary>
    /// <param name="request">Reset token and new password.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
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