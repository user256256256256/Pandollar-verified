using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using PANDOLLAR.Hubs;
using System.Linq;
using System.Threading.Tasks;
using PANDOLLAR.Data;
using Microsoft.EntityFrameworkCore;

namespace PANDOLLAR.Services
{
    public class NotificationService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly PandollarDbContext _dbContext;

        public NotificationService(UserManager<IdentityUser> userManager, IHubContext<NotificationHub> hubContext, PandollarDbContext dbContext)
        {
            _userManager = userManager;
            _hubContext = hubContext;
            _dbContext = dbContext;
        }

        private async Task<bool> IsUserSystemAdministrator(string userEmail)
        {
            var user = await _userManager.FindByEmailAsync(userEmail);
            var roles = await _userManager.GetRolesAsync(user);
            bool isAdmin = roles.Contains("System Administrator");
            Console.WriteLine($"[NotificationService] IsUserSystemAdministrator - User: {userEmail}, IsAdmin: {isAdmin}");
            return isAdmin;
        }

        public async Task NotifySystemAdministratorCreation(string userEmail)
        {
            Console.WriteLine($"[NotificationService] NotifyNewAccountCreation called for {userEmail}");
            if (await IsUserSystemAdministrator(userEmail))
            {
                var user = await _userManager.FindByEmailAsync(userEmail);
                var userName = user.UserName;
                Console.WriteLine($"[NotificationService] User is a System Administrator. Triggering notification.");
                await TriggerToastNotification("info", $"A new user account has been created. Username: {userName}");
            }
        }

        public async Task NotifySystemAdministratorDeletion(string userEmail)
        {
            Console.WriteLine($"[NotificationService] NotifySystemAdministratorDeletion called for {userEmail}");
            if (await IsUserSystemAdministrator(userEmail))
            {
                var user = await _userManager.FindByEmailAsync(userEmail);
                var userName = user.UserName;
                Console.WriteLine($"[NotificationService] User is a System Administrator. Triggering notification.");
                await TriggerToastNotification("info", $"A new user account has been deleted. Username: {userName}");
            }
        }

        public async Task NotifyNewCompanyCreation(Guid companyId)
        {
            var company = await _dbContext.Companies.Where(c => c.CompanyId == companyId).FirstOrDefaultAsync();
            var companyName = company.CompanyName;
            Console.WriteLine($"[NotificationService] User is a System Administrator. Triggering notification.");
            await TriggerToastNotification("info", $"{companyName} has joined PANDOLLAR.");
        }

        public async Task NotifyCompanyDeactivation(string userEmail, Guid companyId)
        {
            Console.WriteLine($"[NotificationService] NotifyAccountDeactivation called for {userEmail}");
            if (await IsUserSystemAdministrator(userEmail))
            {
                var company = await _dbContext.Companies.Where(c => c.CompanyId == companyId).FirstOrDefaultAsync();
                var companyName = company.CompanyName;
                Console.WriteLine($"[NotificationService] User is a System Administrator. Triggering notification.");
                await TriggerToastNotification("warning", $"{companyName} company has been deativated.");
            }
        }

        public async Task NotifyCompanyActivation(string userEmail, Guid companyId)
        {
            Console.WriteLine($"[NotificationService] NotifyAccountActivation called for {userEmail}");
            if (await IsUserSystemAdministrator(userEmail))
            {
                var company = await _dbContext.Companies.Where(c => c.CompanyId == companyId).FirstOrDefaultAsync();
                var companyName = company.CompanyName;
                Console.WriteLine($"[NotificationService] User is a System Administrator. Triggering notification.");
                await TriggerToastNotification("success", $"{companyName} has subscribed to the system.");
            }
        }

        public async Task NotifyCompanyDeletion(Guid companyId)
        {
            var company = await _dbContext.Companies.Where(c => c.CompanyId == companyId).FirstOrDefaultAsync();
            var companyName = company.CompanyName;
            Console.WriteLine($"[NotificationService] User is a System Administrator. Triggering notification.");
            await TriggerToastNotification("warning", $"{companyName} company has been deleted.");
        }

        public async Task NotifyDataImportSuccess(string userEmail, Guid companyId)
        {
            Console.WriteLine($"[NotificationService] NotifyDataImportSuccess called for {userEmail}");
            if (await IsUserSystemAdministrator(userEmail))
            {
                var company = await _dbContext.Companies.Where(c => c.CompanyId == companyId).FirstOrDefaultAsync();
                var companyName = company.CompanyName;
                Console.WriteLine($"[NotificationService] User is a System Administrator. Triggering notification.");
                await TriggerToastNotification("success", $"{companyName} data import completed successfully!");
            }
        }


        public async Task NotifyDataImportFailure(string userEmail, string errorDetails, Guid companyId)
        {
            Console.WriteLine($"[NotificationService] NotifyDataImportFailure called for {userEmail}. Error: {errorDetails}");
            if (await IsUserSystemAdministrator(userEmail))
            {
                var company = await _dbContext.Companies.Where(c => c.CompanyId == companyId).FirstOrDefaultAsync();
                var companyName = company.CompanyName;
                Console.WriteLine($"[NotificationService] User is a System Administrator. Triggering notification.");
                await TriggerToastNotification("error", $"{companyName} data import failed: {errorDetails}.");
            }
        }

        public async Task NotifySystemDowntime(string userEmail)
        {
            Console.WriteLine($"[NotificationService] NotifySystemDowntime called for {userEmail}");
            if (await IsUserSystemAdministrator(userEmail))
            {
                Console.WriteLine($"[NotificationService] User is a System Administrator. Triggering notification.");
                await TriggerToastNotification("warning", "Scheduled system maintenance will occur on [date/time]. Please save your work.");
            }
        }

        public async Task NotifySecurityAlert(string userEmail)
        {
            Console.WriteLine($"[NotificationService] NotifySecurityAlert called for {userEmail}");
            if (await IsUserSystemAdministrator(userEmail))
            {
                var user = await _userManager.FindByEmailAsync(userEmail);
                var userName = user.UserName;
                var Email = user.Email;

                Console.WriteLine($"[NotificationService] User is a System Administrator. Triggering notification.");
                await TriggerToastNotification("error", $"Security alert: Unauthorized access attempt detected. Name: {userName} Email: {Email}.");
            }
        }

        public async Task NotifyUnusualUserActivity(string userEmail)
        {
            Console.WriteLine($"[NotificationService] NotifyUnusualUserActivity called for {userEmail}");
            if (await IsUserSystemAdministrator(userEmail))
            {
                Console.WriteLine($"[NotificationService] User is a System Administrator. Triggering notification.");
                await TriggerToastNotification("warning", "Unusual user activity detected. Please investigate.");
            }
        }

        public async Task NotifyServiceUpdate(string userEmail)
        {
            Console.WriteLine($"[NotificationService] NotifyServiceUpdate called for {userEmail}");
            if (await IsUserSystemAdministrator(userEmail))
            {
                Console.WriteLine($"[NotificationService] User is a System Administrator. Triggering notification.");
                await TriggerToastNotification("info", "New health service has been added. Check it out in the services section.");
            }
        }

        private async Task TriggerToastNotification(string type, string message)
        {
            Console.WriteLine($"[NotificationService] Triggering toast notification - Type: {type}, Message: {message}");
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", type, message);
        }
    }
}
