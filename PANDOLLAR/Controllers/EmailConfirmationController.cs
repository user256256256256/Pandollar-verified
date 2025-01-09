using Microsoft.AspNetCore.Mvc;

namespace PANDOLLAR.Controllers
{
    public class EmailConfirmationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
