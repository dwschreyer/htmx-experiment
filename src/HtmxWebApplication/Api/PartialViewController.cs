using HtmxWebApplication.Data;
using Microsoft.AspNetCore.Mvc;

namespace HtmxWebApplication.Api;

[Route("views/[controller]")]
public class PartialViewController : Controller
{
    [HttpGet("People")]
    public IActionResult GetPeople()
    {
        return PartialView("_People", DataServices.GetPeople());
    }

    [HttpGet("People/{id}")]
    public IActionResult GetPeople(int id)
    {
        var person = DataServices.GetPeople()
            .Items.Single(x => x.Id == id);
        return PartialView("_Person", person);
    }
}
