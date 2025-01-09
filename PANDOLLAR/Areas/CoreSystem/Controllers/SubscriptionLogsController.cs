using PANDOLLAR.Data;
using PANDOLLAR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PANDOLLAR.Areas.CoreSystem.Controllers
{
    [Area("CoreSystem")]
    [Route("CoreSystem/[controller]/[action]/{userId?}")]
    public class SubscriptionLogsController : Controller
    {
        private readonly PandollarDbContext  _dbContext;

        public SubscriptionLogsController(PandollarDbContext  dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: Subscription logs associated with a specific user
        public async Task<IActionResult> Index(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required.");
            }

            try
            {
                var decodedUserId = HashingHelper.DecodeString(userId);

                var user = await _dbContext.AspNetUsers.Where(c => c.Id == decodedUserId)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound();
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
