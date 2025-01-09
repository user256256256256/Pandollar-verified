using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using PANDOLLAR.Areas.CoreSystem.Models;
using PANDOLLAR.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PANDOLLAR.Controllers
{
    [Route("api/[controller]/[action]")]
    public class DataMigrationsAPIController : Controller
    {
        private PandollarDbContext  _context;

        public DataMigrationsAPIController(PandollarDbContext  context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of DataMigrations with their associated company basing on the selected company.
        /// Supports filtering, sorting, and pagination.
        /// </summary>
        /// <param name="loadOptions">The options for filtering, sorting, and paging data.</param>
        /// <returns>Returns the processed Migration data.</returns> 
        [HttpGet]
        public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions, Guid companyId)
        {
            try
            {
                var datamigrations = _context.DataMigrations
                    .Include(c => c.Company) // Ensure Company is eagerly loaded
                    .Select(i => new
                    {
                        i.MigrationId,
                        i.SourceSystem,
                        i.DestinationSystem,
                        i.Status,
                        i.StartDate,
                        i.EndDate,
                        i.RecordsMigrated,
                        i.ErrorCount,
                        i.Log,
                        i.MappingRules,
                        i.CompanyId,
                        Company = new
                        {
                            i.Company.CompanyName,
                            i.Company.CompanyEmail,
                            i.Company.CompanyPhone
                        }
                    }).Where(a => a.CompanyId == companyId).OrderBy(a => a.MigrationId);

                // Apply filtering, sorting, and paging using DataSourceLoader
                var transformedData = await DataSourceLoader.LoadAsync(datamigrations, loadOptions);

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

        [HttpPost]
        public async Task<IActionResult> Post(string values)
        {
            try
            {
                var model = new DataMigration();
                var valuesDict = JsonConvert.DeserializeObject<IDictionary>(values);
                PopulateModel(model, valuesDict);

                if (!TryValidateModel(model))
                    return BadRequest(GetFullErrorMessage(ModelState));

                var result = _context.DataMigrations.Add(model);
                await _context.SaveChangesAsync();

                return Json(new { result.Entity.MigrationId });
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
                // Retrieve the data migration by its unique identifier
                var model = await _context.DataMigrations.FirstOrDefaultAsync(item => item.MigrationId == key);
                if (model == null)
                {
                    Console.WriteLine($"Data migration not found with key: {key}");
                    return StatusCode(409, "Object not found");
                }

                Console.WriteLine($"Data migration found with key: {key}, proceeding with updates.");

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
                    Console.WriteLine("Data migration updated successfully in the database.");
                    return Ok();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Handle the concurrency exception
                    Console.WriteLine("Concurrency exception occurred while updating data migration.");
                    var entry = ex.Entries.Single();
                    var databaseValues = entry.GetDatabaseValues();
                    if (databaseValues == null)
                    {
                        Console.WriteLine("The record you attempted to edit was deleted by another user.");
                        return NotFound(new { success = false, message = "The record you attempted to edit was deleted by another user." });
                    }
                    else
                    {
                        var dbValues = (DataMigration)databaseValues.ToObject();
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
        /// Deletes a migration  data by its unique identifier.
        /// </summary>
        /// <param name="key">The unique identifier of the migration to delete.</param>
        /// <returns>Returns a status code indicating the result of the operation.</returns>
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid key)
        {
            try
            {
                // Log the entry point with the key being used for deletion
                Console.WriteLine($"Delete request received for Migration with ID: {key}");

                // Retrieve the migration to delete
                var model = await _context.DataMigrations.FirstOrDefaultAsync(item => item.MigrationId == key);

                // Check if the model is null
                if (model == null)
                {
                    // Log that the migration was not found
                    Console.WriteLine($"No Migration found with ID: {key}");
                    // Return not found if the migration does not exist
                    return NotFound($"Migration with ID {key} not found.");
                }

                // Log the migration that was found for deletion
                Console.WriteLine($"Found Migration with ID: {key}");

                // Remove the migration record
                _context.DataMigrations.Remove(model);

                // Log the removal of the migration
                Console.WriteLine($"Removing Migration with ID: {key}");

                // Save the changes to the database
                await _context.SaveChangesAsync();

                // Log successful deletion
                Console.WriteLine($"Successfully deleted Migration with ID: {key}");

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
        /// Populates the company model with the given values.
        /// </summary>
        private void PopulateModel(DataMigration model, IDictionary values)
        {
            string MIGRATION_ID = nameof(DataMigration.MigrationId);
            string SOURCE_SYSTEM = nameof(DataMigration.SourceSystem);
            string DESTINATION_SYSTEM = nameof(DataMigration.DestinationSystem);
            string STATUS = nameof(DataMigration.Status);
            string START_DATE = nameof(DataMigration.StartDate);
            string END_DATE = nameof(DataMigration.EndDate);
            string RECORDS_MIGRATED = nameof(DataMigration.RecordsMigrated);
            string ERROR_COUNT = nameof(DataMigration.ErrorCount);
            string LOG = nameof(DataMigration.Log);
            string MAPPING_RULES = nameof(DataMigration.MappingRules);

            if (values.Contains(MIGRATION_ID))
            {
                model.MigrationId = ConvertTo<System.Guid>(values[MIGRATION_ID]);
            }

            if (values.Contains(SOURCE_SYSTEM))
            {
                model.SourceSystem = Convert.ToString(values[SOURCE_SYSTEM]);
            }

            if (values.Contains(DESTINATION_SYSTEM))
            {
                model.DestinationSystem = Convert.ToString(values[DESTINATION_SYSTEM]);
            }

            if (values.Contains(STATUS))
            {
                model.Status = Convert.ToString(values[STATUS]);
            }

            if (values.Contains(START_DATE))
            {
                model.StartDate = Convert.ToDateTime(values[START_DATE]);
            }

            if (values.Contains(END_DATE))
            {
                model.EndDate = values[END_DATE] != null ? Convert.ToDateTime(values[END_DATE]) : (DateTime?)null;
            }

            if (values.Contains(RECORDS_MIGRATED))
            {
                model.RecordsMigrated = values[RECORDS_MIGRATED] != null ? Convert.ToInt32(values[RECORDS_MIGRATED]) : (int?)null;
            }

            if (values.Contains(ERROR_COUNT))
            {
                model.ErrorCount = values[ERROR_COUNT] != null ? Convert.ToInt32(values[ERROR_COUNT]) : (int?)null;
            }

            if (values.Contains(LOG))
            {
                model.Log = Convert.ToString(values[LOG]);
            }

            if (values.Contains(MAPPING_RULES))
            {
                model.MappingRules = Convert.ToString(values[MAPPING_RULES]);
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