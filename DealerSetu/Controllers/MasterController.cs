using DealerSetu_Data.Common;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Mvc;
using DealerSetu.Repository.Common;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu.Controllers
{
    public class MasterController : ControllerBase
    {
        private readonly IMasterService _masterService;
        private readonly Utility _utility;
        private readonly JwtHelper _jwtHelper;
        private readonly IFileValidationService _fileValidationService;

        public MasterController(
            IMasterService masterService,
            Utility utility,
            JwtHelper jwtHelper,
            IConfiguration configuration,
            IFileValidationService fileValidationService)
        {
            if (masterService == null) throw new ArgumentNullException(nameof(masterService));
            if (utility == null) throw new ArgumentNullException(nameof(utility));
            if (jwtHelper == null) throw new ArgumentNullException(nameof(jwtHelper));
            if (fileValidationService == null) throw new ArgumentNullException(nameof(fileValidationService));

            _masterService = masterService;
            _utility = utility;
            _jwtHelper = jwtHelper;
            _fileValidationService = fileValidationService;
        }

        [HttpGet("GetEmployeeList")]
        public async Task<IActionResult> GetEmployeeList(string keyword, string role, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                var response = await _masterService.EmployeeMasterService(keyword, role, pageIndex, pageSize);

                if (response.isError == true)
                {
                    return StatusCode(500, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateErrorResponse(ex.Message));
            }
        }

        [HttpGet("GetRoles")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var response = await _masterService.RolesDropdownService();

                if (response.isError == true)
                {
                    return StatusCode(500, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateErrorResponse(ex.Message));
            }
        }

        [HttpGet("DownloadFiles")]
        public async Task<IActionResult> DownloadFormats(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(new { error = "Filename cannot be empty or null." });
            }

            if (!_utility.IsValidFileName(fileName))
            {
                return BadRequest(new { error = "Invalid filename format." });
            }

            try
            {
                var downloadUrl = await _masterService.GenerateDownloadUrlAsync(fileName);

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    return NotFound(new { error = "File not found or unavailable." });
                }

                return Ok(new { downloadUrl });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }

        [HttpPost("UploadEmployeeExcelFile")]
        public async Task<IActionResult> UploadEmployeeExcelFile(IFormFile file, string role)
        {
            if (file == null || string.IsNullOrEmpty(role))
            {
                return BadRequest(new { success = false, message = "File or role is missing." });
            }

            try
            {
                var result = await _masterService.ProcessEmployeeExcelFile(file, role);

                if ((bool)result.isError)
                {
                    return BadRequest(new { success = false, message = result.Message });
                }

                return Ok(new { success = true, message = "File processed and data saved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred.",
                    errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("UploadDealerExcelFile")]
        public async Task<IActionResult> UploadDealerExcelFile(IFormFile file)
        {
            if (file == null)
            {
                return BadRequest(new { success = false, message = "File is missing." });
            }

            try
            {
                var result = await _masterService.ProcessDealerExcelFile(file);

                if ((bool)result.isError)
                {
                    return BadRequest(new { success = false, message = result.Message });
                }

                return Ok(new { success = true, message = "File processed and data saved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred.",
                    errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("GetDealerList")]
        public async Task<IActionResult> GetDealerList([FromBody] DealerListRequest request)
        {
            if (request == null)
            {
                return BadRequest(CreateErrorResponse("Invalid request data.", "400"));
            }

            try
            {
                var keyword = string.IsNullOrWhiteSpace(request.Keyword) || request.Keyword == "string" ? null : request.Keyword;
                var response = await _masterService.DealerMasterService(keyword, request.PageIndex, request.PageSize);

                if (response.isError == true)
                {
                    return StatusCode(500, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateErrorResponse(ex.Message));
            }
        }

        [HttpPost("UpdateEmployeeDetails")]
        public async Task<IActionResult> UpdateEmployeeDetails([FromBody] UpdateEmployeeDetailsRequest request)
        {
            try
            {
                // Extract and validate UserId from JWT token
                var empIdClaim = _jwtHelper.GetClaimValue(HttpContext, "UserId");
                if (string.IsNullOrEmpty(empIdClaim))
                {
                    return BadRequest(new { error = "Missing UserId claim in the token" });
                }

                // Input validation
                var validationResult = ValidateEmployeeUpdateRequest(request);
                if (validationResult != null)
                {
                    return validationResult;
                }

                // Sanitize inputs
                string sanitizedName = _fileValidationService.SanitizeInput(request.EmpName);
                string sanitizedEmail = _fileValidationService.SanitizeEmail(request.Email);

                // Update employee details with sanitized inputs
                var result = await _masterService.UpdateEmployeeDetailsService(
                    request.UserId,
                    sanitizedName,
                    sanitizedEmail
                );

                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { rowsAffected = result.RowsAffected, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }

        [HttpPost("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteUserRequest request)
        {
            try
            {
                if (request == null || request.UserId == null)
                {
                    return BadRequest(new { error = "UserId cannot be empty" });
                }

                var message = await _masterService.DeleteUserService(request.UserId);
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }

        [HttpPost("UpdateDealerDetails")]
        public async Task<IActionResult> UpdateDealerDetails([FromBody] UpdateDealerDetailsRequest request)
        {
            try
            {
                // Input validation
                var validationResult = ValidateDealerUpdateRequest(request);
                if (validationResult != null)
                {
                    return validationResult;
                }

                // Sanitize inputs
                var sanitizedInputs = SanitizeDealerInputs(request);

                // Update dealer details
                var result = await _masterService.UpdateDealerDetailsService(
                    request.UserId,
                    sanitizedInputs.Item1, // EmpName
                    sanitizedInputs.Item2, // Location
                    sanitizedInputs.Item3, // District
                    sanitizedInputs.Item4, // Zone
                    sanitizedInputs.Item5  // State
                );

                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { rowsAffected = result.RowsAffected, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }

        #region Helper Methods

        private ServiceResponse CreateErrorResponse(string errorMessage, string code = "500")
        {
            return new ServiceResponse
            {
                isError = true,
                Error = errorMessage,
                Message = "An unexpected error occurred",
                Status = "Error",
                Code = code
            };
        }

        private IActionResult ValidateEmployeeUpdateRequest(UpdateEmployeeDetailsRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request cannot be null" });
            }

            if (request.UserId == null || !request.UserId.HasValue)
            {
                return BadRequest(new { error = "UserId cannot be empty" });
            }

            if (string.IsNullOrWhiteSpace(request.EmpName) || request.EmpName == "string")
            {
                return BadRequest(new { error = "Employee Name cannot be empty" });
            }

            var sanitizedEmpName = _fileValidationService.SanitizeInput(request.EmpName);
            if (string.IsNullOrWhiteSpace(sanitizedEmpName))
            {
                return BadRequest(new { error = "Employee Name contains only invalid characters" });
            }

            if (string.IsNullOrWhiteSpace(request.Email) || request.Email == "string")
            {
                return BadRequest(new { error = "Email cannot be empty" });
            }

            var sanitizedEmail = _fileValidationService.SanitizeEmail(request.Email);
            if (string.IsNullOrWhiteSpace(sanitizedEmail) || !_utility.IsValidEmail(sanitizedEmail))
            {
                return BadRequest(new { error = "Invalid email format" });
            }

            return null;
        }

        private IActionResult ValidateDealerUpdateRequest(UpdateDealerDetailsRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request cannot be null" });
            }

            if (request.UserId == null || !request.UserId.HasValue)
            {
                return BadRequest(new { error = "UserId cannot be empty" });
            }

            if (string.IsNullOrWhiteSpace(request.EmpName) || request.EmpName == "string")
            {
                return BadRequest(new { error = "Employee name cannot be empty" });
            }

            var sanitizedEmpName = _fileValidationService.SanitizeInput(request.EmpName);
            if (string.IsNullOrWhiteSpace(sanitizedEmpName))
            {
                return BadRequest(new { error = "Employee name contains only invalid characters" });
            }

            return null;
        }

        private Tuple<string, string, string, string, string> SanitizeDealerInputs(UpdateDealerDetailsRequest request)
        {
            string empName = _fileValidationService.SanitizeInput(request.EmpName);
            string location = request.Location != "string" ? _fileValidationService.SanitizeInput(request.Location) : null;
            string district = request.District != "string" ? _fileValidationService.SanitizeInput(request.District) : null;
            string zone = request.Zone != "string" ? _fileValidationService.SanitizeInput(request.Zone) : null;
            string state = request.State != "string" ? _fileValidationService.SanitizeInput(request.State) : null;

            return new Tuple<string, string, string, string, string>(
                empName, location, district, zone, state
            );
        }

        #endregion
    }
}