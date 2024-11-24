using Microsoft.AspNetCore.Mvc;

namespace PANDOLLAR.Areas.CoreSystem.Controllers
{
    [Area("CoreSystem")]
    public class AuditLogsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
