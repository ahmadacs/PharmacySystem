using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Resources;
using Domain.Entities.Medicines;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Commands;

public sealed class DeleteBatchCommandHandler : IRequestHandler<DeleteBatchCommand, Result>
{
    private readonly IBaseRepository<MedicineBatch> _batches;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DeleteBatchCommandHandler(IBaseRepository<MedicineBatch> batches, IUnitOfWork uow, IStringLocalizer<SharedResource> localizer)
    {
        _batches = batches;
        _uow = uow;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteBatchCommand request, CancellationToken cancellationToken)
    {
        var batchSpec = new Specification<MedicineBatch, MedicineBatch>(b => b).Tracked();
        batchSpec.Where(b => b.Id == request.Id);
        var batch = await _batches.GetAsync(batchSpec, cancellationToken);
        if (batch is null)
            return Result.Failure(_localizer["ResourceNotFound", "MedicineBatch", request.Id].Value, 404);

        _batches.Remove(batch);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
