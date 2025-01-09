using Microsoft.AspNetCore.Mvc;
using NuGet.Common;

namespace PANDOLLAR.Controllers
{
    public class ForgotPasswordController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ResetPassword()
        {
            // Read from TempData
            var token = TempData["Token"] as string;
            var email = TempData["Email"] as string;

            // Check if TempData has the expected values
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                // Handle the case where token or email is not available
                return RedirectToAction("Error", "Home", new { message = "Invalid password recovery request." });
            }

            // Pass the token and email to the view via ViewData
            ViewData["Token"] = token;
            ViewData["Email"] = email;

            return View();
        }

    }
}
