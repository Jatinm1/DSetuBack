using DealerSetu.Repository.Common;
using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
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
        private readonly Utility _utility;

        public RequestController(IRequestService requestService, JwtHelper jwtHelper, Utility utility)
        {
            _requestService = requestService;
            _jwtHelper = jwtHelper;
            _utility = utility;
        }

        /// <summary>
        /// Retrieves available request types for filtering
        /// </summary>
        [HttpGet("RequestTypeFilter")]
        public async Task<IActionResult> RequestTypeFilter()
        {
            try
            {
                var response = await _requestService.RequestTypeFilterService();
                return (bool)response.isError ? StatusCode(500, response) : Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// Retrieves available HP categories for filtering
        /// </summary>
        [HttpGet("HPCategoryFilter")]
        public async Task<IActionResult> HPCategoryFilter()
        {
            try
            {
                var response = await _requestService.HPCategoryService();
                return (bool)response.isError ? StatusCode(500, response) : Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// Retrieves paginated list of requests based on filter criteria
        /// </summary>
        [HttpPost("GetRequestList")]
        public async Task<IActionResult> GetRequestList([FromBody] RequestReq request)
        {
            try
            {
                if (request == null)
                    return BadRequest(_utility.CreateErrorResponse("Invalid payload", "Request body cannot be null.", "400"));

                var (empNo, roleId) = GetUserClaims();
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
                return StatusCode(500, CreateErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// Submits a new request with validation
        /// </summary>
        [HttpPost("SubmitRequest")]
        public async Task<IActionResult> SubmitRequest([FromBody] RequestSubmissionModel request)
        {
            try
            {
                if (request == null)
                    return BadRequest(_utility.CreateErrorResponse("Invalid payload", "Request body cannot be null.", "400"));

                if (request.RequestTypeId == "1" && string.IsNullOrWhiteSpace(request.HpCategory))
                    return BadRequest(_utility.CreateErrorResponse("Invalid payload", "HP Category is Required for Demo Tractor Request", "400"));

                var (empNo, roleId) = GetUserClaims();
                if (string.IsNullOrEmpty(empNo) || string.IsNullOrEmpty(roleId))
                    return Unauthorized("User not authenticated properly");

                if (roleId != "1")
                    return BadRequest("Invalid Role");

                var result = await _requestService.SubmitRequestService(request, empNo, roleId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, CreateErrorResponse(ex.Message));
            }
        }

        private (string empNo, string roleId) GetUserClaims()
        {
            var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
            var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");
            return (empNo, roleId);
        }

        private ServiceResponse CreateErrorResponse(string message)
        {
            return new ServiceResponse
            {
                isError = true,
                Error = message,
                Message = "An unexpected error occurred",
                Status = "Error",
                Code = "500"
            };
        }
    }
}