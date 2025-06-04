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

        [HttpPost]
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
    }
}
