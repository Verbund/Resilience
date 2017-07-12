using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Resilience.Api.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            if (id < 0)
            {
                return BadRequest("id must not be negative.");
            }

            return new ObjectResult(id);
        }
    }
}
