using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Resources;
using Domain.Entities.Medicines;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Commands;

public sealed class DeleteBatchCommandHandler : IRequestHandler<DeleteBatchCommand, Result>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DeleteBatchCommandHandler(IMedicineRepository repo, IUnitOfWork uow, IStringLocalizer<SharedResource> localizer)
    {
        _repo = repo;
        _uow = uow;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteBatchCommand request, CancellationToken cancellationToken)
    {
        var batch = await _repo.GetBatchByIdAsync(request.Id, cancellationToken);
        if (batch is null)
            return Result.Failure(_localizer["ResourceNotFound", "MedicineBatch", request.Id].Value, 404);

        _repo.RemoveBatch(batch);
        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message, 422);
        }

        return Result.Success();
    }
}