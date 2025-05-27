using DealerSetu.Repository.Common;
using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Mvc;
using DealerSetu_Data.Models.ViewModels;

namespace DealerSetu.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NewDealerActivityController : ControllerBase
    {
        private readonly INewDealerActivityService _newDealerService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IFileValidationService _fileValidationService;
        private readonly JwtHelper _jwtHelper;
        private readonly ValidationHelper _validationHelper;
        private readonly Utility _utility;
        private readonly FileLoggerService _logger;
        private const long MaxFileSize = 20 * 1024 * 1024; // 20 MB


        public NewDealerActivityController(
            INewDealerActivityService newDealerService,
            IBlobStorageService blobStorageService,
            JwtHelper jwtHelper,
            ValidationHelper validationHelper,
            Utility utility,
            IFileValidationService fileValidationService
            )
        {
            _newDealerService = newDealerService;
            _blobStorageService = blobStorageService;
            _jwtHelper = jwtHelper;
            _utility = utility;
            _validationHelper = validationHelper;
            _fileValidationService = fileValidationService;
            _logger = new FileLoggerService();
        }

        [HttpPost("NewDealerActivity")]
        public async Task<IActionResult> NewDealerActivityApproved([FromBody] ClaimReqModel request)
        {
            try
            {
                var validationResult = _validationHelper.ValidateNewDealerApprovedRequest(request);
                if (validationResult != null)
                {
                    return BadRequest(validationResult);
                }

                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                var filter = new FilterModel
                {
                    EmpNo = empNo,
                    RoleId = roleId,
                    ClaimNo = request.ClaimNo,
                    State = request.State,
                    Status = request.Status,
                    Export = request.Export,
                    From = request.From,
                    To = request.To
                };

                var result = await _newDealerService.NewDealerActivityListService(filter, (int)request.PageIndex, (int)request.PageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in NewDealerActivityApproved", ex);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("NewDealerPendingList")]
        public async Task<IActionResult> NewDealerActivityPending([FromBody] PendingClaimReqModel request)
        {
            try
            {
                var validationResult = _validationHelper.ValidateNewDealerPendingRequest(request);
                if (validationResult != null)
                {
                    return BadRequest(validationResult);
                }

                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                var filter = new FilterModel
                {
                    EmpNo = empNo,
                    RoleId = roleId,
                    ClaimNo = request.ClaimNo,
                    From = request.From,
                    To = request.To
                };

                var result = await _newDealerService.NewDealerPendingListService(filter, request.PageIndex, request.PageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in NewDealerActivityPending", ex);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("GetDealerData")]
        public async Task<IActionResult> GetDealerData([FromBody] DealerDataReqModel request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                var result = await _newDealerService.DealerDataService(request.RequestNo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in GetDealerData", ex);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("DealerStates")]
        public async Task<IActionResult> DealerStates()
        {
            try
            {
                var response = await _newDealerService.DealerStatesService();

                if (response.isError == true)
                {
                    return StatusCode(500, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in DealerStates", ex);
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

        [HttpPost("SubmitClaim")]
        public async Task<IActionResult> SubmitClaim([FromBody] ClaimSubmissionRequest request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                var result = await _newDealerService.SubmitClaimService(
                    request.RequestNo,
                    request.DealerNo,
                    request.ActivityData,
                    empNo
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in SubmitClaim", ex);
                return StatusCode(500, "An error occurred while submitting the claim.");
            }
        }

        [HttpPost("UpdateClaim")]
        public async Task<IActionResult> UpdateClaim([FromBody] ClaimUpdationRequest request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                var result = await _newDealerService.UpdateClaimService(
                    request.claimId,
                    request.ActivityData,
                    empNo
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in UpdateClaim", ex);
                return StatusCode(500, "An error occurred while Updating the claim.");
            }
        }

        [HttpPost("GetClaimDetails")]
        public async Task<IActionResult> GetClaimDetails([FromBody] ClaimDetailReq request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");


                var result = await _newDealerService.ClaimDetailsService(request.ClaimId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in GetClaimDetails", ex);
                return StatusCode(500, "An error occurred while fetching claim details.");
            }
        }

        [HttpPost("ApproveRejectClaim")]
        public async Task<IActionResult> ApproveRejectClaim([FromBody] ClaimApproveRejectRequest request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");
                var filter = new FilterModel
                {
                    EmpNo = empNo,
                    RoleId = roleId,
                    ClaimId = request.ClaimId,
                    IsApproved = request.IsApproved,
                    RejectRemarks = request.RejectRemarks
                };
                var result = await _newDealerService.ApproveRejectClaimService(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in ApproveRejectClaim", ex);
                return BadRequest(ex.Message);
            }
        }


        //*******************************************ACTUAL CLAIM APIS*****************************************

        [HttpPost("AddActualClaim")]
        public async Task<IActionResult> AddUpdateClaim([FromForm] ActualClaimAddUpdateRequest request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var model = new ActualClaimModel
                {
                    ActivityId = request.ActivityId,
                    EmpNo = empNo,
                    ActualExpenses = request.ActualExpenses,
                    DateOfActivity = request.DateOfActivity,
                    CustomerContacted = request.CustomerContacted,
                    Enquiry = request.Enquiry,
                    Delivery = request.Delivery,
                    ActualClaimOn = DateTime.Now
                };
                if (request.Image1 == null)
                {
                    return StatusCode(400, "Atleast 1 Image is required");
                }
                // Process image uploads if present
                #region Validation For Image Files
                // Validate each image if present
                if (request.Image1 != null)
                {
                    var validationResult = await _fileValidationService.ValidateImageAsync(request.Image1, MaxFileSize);
                    if ((bool)validationResult.isError)
                    {
                        return StatusCode(int.Parse(validationResult.Code), new { Message = validationResult.Message });
                    }
                }
                if (request.Image2 != null)
                {
                    var validationResult = await _fileValidationService.ValidateImageAsync(request.Image2, MaxFileSize);
                    if ((bool)validationResult.isError)
                    {
                        return StatusCode(int.Parse(validationResult.Code), new { Message = validationResult.Message });
                    }
                }
                if (request.Image3 != null)
                {
                    var validationResult = await _fileValidationService.ValidateImageAsync(request.Image3, MaxFileSize);
                    if ((bool)validationResult.isError)
                    {
                        return StatusCode(int.Parse(validationResult.Code), new { Message = validationResult.Message });
                    }
                }
                #endregion
                if (request.Image1 != null && request.Image1.Length > 0)
                {
                    model.Image1 = await _blobStorageService.UploadFileAsync(request.Image1);
                }
                if (request.Image2 != null && request.Image2.Length > 0)
                {
                    model.Image2 = await _blobStorageService.UploadFileAsync(request.Image2);
                }
                if (request.Image3 != null && request.Image3.Length > 0)
                {
                    model.Image3 = await _blobStorageService.UploadFileAsync(request.Image3);
                }
                var result = await _newDealerService.AddActualClaimService(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in AddUpdateClaim", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPost("GetActualClaimDetails")]
        public async Task<IActionResult> GetActualClaimDetails([FromBody] ActualClaimDetailReq request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");
                var result = await _newDealerService.ActualClaimDetailsService(request.ActivityId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in GetActualClaimDetails", ex);
                return StatusCode(500, "An error occurred while fetching claim details.");
            }
        }

        [HttpPost("GetActualClaimList")]
        public async Task<IActionResult> GetActualClaimList([FromBody] ClaimDetailReq request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                var filter = new FilterModel
                {
                    EmpNo = empNo,
                    RoleId = roleId,
                    ClaimId = request.ClaimId
                };


                var result = await _newDealerService.ActualClaimListService(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in GetActualClaimList", ex);
                return StatusCode(500, "An error occurred while fetching claim details.");
            }
        }

        [HttpPost("ApproveRejectActualClaim")]
        public async Task<IActionResult> ApproveRejectActualClaim([FromBody] ActualClaimApproveRejectRequest request)
        {
            try
            {

                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                var filter = new FilterModel
                {
                    EmpNo = empNo,
                    RoleId = roleId,
                    ActivityId = request.ActivityId,
                    IsApproved = request.IsApproved,
                    RejectRemarks = request.RejectRemarks

                };

                var result = await _newDealerService.ApproveRejectActualClaimService(filter);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in ApproveRejectActualClaim", ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("AddRemarks")]
        public async Task<IActionResult> AddActualRemarks([FromBody] AddActualRemarkModel request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");
                var result = await _newDealerService.AddActualRemarksService(request.ClaimId, request.ActualRemarks);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in AddActualRemarks", ex);
                return StatusCode(500, "An error occurred while fetching claim details.");
            }
        }
    }
}