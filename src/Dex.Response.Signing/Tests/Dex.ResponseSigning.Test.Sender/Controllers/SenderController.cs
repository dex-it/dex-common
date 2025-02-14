using Dex.ResponseSigning.Test.Sender.RefitClients;
using Microsoft.AspNetCore.Mvc;

namespace Dex.ResponseSigning.Test.Sender.Controllers;

[ApiController]
[Route("[controller]")]
public class SenderController : ControllerBase
{
    private readonly HttpClient _client;
    private readonly IRespondentApi _refitClient;

    public SenderController(IHttpClientFactory factory, IRespondentApi refitClient)
    {
        _client = factory.CreateClient("TestClient");
        _refitClient = refitClient;
    }

    [HttpGet("failed/native")]
    public async Task<ActionResult> Get()
    {
        var response = await _client.GetAsync("Respondent/failed");

        return Ok(await response.Content.ReadAsStringAsync());
    }

    [HttpGet("success/refit")]
    public async Task<ActionResult> GetRefit()
    {
        var response = await _refitClient.Test();
        return Ok(response);
    }
}