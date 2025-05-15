using Microsoft.AspNetCore.Mvc;
using Cwiczenie8.Services;

namespace Cwiczenie8.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController(IDbService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllTrips()
    {
        var trips = await service.GetAllTripsAsync();
        return Ok(trips);
    }
}