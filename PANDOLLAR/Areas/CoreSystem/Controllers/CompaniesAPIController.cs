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
using PANDOLLAR.Areas.CoreSystem.Models;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MedisatERP.Controllers
{
    [Route("api/[controller]/[action]")]
    public class CompaniesAPIController : Controller
    {
        private readonly  PandollarDbContext _context;

        // Constructor to initialize the context and HttpClient
        public CompaniesAPIController(PandollarDbContext context)
        {
            _context = context;
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
            var companies = _context.Companies
                                    .Include(c => c.Address)  // Ensure Address data is eagerly loaded
                                    .Select(i => new {
                                        i.CompanyId,
                                        i.CompanyName,
                                        i.ContactPerson,
                                        i.CompanyEmail,
                                        i.CompanyPhone,
                                        i.ExpDate,
                                        i.ApiCode,
                                        i.CompanyStatus,
                                        i.SubscriptionAmount,
                                        i.CompanyInitials,
                                        i.SmsAccount,
                                        i.PayAccount,
                                        i.PayBank,
                                        i.PayAccountName,
                                        i.Motto,
                                        i.CompanyType,
                                        i.CreatedAt,
                                        Address = new
                                        {
                                            i.Address.AddressId,
                                            i.Address.Street,
                                            i.Address.City,
                                            i.Address.State,
                                            i.Address.PostalCode,
                                            i.Address.Country
                                        }
                                    });

            // Apply filtering, sorting, and paging using DataSourceLoader
            var transformedData = await DataSourceLoader.LoadAsync(companies, loadOptions);

            return Json(transformedData); // Return the processed data
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

                // If address data is provided, populate the address model
                if (addressData != null)
                {
                    model.Address.Street = addressData["Street"]?.ToString();
                    model.Address.City = addressData["City"]?.ToString();
                    model.Address.State = addressData["State"]?.ToString();
                    model.Address.PostalCode = addressData["PostalCode"]?.ToString();
                    model.Address.Country = addressData["Country"]?.ToString();
                }

                // Set CreatedAt field if not already set
                if (model.CreatedAt == null)
                {
                    model.CreatedAt = DateTime.Now;
                }

                // Save the new company record to the database
                _context.Companies.Add(model);
                await _context.SaveChangesAsync();

                // Return the status ok (200) of the newly created company
                return Ok();
            }
            catch (Exception ex)
            {
                // Return an internal server error if an exception occurs
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
                    return StatusCode(404, "Company not found");

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
                    return BadRequest(GetFullErrorMessage(ModelState));

                // Save the changes to the database
                await _context.SaveChangesAsync();

                return Ok(); // Return a success response
            }
            catch (Exception ex)
            {
                // Return an internal server error if an exception occurs
                return StatusCode(500, $"Internal Server error: {ex.Message}");
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
                    // Return not found if the company does not exist
                    return NotFound($"Company with ID {key} not found.");
                }

                // Remove the associated address, if it exists
                if (model.Address != null)
                {
                    _context.CompanyAddresses.Remove(model.Address);
                }

                // Remove the company record itself
                _context.Companies.Remove(model);

                // Save the changes to the database
                await _context.SaveChangesAsync();

                return NoContent(); // Return No Content status after successful deletion
            }
            catch (Exception ex)
            {
                // Return an internal server error if an exception occurs
                return StatusCode(500, $"An internal server error occurred: {ex.Message}");
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
            string EXP_DATE = nameof(Company.ExpDate);
            string API_CODE = nameof(Company.ApiCode);
            string COMPANY_STATUS = nameof(Company.CompanyStatus);
            string SUBSCRIPTION_AMOUNT = nameof(Company.SubscriptionAmount);
            string COMPANY_INITIALS = nameof(Company.CompanyInitials);
            string SMS_ACCOUNT = nameof(Company.SmsAccount);
            string PAY_ACCOUNT = nameof(Company.PayAccount);
            string PAY_BANK = nameof(Company.PayBank);
            string PAY_ACCOUNT_NAME = nameof(Company.PayAccountName);
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

            if (values.ContainsKey(EXP_DATE))
            {
                model.ExpDate = values[EXP_DATE] != null ? Convert.ToDateTime(values[EXP_DATE]) : (DateTime?)null;
            }

            if (values.ContainsKey(API_CODE))
            {
                model.ApiCode = Convert.ToString(values[API_CODE]);
            }

            if (values.ContainsKey(COMPANY_STATUS))
            {
                model.CompanyStatus = Convert.ToString(values[COMPANY_STATUS]);
            }

            if (values.ContainsKey(SUBSCRIPTION_AMOUNT))
            {
                model.SubscriptionAmount = values[SUBSCRIPTION_AMOUNT] != null ? Convert.ToDecimal(values[SUBSCRIPTION_AMOUNT], CultureInfo.InvariantCulture) : (decimal?)null;
            }

            if (values.ContainsKey(COMPANY_INITIALS))
            {
                model.CompanyInitials = Convert.ToString(values[COMPANY_INITIALS]);
            }

            if (values.ContainsKey(SMS_ACCOUNT))
            {
                model.SmsAccount = Convert.ToString(values[SMS_ACCOUNT]);
            }

            if (values.ContainsKey(PAY_ACCOUNT))
            {
                model.PayAccount = Convert.ToString(values[PAY_ACCOUNT]);
            }

            if (values.ContainsKey(PAY_BANK))
            {
                model.PayBank = Convert.ToString(values[PAY_BANK]);
            }

            if (values.ContainsKey(PAY_ACCOUNT_NAME))
            {
                model.PayAccountName = Convert.ToString(values[PAY_ACCOUNT_NAME]);
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

            if (values.ContainsKey(CREATED_AT))
            {
                model.CreatedAt = values[CREATED_AT] != null ? Convert.ToDateTime(values[CREATED_AT]) : (DateTime?)null;
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
    }
}
