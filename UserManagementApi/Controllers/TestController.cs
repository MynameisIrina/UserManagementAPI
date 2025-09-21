using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Hello, world!");
    
    [HttpGet("exception")]
    public IActionResult Exception() => throw new Exception("Test exception");
}