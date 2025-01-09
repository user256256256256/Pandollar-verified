using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using PANDOLLAR.Data;
using PANDOLLAR.Areas.CoreSystem.Models;
using Microsoft.Data.SqlClient;

namespace PANDOLLAR.Controllers
{
    [Route("api/[controller]/[action]")]
    public class TrialNotificationsAPIController : Controller
    {
        private PandollarDbContext  _context;

        public TrialNotificationsAPIController(PandollarDbContext  context) {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var trialnotifications = _context.TrialNotifications.Select(i => new
                {
                    i.Id,
                    i.CompanyId,
                    i.TrialStartDate,
                    i.TrialEndDate,
                    i.IsNotified,
                    i.ReminderDate,
                    i.SentAt,
                    i.NotificationTypeId,
                    Company = new
                    {
                        i.Company.CompanyName,
                        i.Company.CompanyEmail,
                        i.Company.CompanyPhone
                    },
                    NotificationType = new
                    {
                        i.NotificationType.Type,
                        i.NotificationType.Message
                    }
                });

                // Debug the query before applying DataSourceLoader
                Console.WriteLine("Trial notification query built. Applying DataSourceLoader...");

                // Apply filtering, sorting, and paging using DataSourceLoader
                var transformedData = await DataSourceLoader.LoadAsync(trialnotifications, loadOptions);

                // Serialize the retrieved object and log it
                var serializedData = JsonConvert.SerializeObject(transformedData, Formatting.Indented);
                Console.WriteLine($"Data transformation completed. Retrieved data: {serializedData}");

                // Return the transformed data as JSON
                return Json(transformedData);
            }
            catch (SqlException ex)
            {
                // Log the exception
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "A database error occurred. Please try again later." });
            }
            catch (InvalidOperationException ex)
            {
                // Log the exception
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "An internal server error occurred. Please try again later." });
            }
            catch (Exception ex)
            {
                // Log the exception message and stack trace for debugging purposes
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later.", error = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Post(string values)
        {
            try
            {
                var model = new TrialNotification();
                var valuesDict = JsonConvert.DeserializeObject<IDictionary>(values);
                PopulateModel(model, valuesDict);

                if (!TryValidateModel(model))
                    return BadRequest(GetFullErrorMessage(ModelState));

                var result = _context.TrialNotifications.Add(model);
                await _context.SaveChangesAsync();

                return Json(new { result.Entity.Id });
            }
            catch (JsonSerializationException ex)
            {
                // Log the exception
                Console.WriteLine($"JSON serialization error: {ex.Message}");
                return BadRequest(new { message = "Invalid input format. Please check your data and try again." });
            }
            catch (SqlException ex)
            {
                // Log the exception
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "A database error occurred. Please try again later." });
            }
            catch (InvalidOperationException ex)
            {
                // Log the exception
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "An internal server error occurred. Please try again later." });
            }
            catch (Exception ex)
            {
                // Log the exception message and stack trace for debugging purposes
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later.", error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Put(Guid key, string values)
        {
            try
            {
                // Retrieve the trial notification by its unique identifier
                var model = await _context.TrialNotifications.FirstOrDefaultAsync(item => item.Id == key);
                if (model == null)
                {
                    Console.WriteLine($"Trial notification not found with key: {key}");
                    return StatusCode(409, "Object not found");
                }

                Console.WriteLine($"Trial notification found with key: {key}, proceeding with updates.");

                // Deserialize the incoming updated values
                var valuesDict = JsonConvert.DeserializeObject<IDictionary>(values);
                PopulateModel(model, valuesDict);

                // Validate the updated model before saving
                if (!TryValidateModel(model))
                {
                    Console.WriteLine("Model validation failed.");
                    return BadRequest(GetFullErrorMessage(ModelState));
                }

                Console.WriteLine("Model validated successfully.");

                try
                {
                    // Save the changes to the database
                    await _context.SaveChangesAsync();
                    Console.WriteLine("Trial notification updated successfully in the database.");
                    return Ok();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Handle the concurrency exception
                    Console.WriteLine("Concurrency exception occurred while updating trial notification.");
                    var entry = ex.Entries.Single();
                    var databaseValues = entry.GetDatabaseValues();
                    if (databaseValues == null)
                    {
                        Console.WriteLine("The record you attempted to edit was deleted by another user.");
                        return NotFound(new { success = false, message = "The record you attempted to edit was deleted by another user." });
                    }
                    else
                    {
                        var dbValues = (TrialNotification)databaseValues.ToObject();
                        Console.WriteLine("The record you attempted to edit was modified by another user.");

                        // Optionally, reload the entity with current database values
                        await entry.ReloadAsync();
                        return Conflict(new { success = false, message = "The record you attempted to edit was modified by another user.", currentValues = dbValues });
                    }
                }
            }
            catch (JsonSerializationException ex)
            {
                // Log the exception
                Console.WriteLine($"JSON serialization error: {ex.Message}");
                return BadRequest(new { message = "Invalid input format. Please check your data and try again." });
            }
            catch (SqlException ex)
            {
                // Log the exception (consider using a logging framework)
                Console.WriteLine(ex);  // Replace with your logging mechanism
                return StatusCode(500, new { message = "A database error occurred. Please try again later." });
            }
            catch (InvalidOperationException ex)
            {
                // Log the exception
                Console.WriteLine(ex);  // Replace with your logging mechanism
                return StatusCode(500, new { message = "An internal server error occurred. Please try again later." });
            }
            catch (Exception ex)
            {
                // Log the exception message and stack trace for debugging purposes
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Return a standardized error response
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later.", error = ex.Message });
            }
        }


        [HttpDelete]
        public async Task<IActionResult> Delete(Guid key)
        {
            try
            {
                var model = await _context.TrialNotifications.FirstOrDefaultAsync(item => item.Id == key);

                if (model == null)
                {
                    Console.WriteLine($"Trial notification not found with key: {key}");
                    return NotFound($"Trial notification with ID {key} not found.");
                }

                _context.TrialNotifications.Remove(model);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Successfully deleted Trial notification with ID: {key}");
                return NoContent();
            }
            catch (SqlException ex)
            {
                // Log the exception
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "A database error occurred. Please try again later." });
            }
            catch (InvalidOperationException ex)
            {
                // Log the exception
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "An internal server error occurred. Please try again later." });
            }
            catch (Exception ex)
            {
                // Log the exception message and stack trace for debugging purposes
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later.", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CompaniesLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = from i in _context.Companies
                             orderby i.CompanyName
                             select new
                             {
                                 Value = i.CompanyId,
                                 Text = i.CompanyName
                             };
                return Json(await DataSourceLoader.LoadAsync(lookup, loadOptions));
            }
            catch (SqlException ex)
            {
                // Log the exception
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "A database error occurred. Please try again later." });
            }
            catch (InvalidOperationException ex)
            {
                // Log the exception
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "An internal server error occurred. Please try again later." });
            }
            catch (Exception ex)
            {
                // Log the exception message and stack trace for debugging purposes
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later.", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TrialNotificationLookupsLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = from i in _context.TrialNotificationLookups
                             orderby i.Type
                             select new
                             {
                                 Value = i.Id,
                                 Text = i.Type
                             };
                return Json(await DataSourceLoader.LoadAsync(lookup, loadOptions));
            }
            catch (SqlException ex)
            {
                // Log the exception
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "A database error occurred. Please try again later." });
            }
            catch (InvalidOperationException ex)
            {
                // Log the exception
                Console.WriteLine(ex);
                return StatusCode(500, new { message = "An internal server error occurred. Please try again later." });
            }
            catch (Exception ex)
            {
                // Log the exception message and stack trace for debugging purposes
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later.", error = ex.Message });
            }
        }


        private void PopulateModel(TrialNotification model, IDictionary values) {
            string ID = nameof(TrialNotification.Id);
            string COMPANY_ID = nameof(TrialNotification.CompanyId);
            string TRIAL_START_DATE = nameof(TrialNotification.TrialStartDate);
            string TRIAL_END_DATE = nameof(TrialNotification.TrialEndDate);
            string IS_NOTIFIED = nameof(TrialNotification.IsNotified);
            string REMINDER_DATE = nameof(TrialNotification.ReminderDate);
            string SENT_AT = nameof(TrialNotification.SentAt);
            string NOTIFICATION_TYPE_ID = nameof(TrialNotification.NotificationTypeId);

            if(values.Contains(ID)) {
                model.Id = ConvertTo<System.Guid>(values[ID]);
            }

            if(values.Contains(COMPANY_ID)) {
                model.CompanyId = values[COMPANY_ID] != null ? ConvertTo<System.Guid>(values[COMPANY_ID]) : (Guid?)null;
            }

            if(values.Contains(TRIAL_START_DATE)) {
                model.TrialStartDate = Convert.ToDateTime(values[TRIAL_START_DATE]);
            }

            if(values.Contains(TRIAL_END_DATE)) {
                model.TrialEndDate = Convert.ToDateTime(values[TRIAL_END_DATE]);
            }

            if(values.Contains(IS_NOTIFIED)) {
                model.IsNotified = Convert.ToBoolean(values[IS_NOTIFIED]);
            }

            if(values.Contains(REMINDER_DATE)) {
                model.ReminderDate = values[REMINDER_DATE] != null ? Convert.ToDateTime(values[REMINDER_DATE]) : (DateTime?)null;
            }

            if(values.Contains(SENT_AT)) {
                model.SentAt = values[SENT_AT] != null ? Convert.ToDateTime(values[SENT_AT]) : (DateTime?)null;
            }

            if(values.Contains(NOTIFICATION_TYPE_ID)) {
                model.NotificationTypeId = Convert.ToInt32(values[NOTIFICATION_TYPE_ID]);
            }
        }

        private T ConvertTo<T>(object value) {
            var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
            if(converter != null) {
                return (T)converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
            } else {
                // If necessary, implement a type conversion here
                throw new NotImplementedException();
            }
        }

        private string GetFullErrorMessage(ModelStateDictionary modelState) {
            var messages = new List<string>();

            foreach(var entry in modelState) {
                foreach(var error in entry.Value.Errors)
                    messages.Add(error.ErrorMessage);
            }

            return String.Join(" ", messages);
        }
    }
}