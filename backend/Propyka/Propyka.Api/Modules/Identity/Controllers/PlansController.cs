using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Propyka.Api.Common.Plans;

namespace Propyka.Api.Modules.Identity.Controllers;

[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    /// <summary>The plan catalogue for the pricing page. Public.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<PlanDefinition>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PlanDefinition>> Get() => Ok(PlanCatalog.All);
}
