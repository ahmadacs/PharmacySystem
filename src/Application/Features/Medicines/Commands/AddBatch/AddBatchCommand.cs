using Application.Common.Caching;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Medicines.Commands;

[InvalidateCache(CacheTags.Medicines, CacheTags.Inventory)]
public sealed record AddBatchCommand(AddBatchRequest Request, string? Reason = null) : IRequest<Result<Guid>>
{
    /// <summary>
    /// Auto-recorded reason (NOT user-entered) used when a batch is created via
    /// the Medicines screen, where no reason field is presented. The Adjust Stock
    /// "Receive" path always supplies its own mandatory reason.
    /// </summary>
    public const string DefaultCreationReason = "New batch added via the Medicines screen";
}