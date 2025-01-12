using PANDOLLAR.Areas.CoreSystem.Models;
using PANDOLLAR.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PANDOLLAR.Services;
using System.ComponentModel.Design;

namespace PANDOLLAR.Services
{
    // RoleRedirectService.cs
    public class RoleRedirectService : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly PandollarDbContext _dbContext;
        private readonly IErrorCodeService _errorCodeService;


        public RoleRedirectService(UserManager<IdentityUser> userManager, PandollarDbContext dbContext, IErrorCodeService errorCodeService)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _errorCodeService = errorCodeService;
        }

        public async Task<ActionResult> HandleRoleRedirectAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            // Retrieve user roles
            var roles = await _userManager.GetRolesAsync(user);
            Console.WriteLine($"Roles for user {email}: {string.Join(", ", roles)}");

            // Retrieve the custom AspNetUser data
            var aspNetUser = await _dbContext.Set<AspNetUser>().FirstOrDefaultAsync(u => u.Email == email);
            if (aspNetUser == null)
            {
                Console.WriteLine($"No custom AspNetUser found for email: {email}");
                return new BadRequestObjectResult(new { success = false, mresponse = "Invalid user data" });
            }

            var userId = aspNetUser.Id;
            var companyId = aspNetUser.CompanyId;
            Console.WriteLine($"AspNetUser found, UserId: {userId}");

            string redirectUrl = null;

            // Role-based redirection logic
            if (roles.Contains("System Administrator"))
            {
                Console.WriteLine("User is a System Administrator.");
                if (userId != null)
                {
                    var encodedUserId = HashingHelper.EncodeString(userId);
                    Console.WriteLine($"Encoded UserId: {encodedUserId}");
                    redirectUrl = $"/CoreSystem/SystemManager/Index/{encodedUserId}";
                }
            }
            else if (roles.Contains("Company Administrator"))
            {
                Console.WriteLine("User is a  Company Administrator.");

                if (userId != null)
                {
                    var encodedUserId = HashingHelper.EncodeString(userId);
                    var encodedCompanyId = HashingHelper.EncodeGuidID(companyId.Value);
                    Console.WriteLine($"Encoded UserId: {encodedUserId}");
                    redirectUrl = $"/Company/CompanySystem/Index/{encodedUserId}/{encodedCompanyId}";
                }
            }
            else
            {
                Console.WriteLine("User is neither a System Administrator nor a Nutrition Company Administrator.");
                return new OkObjectResult(new { success = true, mresponse = "Login successful" });
            }

            // Return the redirect URL if we constructed one
            if (redirectUrl != null)
            {
                return new OkObjectResult(new { success = true, mresponse = "Login successful", redirectUrl });
            }

            // If no redirect URL was found, fallback to successful login
            return new OkObjectResult(new { success = true, mresponse = "Login successful" });
        }

        public async Task<string> HandleSystemAdminToCompanyRedirection(string email, Guid companyId)
        {
            try
            {
                // Retrieve the user by email
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    Console.WriteLine($"User not found for email: {email}");
                    return null;
                }

                // Retrieve user roles
                var roles = await _userManager.GetRolesAsync(user);
                Console.WriteLine($"Roles for user {email}: {string.Join(", ", roles)}");

                // Retrieve the custom AspNetUser data
                var aspNetUser = await _dbContext.Set<AspNetUser>().FirstOrDefaultAsync(u => u.Email == email);
                if (aspNetUser == null)
                {
                    Console.WriteLine($"No custom AspNetUser found for email: {email}");
                    return null;
                }

                var userId = aspNetUser.Id;
                Console.WriteLine($"AspNetUser found, UserId: {userId}");

                string redirectUrl = null;
                if (roles.Contains("System Administrator") && roles.Contains("Company Administrator"))
                {
                    Console.WriteLine("User is a System Administrator and a Company Administrator.");
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var encodedUserId = HashingHelper.EncodeString(userId);
                        var encodedCompanyId = HashingHelper.EncodeGuidID(companyId);
                        Console.WriteLine($"Encoded UserId: {encodedUserId}");
                        redirectUrl = $"/Company/CompanySystem/Index/{encodedUserId}/{encodedCompanyId}";
                    }
                }

                if (!string.IsNullOrEmpty(redirectUrl))
                {
                    Console.WriteLine($"Redirecting to URL: {redirectUrl}");
                    return redirectUrl;
                }

                Console.WriteLine("No applicable role found for redirection.");
                return null;
            }
            catch (Exception ex)
            {
                // Handle and log the exception
                Console.WriteLine($"Error occurred while redirecting: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }


    }
}
