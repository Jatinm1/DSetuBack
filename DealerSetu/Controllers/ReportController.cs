using DealerSetu_Data.Common;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Mvc;
using DealerSetu.Repository.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu.Controllers
{
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly JwtHelper _jwtHelper;
        private readonly ValidationHelper _validationHelper;
        private readonly Utility _utility;
        private readonly ILogger<ReportController> _logger;
        private readonly FileLoggerService _fileLogger;

        public ReportController(
            IReportService reportService,
            JwtHelper jwtHelper,
            ValidationHelper validationHelper,
            ILogger<ReportController> logger,
            Utility utility,
            FileLoggerService fileLogger)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _jwtHelper = jwtHelper ?? throw new ArgumentNullException(nameof(jwtHelper));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _utility = utility ?? throw new ArgumentNullException(nameof(utility));
            _fileLogger = fileLogger ?? throw new ArgumentNullException(nameof(fileLogger));
        }

        [HttpPost("GetReportList")]
        public async Task<IActionResult> GetReportList([FromBody] ReportRequest payload)
        {
            _logger.LogInformation("GetReportList request received");
            //_fileLogger.LogInformation("ReportController", "GetReportList request received");

            try
            {
                // Input validation
                if (payload == null)
                {
                    _fileLogger.LogWarning("ReportController", "Invalid payload - Request body is null");
                    return BadRequest(_utility.CreateErrorResponse("Invalid payload", "Request body cannot be null.", "400"));
                }

                // Validate pagination and date range
                var validationError = ValidateReportRequest(payload);
                if (validationError != null)
                {
                    _fileLogger.LogWarning("ReportController", $"Validation error in GetReportList");
                    return BadRequest(validationError);
                }

                // Extract and validate claims
                var (empNo, roleId, authError) = GetAndValidateAuthClaims();
                if (authError != null)
                {
                    _fileLogger.LogWarning("ReportController", "Authentication error - Invalid claims");
                    return Unauthorized(authError);
                }

                // Construct filter model with validated input
                var filter = CreateFilterModel(payload.From, payload.To, empNo, roleId);
                //_fileLogger.LogInformation("ReportController", $"Filter created with date range: {payload.From} to {payload.To}");

                // Call the service layer and validate the response
                var response = await _reportService.RequestSectionReportService(filter, payload.PageIndex, payload.PageSize);

                if (response == null)
                {
                    _fileLogger.LogInformation("ReportController", "No reports found for the given filters");
                    return NotFound(_utility.CreateErrorResponse("No data found", "No reports found for the given filters.", "404"));
                }

                //_fileLogger.LogInformation("ReportController", "GetReportList executed successfully");
                // Return successful response
                return Ok(response);
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("ReportController", "Exception in GetReportList", ex);
                return await _utility.HandleInternalServerError(this, ex, _logger, "GetReportList");
            }
        }

        [HttpPost("GetRejectList")]
        public async Task<IActionResult> GetRejectList([FromBody] RejectListRequest request)
        {
            _logger.LogInformation("GetRejectList request received");
            //_fileLogger.LogInformation("ReportController", "GetRejectList request received");

            try
            {
                // Validate the request payload
                if (request == null)
                {
                    _fileLogger.LogWarning("ReportController", "Invalid request - Request payload is null");
                    return BadRequest(_utility.CreateErrorResponse("Invalid Request", "Request payload cannot be null.", "400"));
                }

                // Validate date range
                var dateRangeValidation = _validationHelper.ValidateDateRange(request.From, request.To);
                if (dateRangeValidation != null)
                {
                    _fileLogger.LogWarning("ReportController", "Date range validation error");
                    return BadRequest(dateRangeValidation);
                }

                // Extract and validate claims
                var (empNo, roleId, authError) = GetAndValidateAuthClaims();
                if (authError != null)
                {
                    _fileLogger.LogWarning("ReportController", "Authentication error - Invalid claims");
                    return Unauthorized(authError);
                }

                // Construct filter model with validated input
                var filter = CreateFilterModel(request.From, request.To, empNo, roleId);
                //_fileLogger.LogInformation("ReportController", $"Filter created with date range: {request.From} to {request.To}");

                // Call the service layer to fetch the rejection list
                var result = await _reportService.RejectedRequestReportService(filter);

                // Handle cases where no data is found
                if (result == null)
                {
                    _fileLogger.LogInformation("ReportController", "No rejection records found for the given filters");
                    return NotFound(_utility.CreateErrorResponse("No Data Found", "No rejection records found for the given filters.", "404"));
                }

                //_fileLogger.LogInformation("ReportController", "GetRejectList executed successfully");
                // Return the successful response with data
                return Ok(_utility.CreateSuccessResponse(
                    "Rejection records retrieved successfully.", result));
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("ReportController", "Exception in GetRejectList", ex);
                return await _utility.HandleInternalServerError(this, ex, _logger, "GetRejectList");
            }
        }

        [HttpGet("GetNewDealerstatewise")]
        public async Task<IActionResult> GetNewDealerstatewise(int fy)
        {
            _logger.LogInformation($"GetNewDealerstatewise request received for fiscal year: {fy}");
            //_fileLogger.LogInformation("ReportController", $"GetNewDealerstatewise request received for fiscal year: {fy}");

            try
            {
                // Validate the 'fy' parameter
                if (!_utility.IsValidFiscalYear(fy, out string errorMessage))
                {
                    _fileLogger.LogWarning("ReportController", $"Invalid fiscal year: {fy}, Error: {errorMessage}");
                    return BadRequest(_utility.CreateErrorResponse("Invalid fiscal year", errorMessage, "400"));
                }

                //_fileLogger.LogInformation("ReportController", $"Calling NewDealerStatewiseReportService with fiscal year: {fy}");
                // Call the service method
                List<DealerstateModel> result = await _reportService.NewDealerStatewiseReportService(fy);

                // Check if the service returned any data
                if (result == null || result.Count == 0)
                {
                    _fileLogger.LogInformation("ReportController", $"No data found for fiscal year: {fy}");
                    return NotFound(_utility.CreateErrorResponse("No data found", "No data found for the specified fiscal year.", "404"));
                }

                //_fileLogger.LogInformation("ReportController", $"GetNewDealerstatewise executed successfully for fiscal year: {fy}, returned {result.Count} records");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("ReportController", $"Exception in GetNewDealerstatewise for fiscal year: {fy}", ex);
                return await _utility.HandleInternalServerError(this, ex, _logger, "GetNewDealerstatewise");
            }
        }

        [HttpPost("GetDemoReqList")]
        public async Task<IActionResult> GetDemoReqList([FromBody] DemoReqListRequest request)
        {
            _logger.LogInformation("GetDemoReqList request received");
            //_fileLogger.LogInformation("ReportController", "GetDemoReqList request received");

            try
            {
                // Validate the request body
                if (request == null)
                {
                    _fileLogger.LogWarning("ReportController", "GetDemoReqList - Request body is empty");
                    return BadRequest(_utility.CreateErrorResponse("Request body is empty", "Please provide the request body.", "400"));
                }

                // Initialize filter parameters
                var filter = new FilterModel();

                // Validate and sanitize ReqNo
                if (!string.IsNullOrWhiteSpace(request.ReqNo))
                {
                    if (!_utility.ValidateStringInput(request.ReqNo, 50, out string errorMessage))
                    {
                        _fileLogger.LogWarning("ReportController", $"Invalid request number: {request.ReqNo}, Error: {errorMessage}");
                        return BadRequest(_utility.CreateErrorResponse("Invalid request number", errorMessage, "400"));
                    }
                    filter.RequestNo = request.ReqNo.Trim();
                    //_fileLogger.LogInformation("ReportController", $"Request number filter applied: {filter.RequestNo}");
                }

                // Validate pagination and date range
                var validationError = ValidatePagedRequest(request.PageIndex, request.PageSize, request.From, request.To);
                if (validationError != null)
                {
                    _fileLogger.LogWarning("ReportController", "GetDemoReqList - Validation failed for pagination or date range");
                    return BadRequest(validationError);
                }

                filter.From = request.From;
                filter.To = request.To;
                //_fileLogger.LogInformation("ReportController", $"Date range filter applied: {request.From} to {request.To}");

                // Call the service method
                //_fileLogger.LogInformation("ReportController", "Calling DemoTractorReportService");
                var response = await _reportService.DemoTractorReportService(filter, request.PageIndex, request.PageSize);

                // Check if the response contains data
                if (response == null || response.result == null)
                {
                    _fileLogger.LogInformation("ReportController", "GetDemoReqList - No data found");
                    return NotFound(_utility.CreateSuccessResponse("No data found."));
                }

                //_fileLogger.LogInformation("ReportController", "GetDemoReqList executed successfully");
                // Return the response
                return Ok(response);
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("ReportController", "Exception in GetDemoReqList", ex);
                return await _utility.HandleInternalServerError(this, ex, _logger, "GetDemoReqList");
            }
        }

        [HttpGet("GetDropdownOptions")]
        public IActionResult GetDropdownOptions()
        {
            _logger.LogInformation("GetDropdownOptions request received");
            //_fileLogger.LogInformation("ReportController", "GetDropdownOptions request received");

            try
            {
                // Dropdown options with keys and values
                var dropdownOptions = new List<DropdownOption>
                {
                    new DropdownOption { Key = 0, Value = "All" },
                    new DropdownOption { Key = 1, Value = "Pending by HO" }
                };

                //_fileLogger.LogInformation("ReportController", "GetDropdownOptions executed successfully");
                return Ok(dropdownOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetDropdownOptions");
                _fileLogger.LogError("ReportController", "Exception in GetDropdownOptions", ex);
                return StatusCode(500, _utility.CreateErrorResponse("Server Error", "An error occurred while fetching dropdown options.", "500"));
            }
        }

        [HttpPost("GetNewDealerActivityListing")]
        public async Task<IActionResult> GetNewDealerActivityListing([FromBody] DealerActivityRequest request)
        {
            _logger.LogInformation("GetNewDealerActivityListing request received");
            //_fileLogger.LogInformation("ReportController", "GetNewDealerActivityListing request received");

            try
            {
                // Check if the request body is null
                if (request == null)
                {
                    _fileLogger.LogWarning("ReportController", "GetNewDealerActivityListing - Request body is empty");
                    return BadRequest(_utility.CreateErrorResponse("Request body is empty", "Please provide the request body.", "400"));
                }

                // For non-export requests, validate pagination
                if (!request.Export)
                {
                    var paginationValidation = _validationHelper.ValidatePagination((int)request.PageIndex, (int)request.PageSize);
                    if (paginationValidation != null)
                    {
                        _fileLogger.LogWarning("ReportController", $"Pagination validation error: PageIndex={request.PageIndex}, PageSize={request.PageSize}");
                        return BadRequest(paginationValidation);
                    }
                }

                // Initialize the filter model
                var filter = new FilterModel { Export = request.Export };
                //_fileLogger.LogInformation("ReportController", $"Export flag set to: {request.Export}");

                // Validate and apply ReqNo filter if provided
                if (!string.IsNullOrWhiteSpace(request.ReqNo))
                {
                    if (!_utility.ValidateStringInput(request.ReqNo, 50, out string errorMessage))
                    {
                        _fileLogger.LogWarning("ReportController", $"Invalid request number: {request.ReqNo}, Error: {errorMessage}");
                        return BadRequest(_utility.CreateErrorResponse("Invalid request number", errorMessage, "400"));
                    }
                    filter.RequestNo = request.ReqNo.Trim();
                    //_fileLogger.LogInformation("ReportController", $"Request number filter applied: {filter.RequestNo}");
                }

                // Validate date range
                var dateRangeValidation = _validationHelper.ValidateDateRange(request.From, request.To);
                if (dateRangeValidation != null)
                {
                    _fileLogger.LogWarning("ReportController", $"Date range validation error: From={request.From}, To={request.To}");
                    return BadRequest(dateRangeValidation);
                }

                filter.From = request.From;
                filter.To = request.To;
                //_fileLogger.LogInformation("ReportController", $"Date range filter applied: {request.From} to {request.To}");

                // Determine the PendingByHO filter based on the dropdown selection
                bool? pendingByHO = request.DropdownSelection switch
                {
                    1 => true,  // Pending by HO
                    0 => null,  // All
                    _ => null   // Default case
                };
                //_fileLogger.LogInformation("ReportController", $"PendingByHO filter set to: {pendingByHO}");

                // Call the service method
                //_fileLogger.LogInformation("ReportController", "Calling NewDealerActivityReportService");
                var response = await _reportService.NewDealerActivityReportService(filter, pendingByHO, (int)request.PageIndex, (int)request.PageSize);

                // Check if the response is null or empty
                if (response == null || response.result == null)
                {
                    _fileLogger.LogInformation("ReportController", "GetNewDealerActivityListing - No data found");
                    return NotFound(_utility.CreateSuccessResponse("No data found."));
                }

                //_fileLogger.LogInformation("ReportController", "GetNewDealerActivityListing executed successfully");
                // Return the successful response
                return Ok(response);
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("ReportController", "Exception in GetNewDealerActivityListing", ex);
                return await _utility.HandleInternalServerError(this, ex, _logger, "GetNewDealerActivityListing");
            }
        }

        [HttpPost("GetNewDealerClaimListing")]
        public async Task<IActionResult> GetNewDealerClaimListing([FromBody] GetNewDealerClaimListingRequest request)
        {
            _logger.LogInformation("GetNewDealerClaimListing request received");
            //_fileLogger.LogInformation("ReportController", "GetNewDealerClaimListing request received");

            try
            {
                // Check if the request is null
                if (request == null)
                {
                    _fileLogger.LogWarning("ReportController", "GetNewDealerClaimListing - Request body is empty");
                    return BadRequest(_utility.CreateErrorResponse("Request body is empty", "Please provide the required request parameters.", "400"));
                }

                // Validate pagination
                var paginationValidation = _validationHelper.ValidatePagination(request.PageIndex, request.PageSize);
                if (paginationValidation != null)
                {
                    _fileLogger.LogWarning("ReportController", $"Pagination validation error: PageIndex={request.PageIndex}, PageSize={request.PageSize}");
                    return BadRequest(paginationValidation);
                }

                // Validate request number
                var requestNoValidation = _validationHelper.ValidateRequestNo(request.RequestNo);
                if (requestNoValidation != null)
                {
                    _fileLogger.LogWarning("ReportController", $"Request number validation error: {request.RequestNo}");
                    return BadRequest(requestNoValidation);
                }

                // Validate date range
                var dateRangeValidation = _validationHelper.ValidateDateRange(request.From, request.To);
                if (dateRangeValidation != null)
                {
                    _fileLogger.LogWarning("ReportController", $"Date range validation error: From={request.From}, To={request.To}");
                    return BadRequest(dateRangeValidation);
                }

                // Initialize the filter model
                var filter = new FilterModel
                {
                    RequestNo = request.RequestNo,
                    From = request.From,
                    To = request.To
                };
                //_fileLogger.LogInformation("ReportController", $"Filter created - ReqNo: {request.RequestNo}, Date range: {request.From} to {request.To}");

                // Call the service method
                //_fileLogger.LogInformation("ReportController", "Calling NewDealerClaimReportService");
                var response = await _reportService.NewDealerClaimReportService(filter, request.PageIndex, request.PageSize);

                if (response == null)
                {
                    _fileLogger.LogInformation("ReportController", "GetNewDealerClaimListing - No data found");
                    return NotFound(_utility.CreateSuccessResponse("No data found."));
                }

                //_fileLogger.LogInformation("ReportController", "GetNewDealerClaimListing executed successfully");
                // Return the response
                return Ok(response);
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("ReportController", "Exception in GetNewDealerClaimListing", ex);
                return await _utility.HandleInternalServerError(this, ex, _logger, "GetNewDealerClaimListing");
            }
        }

        #region Helper Methods

        private object ValidateReportRequest(ReportRequest request)
        {
            // Validate pagination
            var paginationValidation = _validationHelper.ValidatePagination(request.PageIndex, request.PageSize);
            if (paginationValidation != null)
            {
                _fileLogger.LogWarning("ReportController", $"Pagination validation error: PageIndex={request.PageIndex}, PageSize={request.PageSize}");
                return paginationValidation;
            }

            // Validate date range
            var dateRangeValidation = _validationHelper.ValidateDateRange(request.From, request.To);
            if (dateRangeValidation != null)
            {
                _fileLogger.LogWarning("ReportController", $"Date range validation error: From={request.From}, To={request.To}");
                return dateRangeValidation;
            }

            return null;
        }

        private object ValidatePagedRequest(int pageIndex, int pageSize, DateTime? from, DateTime? to)
        {
            // Validate pagination
            var paginationValidation = _validationHelper.ValidatePagination(pageIndex, pageSize);
            if (paginationValidation != null)
            {
                _fileLogger.LogWarning("ReportController", $"Pagination validation error: PageIndex={pageIndex}, PageSize={pageSize}");
                return paginationValidation;
            }

            // Validate date range
            var dateRangeValidation = _validationHelper.ValidateDateRange(from, to);
            if (dateRangeValidation != null)
            {
                _fileLogger.LogWarning("ReportController", $"Date range validation error: From={from}, To={to}");
                return dateRangeValidation;
            }

            return null;
        }

        private (string EmpNo, string RoleId, object ErrorResponse) GetAndValidateAuthClaims()
        {
            var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
            var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

            if (string.IsNullOrEmpty(empNo) || string.IsNullOrEmpty(roleId))
            {
                _fileLogger.LogWarning("ReportController", "Authentication failed: UserId or RoleId is not present or invalid in the token");
                return (null, null, _utility.CreateErrorResponse(
                    "Unauthorized", "UserId or RoleId is not present or invalid in the token.", "401"));
            }

            return (empNo.Trim(), roleId.Trim(), null);
        }

        private FilterModel CreateFilterModel(DateTime? from, DateTime? to, string empNo, string roleId)
        {
            return new FilterModel
            {
                From = from,
                To = to,
                EmpNo = empNo,
                RoleId = roleId
            };
        }

        #endregion
    }
}