using PANDOLLAR.Data;
using PANDOLLAR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedisatERP.Areas.CoreSystem.Controllers
{
    [Area("CoreSystem")]
    [Route("CoreSystem/[controller]/[action]/{userId?}")]
    public class SubscriptionManagerController : Controller
    {
        private readonly PandollarDbContext  _dbContext;

        // Constructor to inject DbContext
        public SubscriptionManagerController(PandollarDbContext  dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: Subscription Manager
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

                // Retrieve the subscription details for the user
                //var subscription = await _dbContext.Subscriptions
                //    .Where(s => s.UserId == decodedUserId)
                //    .FirstOrDefaultAsync();

                //// Retrieve any payments, logs, or related data for the user (if needed)
                //var payments = await _dbContext.Payments.Where(p => p.UserId == decodedUserId).ToListAsync();
                //var subscriptionLogs = await _dbContext.SubscriptionLogs.Where(s => s.UserId == decodedUserId).ToListAsync();

                //// Attach the relevant data to the user model (if required)
                //user.Subscription = subscription;
                //user.Payments = payments;
                //user.SubscriptionLogs = subscriptionLogs;

                // Return the user and their subscription details to the view
                return View(user);
            }
            catch (FormatException)
            {
                return BadRequest("Invalid User ID format.");
            }
        }
    }
}
