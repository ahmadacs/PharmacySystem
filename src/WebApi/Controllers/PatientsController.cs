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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPhone(string phone, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPatientByPhoneQuery(phone), cancellationToken);
        return result is null ? NotFound(new { message = "Patient not found." }) : Ok(result);
    }

    /// <summary>Lists prescriptions for a patient.</summary>
    [HttpGet("{id:guid}/prescriptions")]
    [Authorize(Policy = Application.Common.Security.Permissions.Prescriptions.Create)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> GetPrescriptions(Guid id, CancellationToken cancellationToken)
        => OkResponse(new GetPatientPrescriptionsQuery(id), cancellationToken);
}
