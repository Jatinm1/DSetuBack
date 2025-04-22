using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Services.IServices;
using DealerSetu_Services.Services;
using Microsoft.AspNetCore.Mvc;
namespace DealerSetu.Controllers

{
    [ApiController]
    [Route("[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly JwtHelper _jwtHelper;        
        private readonly IHomeDashboardService _homeService;        
        public DashboardController(JwtHelper jwtHelper, IHomeDashboardService homeService)
        {
            _jwtHelper = jwtHelper;
            _homeService = homeService;
        }
        [HttpGet("DashboardAPI")]
        public IActionResult GetDashboard()
        {
            var response = new { message = "Home Dashboard API Test" };
            return Ok(response);
        }

        [HttpGet("PendingCount")]
        public async Task<IActionResult> PendingCount()
        {
            try
            {

                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                var filter = new FilterModel
                {
                    EmpNo = empNo,
                    RoleId = roleId
                };
                var response = await _homeService.PendingCountService(filter);

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

    }
}



































//using DealerSetu_Data.Models;
//using DealerSetu_Services.IServices;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;

//namespace DealerSetu.Controllers
//{
//    // Controllers/DashboardController.cs
//    [ApiController]
//    [Route("api/[controller]")]
//    public class DashboardController : ControllerBase
//    {
//        private readonly IHomeDashboardService _dashboardService;

//        public DashboardController(IHomeDashboardService dashboardService)
//        {
//            _dashboardService = dashboardService;
//        }

//        [HttpGet]
//        public async Task<ActionResult<HomeDashboard>> GetDashboard(string userID)
//        {
//            var userId = userID;
//            if (string.IsNullOrEmpty(userId))
//                return Unauthorized();

//            try
//            {
//                var dashboard = await _dashboardService.GetDashboardDataAsync(userId);
//                return Ok(dashboard);
//            }
//            catch (Exception ex)
//            {
//                return NotFound(ex.Message);
//            }

//        }
//    }
//}
