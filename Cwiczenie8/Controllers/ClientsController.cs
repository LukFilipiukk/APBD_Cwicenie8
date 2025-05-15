using Microsoft.AspNetCore.Mvc;
using Cwiczenie8.DTOs;
using Cwiczenie8.Services;
using Cwiczenie8.Exceptions;

namespace WepApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController(IDbService service) : ControllerBase
{
    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        try
        {
            var trips = await service.GetClientTripsAsync(id);
            return Ok(trips);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddClient([FromBody] ClientCreateDto client)
    {
        var createdId = await service.AddClientAsync(client);
        return Created($"/api/clients/{createdId}", new { id = createdId });
    }

    [HttpPut("{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientToTrip(int id, int tripId)
    {
        try
        {
            await service.RegisterClientToTripAsync(id, tripId);
            return Ok("Klient zarejestrowany.");
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("{id}/trips/{tripId}")]
    public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
    {
        try
        {
            await service.UnregisterClientFromTripAsync(id, tripId);
            return Ok("Klient usuniety.");
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}