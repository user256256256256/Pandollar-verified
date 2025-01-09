using PANDOLLAR.Data;
using PANDOLLAR.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace PANDOLLAR.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ForgotPasswordAPIController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IErrorCodeService _errorCodeService;

        public ForgotPasswordAPIController(UserManager<IdentityUser> userManager, IEmailSender emailSender, IErrorCodeService errorCodeService)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _errorCodeService = errorCodeService;
        }

        [HttpGet]
        public async Task<IActionResult> SendRecoveryToken(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var errorDetails = _errorCodeService.GetErrorDetails("USER_NOT_FOUND");
                return Json(new { success = true, mresponse = errorDetails.ErrorMessage });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ConfirmPasswordRecoveryToken", "ForgotPasswordAPI", new { token, email = user.Email }, protocol: Request.Scheme);

            Console.WriteLine(callbackUrl);

            try
            {
                await _emailSender.SendEmailAsync(email, "Reset Password", $"Please reset your password by <a href='{callbackUrl}'>clicking here</a>.");
            }
            catch (Exception ex)
            {
                var emailErrorDetails = _errorCodeService.GetErrorDetails("EMAIL_SEND_ERROR");
                Console.WriteLine($"Error sending email to {email}: {ex.Message}");
                return Json(new { success = false, mresponse = emailErrorDetails.ErrorMessage });
            }

            return Json(new { success = true, mresponse = "Token sent successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmPasswordRecoveryToken(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                var invalidRequestDetails = _errorCodeService.GetErrorDetails("INVALID_REQUEST");
                return RedirectToAction("Error", "Home", new { message = invalidRequestDetails.ErrorMessage });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var userNotFoundDetails = _errorCodeService.GetErrorDetails("USER_NOT_FOUND");
                return RedirectToAction("Error", "Home", new { message = userNotFoundDetails.ErrorMessage });
            }

            TempData["Token"] = token;
            TempData["Email"] = email;

            return RedirectToAction("ResetPassword", "ForgotPassword");
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email, string newPassword)
        {
            // Debugging line to check if token and email are received correctly
            Console.WriteLine($"ResetPassword called with token: {token}, email: {email}");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                var invalidRequestDetails = _errorCodeService.GetErrorDetails("INVALID_REQUEST");
                Console.WriteLine("Invalid request: token or email is null or empty.");
                return RedirectToAction("Error", "Home", new { message = invalidRequestDetails.ErrorMessage });
            }

            // Check if user exists
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var userNotFoundDetails = _errorCodeService.GetErrorDetails("USER_NOT_FOUND");
                Console.WriteLine($"User not found for email: {email}");
                return RedirectToAction("Error", "Home", new { message = userNotFoundDetails.ErrorMessage });
            }

            // Attempt to reset password
            Console.WriteLine($"Attempting to reset password for user: {email}");
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                Console.WriteLine("Password reset succeeded.");
                return Json(new { success = true, mresponse = "Password updated successfully." });
            }

            // If reset failed
            var passwordResetFailedDetails = _errorCodeService.GetErrorDetails("PASSWORD_RESET_FAILED");
            Console.WriteLine("Password reset failed.");
            return Json(new { success = false, mresponse = passwordResetFailedDetails.ErrorMessage });
        }
    }


}
