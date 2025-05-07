using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("prototype")]
public class PrototypeController : ControllerBase {

    private readonly ILogger<PrototypeController> _logger;

    public PrototypeController(ILogger<PrototypeController> logger) {
        _logger = logger;
    }

    [HttpGet("testendpoint", Name = "GetTestEndpoint")]
    public string Get() {
        return "You successfully connected to the MAL Prototype test endpoint";
    }
}