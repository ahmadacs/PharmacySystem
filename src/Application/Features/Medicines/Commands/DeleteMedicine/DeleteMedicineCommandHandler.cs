using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Resources;
using Domain.Entities.Medicines;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Commands;

public sealed class DeleteMedicineCommandHandler : IRequestHandler<DeleteMedicineCommand, Result>
{
    private readonly IBaseRepository<Medicine> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DeleteMedicineCommandHandler(IBaseRepository<Medicine> repo, IUnitOfWork uow, IStringLocalizer<SharedResource> localizer)
    {
        _repo = repo;
        _uow = uow;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteMedicineCommand request, CancellationToken cancellationToken)
    {
        var byIdSpec = new Specification<Medicine, Medicine>(m => m).Tracked();
        byIdSpec.Where(m => m.Id == request.Id);
        var medicine = await _repo.GetAsync(byIdSpec, cancellationToken);
        if (medicine is null)
            return Result.Failure(_localizer["ResourceNotFound", "Medicine", request.Id].Value, 404);

        _repo.Remove(medicine);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}