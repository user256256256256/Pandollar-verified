using PANDOLLAR.Areas.CoreSystem.Models;
using PANDOLLAR.Data;
using PANDOLLAR.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace PANDOLLAR.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class TwoFAAPIController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly PandollarDbContext _dbContext;
        private readonly RoleRedirectService _roleRedirectService;
        private readonly IErrorCodeService _errorCodeService;

        public TwoFAAPIController(UserManager<IdentityUser> userManager,
                        SignInManager<IdentityUser> signInManager,
                        IEmailSender emailSender,
                        ILogger<TwoFAAPIController> logger, PandollarDbContext dbContext, RoleRedirectService roleRedirectService, IErrorCodeService errorCodeService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _dbContext = dbContext;
            _roleRedirectService = roleRedirectService;
            _errorCodeService = errorCodeService;
        }

        // Action to send 2FA code via GET request
        [HttpGet]
        public async Task<ActionResult> SendCode(string userId, string method)
        {
            Console.WriteLine("Entered SendCode method");
            Console.WriteLine($"User ID: {userId}, Method: {method}");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                var errorDetails = _errorCodeService.GetErrorDetails("USER_NOT_FOUND");
                Console.WriteLine(errorDetails.ErrorMessage);
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }

            var twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!twoFactorEnabled)
            {
                var errorDetails = _errorCodeService.GetErrorDetails("TWO_FACTOR_NOT_ENABLED");
                Console.WriteLine(errorDetails.ErrorMessage);
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }

            var providers = await _userManager.GetValidTwoFactorProvidersAsync(user);
            if (!providers.Contains("Email"))
            {
                var errorDetails = _errorCodeService.GetErrorDetails("NO_VALID_2FA_PROVIDERS");
                Console.WriteLine(errorDetails.ErrorMessage);
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }

            string code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
            Console.WriteLine($"Generated 2FA code: {code}");

            if (string.IsNullOrEmpty(user.Email) || !IsValidEmail(user.Email))
            {
                var errorDetails = _errorCodeService.GetErrorDetails("INVALID_EMAIL_ADDRESS");
                Console.WriteLine(errorDetails.ErrorMessage);
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }

            await _emailSender.SendEmailAsync(user.Email, "Your Security Code", $"Your security code is: {code}");
            Console.WriteLine($"Email sent to: {user.Email}");

            return Json(new { success = true, mresponse = "Code sent successfully to your email. Expiring in 5 minutes." });
        }

        // Helper method to validate email address format
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet]
        public async Task<ActionResult> VerifyCode(string userId, string provider, string code, bool rememberMe)
        {
            // If remember me is checked, remember browser is set to true
            var rememberBrowser = rememberMe;

            Console.WriteLine("Entered VerifyCode method");
            Console.WriteLine($"UserId: {userId}, Provider: {provider}, Code: {code}");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                var errorDetails = _errorCodeService.GetErrorDetails("USER_NOT_FOUND");
                Console.WriteLine(errorDetails.ErrorMessage);
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }

            var providers = await _userManager.GetValidTwoFactorProvidersAsync(user);
            Console.WriteLine($"Valid 2FA providers: {string.Join(", ", providers)}");

            string normalizedProvider = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(provider.ToLower());
            Console.WriteLine($"Normalized Provider: {normalizedProvider}");

            if (!providers.Contains(normalizedProvider))
            {
                var errorDetails = _errorCodeService.GetErrorDetails("INVALID_2FA_PROVIDER");
                Console.WriteLine($"Invalid 2FA provider. Valid providers: {string.Join(", ", providers)}");
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }

            var result = await _signInManager.TwoFactorSignInAsync(normalizedProvider, code, rememberMe, rememberBrowser);

            if (result.Succeeded)
            {
                // Use RoleRedirectService to handle role-based redirection
                var roleRedirectResult = await _roleRedirectService.HandleRoleRedirectAsync(user.Email);
                return roleRedirectResult;  // Return the redirect result
            }
            else if (result.IsLockedOut)
            {
                var errorDetails = _errorCodeService.GetErrorDetails("USER_LOCKED_OUT");
                Console.WriteLine(errorDetails.ErrorMessage);
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }
            else if (result.IsNotAllowed)
            {
                var errorDetails = _errorCodeService.GetErrorDetails("SIGN_IN_NOT_ALLOWED");
                Console.WriteLine(errorDetails.ErrorMessage);
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }
            else
            {
                var errorDetails = _errorCodeService.GetErrorDetails("AUTHENTICATION_FAILED");
                Console.WriteLine(errorDetails.ErrorMessage);
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }
        }


    }
}



