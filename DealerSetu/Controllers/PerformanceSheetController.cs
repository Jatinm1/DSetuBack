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
        public async Task<ActionResult<IEnumerable<DealerModel>>> GetTrackingDealersPost([FromBody] GetTrackingDealersRequest request)
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

                var dealers = await _performanceSheetService.GetTrackingDealersAsync(
                    request.Keyword,
                    request.Month,
                    request.FYear,
                    empNo);

                return Ok(dealers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetTrackingDealersPost endpoint");
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
    }
}
