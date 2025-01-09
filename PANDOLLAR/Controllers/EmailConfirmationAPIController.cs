using PANDOLLAR.Data;
using PANDOLLAR.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PANDOLLAR.Controllers
{
    public class EmailConfirmationAPIController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public EmailConfirmationAPIController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Action to handle email confirmation
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Error", "Home", new { message = "Invalid email confirmation request." });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return RedirectToAction("Error", "Home", new { message = "User not found." });
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "EmailConfirmation");
            }

            return RedirectToAction("Error", "Home", new { message = "Email confirmation failed." });
        }
    }
}
