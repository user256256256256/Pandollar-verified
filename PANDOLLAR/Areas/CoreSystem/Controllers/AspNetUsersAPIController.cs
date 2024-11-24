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

namespace MedisatERP.Controllers
{
    [Route("api/[controller]/[action]")]
    public class AspNetUsersAPIController : Controller
    {
        private PandollarDbContext _context;

        public AspNetUsersAPIController(PandollarDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of User Accounts with their associated address  data basing on the selected company.
        /// Supports filtering, sorting, and pagination.
        /// </summary>
        /// <param name="loadOptions">The options for filtering, sorting, and paging data.</param>
        /// <returns>Returns the processed User Accounts data.</returns>
        [HttpGet]
        public async Task<IActionResult> Get(DataSourceLoadOptions loadOptions, string roleId)
        {
            var users = _context.AspNetUsers
                .Include(u => u.Roles)  // Include roles to display them
                .Where(u => u.Roles.Any(r => r.Id == roleId))  // Filter users who have the given roleId
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.PhoneNumber,
                    u.EmailConfirmed,
                    u.PhoneNumberConfirmed,
                    u.LockoutEnabled,
                    // You can also select roles, if needed, but ensure you handle the collection properly
                    Roles = u.Roles.Select(r => r.Name).ToList()  // Example: selecting role names
                })
                .OrderBy(u => u.Id);  // You can adjust this sorting if needed

            // Apply filtering, sorting, and paging using DataSourceLoader
            var transformedData = await DataSourceLoader.LoadAsync(users, loadOptions);

            return Json(transformedData); // Return the processed data
        }


        // Not yet implemented --> While implementing you likely to abstract the Roles data;
        [HttpPost]
        public async Task<IActionResult> Post(string values)
        {
            var model = new AspNetUser();
            var valuesDict = JsonConvert.DeserializeObject<IDictionary>(values);
            PopulateModel(model, valuesDict);

            if (!TryValidateModel(model))
                return BadRequest(GetFullErrorMessage(ModelState));

            var result = _context.AspNetUsers.Add(model);
            await _context.SaveChangesAsync();

            return Json(new { result.Entity.Id });
        }

        // Incomplete, on execution -- the UserRoles table need to be updated both userId and roleId
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
                // Retrieve the user by unique identifier and include the roles
                var model = await _context.AspNetUsers
                    .Include(u => u.Roles)  // Include the Roles collection to manage the user's roles
                    .FirstOrDefaultAsync(u => u.Id == key);

                if (model == null)
                    return StatusCode(409, "User not found");

                // Deserialize the incoming updated values
                var valuesDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);

                // Log the deserialized dictionary for easier inspection
                Console.WriteLine("Deserialized values:");
                Console.WriteLine(JsonConvert.SerializeObject(valuesDict, Formatting.Indented)); // Pretty-print the JSON

                // Extract roles data from the request if provided
                var rolesData = valuesDict.ContainsKey("Roles") ? valuesDict["Roles"] : null;

                // If roles data is provided, update the user's roles
                if (rolesData != null)
                {
                    Console.WriteLine("Received Roles data:");
                    Console.WriteLine(JsonConvert.SerializeObject(rolesData, Formatting.Indented)); // Pretty-print roles data

                    // Clear the existing roles if necessary
                    model.Roles.Clear();

                    // Loop through the role names (rolesData is expected to be an object with role names as keys)
                    // If Roles is a string, treat it as a single role, otherwise as an array
                    if (rolesData is string roleName)
                    {
                        // Handle the case where Roles is a single string (e.g., "SystemAdministrator")
                        var role = await _context.AspNetRoles
                            .FirstOrDefaultAsync(r => r.Name == roleName);

                        if (role != null && !model.Roles.Contains(role))
                        {
                            model.Roles.Add(role); // Add the role to the user's roles
                        }
                    }
                    else if (rolesData is JArray roleArray)
                    {
                        // Handle the case where Roles is an array (e.g., ["SystemAdmin", "Editor"])
                        foreach (var roleArrayName in roleArray)  // Renamed to roleArrayName
                        {
                            var role = await _context.AspNetRoles
                                .FirstOrDefaultAsync(r => r.Name == roleArrayName.ToString());

                            if (role != null && !model.Roles.Contains(role))
                            {
                                model.Roles.Add(role); // Add the role to the user's roles
                            }
                        }
                    }
                }

                // Update other user information based on the provided values
                PopulateModel(model, valuesDict);

                // Validate the updated model
                if (!TryValidateModel(model))
                    return BadRequest(GetFullErrorMessage(ModelState));

                // Save the changes to the database
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                // Return an internal server error if an exception occurs
                return StatusCode(500, $"Internal Server error: {ex.Message}");
            }
        }



        [HttpDelete]
        public async Task Delete(string key)
        {
            var model = await _context.AspNetUsers.FirstOrDefaultAsync(item => item.Id == key);

            _context.AspNetUsers.Remove(model);
            await _context.SaveChangesAsync();
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