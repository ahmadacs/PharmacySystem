namespace Application.Features.Auth.Dtos;

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string? FullName,
    string? Role,
    IReadOnlyList<string> Permissions);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    CurrentUserDto User);