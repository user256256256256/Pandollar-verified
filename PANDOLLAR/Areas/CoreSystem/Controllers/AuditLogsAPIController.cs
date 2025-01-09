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
    public class AuditLogsAPIController : Controller
    {
        private PandollarDbContext  _context;

        public AuditLogsAPIController(PandollarDbContext  context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of auditLogs with their associated company basing on the selected company.
        /// Supports filtering, sorting, and pagination.
        /// </summary>
        /// <param name="loadOptions">The options for filtering, sorting, and paging data.</param>
        /// <returns>Returns the processed Migration data.</returns> 
        [HttpGet]
        public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions, Guid companyId)
        {
            try
            {
                var auditlogs = _context.AuditLogs
                    .Include(c => c.Company) // Ensure Company is eagerly loaded 
                    .Include(c => c.User) // Ensure ASP User is eagerly loaded
                    .Select(i => new
                    {
                        i.AuditLogId,
                        i.UserId,
                        i.Action,
                        i.Timestamp,
                        i.Details,
                        i.IpAddress,
                        i.DeviceInfo,
                        i.EventType,
                        i.EntityAffected,
                        i.OldValue,
                        i.NewValue,
                        i.ComplianceStatus,
                        i.CompanyId,
                        Company = new
                        {
                            i.Company.CompanyName,
                            i.Company.CompanyEmail,
                            i.Company.CompanyPhone
                        },
                        User = new
                        {
                            i.User.UserName,
                            i.User.Email,
                            i.User.PhoneNumber,
                            i.User.SecurityStamp
                        }
                    }).Where(a => a.CompanyId == companyId).OrderBy(a => a.AuditLogId);

                // Apply filtering, sorting, and paging using DataSourceLoader
                var transformedData = await DataSourceLoader.LoadAsync(auditlogs, loadOptions);

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
                // Log the exception
                Console.WriteLine(ex);  // Replace with your logging mechanism
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(string values)
        {
            try
            {
                var model = new AuditLog();
                var valuesDict = JsonConvert.DeserializeObject<IDictionary>(values);
                PopulateModel(model, valuesDict);

                if (!TryValidateModel(model))
                    return BadRequest(GetFullErrorMessage(ModelState));

                var result = _context.AuditLogs.Add(model);
                await _context.SaveChangesAsync();

                return Json(new { result.Entity.AuditLogId });
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
                // Retrieve the model by key
                var model = await _context.AuditLogs.FirstOrDefaultAsync(item => item.AuditLogId == key);
                if (model == null)
                {
                    Console.WriteLine($"Object not found with key: {key}");
                    return StatusCode(409, "Object not found");
                }

                Console.WriteLine($"Object found with key: {key}, proceeding with updates.");

                // Deserialize the incoming updated values
                var valuesDict = JsonConvert.DeserializeObject<IDictionary>(values);
                PopulateModel(model, valuesDict);

                // Validate the updated model
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
                    Console.WriteLine("Object updated successfully in the database.");
                    return Ok();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Handle the concurrency exception
                    Console.WriteLine("Concurrency exception occurred while updating object.");
                    var entry = ex.Entries.Single();
                    var databaseValues = entry.GetDatabaseValues();
                    if (databaseValues == null)
                    {
                        Console.WriteLine("The record you attempted to edit was deleted by another user.");
                        return NotFound(new { success = false, message = "The record you attempted to edit was deleted by another user." });
                    }
                    else
                    {
                        var dbValues = (AuditLog)databaseValues.ToObject();
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
        /// Deletes a audit logs data by its unique identifier.
        /// </summary>
        /// <param name="key">The unique identifier of the audit logs to delete.</param>
        /// <returns>Returns a status code indicating the result of the operation.</returns>
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid key)
        {
            try
            {
                // Log the entry point with the key being used for deletion
                Console.WriteLine($"Delete request received for Audit Log with ID: {key}");

                // Retrieve the audit log to delete
                var model = await _context.AuditLogs.FirstOrDefaultAsync(item => item.AuditLogId == key);

                // Check if the model is null
                if (model == null)
                {
                    // Log that the audit log was not found
                    Console.WriteLine($"No Audit Log found with ID: {key}");
                    // Return not found if the audit log does not exist
                    return NotFound($"Audit Log with ID {key} not found.");
                }

                // Log the audit log that was found for deletion
                Console.WriteLine($"Found Audit Log with ID: {key}");

                // Remove the record
                _context.AuditLogs.Remove(model);

                // Log the removal of the audit log
                Console.WriteLine($"Removing Audit Log with ID: {key}");

                // Save the changes to the database
                await _context.SaveChangesAsync();

                // Log successful deletion
                Console.WriteLine($"Successfully deleted Audit Log with ID: {key}");

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

        [HttpGet]
        public async Task<IActionResult> AspNetUsersLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = from i in _context.AspNetUsers
                             orderby i.UserName
                             select new
                             {
                                 Value = i.Id,
                                 Text = i.UserName
                             };
                return Json(await DataSourceLoader.LoadAsync(lookup, loadOptions));
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
        private void PopulateModel(AuditLog model, IDictionary values)
        {
            string AUDIT_LOG_ID = nameof(AuditLog.AuditLogId);
            string USER_ID = nameof(AuditLog.UserId);
            string ACTION = nameof(AuditLog.Action);
            string TIMESTAMP = nameof(AuditLog.Timestamp);
            string DETAILS = nameof(AuditLog.Details);
            string IP_ADDRESS = nameof(AuditLog.IpAddress);
            string DEVICE_INFO = nameof(AuditLog.DeviceInfo);
            string EVENT_TYPE = nameof(AuditLog.EventType);
            string ENTITY_AFFECTED = nameof(AuditLog.EntityAffected);
            string OLD_VALUE = nameof(AuditLog.OldValue);
            string NEW_VALUE = nameof(AuditLog.NewValue);
            string COMPLIANCE_STATUS = nameof(AuditLog.ComplianceStatus);

            if (values.Contains(AUDIT_LOG_ID))
            {
                model.AuditLogId = ConvertTo<System.Guid>(values[AUDIT_LOG_ID]);
            }

            if (values.Contains(USER_ID))
            {
                model.UserId = Convert.ToString(values[USER_ID]);
            }

            if (values.Contains(ACTION))
            {
                model.Action = Convert.ToString(values[ACTION]);
            }

            if (values.Contains(TIMESTAMP))
            {
                model.Timestamp = Convert.ToDateTime(values[TIMESTAMP]);
            }

            if (values.Contains(DETAILS))
            {
                model.Details = Convert.ToString(values[DETAILS]);
            }

            if (values.Contains(IP_ADDRESS))
            {
                model.IpAddress = Convert.ToString(values[IP_ADDRESS]);
            }

            if (values.Contains(DEVICE_INFO))
            {
                model.DeviceInfo = Convert.ToString(values[DEVICE_INFO]);
            }

            if (values.Contains(EVENT_TYPE))
            {
                model.EventType = Convert.ToString(values[EVENT_TYPE]);
            }

            if (values.Contains(ENTITY_AFFECTED))
            {
                model.EntityAffected = Convert.ToString(values[ENTITY_AFFECTED]);
            }

            if (values.Contains(OLD_VALUE))
            {
                model.OldValue = Convert.ToString(values[OLD_VALUE]);
            }

            if (values.Contains(NEW_VALUE))
            {
                model.NewValue = Convert.ToString(values[NEW_VALUE]);
            }

            if (values.Contains(COMPLIANCE_STATUS))
            {
                model.ComplianceStatus = Convert.ToString(values[COMPLIANCE_STATUS]);
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