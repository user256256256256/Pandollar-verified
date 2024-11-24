using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PANDOLLAR.Areas.CoreSystem.Models;


[ApiController]
[Route("api/[controller]/[action]")]
public class LoginAPIController : ControllerBase
{
	private readonly UserManager<IdentityUser> _userManager;
	private readonly SignInManager<IdentityUser> _signInManager;
	private readonly PandollarDbContext _dbContext;

	public LoginAPIController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, PandollarDbContext dbContext)
	{
		_userManager = userManager;
		_signInManager = signInManager;
		_dbContext = dbContext;
	}

	[HttpGet]
	public async Task<ActionResult> LoginCheck(string email, string password)
	{
		// Find user by email
		var user = await _userManager.FindByEmailAsync(email);
		if (user == null)
		{
			return BadRequest(new { success = false, mresponse = "User not found" });
		}

		// Attempt login
		var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);

		if (result.Succeeded)
		{
			// Get the roles of the user
			var roles = await _userManager.GetRolesAsync(user);
			if (roles.Contains("System Administrator"))
			{
				// Redirect to "CoreSystem/SystemManager" if the user is a "System Administrator"
				return Ok(new { success = true, mresponse = "Login successful", redirectUrl = "/CoreSystem/SystemManager" });
			}
			else
			{
				// If not a "System Administrator", return a success message but no redirection
				return Ok(new { success = true, mresponse = "Login successful" });
			}
		}

		// Return failure response for invalid login attempt
		return BadRequest(new { success = false, mresponse = "Invalid login attempt" });
	}
}
