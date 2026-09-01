using Application.Common.Models;
using Application.Features.Auth.Dtos;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest<Result>;