using Microsoft.AspNetCore.Mvc;

namespace PANDOLLAR.Controllers
{
    //See ways to handel view exceptions during error logging and testing
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error(string message) 
        {
            ViewData["ErrorMessage"] = message; 
            return View(); 
        }
    }
}
