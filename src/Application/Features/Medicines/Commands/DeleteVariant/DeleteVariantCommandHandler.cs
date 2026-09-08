using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Resources;
using Domain.Entities.Medicines;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Commands;

public sealed class DeleteVariantCommandHandler : IRequestHandler<DeleteVariantCommand, Result>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DeleteVariantCommandHandler(IMedicineRepository repo, IUnitOfWork uow, IStringLocalizer<SharedResource> localizer)
    {
        _repo = repo;
        _uow = uow;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await _repo.GetVariantByIdAsync(request.Id, cancellationToken);
        if (variant is null)
            return Result.Failure(_localizer["ResourceNotFound", "MedicineVariant", request.Id].Value, 404);

        _repo.RemoveVariant(variant);
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