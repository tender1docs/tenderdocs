using Microsoft.AspNetCore.Mvc;
using TenderDocs.Application.Features.Dashboard;

namespace TenderDocs.Api.Controllers;

public class DashboardController : ApiControllerBase
{
    /// <summary>Totals, by-type counts, expiring/expired list, recent projects and uploads.</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> Stats(CancellationToken ct)
        => Ok(await Mediator.Send(new GetDashboardStatsQuery(), ct));
}
