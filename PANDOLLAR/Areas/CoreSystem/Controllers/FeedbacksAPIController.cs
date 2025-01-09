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
using System.ComponentModel.Design;
using PANDOLLAR.Areas.CoreSystem.Models;
using PANDOLLAR.Data;
using Microsoft.Data.SqlClient;

namespace PANDOLLAR.Controllers
{
    [Route("api/[controller]/[action]")]
    public class FeedbacksAPIController : Controller
    {
        private PandollarDbContext  _context;

        public FeedbacksAPIController(PandollarDbContext  context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of feedbacks with their associated user basing on the selected company.
        /// Supports filtering, sorting, and pagination.
        /// </summary>
        /// <param name="loadOptions">The options for filtering, sorting, and paging data.</param>
        /// <returns>Returns the processed feedback data.</returns> 
        [HttpGet]
        public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions, Guid companyId)
        {
            try
            {
                var feedbacks = _context.Feedbacks
                   .Include(c => c.User) // Ensure ASP User is eagerly loaded
                   .Select(i => new
                   {
                       i.FeedbackId,
                       i.UserId,
                       i.FeedbackText,
                       i.Rating,
                       i.Category,
                       i.SubmittedAt,
                       i.Resolved,
                       i.CompanyId,
                       User = new
                       {
                           i.User.UserName,
                           i.User.Email,
                           i.User.PhoneNumber
                       }
                   }).Where(a => a.CompanyId == companyId).OrderBy(a => a.FeedbackId);

                // Apply filtering, sorting, and paging using DataSourceLoader
                var transformedData = await DataSourceLoader.LoadAsync(feedbacks, loadOptions);

                return Json(transformedData); // Return the processed data
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


        /// <summary>
        /// Adds a new feedback with the associated users info
        /// </summary>
        /// <param name="values">The incoming values as a JSON string.</param>
        /// <returns>Returns the Status OK  of the newly created company client.</returns>
        [HttpPost]
        public async Task<IActionResult> Post(string values)
        {
            try
            {
                // Deserialize the incoming request values
                var model = new Feedback();
                var valuesDict = JsonConvert.DeserializeObject<IDictionary>(values);
                PopulateModel(model, valuesDict);

                if (!TryValidateModel(model))
                    return BadRequest(GetFullErrorMessage(ModelState));

                var result = _context.Feedbacks.Add(model);
                await _context.SaveChangesAsync();

                return Ok();
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


        /// <summary>
        /// Updates an existing company and its address data.
        /// </summary>
        /// <param name="key">The unique identifier of the company to update.</param>
        /// <param name="values">The incoming updated values as a JSON string.</param>
        /// <returns>Returns a success status if the company is updated.</returns>
        [HttpPut]
        public async Task<IActionResult> Put(Guid key, string values)
        {
            try
            {
                // Retrieve the feedback by its unique identifier
                var model = await _context.Feedbacks.FirstOrDefaultAsync(item => item.FeedbackId == key);
                if (model == null)
                {
                    Console.WriteLine($"Feedback entity not found with key: {key}");
                    return StatusCode(404, "Feedback entity not found");
                }

                Console.WriteLine($"Feedback entity found with key: {key}, proceeding with updates.");

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
                    Console.WriteLine("Feedback entity updated successfully in the database.");
                    return Ok();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Handle the concurrency exception
                    Console.WriteLine("Concurrency exception occurred while updating feedback entity.");
                    var entry = ex.Entries.Single();
                    var databaseValues = entry.GetDatabaseValues();
                    if (databaseValues == null)
                    {
                        Console.WriteLine("The record you attempted to edit was deleted by another user.");
                        return NotFound(new { success = false, message = "The record you attempted to edit was deleted by another user." });
                    }
                    else
                    {
                        var dbValues = (Feedback)databaseValues.ToObject();
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



        /// <summary>
        /// Deletes a feedack  by its unique identifier.
        /// </summary>
        /// <param name="key">The unique identifier of the migration to delete.</param>
        /// <returns>Returns a status code indicating the result of the operation.</returns>
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid key)
        {
            try
            {
                // Log the entry point with the key being used for deletion
                Console.WriteLine($"Delete request received for Feedback with ID: {key}");

                // Retrieve the feedback to delete
                var model = await _context.Feedbacks.FirstOrDefaultAsync(item => item.FeedbackId == key);

                // Check if the model is null
                if (model == null)
                {
                    // Log that the model was not found
                    Console.WriteLine($"No Feedback found with ID: {key}");
                    // Return not found if the feedback does not exist
                    return NotFound($"Feedback with ID {key} not found.");
                }

                // Log the model that was found for deletion
                Console.WriteLine($"Found Feedback with ID: {key}");

                // Remove the feedback record
                _context.Feedbacks.Remove(model);

                // Log the removal of the feedback
                Console.WriteLine($"Removing Feedback with ID: {key}");

                // Save the changes to the database
                await _context.SaveChangesAsync();

                // Log successful deletion
                Console.WriteLine($"Successfully deleted Feedback with ID: {key}");

                return NoContent(); // Return No Content status after successful deletion
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



        /// <summary>
        /// Populates the model with the given values.
        /// </summary>
        private void PopulateModel(Feedback model, IDictionary values)
        {
            string FEEDBACK_ID = nameof(Feedback.FeedbackId);
            string USER_ID = nameof(Feedback.UserId);
            string FEEDBACK_TEXT = nameof(Feedback.FeedbackText);
            string RATING = nameof(Feedback.Rating);
            string CATEGORY = nameof(Feedback.Category);
            string SUBMITTED_AT = nameof(Feedback.SubmittedAt);
            string RESOLVED = nameof(Feedback.Resolved);

            if (values.Contains(FEEDBACK_ID))
            {
                model.FeedbackId = ConvertTo<System.Guid>(values[FEEDBACK_ID]);
            }

            if (values.Contains(USER_ID))
            {
                model.UserId = Convert.ToString(values[USER_ID]);
            }

            if (values.Contains(FEEDBACK_TEXT))
            {
                model.FeedbackText = Convert.ToString(values[FEEDBACK_TEXT]);
            }

            if (values.Contains(RATING))
            {
                model.Rating = values[RATING] != null ? Convert.ToInt32(values[RATING]) : (int?)null;
            }

            if (values.Contains(CATEGORY))
            {
                model.Category = Convert.ToString(values[CATEGORY]);
            }

            if (values.Contains(SUBMITTED_AT))
            {
                model.SubmittedAt = Convert.ToDateTime(values[SUBMITTED_AT]);
            }

            if (values.Contains(RESOLVED))
            {
                model.Resolved = Convert.ToBoolean(values[RESOLVED]);
            }
        }

        private T ConvertTo<T>(object value)
        {
            var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
            if (converter != null)
            {
                return (T)converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
            }
            else
            {
                // If necessary, implement a type conversion here
                throw new NotImplementedException();
            }
        }

        private string GetFullErrorMessage(ModelStateDictionary modelState)
        {
            var messages = new List<string>();

            foreach (var entry in modelState)
            {
                foreach (var error in entry.Value.Errors)
                    messages.Add(error.ErrorMessage);
            }

            return String.Join(" ", messages);
        }
    }
}