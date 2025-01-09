
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
using Newtonsoft.Json.Linq;
using PANDOLLAR.Areas.CoreSystem.Models;
using PANDOLLAR.Data;
using PANDOLLAR.Models;
using Microsoft.Data.SqlClient;

namespace PANDOLLAR.Controllers
{
    [Route("api/[controller]/[action]")]
    public class CompanyClientsAPIController : Controller
    {
        private PandollarDbContext  _context;

        public CompanyClientsAPIController(PandollarDbContext  context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of company clients with their associated address  data basing on the selected company.
        /// Supports filtering, sorting, and pagination.
        /// </summary>
        /// <param name="loadOptions">The options for filtering, sorting, and paging data.</param>
        /// <returns>Returns the processed company clients data.</returns>
        [HttpGet]
        public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions, Guid companyId)
        {
            try
            {
                var companyclients = _context.CompanyClients
                    .Include(c => c.Address) // Ensure Address data is eagerly loaded
                    .Select(i => new
                    {
                        i.ClientId,
                        i.CompanyId,
                        i.ClientName,
                        i.DateOfBirth,
                        i.Gender,
                        i.Email,
                        i.PhoneNumber,
                        i.AddressId,
                        i.EmergencyContactName,
                        i.EmergencyContactPhone,
                        i.MaritalStatus,
                        i.Nationality,
                        i.CreatedAt,
                        i.UpdatedAt,
                        Address = new
                        {
                            i.Address.AddressId,
                            i.Address.Street,
                            i.Address.City,
                            i.Address.State,
                            i.Address.PostalCode,
                            i.Address.Country,
                        }
                    }).Where(a => a.CompanyId == companyId).OrderBy(a => a.ClientId);

                // Apply filtering, sorting, and paging using DataSourceLoader
                var transformedData = await DataSourceLoader.LoadAsync(companyclients, loadOptions);

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
        /// Adds a new company with its associated address data.
        /// </summary>
        /// <param name="values">The incoming values as a JSON string.</param>
        /// <returns>Returns the Status OK  of the newly created company client.</returns>
        [HttpPost]
        public async Task<IActionResult> Post(string values)
        {
            // Add a error check messsage to ensure that client Gender exists in the enum.
            // So do phone number country code for contacts
            // So do a country
            // When you accomplish the task, leverage the knowledge with user accounts and also include Gender here you use select box

            try
            {
                // Deserialize the incoming request values 
                var valuesDict = JsonConvert.DeserializeObject<IDictionary<string, object>>(values);

                // Extract the address data if available
                var addressData = valuesDict.ContainsKey("Address") ? valuesDict["Address"] as JObject : null;

                // Create a new Company Clients Instance and populate it with the provided data
                var model = new CompanyClient();
                var companyClientsData = valuesDict.Where(kv => kv.Key != "Address")
                                                   .ToDictionary(kv => kv.Key, kv => kv.Value);

                PopulateModel(model, companyClientsData); // Populate the company clients model.

                // Initialize Address if not provided
                if (model.Address == null)
                {
                    model.Address = new ClientAddress();
                }

                // If address data is provided, populate the address model
                if (addressData != null)
                {
                    model.Address.Street = addressData["Street"]?.ToString();
                    model.Address.City = addressData["City"]?.ToString();
                    model.Address.State = addressData["State"]?.ToString();
                    model.Address.PostalCode = addressData["PostalCode"]?.ToString();
                    model.Address.Country = addressData["Country"]?.ToString();
                }

                // Save the new company client record to the database 
                _context.CompanyClients.Add(model);
                await _context.SaveChangesAsync();

                // Return the status ok (200) of the newly created company client
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
                // Retrieve the company client by its unique identifier
                var model = await _context.CompanyClients
                    .Include(c => c.Address)
                    .FirstOrDefaultAsync(item => item.ClientId == key);

                if (model == null)
                {
                    Console.WriteLine($"Company client not found with key: {key}");
                    return StatusCode(404, "Company client not found");
                }

                Console.WriteLine($"Company client found with key: {key}, proceeding with updates.");

                // Deserialize the incoming updated values
                var valuesDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);

                // Extract address data from the request, if provided
                var addressData = valuesDict.ContainsKey("Address") ? valuesDict["Address"] as JObject : null;

                // If address data is provided, update the address
                if (addressData != null)
                {
                    if (model.Address == null)
                    {
                        model.Address = new ClientAddress();
                    }

                    model.Address.Street = addressData["Street"]?.ToString();
                    model.Address.City = addressData["City"]?.ToString();
                    model.Address.State = addressData["State"]?.ToString();
                    model.Address.PostalCode = addressData["PostalCode"]?.ToString();
                    model.Address.Country = addressData["Country"]?.ToString();
                }

                // Extract company clients data and update the company clients fields
                var companyClientsData = valuesDict.Where(kv => kv.Key != "Address")
                                                   .ToDictionary(kv => kv.Key, kv => kv.Value);
                PopulateModel(model, companyClientsData);

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
                    Console.WriteLine("Company client updated successfully in the database.");
                    return Ok();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Handle the concurrency exception
                    Console.WriteLine("Concurrency exception occurred while updating company client.");
                    var entry = ex.Entries.Single();
                    var databaseValues = entry.GetDatabaseValues();
                    if (databaseValues == null)
                    {
                        Console.WriteLine("The record you attempted to edit was deleted by another user.");
                        return NotFound(new { success = false, message = "The record you attempted to edit was deleted by another user." });
                    }
                    else
                    {
                        var dbValues = (CompanyClient)databaseValues.ToObject();
                        Console.WriteLine("The record you attempted to edit was modified by another user.");
                        Console.WriteLine($"Current values: ClientName: {dbValues.ClientName}, Address: {dbValues.Address.Street}, City: {dbValues.Address.City}, State: {dbValues.Address.State}, PostalCode: {dbValues.Address.PostalCode}, Country: {dbValues.Address.Country}");

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
        /// Deletes a company clients and its associated address data by its unique identifier.
        /// </summary>
        /// <param name="key">The unique identifier of the company to delete.</param>
        /// <returns>Returns a status code indicating the result of the operation.</returns>
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid key)
        {
            try
            {
                // Log the entry point with the key being used for deletion
                Console.WriteLine($"Delete request received for CompanyClient with ID: {key}");

                // Retrieve the company client to delete
                var model = await _context.CompanyClients
                    .Include(c => c.Address) // Ensure Address data is eagerly loaded
                    .FirstOrDefaultAsync(item => item.ClientId == key);

                // Check if the model is null
                if (model == null)
                {
                    // Log that the model was not found
                    Console.WriteLine($"No CompanyClient found with ID: {key}");
                    // Return not found if the company client does not exist
                    return NotFound($"Company client with ID {key} not found.");
                }

                // Log the model that was found for deletion
                Console.WriteLine($"Found CompanyClient with ID: {key}. Address exists: {model.Address != null}");

                // Remove the associated address, if it exists
                if (model.Address != null)
                {
                    // Log the removal of the address
                    Console.WriteLine($"Removing associated Address with ID: {model.Address.AddressId}");
                    _context.ClientAddresses.Remove(model.Address);
                }

                // Log the removal of the company client itself
                Console.WriteLine($"Removing CompanyClient with ID: {key}");
                _context.CompanyClients.Remove(model);

                // Save the changes to the database
                await _context.SaveChangesAsync();

                // Log successful deletion
                Console.WriteLine($"Successfully deleted CompanyClient with ID: {key}");

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
        /// Populates the  model with the given values.
        /// </summary>
        private void PopulateModel(CompanyClient model, IDictionary values)
        {
            string CLIENT_ID = nameof(CompanyClient.ClientId);
            string COMPANY_ID = nameof(CompanyClient.CompanyId);
            string CLIENT_NAME = nameof(CompanyClient.ClientName);
            string DATE_OF_BIRTH = nameof(CompanyClient.DateOfBirth);
            string GENDER = nameof(CompanyClient.Gender);
            string EMAIL = nameof(CompanyClient.Email);
            string PHONE_NUMBER = nameof(CompanyClient.PhoneNumber);
            string ADDRESS_ID = nameof(CompanyClient.AddressId);
            string EMERGENCY_CONTACT_NAME = nameof(CompanyClient.EmergencyContactName);
            string EMERGENCY_CONTACT_PHONE = nameof(CompanyClient.EmergencyContactPhone);
            string MARITAL_STATUS = nameof(CompanyClient.MaritalStatus);
            string NATIONALITY = nameof(CompanyClient.Nationality);
            string CREATED_AT = nameof(CompanyClient.CreatedAt);
            string UPDATED_AT = nameof(CompanyClient.UpdatedAt);

            if (values.Contains(CLIENT_ID))
            {
                model.ClientId = ConvertTo<System.Guid>(values[CLIENT_ID]);
            }

            if (values.Contains(COMPANY_ID))
            {
                model.CompanyId = ConvertTo<System.Guid>(values[COMPANY_ID]);
            }

            if (values.Contains(CLIENT_NAME))
            {
                model.ClientName = Convert.ToString(values[CLIENT_NAME]);
            }

            if (values.Contains(DATE_OF_BIRTH))
            {
                model.DateOfBirth = Convert.ToDateTime(values[DATE_OF_BIRTH]);
            }

            if (values.Contains(GENDER))
            {
                model.Gender = Convert.ToString(values[GENDER]);
            }

            if (values.Contains(EMAIL))
            {
                model.Email = Convert.ToString(values[EMAIL]);
            }

            if (values.Contains(PHONE_NUMBER))
            {
                model.PhoneNumber = Convert.ToString(values[PHONE_NUMBER]);
            }

            if (values.Contains(ADDRESS_ID))
            {
                model.AddressId = ConvertTo<System.Guid>(values[ADDRESS_ID]);
            }

            if (values.Contains(EMERGENCY_CONTACT_NAME))
            {
                model.EmergencyContactName = Convert.ToString(values[EMERGENCY_CONTACT_NAME]);
            }

            if (values.Contains(EMERGENCY_CONTACT_PHONE))
            {
                model.EmergencyContactPhone = Convert.ToString(values[EMERGENCY_CONTACT_PHONE]);
            }

            if (values.Contains(MARITAL_STATUS))
            {
                model.MaritalStatus = Convert.ToString(values[MARITAL_STATUS]);
            }

            if (values.Contains(NATIONALITY))
            {
                model.Nationality = Convert.ToString(values[NATIONALITY]);
            }

            if (values.Contains(CREATED_AT))
            {
                model.CreatedAt = Convert.ToDateTime(values[CREATED_AT]);
            }

            if (values.Contains(UPDATED_AT))
            {
                model.UpdatedAt = values[UPDATED_AT] != null ? Convert.ToDateTime(values[UPDATED_AT]) : (DateTime?)null;
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