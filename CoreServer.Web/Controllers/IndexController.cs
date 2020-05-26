using Microsoft.AspNetCore.Mvc;

namespace CoreServer.Controllers
{
    [ApiController]
    [Route("")]
    public class IndexController : ControllerBase
    {
        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            var listActionUrl = Url.Action("List", "Files");
            return Redirect(listActionUrl);
        }
    }
}
