using Dex.ResponseSigning.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Dex.ResponseSigning.Test.Respondent.Controllers;

[ApiController]
[Route("[controller]")]
public class RespondentController : ControllerBase
{
    [HttpGet]
    [SignResponseFilter]
    public ActionResult<object> Get()
    {
        return Ok(new
        {
            TestNum = 180,
            TestString = "Test",
            TestDate = DateTime.Now
        });
    }

    [HttpGet("failed")]
    [SignResponseFilter]
    public ActionResult<object> FailedGet()
    {
        return BadRequest();
    }
}