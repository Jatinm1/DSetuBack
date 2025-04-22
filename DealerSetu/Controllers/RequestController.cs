using DealerSetu.Repository.Common;
using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace DealerSetu.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        private readonly IRequestService _requestService;
        private readonly JwtHelper _jwtHelper;
        private readonly ValidationHelper _validationHelper;
        private readonly Utility _utility;

        public RequestController(IRequestService requestService, JwtHelper jwtHelper, ValidationHelper validationHelper,Utility utility)
        {
            _requestService = requestService;
            _jwtHelper = jwtHelper;
            _utility = utility;
            _validationHelper = validationHelper;
        }

        /// <summary>
        /// Retrieves the filtered request types.
        /// </summary>
        [HttpGet("RequestTypeFilter")]
        public async Task<IActionResult> RequestTypeFilter()
        {
            try
            {
                var response = await  _requestService.RequestTypeFilterService();

                if (response.isError == true)
                {
                    return StatusCode(500, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "An unexpected error occurred",
                    Status = "Error",
                    Code = "500"
                });
            }
        }


        [HttpGet("HPCategoryFilter")]
        public async Task<IActionResult> HPCategoryFilter()
        {
            try
            {
                var response = await _requestService.HPCategoryService();

                if (response.isError == true)
                {
                    return StatusCode(500, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "An unexpected error occurred",
                    Status = "Error",
                    Code = "500"
                });
            }
        }


        [HttpPost("GetRequestList")]
        public async Task<IActionResult> GetRequestList([FromBody] RequestReq request)
        {
            try
            {

                if (request == null)
                {
                    return BadRequest(_utility.CreateErrorResponse(
                        "Invalid payload", "Request body cannot be null.", "400"));
                }

                // Validate pagination
                var paginationValidation = _validationHelper.ValidatePagination(request.PageIndex, request.PageSize);
                if (paginationValidation != null)
                {
                    return BadRequest(paginationValidation);
                }

                // Validate date range
                var dateRangeValidation = _validationHelper.ValidateDateRange(request.From, request.To);
                if (dateRangeValidation != null)
                {
                    return BadRequest(dateRangeValidation);
                }

                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                var filter = new FilterModel
                {
                    EmpNo = empNo,
                    RoleId = roleId,
                    RequestTypeId = request.RequestTypeId,
                    RequestNo = request.RequestNo,
                    From = request.From,
                    To = request.To
                };

                var result = await _requestService.RequestListService(filter, request.PageIndex, request.PageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }


        }

        /// <summary>
        /// Submits a new request.
        /// </summary>
        [HttpPost("SubmitRequest")]
        public async Task<IActionResult> SubmitRequest([FromBody] RequestSubmissionModel request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(_utility.CreateErrorResponse(
                        "Invalid payload", "Request body cannot be null.", "400"));
                }

                // Retrieve user details from JWT claims
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                if (string.IsNullOrEmpty(empNo) || string.IsNullOrEmpty(roleId))
                {
                    return Unauthorized("User not authenticated properly");
                }

                //if (roleId != "1")
                //{
                //    return BadRequest("Invalid Role");
                //}

                var result = await _requestService.SubmitRequestService(
                    request.RequestTypeId,
                    request.Message,
                    request.HpCategory,
                    empNo);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
