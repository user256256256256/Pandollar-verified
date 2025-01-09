using PANDOLLAR.Data;
using PANDOLLAR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PANDOLLAR.Areas.CoreSystem.Controllers
{
    [Area("CoreSystem")]
    [Route("CoreSystem/[controller]/[action]")]
    public class SubscriptionPlansController : Controller
    {
        private readonly PandollarDbContext  _dbContext;

        public SubscriptionPlansController(PandollarDbContext  dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: Payments associated with a specific user
        public async Task<IActionResult> Index(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required.");
            }

            try
            {
                // Decode the userId from the URL
                var decodedUserId = HashingHelper.DecodeString(userId);

                // Retrieve the user using the decodedUserId from the db
                var user = await _dbContext.AspNetUsers.Where(c => c.Id == decodedUserId)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(); // Return a 404 if the user is not found
                }


                return View(user);
            }
            catch (FormatException)
            {
                return BadRequest("Invalid User ID format.");
            }
        }

    }
}
