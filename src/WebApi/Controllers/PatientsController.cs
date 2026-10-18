using Application.Features.Patients.Dtos;
using Application.Features.Patients.Queries.GetPatientByPhone;
using Application.Features.Patients.Queries.GetPatientPrescriptions;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers;

[ApiVersion("1.0")]
public sealed class PatientsController(ISender sender) : ApiControllerBase(sender)
{

    [HttpGet("by-phone/{phone}")]
    [HasPermission(Application.Common.Security.Permissions.Prescriptions.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPhone(string phone, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPatientByPhoneQuery(phone), cancellationToken);
        var patient = result.Value;

        if (patient is null)
            return Ok(PatientMapping.ToNotFoundCheck());

        return Ok(patient.ToCheckDto());
    }

    [HttpGet("{id:guid}/prescriptions")]
    [HasPermission(Application.Common.Security.Permissions.Prescriptions.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> GetPrescriptions(Guid id, [FromQuery] int lookbackDays = 90, CancellationToken cancellationToken = default)
        => OkResponse(new GetPatientPrescriptionsQuery(id, lookbackDays), cancellationToken);
}
