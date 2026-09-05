using Application.Common.Models;
using Application.Features.Dashboard.Dtos;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery : IRequest<Result<DashboardSummaryDto>>;
