using Application.Common.Models;
using Application.Features.Auth.Dtos;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed record ForgotPasswordCommand(ForgotPasswordRequest Request) : IRequest<Result>;