using DealerSetu.Repository.Common;
using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DealerSetu.Controllers
{
    [Authorize(Policy = "ReportSectionAccess")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly JwtHelper _jwtHelper;
        private readonly ValidationHelper _validationHelper;
        private readonly Utility _utility;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IReportService reportService,
            JwtHelper jwtHelper,
            ValidationHelper validationHelper,
            ILogger<ReportController> logger,
            Utility utility)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _jwtHelper = jwtHelper ?? throw new ArgumentNullException(nameof(jwtHelper));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _utility = utility ?? throw new ArgumentNullException(nameof(utility));
        }

        [HttpPost("GetReportList")]
        public async Task<IActionResult> GetReportList([FromBody] ReportRequest payload)
        {
            try
            {
                if (payload == null)
                    return BadRequest(_utility.CreateErrorResponse("Invalid payload", "Request body cannot be null.", "400"));

                var validationError = ValidateRequest(payload.PageIndex, payload.PageSize, payload.From, payload.To);
                if (validationError != null)
                    return BadRequest(validationError);

                var (empNo, roleId, authError) = GetAndValidateAuthClaims();
                if (authError != null)
                    return Unauthorized(authError);

                var filter = CreateFilterModel(payload.From, payload.To, empNo, roleId);
                var response = await _reportService.RequestSectionReportService(filter, payload.PageIndex, payload.PageSize);

                return response == null
                    ? NotFound(_utility.CreateErrorResponse("No data found", "No reports found for the given filters.", "404"))
                    : Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportList");
                return StatusCode(500, _utility.CreateErrorResponse("Server Error", "An unexpected error occurred while processing your request.", "500"));
            }
        }

        [HttpPost("GetRejectList")]
        public async Task<IActionResult> GetRejectList([FromBody] RejectListRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(_utility.CreateErrorResponse("Invalid Request", "Request payload cannot be null.", "400"));

                var dateRangeValidation = _validationHelper.ValidateDateRange(request.From, request.To);
                if (dateRangeValidation != null)
                    return BadRequest(dateRangeValidation);

                var (empNo, roleId, authError) = GetAndValidateAuthClaims();
                if (authError != null)
                    return Unauthorized(authError);

                var filter = CreateFilterModel(request.From, request.To, empNo, roleId);
                var result = await _reportService.RejectedRequestReportService(filter);

                return result == null
                    ? NotFound(_utility.CreateErrorResponse("No Data Found", "No rejection records found for the given filters.", "404"))
                    : Ok(_utility.CreateSuccessResponse("Rejection records retrieved successfully.", result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRejectList");
                return StatusCode(500, _utility.CreateErrorResponse("Server Error", "An unexpected error occurred while processing your request.", "500"));
            }
        }

        [HttpGet("GetNewDealerstatewise")]
        public async Task<IActionResult> GetNewDealerstatewise(int fy)
        {
            try
            {
                if (!_utility.IsValidFiscalYear(fy, out string errorMessage))
                    return BadRequest(_utility.CreateErrorResponse("Invalid fiscal year", errorMessage, "400"));

                var result = await _reportService.NewDealerStatewiseReportService(fy);

                return result == null || result.Count == 0
                    ? NotFound(_utility.CreateErrorResponse("No data found", "No data found for the specified fiscal year.", "404"))
                    : Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNewDealerstatewise");
                return StatusCode(500, _utility.CreateErrorResponse("Server Error", "An unexpected error occurred while processing your request.", "500"));
            }
        }

        [HttpPost("GetDemoReqList")]
        public async Task<IActionResult> GetDemoReqList([FromBody] DemoReqListRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(_utility.CreateErrorResponse("Request body is empty", "Please provide the request body.", "400"));

                var filter = new FilterModel();

                if (!string.IsNullOrWhiteSpace(request.ReqNo))
                {
                    if (!_utility.ValidateStringInput(request.ReqNo, 50, out string errorMessage))
                        return BadRequest(_utility.CreateErrorResponse("Invalid request number", errorMessage, "400"));
                    filter.RequestNo = request.ReqNo.Trim();
                }

                var validationError = ValidateRequest(request.PageIndex, request.PageSize, request.From, request.To);
                if (validationError != null)
                    return BadRequest(validationError);

                filter.From = request.From;
                filter.To = request.To;

                var response = await _reportService.DemoTractorReportService(filter, request.PageIndex, request.PageSize);

                return response == null || response.result == null
                    ? NotFound(_utility.CreateSuccessResponse("No data found."))
                    : Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDemoReqList");
                return StatusCode(500, _utility.CreateErrorResponse("Server Error", "An unexpected error occurred while processing your request.", "500"));
            }
        }

        [HttpGet("GetDropdownOptions")]
        public IActionResult GetDropdownOptions()
        {
            try
            {
                var dropdownOptions = new List<DropdownOption>
                {
                    new DropdownOption { Key = 0, Value = "All" },
                    new DropdownOption { Key = 1, Value = "Pending by HO" }
                };

                return Ok(dropdownOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDropdownOptions");
                return StatusCode(500, _utility.CreateErrorResponse("Server Error", "An unexpected error occurred while processing your request.", "500"));
            }
        }

        [HttpPost("GetNewDealerActivityListing")]
        public async Task<IActionResult> GetNewDealerActivityListing([FromBody] DealerActivityRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(_utility.CreateErrorResponse("Request body is empty", "Please provide the request body.", "400"));

                if (!request.Export)
                {
                    var paginationValidation = _validationHelper.ValidatePagination((int)request.PageIndex, (int)request.PageSize);
                    if (paginationValidation != null)
                        return BadRequest(paginationValidation);
                }

                var filter = new FilterModel { Export = request.Export };

                if (!string.IsNullOrWhiteSpace(request.ReqNo))
                {
                    if (!_utility.ValidateStringInput(request.ReqNo, 50, out string errorMessage))
                        return BadRequest(_utility.CreateErrorResponse("Invalid request number", errorMessage, "400"));
                    filter.RequestNo = request.ReqNo.Trim();
                }

                var dateRangeValidation = _validationHelper.ValidateDateRange(request.From, request.To);
                if (dateRangeValidation != null)
                    return BadRequest(dateRangeValidation);

                filter.From = request.From;
                filter.To = request.To;

                bool? pendingByHO = request.DropdownSelection switch
                {
                    1 => true,
                    0 => null,
                    _ => null
                };

                var response = await _reportService.NewDealerActivityReportService(filter, pendingByHO, (int)request.PageIndex, (int)request.PageSize);

                return response == null || response.result == null
                    ? NotFound(_utility.CreateSuccessResponse("No data found."))
                    : Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNewDealerActivityListing");
                return StatusCode(500, _utility.CreateErrorResponse("Server Error", "An unexpected error occurred while processing your request.", "500"));
            }
        }

        [HttpPost("GetNewDealerClaimListing")]
        public async Task<IActionResult> GetNewDealerClaimListing([FromBody] GetNewDealerClaimListingRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(_utility.CreateErrorResponse("Request body is empty", "Please provide the required request parameters.", "400"));

                var paginationValidation = _validationHelper.ValidatePagination(request.PageIndex, request.PageSize);
                if (paginationValidation != null)
                    return BadRequest(paginationValidation);

                var requestNoValidation = _validationHelper.ValidateRequestNo(request.RequestNo);
                if (requestNoValidation != null)
                    return BadRequest(requestNoValidation);

                var dateRangeValidation = _validationHelper.ValidateDateRange(request.From, request.To);
                if (dateRangeValidation != null)
                    return BadRequest(dateRangeValidation);

                var filter = new FilterModel
                {
                    RequestNo = request.RequestNo,
                    From = request.From,
                    To = request.To
                };

                var response = await _reportService.NewDealerClaimReportService(filter, request.PageIndex, request.PageSize);

                return response == null
                    ? NotFound(_utility.CreateSuccessResponse("No data found."))
                    : Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNewDealerClaimListing");
                return StatusCode(500, _utility.CreateErrorResponse("Server Error", "An unexpected error occurred while processing your request.", "500"));
            }
        }

        #region Helper Methods

        private object ValidateRequest(int pageIndex, int pageSize, DateTime? from, DateTime? to)
        {
            var paginationValidation = _validationHelper.ValidatePagination(pageIndex, pageSize);
            if (paginationValidation != null)
                return paginationValidation;

            return _validationHelper.ValidateDateRange(from, to);
        }

        private (string EmpNo, string RoleId, object ErrorResponse) GetAndValidateAuthClaims()
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                if (string.IsNullOrEmpty(empNo) || string.IsNullOrEmpty(roleId))
                {
                    return (null, null, _utility.CreateErrorResponse(
                        "Unauthorized", "UserId or RoleId is not present or invalid in the token.", "401"));
                }

                return (empNo.Trim(), roleId.Trim(), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAndValidateAuthClaims");
                return (null, null, _utility.CreateErrorResponse("Server Error", "An unexpected error occurred while processing your request.", "500"));
            }
        }

        private FilterModel CreateFilterModel(DateTime? from, DateTime? to, string empNo, string roleId)
        {
            try
            {
                return new FilterModel
                {
                    From = from,
                    To = to,
                    EmpNo = empNo,
                    RoleId = roleId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateFilterModel");
                throw;
            }
        }

        #endregion
    }
}