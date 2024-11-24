using Microsoft.AspNetCore.Mvc;

namespace PANDOLLAR.Areas.SystemManager.Controllers
{
    [Area("CoreSystem")]
    public class SystemManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
