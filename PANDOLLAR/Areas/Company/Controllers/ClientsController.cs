using Microsoft.AspNetCore.Mvc;
using PANDOLLAR.Data;  // Assuming this namespace contains the DbContext
using Microsoft.EntityFrameworkCore;
using PANDOLLAR.Services; // Assuming this has any helper methods for decoding or processing IDs, if needed

namespace PANDOLLAR.Areas.Company.Controllers
{
    [Area("Company")]
    [Route("Company/[controller]/[action]/{userId?}/{companyId?}")]
    public class ClientsController : Controller
    {
        private readonly PandollarDbContext _dbContext;

        // Constructor to inject the DbContext
        public ClientsController(PandollarDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(string userId, string companyId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId))
            {
                return BadRequest("User ID and valid Company ID are required.");
            }

            try
            {
                // Decode the userId from the URL
                var decodedUserId = HashingHelper.DecodeString(userId);

                var decodedCompanyId = HashingHelper.DecodeGuidID(companyId);

                // Retrieve the user using the decodedUserId from the db
                var user = await _dbContext.AspNetUsers
                                           .Where(c => c.Id == decodedUserId)
                                           .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(); // Return a 404 if the user is not found
                }

                // Pass the user model and companyId to the view, which will be available in the layout
                ViewData["CompanyId"] = decodedCompanyId;
                // Pass the user model to the view, which will be available in the layout
                return View(user);
            }
            catch (FormatException)
            {
                // Handle invalid Base64 string
                return BadRequest("Invalid User ID format.");
            }
        }
    }
}
