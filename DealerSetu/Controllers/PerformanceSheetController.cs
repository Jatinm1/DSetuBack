using DealerSetu_Data.Common;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealerSetu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "PerformanceAccess")]
    public class PerformanceSheetController : ControllerBase
    {
        private readonly IPerformanceSheetService _performanceSheetService;
        private readonly JwtHelper _jwtHelper;

        public PerformanceSheetController(IPerformanceSheetService performanceSheetService, JwtHelper jwtHelper)
        {
            _performanceSheetService = performanceSheetService;
            _jwtHelper = jwtHelper;
        }

        // Common authentication helper
        private string GetAuthenticatedEmpNo()
        {
            var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
            return string.IsNullOrEmpty(empNo) ? throw new UnauthorizedAccessException("User not authenticated") : empNo;
        }

        // Common validation helper
        private void ValidateModel()
        {
            if (!ModelState.IsValid)
                throw new ArgumentException("Invalid model state");
        }

        [HttpPost("GetTrackingDealers")]
        public async Task<ActionResult<IEnumerable<DealerModel>>> GetTrackingDealersPost([FromBody] DealersRequestModel request)
        {
            try
            {
                try
                {
                    ValidateModel();
                    var empNo = GetAuthenticatedEmpNo();
                    var dealers = await _performanceSheetService.GetTrackingDealersServiceAsync(request, empNo);
                    return Ok(dealers);
                }
                catch (UnauthorizedAccessException)
                {
                    return Unauthorized("User not authenticated");
                }
                catch (ArgumentException)
                {
                    return BadRequest(ModelState);
                }
                catch
                {
                    return StatusCode(500, "An internal server error occurred");
                }
            }
            catch
            {
                return StatusCode(500, "An internal server error occurred");
            }
        }

        [HttpPost("GetPendingDealers")]
        public async Task<ActionResult<IEnumerable<DealerModel>>> GetPendingDealersPost([FromBody] DealersRequestModel request)
        {
            try
            {
                try
                {
                    ValidateModel();
                    var empNo = GetAuthenticatedEmpNo();
                    var dealers = await _performanceSheetService.GetPendingDealersServiceAsync(request, empNo);
                    return Ok(dealers);
                }
                catch (UnauthorizedAccessException)
                {
                    return Unauthorized("User not authenticated");
                }
                catch (ArgumentException)
                {
                    return BadRequest(ModelState);
                }
                catch
                {
                    return StatusCode(500, "An internal server error occurred");
                }
            }
            catch
            {
                return StatusCode(500, "An internal server error occurred");
            }
        }

        [HttpGet("GetDealerList")]
        public async Task<ActionResult<IEnumerable<DealerListModel>>> GetDealerList()
        {
            try
            {
                try
                {
                    var empNo = GetAuthenticatedEmpNo();
                    var dealers = await _performanceSheetService.GetDealerListServiceAsync(empNo);
                    return Ok(dealers);
                }
                catch (UnauthorizedAccessException)
                {
                    return Unauthorized("User not authenticated");
                }
                catch
                {
                    return StatusCode(500, "An internal server error occurred");
                }
            }
            catch
            {
                return StatusCode(500, "An internal server error occurred");
            }
        }

        [HttpPost("GetPerformanceSheet")]
        public async Task<IActionResult> GetPerformanceSheetAsync([FromBody] PerformanceSheetReqModel request)
        {
            try
            {
                try
                {
                    var result = await _performanceSheetService.GetPerformanceSheetServiceAsync(request);
                    return result == null ? NotFound(new { message = "Performance sheet not found" }) : Ok(result);
                }
                catch (ArgumentException)
                {
                    return BadRequest(new { message = "Invalid parameters provided" });
                }
                catch
                {
                    return StatusCode(500, new { message = "An error occurred while processing your request" });
                }
            }
            catch
            {
                return StatusCode(500, new { message = "An internal server error occurred" });
            }
        }

        [HttpPost("GetDealerBusinessPlan")]
        public async Task<IActionResult> GetDealerBusinessPlan([FromBody] BusinessPlanReqModel request)
        {
            try
            {
                try
                {
                    var result = await _performanceSheetService.GetDealerBusinessPlanServiceAsync(request);
                    return result == null ? NotFound(new { message = "Business Plan not found" }) : Ok(result);
                }
                catch (ArgumentException)
                {
                    return BadRequest(new { message = "Invalid parameters provided" });
                }
                catch
                {
                    return StatusCode(500, new { message = "An error occurred while processing your request" });
                }
            }
            catch
            {
                return StatusCode(500, new { message = "An internal server error occurred" });
            }
        }

        [HttpPost("AddDealerBusinessPlan")]
        public async Task<IActionResult> AddDealerBusinessPlan([FromBody] BusinessPerformancePlan request)
        {
            try
            {
                try
                {
                    request.CreatedBy = GetAuthenticatedEmpNo();
                    var result = await _performanceSheetService.SubmitDealerBusinessPlanServiceAsync(request);
                    return result == null ? NotFound(new { message = "Some Error has occured" }) : Ok(result);
                }
                catch (UnauthorizedAccessException)
                {
                    return Unauthorized("User not authenticated");
                }
                catch (ArgumentException)
                {
                    return BadRequest(new { message = "Invalid parameters provided" });
                }
                catch
                {
                    return StatusCode(500, new { message = "An error occurred while processing your request" });
                }
            }
            catch
            {
                return StatusCode(500, new { message = "An internal server error occurred" });
            }
        }

        // Validation helper for dealer details endpoints
        private static void ValidateDealerRequest(PerformanceSheetReqModel request)
        {
            if (string.IsNullOrWhiteSpace(request.FYear))
                throw new ArgumentException("DealerEmpId, Month, and FYear are required parameters.");
        }

        [HttpPost("GetDealerDetails")]
        public async Task<IActionResult> GetDealerDetailsAsync([FromBody] PerformanceSheetReqModel request)
        {
            try
            {
                try
                {
                    ValidateDealerRequest(request);
                    var result = await _performanceSheetService.GetDealerDetailsServiceAsync(request);
                    return result == null ? NotFound("Dealer details not found.") : Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
                catch
                {
                    return StatusCode(500, "An error occurred while processing your request.");
                }
            }
            catch
            {
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("GetActionPlanDetails")]
        public async Task<IActionResult> GetActionPlanDetails([FromBody] ActionPlanDetailReqModel request)
        {
            try
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(request.FYear))
                        throw new ArgumentException("DealerEmpId, Month, and FYear are required parameters.");

                    var result = await _performanceSheetService.GetActionPlanDetailServiceAsync(request);
                    return result == null ? NotFound("Action plan details not found.") : Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
                catch
                {
                    return StatusCode(500, "An error occurred while processing your request.");
                }
            }
            catch
            {
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("AddActionPlan")]
        public async Task<IActionResult> SubmitActionPlan([FromBody] ActionPlanModel request)
        {
            try
            {
                try
                {
                    request.CreatedBy = GetAuthenticatedEmpNo();
                    var result = await _performanceSheetService.SubmitActionPlanServiceAsync(request);
                    return result == null ? NotFound("Action Plan not submitted.") : Ok(result);
                }
                catch (UnauthorizedAccessException)
                {
                    return Unauthorized("User not authenticated");
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
                catch
                {
                    return StatusCode(500, "An error occurred while processing your request.");
                }
            }
            catch
            {
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("UpdatePerformanceSheet")]
        public async Task<IActionResult> SubmitPerformanceSheetAsync([FromBody] PerformanceSheetUpdateModel request)
        {
            try
            {
                try
                {
                    var empNo = GetAuthenticatedEmpNo();
                    var result = await _performanceSheetService.SubmitPerformanceSheetServiceAsync(request, empNo);
                    return result == null
                        ? StatusCode(500, new { message = "An error occurred while updating Performance Sheet" })
                        : Ok(result);
                }
                catch (UnauthorizedAccessException)
                {
                    return Unauthorized("User not authenticated");
                }
                catch (Exception ex)
                {
                    return BadRequest(new { Success = false, Message = ex.Message });
                }
            }
            catch
            {
                return StatusCode(500, new { message = "An internal server error occurred" });
            }
        }
    }
}