using Microsoft.AspNetCore.Mvc;
using TenderDocs.Application.Features.Search;

namespace TenderDocs.Api.Controllers;

public class SearchController : ApiControllerBase
{
    /// <summary>Global search across documents and projects.</summary>
    [HttpGet]
    public async Task<ActionResult<GlobalSearchResultDto>> Search([FromQuery] string q, CancellationToken ct)
        => Ok(await Mediator.Send(new GlobalSearchQuery(q ?? string.Empty), ct));
}
