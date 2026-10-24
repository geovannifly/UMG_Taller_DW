using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Talle_DW.DTOs;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Obtener() => Ok(await dashboard.ObtenerAsync());
}
