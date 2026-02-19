using Microsoft.AspNetCore.Mvc;

namespace ASIB.Controllers;

public class ErrorController : Controller
{
    [Route("Error")]
    public IActionResult Error()
    {
        return View("Error");
    }
}
