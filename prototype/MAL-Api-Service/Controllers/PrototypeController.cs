using Microsoft.AspNetCore.Mvc;

namespace MAL_Api_Service.Controllers;

[ApiController]
[Route("[controller]")]
public class PrototypeController : ControllerBase {

    private readonly ILogger<PrototypeController> _logger;

    public PrototypeController(ILogger<PrototypeController> logger) {
        _logger = logger;
    }

    [HttpGet("testEndpoint", Name = "GetTestEndpoint")]
    public string Get() {
        return "You successfully connected to the MAL Prototype test endpoint";
    }
}