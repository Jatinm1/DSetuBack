using DealerSetu_Data.Common;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace DealerSetu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PerformanceSheetController : ControllerBase
    {
        private readonly IPerformanceSheetService _performanceSheetService;
        private readonly ILogger<PerformanceSheetController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JwtHelper _jwtHelper;


        public PerformanceSheetController(
            IPerformanceSheetService performanceSheetService,
            ILogger<PerformanceSheetController> logger,
            IHttpContextAccessor httpContextAccessor,
            JwtHelper jwtHelper
            )
        {
            _performanceSheetService = performanceSheetService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _jwtHelper = jwtHelper;
        }        

        [HttpPost("GetTrackingDealers")]
        public async Task<ActionResult<IEnumerable<DealerModel>>> GetTrackingDealersPost([FromBody] DealersRequestModel request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                if (string.IsNullOrEmpty(empNo))
                {
                    return Unauthorized("User not authenticated");
                }

                var dealers = await _performanceSheetService.GetTrackingDealersServiceAsync(request,empNo);

                return Ok(dealers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetTrackingDealersPost endpoint");
                return StatusCode(500, "An internal server error occurred");
            }
        }

        [HttpPost("GetPendingDealers")]
        public async Task<ActionResult<IEnumerable<DealerModel>>> GetPendingDealersPost([FromBody] DealersRequestModel request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                if (string.IsNullOrEmpty(empNo))
                {
                    return Unauthorized("User not authenticated");
                }

                var dealers = await _performanceSheetService.GetPendingDealersServiceAsync(request, empNo);

                return Ok(dealers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetPendingDealers endpoint");
                return StatusCode(500, "An internal server error occurred");
            }
        }

        [HttpGet("GetDealerList")]
        public async Task<ActionResult<IEnumerable<DealerListModel>>> GetDealerList()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                if (string.IsNullOrEmpty(empNo))
                {
                    return Unauthorized("User not authenticated");
                }

                var dealers = await _performanceSheetService.GetDealerListServiceAsync(empNo);

                return Ok(dealers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetDealerList endpoint");
                return StatusCode(500, "An internal server error occurred");
            }
        }

        [HttpPost("GetPerformanceSheet")]
        public async Task<IActionResult> GetPerformanceSheetAsync([FromBody] PerformanceSheetReqModel request)
        {
            try
            {               
                var result = await _performanceSheetService.GetPerformanceSheetServiceAsync(request);

                if (result == null)
                {
                    return NotFound(new { message = "Performance sheet not found" });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid arguments provided for GetPerformanceSheet");
                return BadRequest(new { message = "Invalid parameters provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching performance sheet for DealerEmpId: {dealerEmpId}, Month: {month}, FYear: {fYear}", request.DealerEmpId, request.DealerEmpId, request.DealerEmpId);
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("GetDealerBusinessPlan")]
        public async Task<IActionResult> GetDealerBusinessPlan([FromBody] PerformanceSheetReqModel request)
        {
            try
            {
                var result = await _performanceSheetService.GetDealerBusinessPlanServiceAsync(request);

                if (result == null)
                {
                    return NotFound(new { message = "Business Plan not found" });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid arguments provided for GetDealerBusinessPlan");
                return BadRequest(new { message = "Invalid parameters provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching Business Plan for DealerEmpId: {dealerEmpId}, Month: {month}, FYear: {fYear}", request.DealerEmpId, request.DealerEmpId, request.DealerEmpId);
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("AddDealerBusinessPlan")]
        public async Task<IActionResult> AddDealerBusinessPlan([FromBody] BusinessPerformancePlan request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");

                request.CreatedBy = empNo;
                var result = await _performanceSheetService.SubmitDealerBusinessPlanServiceAsync(request);

                if (result == null)
                {
                    return NotFound(new { message = "Some Error has occured" });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid arguments provided for AddDealerBusinessPlan");
                return BadRequest(new { message = "Invalid parameters provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while submitting Business Plan for DealerEmpId: {dealerEmpId}, FYear: {fYear}", request.DealerEmpId,request.FYear);
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }

        [HttpPost("GetDealerDetails")]
        public async Task<IActionResult> GetDealerDetailsAsync([FromBody] PerformanceSheetReqModel request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.FYear) || request.DealerEmpId == null || request.Month == null)
                {
                    return BadRequest("DealerEmpId, Month, and FYear are required parameters.");
                }

                var result = await _performanceSheetService.GetDealerDetailsServiceAsync(request);

                if (result == null)
                {
                    return NotFound("Dealer details not found.");
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                //_logger.LogWarning(ex, "Invalid parameters provided for GetDealerDetails");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error occurred while getting dealer details for DealerEmpId: {DealerEmpId}", dealerEmpId);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("GetActionPlanDetails")]
        public async Task<IActionResult> GetActionPlanDetails([FromBody] ActionPlanDetailReqModel request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.FYear) || request.DealerEmpId == null || request.Month == null)
                {
                    return BadRequest("DealerEmpId, Month, and FYear are required parameters.");
                }

                var result = await _performanceSheetService.GetActionPlanDetailServiceAsync(request);

                if (result == null)
                {
                    return NotFound("Dealer details not found.");
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                //_logger.LogWarning(ex, "Invalid parameters provided for GetDealerDetails");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error occurred while getting dealer details for DealerEmpId: {DealerEmpId}", dealerEmpId);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("AddActionPlan")]
        public async Task<IActionResult> SubmitActionPlan([FromBody] ActionPlanModel request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");

                request.CreatedBy = empNo;
                var result = await _performanceSheetService.SubmitActionPlanServiceAsync(request);

                if (result == null)
                {
                    return NotFound("Action Plan not submitted.");
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("UpdatePerformanceSheet")]
        public async Task<IActionResult> SubmitPerformanceSheetAsync([FromBody] PerformanceSheetUpdateModel request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");

                var result = await _performanceSheetService.SubmitPerformanceSheetServiceAsync(request,empNo);
                if (result == null)
                {
                    return StatusCode(500, new { message = "An error occurred while updating Performance Sheet" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }
    }
}
