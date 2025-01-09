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

namespace MedisatERP.Controllers
{
    [Route("api/[controller]/[action]")]
    public class PaymentsAPIController : Controller
    {
        private PandollarDbContext  _context;

        public PaymentsAPIController(PandollarDbContext  context) {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var payments = _context.Payments
                    .Include(c => c.Subscription)
                    .Select(i => new
                    {
                        i.Id,
                        i.SubscriptionId,
                        i.PaymentDate,
                        i.PaymentStatusId,
                        i.PaymentMethodId,
                        i.TransactionId,
                        i.IsRefunded,
                        PaymentMethod = new
                        {
                            i.PaymentMethod.Id,
                            i.PaymentMethod.Method
                        },
                        PaymentStatus = new
                        {
                            i.PaymentStatus.Id,
                            i.PaymentStatus.Status
                        },
                        Subscription = new
                        {
                            Company = new
                            {
                                i.Subscription.Company.CompanyId,
                                i.Subscription.Company.CompanyName,
                                i.Subscription.Company.CompanyPhone,
                                i.Subscription.Company.CompanyEmail
                            },
                            SubscriptionPlan = new
                            {
                                i.Subscription.SubscriptionPlan.Description,
                                i.Subscription.SubscriptionPlan.Duration,
                                PlanName = new
                                {
                                    i.Subscription.SubscriptionPlan.PlanName.Id,
                                    i.Subscription.SubscriptionPlan.PlanName.PlanName,
                                    i.Subscription.SubscriptionPlan.PlanName.Price
                                }
                            }
                        }
                    });

                // Debug the query before applying DataSourceLoader
                Console.WriteLine("Payments query built. Applying DataSourceLoader...");

                // Apply filtering, sorting, and paging using DataSourceLoader
                var transformedData = await DataSourceLoader.LoadAsync(payments, loadOptions);

                // Serialize the retrieved object and log it
                var serializedData = JsonConvert.SerializeObject(transformedData, Formatting.Indented);
                Console.WriteLine($"Data transformation completed. Retrieved data: {serializedData}");

                // Return the transformed data as JSON
                return Json(transformedData);
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


        [HttpGet]
        public async Task<IActionResult> GetPaymentsInfo(DataSourceLoadOptions loadOptions, Guid companyId)
        {
            try
            {
                // Log the input parameters for debugging
                Console.WriteLine($"Get method called with companyId: {companyId}");

                var payments = _context.Payments
                    .Include(c => c.Subscription)
                    .Select(i => new
                    {
                        i.Id,
                        i.SubscriptionId,
                        i.PaymentDate,
                        i.PaymentStatusId,
                        i.PaymentMethodId,
                        i.TransactionId,
                        i.IsRefunded,
                        PaymentMethod = new
                        {
                            i.PaymentMethod.Id,
                            i.PaymentMethod.Method
                        },
                        PaymentStatus = new
                        {
                            i.PaymentStatus.Id,
                            i.PaymentStatus.Status
                        },
                        Subscription = new
                        {
                            Company = new
                            {
                                i.Subscription.Company.CompanyId,
                                i.Subscription.Company.CompanyName,
                                i.Subscription.Company.CompanyPhone,
                                i.Subscription.Company.CompanyEmail
                            },
                            SubscriptionPlan = new
                            {
                                i.Subscription.SubscriptionPlan.Description,
                                i.Subscription.SubscriptionPlan.Duration,
                                PlanName = new
                                {
                                    i.Subscription.SubscriptionPlan.PlanName.Id,
                                    i.Subscription.SubscriptionPlan.PlanName.PlanName,
                                    i.Subscription.SubscriptionPlan.PlanName.Price,
                                }
                            }
                        }
                    })
                    .Where(a => a.Subscription.Company.CompanyId == companyId)
                    .OrderBy(a => a.Id);

                // Debug the query before applying DataSourceLoader
                Console.WriteLine("Payments query built. Applying DataSourceLoader...");

                // Apply filtering, sorting, and paging using DataSourceLoader
                var transformedData = await DataSourceLoader.LoadAsync(payments, loadOptions);

                // Serialize the retrieved object and log it
                var serializedData = JsonConvert.SerializeObject(transformedData, Formatting.Indented);
                Console.WriteLine($"Data transformation completed. Retrieved data: {serializedData}");

                // Return the transformed data as JSON
                return Json(transformedData);
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


        [HttpPost]
        public async Task<IActionResult> Post(string values)
        {
            try
            {
                var model = new Payment();
                var valuesDict = JsonConvert.DeserializeObject<IDictionary>(values);
                PopulateModel(model, valuesDict);

                if (!TryValidateModel(model))
                    return BadRequest(GetFullErrorMessage(ModelState));

                var result = _context.Payments.Add(model);
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


        [HttpPut]
        public async Task<IActionResult> Put(Guid key, string values)
        {
            try
            {
                // Retrieve the payment by its unique identifier
                var model = await _context.Payments.FirstOrDefaultAsync(item => item.Id == key);
                if (model == null)
                {
                    Console.WriteLine($"Payment entity not found with key: {key}");
                    return StatusCode(409, "Object not found");
                }

                Console.WriteLine($"Payment entity found with key: {key}, proceeding with updates.");

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
                    Console.WriteLine("Payment entity updated successfully in the database.");
                    return Ok();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Handle the concurrency exception
                    Console.WriteLine("Concurrency exception occurred while updating payment entity.");
                    var entry = ex.Entries.Single();
                    var databaseValues = entry.GetDatabaseValues();
                    if (databaseValues == null)
                    {
                        Console.WriteLine("The record you attempted to edit was deleted by another user.");
                        return NotFound(new { success = false, message = "The record you attempted to edit was deleted by another user." });
                    }
                    else
                    {
                        var dbValues = (Payment)databaseValues.ToObject();
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
                var model = await _context.Payments.FirstOrDefaultAsync(item => item.Id == key);

                if (model == null)
                {
                    Console.WriteLine($"Payment entity not found with key: {key}");
                    return NotFound($"Payment with ID {key} not found.");
                }

                _context.Payments.Remove(model);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Successfully deleted Payment with ID: {key}");
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
        public async Task<IActionResult> PaymentMethodLookupsLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = from i in _context.PaymentMethodLookups
                             orderby i.Method
                             select new
                             {
                                 Value = i.Id,
                                 Text = i.Method
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
        public async Task<IActionResult> PaymentStatusLookupsLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = from i in _context.PaymentStatusLookups
                             orderby i.Status
                             select new
                             {
                                 Value = i.Id,
                                 Text = i.Status
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
        public async Task<IActionResult> SubscriptionsLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = from i in _context.Subscriptions
                             orderby i.PaymentStatus
                             select new
                             {
                                 Value = i.Id,
                                 Text = i.PaymentStatus
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


        private void PopulateModel(Payment model, IDictionary values) {
            string ID = nameof(Payment.Id);
            string SUBSCRIPTION_ID = nameof(Payment.SubscriptionId);
            string PAYMENT_DATE = nameof(Payment.PaymentDate);
            string PAYMENT_STATUS_ID = nameof(Payment.PaymentStatusId);
            string PAYMENT_METHOD_ID = nameof(Payment.PaymentMethodId);
            string TRANSACTION_ID = nameof(Payment.TransactionId);
            string IS_REFUNDED = nameof(Payment.IsRefunded);

            if(values.Contains(ID)) {
                model.Id = ConvertTo<System.Guid>(values[ID]);
            }

            if(values.Contains(SUBSCRIPTION_ID)) {
                model.SubscriptionId = values[SUBSCRIPTION_ID] != null ? ConvertTo<System.Guid>(values[SUBSCRIPTION_ID]) : (Guid?)null;
            }

            if(values.Contains(PAYMENT_DATE)) {
                model.PaymentDate = Convert.ToDateTime(values[PAYMENT_DATE]);
            }

            if(values.Contains(PAYMENT_STATUS_ID)) {
                model.PaymentStatusId = Convert.ToInt32(values[PAYMENT_STATUS_ID]);
            }

            if(values.Contains(PAYMENT_METHOD_ID)) {
                model.PaymentMethodId = Convert.ToInt32(values[PAYMENT_METHOD_ID]);
            }

            if(values.Contains(TRANSACTION_ID)) {
                model.TransactionId = Convert.ToString(values[TRANSACTION_ID]);
            }

            if(values.Contains(IS_REFUNDED)) {
                model.IsRefunded = Convert.ToBoolean(values[IS_REFUNDED]);
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