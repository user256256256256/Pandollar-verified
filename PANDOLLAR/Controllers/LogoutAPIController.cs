using PANDOLLAR.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("api/[controller]/[action]")]
public class LogoutAPIController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IErrorCodeService _errorCodeService;

    public LogoutAPIController(SignInManager<IdentityUser> signInManager, IErrorCodeService errorCodeService)
    {
        _signInManager = signInManager;
        _errorCodeService = errorCodeService;
    }

    [HttpPost]
    public IActionResult LogoutCheck([FromBody] string userId)
    {
        try
        {
            // Check if userId is null or empty
            if (string.IsNullOrEmpty(userId))
            {
                var errorDetails = _errorCodeService.GetErrorDetails("INVALID_USER_ID");
                Console.WriteLine("userId is null or empty");
                return Json(new { success = false, message = errorDetails.ErrorMessage });
            }

            // Optionally decode the userId if needed
            var decodedUserId = HashingHelper.DecodeString(userId);
            if (decodedUserId == null)
            {
                var errorDetails = _errorCodeService.GetErrorDetails("INVALID_USER_ID_AFTER_DECODING");
                Console.WriteLine("Decoded userId is null");
                return Json(new { success = false, message = errorDetails.ErrorMessage });
            }

            Console.WriteLine("The decoded user id is :" + decodedUserId);

            // Log out successful
            return Json(new { success = true, message = "Logout successful.", redirectUrl = "/" });
        }
        catch (Exception ex)
        {
            var errorDetails = _errorCodeService.GetErrorDetails("LOGOUT_FAILED");
            Console.WriteLine($"Logout failed: {ex.ToString()}");
            return Json(new { success = false, message = errorDetails.ErrorMessage });
        }
    }
}


