using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Propyka.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropykaController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Propyka API is working!");
        }

    }
}
