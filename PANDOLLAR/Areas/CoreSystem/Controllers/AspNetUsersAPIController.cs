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
using Newtonsoft.Json.Linq;
using PANDOLLAR.Areas.CoreSystem.Models;
using PANDOLLAR.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PANDOLLAR.Services;
using Microsoft.Data.SqlClient;

namespace PANDOLLAR.Controllers
{
    [Route("api/[controller]/[action]")]
    public class AspNetUsersAPIController : Controller
    {
        private readonly PandollarDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly IErrorCodeService _errorCodeService;
        public AspNetUsersAPIController(PandollarDbContext context, UserManager<IdentityUser> userManager, NotificationService notificationService, IErrorCodeService errorCodeService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _errorCodeService = errorCodeService;
        }

        /// <summary>
        /// Retrieves a list of User Accounts with their associated address  data basing on the selected company.
        /// Supports filtering, sorting, and pagination.
        /// </summary>
        /// <param name="loadOptions">The options for filtering, sorting, and paging data.</param>
        /// <returns>Returns the processed User Accounts data.</returns>
        [HttpGet]
        public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions, Guid? companyId)
        {
            try
            {
                var usersQuery = _context.AspNetUsers.AsQueryable();

                // Filter by CompanyId if provided, otherwise fetch users with a NULL CompanyId
                if (companyId.HasValue)
                {
                    usersQuery = usersQuery.Where(u => u.CompanyId == companyId.Value);
                }
                else
                {
                    usersQuery = usersQuery.Where(u => u.CompanyId == null);
                }

                // Fetch user data without roles
                var users = await usersQuery
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.Email,
                        u.PhoneNumber,
                        u.EmailConfirmed,
                        u.PhoneNumberConfirmed,
                        u.LockoutEnabled,
                        u.LockoutEnd,
                        u.AccessFailedCount,
                        u.BioData,
                        u.ProfileImagePath,
                        u.NormalizedUserName,
                        u.NormalizedEmail,
                        u.TwoFactorEnabled
                    })
                    .OrderBy(u => u.Id)
                    .ToListAsync();

                // Fetch roles separately and join them in-memory
                var userIds = users.Select(u => u.Id).ToList();
                var userRoles = await _context.AspNetUserRoles
                    .Where(ur => userIds.Contains(ur.UserId))
                    .Join(_context.AspNetRoles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .ToListAsync();

                var usersWithRoles = users.Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.PhoneNumber,
                    u.EmailConfirmed,
                    u.PhoneNumberConfirmed,
                    u.LockoutEnabled,
                    u.LockoutEnd,
                    u.AccessFailedCount,
                    u.BioData,
                    u.ProfileImagePath,
                    u.NormalizedUserName,
                    u.NormalizedEmail,
                    u.TwoFactorEnabled,
                    CurrentRoles = string.Join(", ", userRoles.Where(ur => ur.UserId == u.Id).Select(ur => ur.Name)) // Join roles as a comma-separated string
                });

                // Apply filtering, sorting, and paging using DataSourceLoader
                var transformedData = DataSourceLoader.Load(usersWithRoles, loadOptions);

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
                // Log the exception
                Console.WriteLine(ex);  // Replace with your logging mechanism
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AspNetUser userInput)
        {
            try
            {
                // Log the content of the incoming request for debugging
                if (userInput == null)
                {
                    var errorDetails = _errorCodeService.GetErrorDetails("MISSING_CREDENTIALS");
                    Console.WriteLine("Received user input: null");
                    return Json(new { success = false, message = errorDetails.ErrorMessage });
                }

                var userInputJson = JsonConvert.SerializeObject(userInput, Formatting.Indented);
                Console.WriteLine($"Received user input: {userInputJson}");

                // List of restricted roles (e.g., "System Administrator")
                var restrictedRoles = new List<string> { "System Administrator" };

                // List of roles that require enabling TwoFactorAuthentication (e.g., "System Administrator", "Company Administrator")
                var rolesAssigned2FA = new List<string> { "System Administrator", "Company Administrator" };

                // Handle roles if provided in the user input
                if (userInput.Roles != null && userInput.Roles.Any())
                {
                    foreach (var roleObject in userInput.Roles)
                    {
                        // Assuming each role is an object with an Id property (of type Guid)
                        if (roleObject == null || roleObject.Id == null)
                        {
                            var errorDetails = _errorCodeService.GetErrorDetails("INVALID_ROLE_OBJECT");
                            Console.WriteLine($"Invalid Role object: {roleObject}");
                            return Json(new { success = false, message = errorDetails.ErrorMessage });
                        }

                        var roleId = roleObject.Id;

                        // Find the role in the database by role ID (Guid)
                        var roleFromDb = await _context.AspNetRoles
                            .FirstOrDefaultAsync(r => r.Id == roleId);

                        if (roleFromDb == null)
                        {
                            var errorDetails = _errorCodeService.GetErrorDetails("ROLE_NOT_FOUND");
                            // If the role doesn't exist, return an error
                            Console.WriteLine($"Role with ID '{roleId}' not found.");
                            return Json(new { success = false, message = errorDetails.ErrorMessage });
                        }

                        // Check if the role is restricted and if the user has a CompanyId
                        if (userInput.CompanyId.HasValue && restrictedRoles.Contains(roleFromDb.Name))
                        {
                            var errorDetails = _errorCodeService.GetErrorDetails("ROLE_NOT_APPLICABLE");
                            // If the role is restricted and user has a CompanyId, terminate and return an error
                            Console.WriteLine($"User has a CompanyId and role '{roleFromDb.Name}' is restricted. Terminating.");
                            return Json(new { success = false, message = errorDetails.ErrorMessage });
                        }

                        // If the role is in rolesAssigned2FA, set TwoFactorEnabled to true
                        if (rolesAssigned2FA.Contains(roleFromDb.Name))
                        {
                            Console.WriteLine($"Role '{roleFromDb.Name}' requires 2FA, enabling TwoFactorEnabled.");
                            userInput.TwoFactorEnabled = true;
                        }
                    }
                }

                // Create an IdentityUser instance for UserManager only after role checks
                var identityUser = new IdentityUser
                {
                    UserName = userInput.UserName,
                    Email = userInput.Email,
                    PhoneNumber = userInput.PhoneNumber,
                    NormalizedUserName = userInput.NormalizedUserName,
                    NormalizedEmail = userInput.NormalizedEmail
                };

                // Password hashing
                var passwordHasher = new PasswordHasher<IdentityUser>();
                identityUser.PasswordHash = passwordHasher.HashPassword(identityUser, userInput.PasswordHash);

                // Create the user using _userManager
                var createResult = await _userManager.CreateAsync(identityUser);
                if (!createResult.Succeeded)
                {
                    var errorDetails = _errorCodeService.GetErrorDetails("USER_CREATION_FAILED");
                    return Json(new { success = false, message = errorDetails.ErrorMessage, errors = createResult.Errors });
                }

                // If the role requires 2FA, ensure it's enabled for the user
                if (userInput.TwoFactorEnabled)
                {
                    await _userManager.SetTwoFactorEnabledAsync(identityUser, true);
                    Console.WriteLine("2FA enabled for the user.");
                }

                // Handle roles after user is created
                if (userInput.Roles != null && userInput.Roles.Any())
                {
                    foreach (var roleObject in userInput.Roles)
                    {
                        var roleId = roleObject.Id;

                        // Find the role in the database by role ID (Guid)
                        var roleFromDb = await _context.AspNetRoles
                            .FirstOrDefaultAsync(r => r.Id == roleId);

                        if (roleFromDb != null)
                        {
                            var roleName = roleFromDb.Name;

                            // Check if the role is restricted and if the user has a CompanyId
                            if (userInput.CompanyId.HasValue && restrictedRoles.Contains(roleName))
                            {
                                // Skip assigning restricted roles
                                Console.WriteLine($"User has a CompanyId and role '{roleName}' is restricted. Skipping assignment.");
                                continue;
                            }

                            var addRoleResult = await _userManager.AddToRoleAsync(identityUser, roleName);

                            if (!addRoleResult.Succeeded)
                            {
                                // If role assignment fails, delete the created user and return an error
                                await _userManager.DeleteAsync(identityUser);
                                var errorDetails = _errorCodeService.GetErrorDetails("ROLE_ASSIGNMENT_FAILED");
                                Console.WriteLine("Role assignment failed. User deleted.");
                                return Json(new { success = false, message = errorDetails.ErrorMessage });
                            }

                            try
                            {
                                // Trigger notification for new account creation
                                Console.WriteLine($"Attempting to notify new account creation for {identityUser.Email}");
                                await _notificationService.NotifySystemAdministratorCreation(identityUser.Email);
                                Console.WriteLine("Notification for new account creation sent successfully.");
                            }
                            catch (Exception ex)
                            {
                                var errorDetails = _errorCodeService.GetErrorDetails("EMAIL_SEND_ERROR");
                                Console.WriteLine($"An error occurred while sending the notification: {ex.Message}");
                                return Json(new { success = false, message = errorDetails.ErrorMessage });
                            }

                            Console.WriteLine($"Role '{roleName}' assigned successfully.");
                        }
                    }
                }

                // Handle the company ID if provided
                if (userInput.CompanyId.HasValue)
                {
                    // Update the CompanyId in the user record if it's provided
                    var userFromDb = await _context.AspNetUsers
                        .FirstOrDefaultAsync(u => u.UserName == identityUser.UserName);

                    if (userFromDb != null)
                    {
                        userFromDb.CompanyId = userInput.CompanyId; // Assign the CompanyId from userInput
                        await _context.SaveChangesAsync(); // Save changes to the database
                        Console.WriteLine($"Company Id '{userFromDb.CompanyId}' assigned successfully.");
                    }
                }

                return Json(new { success = true, message = $"User account for ${userInput.UserName} created successfully!" });
            }
            catch (Exception ex)
            {
                var errorDetails = _errorCodeService.GetErrorDetails("INTERNAL_SERVER_ERROR");
                // Log the exception message and stack trace for debugging purposes
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Return a standardized error response
                return Json(new { success = false, message = errorDetails.ErrorMessage, error = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing User and its Roles Data.
        /// </summary>
        /// <param name="key">The unique identifier of the user to update.</param>
        /// <param name="values">The incoming updated values as a JSON string.</param>
        /// <returns>Returns a success status if update is successful.</returns>
        [HttpPut]
        public async Task<IActionResult> Put(string key, string values)
        {
            try
            {
                // Read the raw request data for logging purposes
                var rawData = await new StreamReader(Request.Body).ReadToEndAsync();
                Console.WriteLine($"Raw Request Data: {rawData}");
                Console.WriteLine($"Attempting to update user with key: {key}");

                // Retrieve the user by unique identifier without including roles
                var model = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == key);
                if (model == null)
                {
                    var errorDetails = _errorCodeService.GetErrorDetails("USER_NOT_FOUND");
                    Console.WriteLine("User not found.");
                    return Json(new { success = false, message = errorDetails.ErrorMessage });
                }

                Console.WriteLine("User found, proceeding with updates.");

                // Deserialize the incoming updated values
                var valuesDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);

                // Check if password is provided and hash it
                if (valuesDict.ContainsKey("Password") && valuesDict["Password"] != null)
                {
                    var password = valuesDict["Password"].ToString();
                    // Initialize PasswordHasher
                    var passwordHasher = new PasswordHasher<AspNetUser>();
                    // Hash the new password
                    var hashedPassword = passwordHasher.HashPassword(model, password);
                    // Update the password hash in the model
                    model.PasswordHash = hashedPassword;
                    Console.WriteLine($"Password has been hashed and updated: {hashedPassword}");
                }

                // Log the deserialized dictionary for easier inspection
                Console.WriteLine("Deserialized values:");
                Console.WriteLine(JsonConvert.SerializeObject(valuesDict, Formatting.Indented)); // Pretty-print the JSON

                // Update other user information based on the provided values
                PopulateModel(model, valuesDict);

                // Validate the updated model
                if (!TryValidateModel(model))
                {
                    var errorDetails = _errorCodeService.GetErrorDetails("INVALID_INPUT");
                    Console.WriteLine("Model validation failed.");
                    return Json(new { success = false, message = errorDetails.ErrorMessage });
                }
                else
                {
                    Console.WriteLine("Model validated successfully.");
                }

                try
                {
                    // Save the changes to the database
                    await _context.SaveChangesAsync();
                    Console.WriteLine("User updated successfully in the database.");
                    return Json(new { success = true, message = "User updated successfully." });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Handle the concurrency exception
                    Console.WriteLine("Concurrency exception occurred while updating user.");
                    var entry = ex.Entries.Single();
                    var databaseValues = entry.GetDatabaseValues();
                    if (databaseValues == null)
                    {
                        var errorDetails = _errorCodeService.GetErrorDetails("RECORD_NOT_FOUND");
                        Console.WriteLine("The record you attempted to edit was deleted by another user.");
                        return Json(new { success = false, message = errorDetails.ErrorMessage });
                    }
                    else
                    {
                        var dbValues = (AspNetUser)databaseValues.ToObject();
                        var errorDetails = _errorCodeService.GetErrorDetails("CONCURRENCY_CONFLICT");
                        Console.WriteLine("The record you attempted to edit was modified by another user.");
                        Console.WriteLine($"Current values: UserName: {dbValues.UserName}, Email: {dbValues.Email}, PhoneNumber: {dbValues.PhoneNumber}");

                        // Optionally, reload the entity with current database values
                        await entry.ReloadAsync();
                        return Json(new { success = false, message = errorDetails.ErrorMessage, currentValues = dbValues });
                    }
                }
            }
            catch (Exception ex)
            {
                // Return an internal server error if an exception occurs
                var errorDetails = _errorCodeService.GetErrorDetails("INTERNAL_SERVER_ERROR");
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return Json(new { success = false, message = errorDetails.ErrorMessage, error = ex.Message });
            }
        }


        [HttpDelete]
        public async Task<IActionResult> Delete(string key)
        {
            try
            {
                // Log the entry point with the key being used for deletion
                Console.WriteLine($"Delete request received for user with ID: {key}");

                // Retrieve the user to delete
                var model = await _context.AspNetUsers.FirstOrDefaultAsync(item => item.Id == key);

                // Check if the user exists
                if (model == null)
                {
                    // Log that the user was not found
                    Console.WriteLine($"No user found with ID: {key}");
                    // Return not found if the user does not exist
                    return NotFound($"User with ID {key} not found.");
                }

                // Step 1: Retrieve the current logo file path from the database
                string currentProfilePath = model.ProfileImagePath;  // Fetch the current logo file name from DB (e.g., "oldLogo.jpg")

                // Step 2: Check if the logo exists and delete it if necessary
                if (!string.IsNullOrEmpty(currentProfilePath))
                {
                    // Ensure the folder path where logos are stored
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "userProfileImages");

                    // Step 3: Construct the full path for the current logo file
                    string currentProfileFullPath = Path.Combine(folderPath, currentProfilePath);

                    // Step 4: Delete the existing logo file if it exists
                    if (System.IO.File.Exists(currentProfileFullPath))
                    {
                        try
                        {
                            Console.WriteLine($"Deleting old profile file: {currentProfileFullPath}");
                            System.IO.File.Delete(currentProfileFullPath);  // Delete the old logo file
                            Console.WriteLine("Old logo file deleted successfully.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error while deleting the old logo file: {ex.Message}");
                            // Optionally, log the error and continue with the company deletion
                        }
                    }
                }

                // Log that the user was found and is about to be deleted
                Console.WriteLine($"Found user with ID: {key}. Preparing to delete.");

                // Remove the user record
                _context.AspNetUsers.Remove(model);

                // Log the removal of the user
                Console.WriteLine($"Removing user with ID: {key}");

                // Save the changes to the database
                await _context.SaveChangesAsync();

                // This is performed if user roles contains "System Administrator"
                await _notificationService.NotifySystemAdministratorDeletion(model.Email);

                // Log successful deletion
                Console.WriteLine($"Successfully deleted user with ID: {key}");

                // Return No Content status after successful deletion
                return NoContent();
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
                Console.WriteLine($"Error occurred while deleting user with ID: {key}. Error: {ex.Message}");
                var errorDetails = _errorCodeService.GetErrorDetails("INTERNAL_SERVER_ERROR");

                // Return a standardized error response
                return Json(new { success = false, message = errorDetails.ErrorMessage, error = ex.Message });
            }
        }


        [HttpPut]
        public async Task<ActionResult> UploadProfilePicture(string userId, IFormFile profilePicture)
        {
            // Log the start of the method
            Console.WriteLine("UploadProfilePicture method started for UserId: " + userId);

            if (profilePicture != null && profilePicture.Length > 0)
            {
                Console.WriteLine("Profile picture is not null for UserId: " + userId);

                // Step 1: Retrieve the current profile picture file path from the database using userId
                string currentProfilePicFilePath = GetCurrentProfilePicFilePath(userId);
                Console.WriteLine("Current profile picture file path retrieved: " + currentProfilePicFilePath);

                // Step 2: Ensure the folder path where profile pictures are stored
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "userProfileImages");
                Console.WriteLine("Folder path for saving profile pictures: " + folderPath);

                // Step 3: Check if there is a valid current profile picture file path, and delete the existing picture if it exists
                if (!string.IsNullOrEmpty(currentProfilePicFilePath))
                {
                    string currentProfilePicFullPath = Path.Combine(folderPath, currentProfilePicFilePath);
                    Console.WriteLine("Checking for the existence of current profile picture file: " + currentProfilePicFullPath);

                    // Step 4: Delete the existing profile picture file if it exists
                    if (System.IO.File.Exists(currentProfilePicFullPath))
                    {
                        try
                        {
                            Console.WriteLine("Deleting old profile picture file: " + currentProfilePicFullPath);
                            System.IO.File.Delete(currentProfilePicFullPath);  // Delete the old profile picture
                            Console.WriteLine("Old profile picture file deleted.");
                        }
                        catch (Exception ex)
                        {
                            var errorDetails = _errorCodeService.GetErrorDetails("FILE_DELETE_FAILED");
                            Console.WriteLine("Error while deleting old file: " + ex.Message);
                            return Json(new { success = false, message = errorDetails.ErrorMessage });
                        }
                    }
                    else
                    {
                        Console.WriteLine("Current profile picture file does not exist. Skipping deletion.");
                    }
                }
                else
                {
                    Console.WriteLine("No current profile picture file path found. Skipping profile picture deletion.");
                }

                // Step 5: Generate a new unique file name for the profile picture to avoid conflicts
                string newFileName = $"{userId}_{Path.GetFileName(profilePicture.FileName)}";
                Console.WriteLine("Generated new file name for the profile picture: " + newFileName);

                // Step 6: Construct the full path for the new profile picture
                string newProfilePicFilePath = Path.Combine(folderPath, newFileName);
                Console.WriteLine("New profile picture file path: " + newProfilePicFilePath);

                // Step 7: Save the new profile picture file to the server
                try
                {
                    using (var fileStream = new FileStream(newProfilePicFilePath, FileMode.Create))
                    {
                        Console.WriteLine("Saving new profile picture file to the server...");
                        await profilePicture.CopyToAsync(fileStream);
                        Console.WriteLine("New profile picture file saved successfully.");
                    }
                }
                catch (Exception ex)
                {
                    var errorDetails = _errorCodeService.GetErrorDetails("FILE_SAVE_FAILED");
                    Console.WriteLine("Error while saving the new file: " + ex.Message);
                    return Json(new { success = false, message = errorDetails.ErrorMessage });
                }

                // Step 8: Update the user's profile picture file path in the database with the new file name
                bool updateSuccess = UpdateUserProfilePicFilePath(userId, newFileName);
                if (!updateSuccess)
                {
                    var errorDetails = _errorCodeService.GetErrorDetails("DB_UPDATE_FAILED");
                    Console.WriteLine("Error while updating the user profile picture path in the database.");
                    return Json(new { success = false, message = errorDetails.ErrorMessage });
                }
                Console.WriteLine("Successfully updated the user profile picture path in the database.");

                // Step 9: Return the relative file path
                string relativeFilePath = $"../../img/userProfileImages/{newFileName}";
                Console.WriteLine("New profile picture file saved successfully. Returning relative path: " + relativeFilePath);

                // Return success response with the relative file path
                return Json(new { success = true, message = "Your profile has been successfully updated." });
            }
            else
            {
                var errorDetails = _errorCodeService.GetErrorDetails("NO_FILE_UPLOADED");
                Console.WriteLine("No file uploaded or file is empty.");
                return Json(new { success = false, message = errorDetails.ErrorMessage });
            }
        }

        // Method to retrieve the current profile picture file path from the database 
        private string GetCurrentProfilePicFilePath(string userId)
        {
            // Find the user by userId (string)
            var user = _context.AspNetUsers.FirstOrDefault(u => u.Id == userId);

            // Check if the user exists
            if (user != null)
            {
                Console.WriteLine($"Retrieved user profile picture path: {user.ProfileImagePath}");
                // Return the ProfilePictureFilePath if found
                return user.ProfileImagePath;
            }
            else
            {
                // Handle the case when the user is not found
                Console.WriteLine("User not found in the database.");
                return null;
            }
        }


        // Method to update the user's profile picture path in the database
        // Method to update the user's profile picture path in the database
        private bool UpdateUserProfilePicFilePath(string userId, string newFileName)
        {
            // Find the user by userId (string)
            var user = _context.AspNetUsers.FirstOrDefault(u => u.Id == userId);

            // Check if the user exists
            if (user != null)
            {
                try
                {
                    // Update the ProfilePictureFilePath with the new file name
                    user.ProfileImagePath = newFileName;

                    // Save changes to the database
                    _context.SaveChanges();

                    // Log success
                    Console.WriteLine("Successfully updated the user profile picture file path in the database.");

                    // Return true indicating the update was successful
                    return true;
                }
                catch (Exception ex)
                {
                    // Log the error and return false if there's an issue
                    Console.WriteLine("Error while updating the user profile picture file path: " + ex.Message);
                    return false;
                }
            }
            else
            {
                // If the user doesn't exist, log the error
                Console.WriteLine("User not found in the database.");
                return false;
            }
        }

        private void PopulateModel(AspNetUser model, IDictionary values)
        {
            string ID = nameof(AspNetUser.Id);
            string USER_NAME = nameof(AspNetUser.UserName);
            string NORMALIZED_USER_NAME = nameof(AspNetUser.NormalizedUserName);
            string EMAIL = nameof(AspNetUser.Email);
            string NORMALIZED_EMAIL = nameof(AspNetUser.NormalizedEmail);
            string EMAIL_CONFIRMED = nameof(AspNetUser.EmailConfirmed);
            string PASSWORD_HASH = nameof(AspNetUser.PasswordHash);
            string SECURITY_STAMP = nameof(AspNetUser.SecurityStamp);
            string CONCURRENCY_STAMP = nameof(AspNetUser.ConcurrencyStamp);
            string PHONE_NUMBER = nameof(AspNetUser.PhoneNumber);
            string PHONE_NUMBER_CONFIRMED = nameof(AspNetUser.PhoneNumberConfirmed);
            string TWO_FACTOR_ENABLED = nameof(AspNetUser.TwoFactorEnabled);
            string LOCKOUT_END = nameof(AspNetUser.LockoutEnd);
            string LOCKOUT_ENABLED = nameof(AspNetUser.LockoutEnabled);
            string ACCESS_FAILED_COUNT = nameof(AspNetUser.AccessFailedCount);
            string BIO_DATA = nameof(AspNetUser.BioData);  // Add BioData to the list

            // Existing property mappings
            if (values.Contains(ID))
            {
                model.Id = Convert.ToString(values[ID]);
            }

            if (values.Contains(USER_NAME))
            {
                model.UserName = Convert.ToString(values[USER_NAME]);
            }

            if (values.Contains(NORMALIZED_USER_NAME))
            {
                model.NormalizedUserName = Convert.ToString(values[NORMALIZED_USER_NAME]);
            }

            if (values.Contains(EMAIL))
            {
                model.Email = Convert.ToString(values[EMAIL]);
            }

            if (values.Contains(NORMALIZED_EMAIL))
            {
                model.NormalizedEmail = Convert.ToString(values[NORMALIZED_EMAIL]);
            }

            if (values.Contains(EMAIL_CONFIRMED))
            {
                model.EmailConfirmed = Convert.ToBoolean(values[EMAIL_CONFIRMED]);
            }

            if (values.Contains(PASSWORD_HASH))
            {
                model.PasswordHash = Convert.ToString(values[PASSWORD_HASH]);
            }

            if (values.Contains(SECURITY_STAMP))
            {
                model.SecurityStamp = Convert.ToString(values[SECURITY_STAMP]);
            }

            if (values.Contains(CONCURRENCY_STAMP))
            {
                model.ConcurrencyStamp = Convert.ToString(values[CONCURRENCY_STAMP]);
            }

            if (values.Contains(PHONE_NUMBER))
            {
                model.PhoneNumber = Convert.ToString(values[PHONE_NUMBER]);
            }

            if (values.Contains(PHONE_NUMBER_CONFIRMED))
            {
                model.PhoneNumberConfirmed = Convert.ToBoolean(values[PHONE_NUMBER_CONFIRMED]);
            }

            if (values.Contains(TWO_FACTOR_ENABLED))
            {
                model.TwoFactorEnabled = Convert.ToBoolean(values[TWO_FACTOR_ENABLED]);
            }

            if (values.Contains(LOCKOUT_END))
            {
                model.LockoutEnd = values[LOCKOUT_END] != null ? ConvertTo<System.DateTimeOffset>(values[LOCKOUT_END]) : (DateTimeOffset?)null;
            }

            if (values.Contains(LOCKOUT_ENABLED))
            {
                model.LockoutEnabled = Convert.ToBoolean(values[LOCKOUT_ENABLED]);
            }

            if (values.Contains(ACCESS_FAILED_COUNT))
            {
                model.AccessFailedCount = Convert.ToInt32(values[ACCESS_FAILED_COUNT]);
            }

            // New addition for BioData
            if (values.Contains(BIO_DATA))
            {
                model.BioData = Convert.ToString(values[BIO_DATA]);  // Update BioData field
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