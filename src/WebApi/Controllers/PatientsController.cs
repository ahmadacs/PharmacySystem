using Application.Features.Patients.Dtos;
using Application.Features.Patients.Queries.GetPatientByPhone;
using Application.Features.Patients.Queries.GetPatientPrescriptions;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiVersion("1.0")]
public sealed class PatientsController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Gets a patient by Saudi phone number.</summary>
    [HttpGet("by-phone/{phone}")]
    [Authorize(Policy = Application.Common.Security.Permissions.Prescriptions.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPhone(string phone, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPatientByPhoneQuery(phone), cancellationToken);
        var patient = result.Value;

        // Always return 200. Use a DTO response so the frontend can bind first/last name fields.
        if (patient is null)
            return Ok(PatientMapping.ToNotFoundCheck());

        return Ok(patient.ToCheckDto());
    }

    /// <summary>Lists prescriptions for a patient within a lookback window (Cancelled/Expired excluded, newest first).</summary>
    [HttpGet("{id:guid}/prescriptions")]
    [Authorize(Policy = Application.Common.Security.Permissions.Prescriptions.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> GetPrescriptions(Guid id, [FromQuery] int lookbackDays = 180, CancellationToken cancellationToken = default)
        => OkResponse(new GetPatientPrescriptionsQuery(id, lookbackDays), cancellationToken);
}
