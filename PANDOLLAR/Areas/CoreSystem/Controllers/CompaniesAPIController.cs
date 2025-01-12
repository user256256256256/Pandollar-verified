using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PANDOLLAR.Areas.CoreSystem.Models;
using PANDOLLAR.Data;
using Microsoft.AspNetCore.Http;
using PANDOLLAR.Services;
using Microsoft.Data.SqlClient;

namespace MedisatERP.Controllers
{
    [Route("api/[controller]/[action]")]
    public class CompaniesAPIController : Controller
    {
        private readonly PandollarDbContext  _context;
        private readonly NotificationService _notificationService;
        private readonly IErrorCodeService _errorCodeService;
        private readonly RoleRedirectService _roleRedirectService;

        // Constructor to initialize the context and HttpClient
        public CompaniesAPIController(PandollarDbContext  context, NotificationService notificationService, IErrorCodeService errorCodeService, RoleRedirectService roleRedirectService)
        {
            _context = context;
            _notificationService = notificationService;
            _errorCodeService = errorCodeService;
            _roleRedirectService = roleRedirectService;
        }

        /// <summary>
        /// Retrieves a list of companies with their associated address data.
        /// Supports filtering, sorting, and pagination.
        /// </summary>
        /// <param name="loadOptions">The options for filtering, sorting, and paging data.</param>
        /// <returns>Returns the processed company data.</returns>
        [HttpGet]
        public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var companies = _context.Companies
                                        .Include(c => c.Address)  // Ensure Address data is eagerly loaded
                                        .Select(i => new
                                        {
                                            i.CompanyId,
                                            i.CompanyName,
                                            i.ContactPerson,
                                            i.CompanyEmail,
                                            i.CompanyPhone,
                                            i.ApiCode,
                                            i.CreatedAt,
                                            i.CompanyInitials,
                                            i.Motto,
                                            i.CompanyType,
                                            i.CompanyLogoFilePath,
                                            Address = new
                                            {
                                                i.Address.AddressId,
                                                i.Address.Street,
                                                i.Address.City,
                                                i.Address.State,
                                                i.Address.PostalCode,
                                                i.Address.Country
                                            },
                                            CompanyStatus = new
                                            {
                                                i.CompanyStatus.StatusId,
                                                i.CompanyStatus.StatusName
                                            }

                                        });

                // Apply filtering, sorting, and paging using DataSourceLoader
                var transformedData = await DataSourceLoader.LoadAsync(companies, loadOptions);

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
        /// <returns>Returns the Status OK  of the newly created company.</returns>
        [HttpPost]
        public async Task<IActionResult> Post(string values)
        {
            try
            {
                // Deserialize the incoming request values
                var valuesDict = JsonConvert.DeserializeObject<IDictionary<string, object>>(values);

                // Extract the address data if available
                var addressData = valuesDict.ContainsKey("Address") ? valuesDict["Address"] as JObject : null;

                // Create a new Company instance and populate it with the provided data
                var model = new Company();
                var companyData = valuesDict.Where(kv => kv.Key != "Address")
                                            .ToDictionary(kv => kv.Key, kv => kv.Value);
                PopulateModel(model, companyData); // Populate the company model

                // Initialize Address if not provided
                if (model.Address == null)
                {
                    model.Address = new CompanyAddress();
                }

                // Generate a new AddressId if no AddressId is provided
                if (model.Address.AddressId == Guid.Empty)
                {
                    model.Address.AddressId = Guid.NewGuid();
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

                // Set the CompanyId if not already set
                if (model.CompanyId == Guid.Empty)
                {
                    model.CompanyId = Guid.NewGuid();
                }
                if (model.StatusId == 0) // Assuming 0 is the default value for unset StatusId
                {
                    model.StatusId = 2; // Set default value to 1 if StatusId is not set
                }


                // Set CreatedAt field if not already set
                if (model.CreatedAt == null)
                {
                    model.CreatedAt = DateTime.Now;
                }

                // Save the new company record to the database
                _context.Companies.Add(model);
                await _context.SaveChangesAsync();

                await _notificationService.NotifyNewCompanyCreation(model.CompanyId);

                // Return the status ok (200) of the newly created company
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
                // Retrieve the company by its unique identifier
                var model = await _context.Companies
                                          .Include(c => c.Address)
                                          .FirstOrDefaultAsync(item => item.CompanyId == key);

                if (model == null)
                {
                    Console.WriteLine($"Company not found with key: {key}");
                    return StatusCode(404, "Company not found");
                }

                Console.WriteLine($"Company found with key: {key}, proceeding with updates.");

                // Deserialize the incoming updated values
                var valuesDict = JsonConvert.DeserializeObject<IDictionary<string, object>>(values);

                // Extract address data from the request, if provided
                var addressData = valuesDict.ContainsKey("Address") ? valuesDict["Address"] as JObject : null;

                // If address data is provided, update the address
                if (addressData != null)
                {
                    if (model.Address == null)
                    {
                        model.Address = new CompanyAddress();
                    }

                    model.Address.Street = addressData["Street"]?.ToString();
                    model.Address.City = addressData["City"]?.ToString();
                    model.Address.State = addressData["State"]?.ToString();
                    model.Address.PostalCode = addressData["PostalCode"]?.ToString();
                    model.Address.Country = addressData["Country"]?.ToString();
                }

                // Extract company data and update the company fields
                var companyData = valuesDict.Where(kv => kv.Key != "Address")
                                            .ToDictionary(kv => kv.Key, kv => kv.Value);
                PopulateModel(model, companyData); // Update company model

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
                    Console.WriteLine("Company updated successfully in the database.");
                    return Ok();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Handle the concurrency exception
                    Console.WriteLine("Concurrency exception occurred while updating company.");
                    var entry = ex.Entries.Single();
                    var databaseValues = entry.GetDatabaseValues();
                    if (databaseValues == null)
                    {
                        Console.WriteLine("The record you attempted to edit was deleted by another user.");
                        return NotFound(new { success = false, message = "The record you attempted to edit was deleted by another user." });
                    }
                    else
                    {
                        var dbValues = (Company)databaseValues.ToObject();
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
        /// Deletes a company and its associated address data by its unique identifier.
        /// </summary>
        /// <param name="key">The unique identifier of the company to delete.</param>
        /// <returns>Returns a status code indicating the result of the operation.</returns>
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid key)
        {
            try
            {
                // Retrieve the company to delete
                var model = await _context.Companies
                                          .Include(c => c.Address) // Ensure Address data is eagerly loaded
                                          .FirstOrDefaultAsync(item => item.CompanyId == key);

                if (model == null)
                {
                    // Log that the company was not found
                    Console.WriteLine($"No Company found with ID: {key}");
                    // Return not found if the company does not exist
                    return NotFound($"Company with ID {key} not found.");
                }

                // Step 1: Retrieve the current logo file path from the database
                string currentLogoFilePath = model.CompanyLogoFilePath;  // Fetch the current logo file name from DB (e.g., "oldLogo.jpg")

                // Step 2: Check if the logo exists and delete it if necessary
                if (!string.IsNullOrEmpty(currentLogoFilePath))
                {
                    // Ensure the folder path where logos are stored
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "companyLogoImages");

                    // Step 3: Construct the full path for the current logo file
                    string currentLogoFullPath = Path.Combine(folderPath, currentLogoFilePath);

                    // Step 4: Delete the existing logo file if it exists
                    if (System.IO.File.Exists(currentLogoFullPath))
                    {
                        try
                        {
                            Console.WriteLine($"Deleting old logo file: {currentLogoFullPath}");
                            System.IO.File.Delete(currentLogoFullPath);  // Delete the old logo file
                            Console.WriteLine("Old logo file deleted successfully.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error while deleting the old logo file: {ex.Message}");
                            // Optionally, log the error and continue with the company deletion
                        }
                    }
                }

               
                await _notificationService.NotifyCompanyDeletion(model.CompanyId);

                // Step 5: Remove the company record itself (cascade delete will take care of the address)
                _context.Companies.Remove(model);

                // Step 6: Save the changes to the database
                await _context.SaveChangesAsync();


                // Log successful deletion
                Console.WriteLine($"Successfully deleted Company with ID: {key}");

                return NoContent(); // Return No Content status after successful deletion
            }
            catch (DbUpdateException ex)
            {
                // Handle foreign key constraint violation
                Console.WriteLine($"Database update error: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "A database error occurred. Please try again later.", error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (JsonSerializationException ex)
            {
                // Log the exception
                Console.WriteLine($"JSON serialization error: {ex.Message}");
                return BadRequest(new { message = "Invalid input format. Please check your data and try again." });
            }
            catch (InvalidOperationException ex)
            {
                // Log the exception
                Console.WriteLine(ex);  // Replace with your logging mechanism
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred. Please try again later." });
            }
            catch (Exception ex)
            {
                // Log the exception message and stack trace for debugging purposes
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Return a standardized error response
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred. Please try again later.", error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult> UploadLogo(string companyId, IFormFile companyLogo)
        {
            // Log the start of the method
            Console.WriteLine("UploadLogo method started for CompanyId: " + companyId);

            if (companyLogo != null && companyLogo.Length > 0)
            {
                // Step 1: Retrieve the current logo file path from the database using companyId
                string currentLogoFilePath = GetCurrentLogoFilePath(companyId);  // This should fetch the current file name from DB (e.g., "oldLogo.jpg")

                // Step 2: Ensure the folder path where logos are stored
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "companyLogoImages");

                // Step 3: Check if there is a valid current logo file path, and delete the existing logo if it exists
                if (!string.IsNullOrEmpty(currentLogoFilePath))
                {
                    string currentLogoFullPath = Path.Combine(folderPath, currentLogoFilePath);

                    // Step 4: Delete the existing logo file if it exists
                    if (System.IO.File.Exists(currentLogoFullPath))
                    {
                        try
                        {
                            Console.WriteLine("Deleting old logo file: " + currentLogoFullPath);
                            System.IO.File.Delete(currentLogoFullPath);  // Delete the old logo
                            Console.WriteLine("Old logo file deleted.");
                        }
                        catch (Exception ex)
                        {
                            var errorDetails = _errorCodeService.GetErrorDetails("FILE_DELETE_FAILED");
                            Console.WriteLine("Error while deleting old file: " + ex.Message);
                            return Json(new { success = false, message = errorDetails.ErrorMessage });
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No current logo file path found. Skipping logo deletion.");
                }

                // Step 5: Generate a new unique file name for the logo to avoid conflicts
                string newFileName = $"{companyId}_{Path.GetFileName(companyLogo.FileName)}";  // You can change the naming logic as per your needs

                // Step 6: Construct the full path for the new logo
                string newLogoFilePath = Path.Combine(folderPath, newFileName);

                // Step 7: Save the new logo file to the server
                try
                {
                    using (var fileStream = new FileStream(newLogoFilePath, FileMode.Create))
                    {
                        Console.WriteLine("Saving new logo file to the server...");
                        await companyLogo.CopyToAsync(fileStream);
                        Console.WriteLine("New logo file saved successfully.");
                    }
                }
                catch (Exception ex)
                {
                    var errorDetails = _errorCodeService.GetErrorDetails("FILE_SAVE_FAILED");
                    Console.WriteLine("Error while saving the new file: " + ex.Message);
                    return Json(new { success = false, message = errorDetails.ErrorMessage });
                }

                // Step 8: Update the company's logo file path in the database with the new file name
                bool updateSuccess = UpdateCompanyLogoFilePath(companyId, newFileName);
                if (!updateSuccess)
                {
                    var errorDetails = _errorCodeService.GetErrorDetails("DB_UPDATE_FAILED");
                    Console.WriteLine("Error while updating the company logo path in the database.");
                    return Json(new { success = false, message = errorDetails.ErrorMessage });
                }

                // Step 9: Return the relative file path 
                string relativeFilePath = $"../../img/companyLogoImages/{newFileName}";
                Console.WriteLine("New logo file saved successfully. Returning relative path: " + relativeFilePath);

                // Return success response with the relative file path
                return Json(new { success = true, message = "Company logo successfully uploaded" });
            }
            else
            {
                var errorDetails = _errorCodeService.GetErrorDetails("NO_FILE_UPLOADED");
                Console.WriteLine("No file uploaded or file is empty.");
                return Json(new { success = false, message = errorDetails.ErrorMessage });
            }
        }

        // Method to retrieve the current logo file path from the database 
        private string GetCurrentLogoFilePath(string companyId)
        {
            // Convert the companyId from string to Guid
            if (Guid.TryParse(companyId, out Guid companyIdGuid))
            {
                // Find the company by CompanyId (Guid)
                var company = _context.Companies.FirstOrDefault(c => c.CompanyId == companyIdGuid);

                // Check if the company exists
                if (company != null)
                {
                    Console.WriteLine($"Retrieved company logo path: {company.CompanyLogoFilePath}");
                    // Return the CompanyLogoFilePath if found
                    return company.CompanyLogoFilePath;
                }
                else
                {
                    // Handle the case when the company is not found
                    return null;
                }
            }
            else
            {
                // If the companyId is not valid, return null or handle error as needed
                Console.WriteLine("Invalid companyId format.");
                return null;
            }
        }

        // Method to update the company's logo path in the database
        private bool UpdateCompanyLogoFilePath(string companyId, string newFileName)
        {
            // Convert the companyId from string to Guid
            if (Guid.TryParse(companyId, out Guid companyIdGuid))
            {
                // Find the company by CompanyId (Guid)
                var company = _context.Companies.FirstOrDefault(c => c.CompanyId == companyIdGuid);

                // Check if the company exists
                if (company != null)
                {
                    try
                    {
                        // Update the CompanyLogoFilePath with the new file name
                        company.CompanyLogoFilePath = newFileName;

                        // Save changes to the database
                        _context.SaveChanges();

                        // Log success
                        Console.WriteLine("Successfully updated the company logo file path in the database.");

                        // Return true indicating the update was successful
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Log the error and return false if there's an issue
                        Console.WriteLine("Error while updating the company logo file path: " + ex.Message);
                        return false;
                    }
                }
                else
                {
                    // If the company doesn't exist, log the error
                    Console.WriteLine("Company not found in the database.");
                    return false;
                }
            }
            else
            {
                // If the companyId is invalid, log the error
                Console.WriteLine("Invalid companyId format.");
                return false;
            }
        }


        [HttpPost]
        public async Task<ActionResult> RedirectToCompany(string userId, string companyId)
        {
            try
            {
                Console.WriteLine("Attempting to retrieve user from the database.");

                // Retrieve the user using the userId from the database
                var user = await _context.AspNetUsers.FirstOrDefaultAsync(c => c.Id == userId);
                if (user == null)
                {
                    Console.WriteLine($"User not found for userId: {userId}");
                    var errorMessage = _errorCodeService.GetErrorDetails("USER_NOT_FOUND");
                    return Json(new { success = false, mresponse = errorMessage });
                }

                // Extract the email from the retrieved user
                var email = user.Email;
                Console.WriteLine($"User retrieved successfully. Email: {email}");

                // Convert the companyId to a Guid
                if (!Guid.TryParse(companyId, out Guid companyIdGuid))
                {
                    Console.WriteLine("Invalid companyId format.");
                    var errorMessage = _errorCodeService.GetErrorDetails("INVALID_INPUT");
                    return Json(new { success = false, mresponse = errorMessage });
                }
                Console.WriteLine($"Company ID: {companyIdGuid}");

                // Use RoleRedirectService to handle role-based redirection
                Console.WriteLine("Attempting to handle role-based redirection.");
                var redirectUrl = await _roleRedirectService.HandleSystemAdminToCompanyRedirection(email, companyIdGuid);
                Console.WriteLine("Role-based redirection completed successfully.");

                if (string.IsNullOrEmpty(redirectUrl))
                {
                    Console.WriteLine("Redirection URL is null or empty.");
                    var errorMessage = _errorCodeService.GetErrorDetails("ROLE_NOT_APPLICABLE");
                    return Json(new { success = false, mresponse = errorMessage });
                }

                return Json(new { success = true, redirectUrl });
            }
            catch (Exception ex)
            {
                // Handle and log the exception
                Console.WriteLine($"Error occurred while redirecting: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Retrieve the error message from the ErrorCodeService
                var errorMessage = _errorCodeService.GetErrorDetails("INTERNAL_SERVER_ERROR");

                // Return an error response with the retrieved error message
                return Json(new { success = false, mresponse = errorMessage });
            }
        }




        [HttpGet]
        public async Task<IActionResult> CompanyStatusLookup(DataSourceLoadOptions loadOptions)
        {
            try
            {
                var lookup = from i in _context.CompanyStatusLookup
                             orderby i.StatusName
                             select new
                             {
                                 Value = i.StatusId,
                                 Text = i.StatusName
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




        /// <summary>
        /// Populates the company model with the given values.
        /// </summary>
        private void PopulateModel(Company model, IDictionary<string, object> values)
        {
            // Define the field names for clarity and ease of use
            string COMPANY_ID = nameof(Company.CompanyId);
            string COMPANY_NAME = nameof(Company.CompanyName);
            string CONTACT_PERSON = nameof(Company.ContactPerson);
            string COMPANY_EMAIL = nameof(Company.CompanyEmail);
            string COMPANY_PHONE = nameof(Company.CompanyPhone);
            string API_CODE = nameof(Company.ApiCode);
            string STATUS_ID = nameof(Company.StatusId);
            string COMPANY_INITIALS = nameof(Company.CompanyInitials);
            string MOTTO = nameof(Company.Motto);
            string COMPANY_TYPE = nameof(Company.CompanyType);
            string ADDRESS_ID = nameof(Company.AddressId);
            string CREATED_AT = nameof(Company.CreatedAt);


            if (values.ContainsKey(COMPANY_ID))
            {
                model.CompanyId = ConvertTo<Guid>(values[COMPANY_ID]);
            }

            if (values.ContainsKey(COMPANY_NAME))
            {
                model.CompanyName = Convert.ToString(values[COMPANY_NAME]);
            }

            if (values.ContainsKey(CONTACT_PERSON))
            {
                model.ContactPerson = Convert.ToString(values[CONTACT_PERSON]);
            }

            if (values.ContainsKey(COMPANY_EMAIL))
            {
                model.CompanyEmail = Convert.ToString(values[COMPANY_EMAIL]);
            }

            if (values.ContainsKey(COMPANY_PHONE))
            {
                model.CompanyPhone = Convert.ToString(values[COMPANY_PHONE]);
            }

            if (values.ContainsKey(CREATED_AT))
            {
                model.CreatedAt = values[CREATED_AT] != null ? Convert.ToDateTime(values[CREATED_AT]) : (DateTime?)null;
            }

            if (values.ContainsKey(API_CODE))
            {
                model.ApiCode = Convert.ToString(values[API_CODE]);
            }

            if (values.ContainsKey(STATUS_ID))
            {
                model.StatusId = Convert.ToInt32(values[STATUS_ID]);
            }           

            if (values.ContainsKey(COMPANY_INITIALS))
            {
                model.CompanyInitials = Convert.ToString(values[COMPANY_INITIALS]);
            }
           
            if (values.ContainsKey(MOTTO))
            {
                model.Motto = Convert.ToString(values[MOTTO]);
            }

            if (values.ContainsKey(COMPANY_TYPE))
            {
                model.CompanyType = Convert.ToString(values[COMPANY_TYPE]);
            }

            if (values.ContainsKey(ADDRESS_ID))
            {
                model.AddressId = ConvertTo<Guid>(values[ADDRESS_ID]);
            }

        }


        /// <summary>
        /// Converts a value to the specified type.
        /// </summary>
        private T ConvertTo<T>(object value)
        {
            var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
            if (converter != null)
            {
                return (T)converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
            }
            else
            {
                throw new NotImplementedException("Conversion not implemented");
            }
        }

        /// <summary>
        /// Retrieves a full error message from the ModelState.
        /// </summary>
        private string GetFullErrorMessage(ModelStateDictionary modelState)
        {
            return string.Join(" ", modelState.SelectMany(entry => entry.Value.Errors).Select(error => error.ErrorMessage));
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanyLogo(Guid companyId)
        {
            // Log the companyId to the console to track if it's null or invalid
            Console.WriteLine($"Received CompanyId: {companyId}");

            // If CompanyId is null or invalid, we return an error response
            if (companyId == Guid.Empty)
            {
                Console.WriteLine("CompanyId is empty or null");
                return BadRequest("Unauthorized access: Invalid Company Id!");
            }

            // Fetch the company logo file path
            var companyLogo = await _context.Companies
                .Where(c => c.CompanyId == companyId)
                .Select(c => c.CompanyLogoFilePath)
                .FirstOrDefaultAsync();

            // Log the logo path or default fallback
            if (string.IsNullOrEmpty(companyLogo))
            {
                companyLogo = "defaultCompanyLogo.jpeg";  // Default logo if not found
                Console.WriteLine("No logo found, using default: " + companyLogo);
            }
            else
            {
                Console.WriteLine($"Found company logo: {companyLogo}");
            }

            // Log the final data being returned
            Console.WriteLine($"Returning logo file path: {companyLogo}");

            // Return the data in the expected format for DevExtreme (including CompanyId)
            return Json(new
            {
                data = new[] {
            new {
                CompanyId = companyId,  // Include the CompanyId as the key field
                CompanyLogoFilePath = companyLogo
            }
        }
            });
        }
    }
}
