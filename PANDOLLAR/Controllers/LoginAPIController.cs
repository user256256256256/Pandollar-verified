using PANDOLLAR.Areas.CoreSystem.Models;
using PANDOLLAR.Data;
using PANDOLLAR.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;


[ApiController]
[Route("api/[controller]/[action]")]
public class LoginAPIController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly RoleRedirectService _roleRedirectService;
	private readonly IEmailSender _emailSender;
    private readonly IErrorCodeService _errorCodeService;

	public LoginAPIController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ApplicationDbContext dbContext, RoleRedirectService roleRedirectService, IEmailSender emailSender, IErrorCodeService errorCodeService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _roleRedirectService = roleRedirectService;
		_emailSender = emailSender;
        _errorCodeService = errorCodeService;

	}


    [HttpGet]
    public async Task<ActionResult> LoginCheck(string email, string password)
    {
        // Validate the input parameters
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            var errorDetails = _errorCodeService.GetErrorDetails("MISSING_CREDENTIALS");
            Console.WriteLine($"Login attempt failed: {errorDetails.ErrorMessage}");
            return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
        }

        Console.WriteLine($"Login attempt started for email: {email}");

        try
        {
            // Try to find the user by email
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var errorDetails = _errorCodeService.GetErrorDetails("USER_NOT_FOUND");
                Console.WriteLine($"User not found for email: {email}");
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }

            // Check if the user's email is confirmed
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                Console.WriteLine($"Email not confirmed for user: {email}");

                // Generate email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Generate confirmation link for EmailConfirmationController
                var confirmationLink = Url.Action("ConfirmEmail", "EmailConfirmationAPI", new { token, email = user.Email }, Request.Scheme);

                // Send confirmation email
                await _emailSender.SendEmailAsync(user.Email, "Confirm your email", $"Please confirm your email by clicking on this link: {confirmationLink}");

                Console.WriteLine($"Confirmation email sent to: {email}");
                Console.WriteLine($"Confirmation link is: {confirmationLink}");

                var errorDetails = _errorCodeService.GetErrorDetails("EMAIL_NOT_CONFIRMED");
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }

            Console.WriteLine($"User found for email: {email}");

            // Attempt to sign in with the provided password
            var result = await _signInManager.PasswordSignInAsync(user, password, false, lockoutOnFailure: true);

            // Log the result of the login attempt
            Console.WriteLine($"PasswordSignInAsync result: RequiresTwoFactor = {result.RequiresTwoFactor}, Succeeded = {result.Succeeded}, IsLockedOut = {result.IsLockedOut}");

            string encodedUserId = null;
            string redirectUrl = null;

            // Handle users who require two-factor authentication
            if (result.RequiresTwoFactor)
            {
                Console.WriteLine("Two-factor authentication is required.");

                // Check if the TwoFactorRememberBrowser cookie is present
                if (await _signInManager.IsTwoFactorClientRememberedAsync(user))
                {
                    // Use RoleRedirectService to handle role-based redirection
                    var roleRedirectResult = await _roleRedirectService.HandleRoleRedirectAsync(email);
                    return roleRedirectResult;
                }

                // Redirect to 2FA if the cookie is not present
                encodedUserId = HashingHelper.EncodeString(user.Id);
                redirectUrl = $"/TwoFA/Index/{encodedUserId}";
                return Json(new { success = true, mresponse = "Login successful", redirectUrl });
            }
            else if (result.Succeeded)
            {
                // Use RoleRedirectService to handle role-based redirection
                var roleRedirectResult = await _roleRedirectService.HandleRoleRedirectAsync(email);
                return roleRedirectResult;
            }
            else if (result.IsLockedOut)
            {
                Console.WriteLine($"Account is locked out for email: {email} at {DateTime.Now}");

                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var lockoutMessage = lockoutEnd.HasValue
                    ? $"Account is locked out until {lockoutEnd.Value.LocalDateTime}"
                    : "Account is locked out";

                Console.WriteLine(lockoutMessage);

                await _emailSender.SendEmailAsync(user.Email, "Account Locked Out", lockoutMessage);

                var errorDetails = _errorCodeService.GetErrorDetails("ACCOUNT_LOCKED");
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }
            else
            {
                var errorDetails = _errorCodeService.GetErrorDetails("INVALID_LOGIN_ATTEMPT");
                return Json(new { success = false, mresponse = errorDetails.ErrorMessage });
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"SQL Error: {ex.Message}");

            var errorDetails = _errorCodeService.GetErrorDetails("DB_CONNECTION_FAILED");
            return RedirectToAction("Error", "Home", new { message = errorDetails.ErrorMessage });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");

            var errorDetails = _errorCodeService.GetErrorDetails("UNKNOWN_ERROR");
            return RedirectToAction("Error", "Home", new { message = errorDetails.ErrorMessage });
        }
    }

}

