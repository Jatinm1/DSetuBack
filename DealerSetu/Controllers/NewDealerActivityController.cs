using DealerSetu.Repository.Common;
using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace DealerSetu.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = "NewDealerActivityAccess")]
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
                    return BadRequest(new ServiceResponse
                    {
                        isError = true,
                        Error = "Validation failed",
                        Message = "Request validation failed.",
                        Status = "Error",
                        Code = "400"
                    });
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
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
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
                    return BadRequest(new ServiceResponse
                    {
                        isError = true,
                        Error = "Validation failed",
                        Message = "Request validation failed.",
                        Status = "Error",
                        Code = "400"
                    });
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
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
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
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
            }
        }

        [HttpGet("DealerStates")]
        public async Task<IActionResult> DealerStates()
        {
            try
            {
                var response = await _newDealerService.DealerStatesService();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in DealerStates", ex);
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
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
                // Validate required fields for each activity
                foreach (var activity in request.ActivityData)
                {
                    if (string.IsNullOrWhiteSpace(activity.ActivityType))
                        return BadRequest(new ServiceResponse
                        {
                            isError = true,
                            Error = "Validation failed",
                            Message = "Required field is missing.",
                            Status = "Error",
                            Code = "400"
                        });

                    if (string.IsNullOrWhiteSpace(activity.ActivityThrough))
                        return BadRequest(new ServiceResponse
                        {
                            isError = true,
                            Error = "Validation failed",
                            Message = "Required field is missing.",
                            Status = "Error",
                            Code = "400"
                        });

                    if (string.IsNullOrWhiteSpace(activity.BudgetRequested))
                        return BadRequest(new ServiceResponse
                        {
                            isError = true,
                            Error = "Validation failed",
                            Message = "Required field is missing.",
                            Status = "Error",
                            Code = "400"
                        });

                    if (string.IsNullOrWhiteSpace(activity.ActivityMonth))
                        return BadRequest(new ServiceResponse
                        {
                            isError = true,
                            Error = "Validation failed",
                            Message = "Required field is missing.",
                            Status = "Error",
                            Code = "400"
                        });
                }

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
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
            }
        }

        [HttpPost("UpdateClaim")]
        public async Task<IActionResult> UpdateClaim([FromBody] ClaimUpdationRequest request)
        {
            try
            {
                foreach (var activity in request.ActivityData)
                {
                    if (string.IsNullOrWhiteSpace(activity.ActivityType))
                        return BadRequest(new ServiceResponse
                        {
                            isError = true,
                            Error = "Validation failed",
                            Message = "Required field is missing.",
                            Status = "Error",
                            Code = "400"
                        });

                    if (string.IsNullOrWhiteSpace(activity.ActivityThrough))
                        return BadRequest(new ServiceResponse
                        {
                            isError = true,
                            Error = "Validation failed",
                            Message = "Required field is missing.",
                            Status = "Error",
                            Code = "400"
                        });

                    if (string.IsNullOrWhiteSpace(activity.BudgetRequested))
                        return BadRequest(new ServiceResponse
                        {
                            isError = true,
                            Error = "Validation failed",
                            Message = "Required field is missing.",
                            Status = "Error",
                            Code = "400"
                        });

                    if (string.IsNullOrWhiteSpace(activity.ActivityMonth))
                        return BadRequest(new ServiceResponse
                        {
                            isError = true,
                            Error = "Validation failed",
                            Message = "Required field is missing.",
                            Status = "Error",
                            Code = "400"
                        });
                }

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
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
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
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
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
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
            }
        }

        [HttpPost("AddActualClaim")]
        public async Task<IActionResult> AddClaim([FromForm] ActualClaimAddRequest request)
        {
            try
            {
                if (request.ActivityId == null || request.ActivityId == 0)
                {
                    return BadRequest(new ServiceResponse
                    {
                        isError = true,
                        Error = "Invalid payload",
                        Message = "Required field is missing or invalid.",
                        Status = "Error",
                        Code = "400"
                    });
                }

                var validationFields = new Dictionary<string, string>
                {
                    { "Enquiry", request.Enquiry },
                    { "ActualExpenses", request.ActualExpenses },
                    { "DateOfActivity", request.DateOfActivity },
                    { "CustomerContacted", request.CustomerContacted },
                    { "Delivery", request.Delivery }
                };

                foreach (var field in validationFields)
                {
                    if (string.IsNullOrWhiteSpace(field.Value))
                    {
                        return BadRequest(new ServiceResponse
                        {
                            isError = true,
                            Error = "Invalid payload",
                            Message = "Required field is missing.",
                            Status = "Error",
                            Code = "400"
                        });
                    }
                }

                // Check if DateOfActivity is after 01/01/2000
                if (!DateTime.TryParseExact(request.DateOfActivity, "dd/MM/yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    return BadRequest(new ServiceResponse
                    {
                        isError = true,
                        Error = "Invalid payload",
                        Message = "Date format is invalid.",
                        Status = "Error",
                        Code = "400"
                    });
                }

                var minValidDate = new DateTime(2000, 1, 1);
                if (parsedDate <= minValidDate)
                {
                    return BadRequest(new ServiceResponse
                    {
                        isError = true,
                        Error = "Invalid payload",
                        Message = "Date value is invalid.",
                        Status = "Error",
                        Code = "400"
                    });
                }

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

                #region Validation For Image Files
                if (request.Image1 != null)
                {
                    var validationResult = await _fileValidationService.ValidateImageAsync(request.Image1, MaxFileSize);
                    if ((bool)validationResult.isError)
                    {
                        return StatusCode(int.Parse(validationResult.Code), new ServiceResponse
                        {
                            isError = true,
                            Error = "File validation failed",
                            Message = "Invalid file provided.",
                            Status = "Error",
                            Code = validationResult.Code
                        });
                    }
                }
                if (request.Image2 != null)
                {
                    var validationResult = await _fileValidationService.ValidateImageAsync(request.Image2, MaxFileSize);
                    if ((bool)validationResult.isError)
                    {
                        return StatusCode(int.Parse(validationResult.Code), new ServiceResponse
                        {
                            isError = true,
                            Error = "File validation failed",
                            Message = "Invalid file provided.",
                            Status = "Error",
                            Code = validationResult.Code
                        });
                    }
                }
                if (request.Image3 != null)
                {
                    var validationResult = await _fileValidationService.ValidateImageAsync(request.Image3, MaxFileSize);
                    if ((bool)validationResult.isError)
                    {
                        return StatusCode(int.Parse(validationResult.Code), new ServiceResponse
                        {
                            isError = true,
                            Error = "File validation failed",
                            Message = "Invalid file provided.",
                            Status = "Error",
                            Code = validationResult.Code
                        });
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
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
            }
        }

        [HttpPost("UpdateActualClaim")]
        public async Task<IActionResult> UpdateActualClaim([FromForm] ActualClaimUpdateRequest request)
        {
            try
            {
                if (request.ActivityId == null || request.ActivityId == 0)
                {
                    return BadRequest(new ServiceResponse
                    {
                        isError = true,
                        Error = "Invalid payload",
                        Message = "Required field is missing or invalid.",
                        Status = "Error",
                        Code = "400"
                    });
                }

                // Validate DateOfActivity if provided
                if (!string.IsNullOrWhiteSpace(request.DateOfActivity))
                {
                    if (!DateTime.TryParse(request.DateOfActivity, out DateTime parsedDate))
                    {
                        return BadRequest(new ServiceResponse
                        {
                            isError = true,
                            Error = "Invalid payload",
                            Message = "Date format is invalid.",
                            Status = "Error",
                            Code = "400"
                        });
                    }

                    var minValidDate = new DateTime(2000, 1, 1);
                    if (parsedDate <= minValidDate)
                    {
                        return BadRequest(new ServiceResponse
                        {
                            isError = true,
                            Error = "Invalid payload",
                            Message = "Date value is invalid.",
                            Status = "Error",
                            Code = "400"
                        });
                    }
                }

                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var model = new ActualClaimUpdateModel
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

                #region Validation For Image Files
                if (request.Image1 != null)
                {
                    var validationResult = await _fileValidationService.ValidateImageAsync(request.Image1, MaxFileSize);
                    if ((bool)validationResult.isError)
                    {
                        return StatusCode(int.Parse(validationResult.Code), new ServiceResponse
                        {
                            isError = true,
                            Error = "File validation failed",
                            Message = "Invalid file provided.",
                            Status = "Error",
                            Code = validationResult.Code
                        });
                    }
                }
                if (request.Image2 != null)
                {
                    var validationResult = await _fileValidationService.ValidateImageAsync(request.Image2, MaxFileSize);
                    if ((bool)validationResult.isError)
                    {
                        return StatusCode(int.Parse(validationResult.Code), new ServiceResponse
                        {
                            isError = true,
                            Error = "File validation failed",
                            Message = "Invalid file provided.",
                            Status = "Error",
                            Code = validationResult.Code
                        });
                    }
                }
                if (request.Image3 != null)
                {
                    var validationResult = await _fileValidationService.ValidateImageAsync(request.Image3, MaxFileSize);
                    if ((bool)validationResult.isError)
                    {
                        return StatusCode(int.Parse(validationResult.Code), new ServiceResponse
                        {
                            isError = true,
                            Error = "File validation failed",
                            Message = "Invalid file provided.",
                            Status = "Error",
                            Code = validationResult.Code
                        });
                    }
                }
                #endregion

                // Upload images if provided
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

                var result = await _newDealerService.UpdateActualClaimService(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("NewDealerActivityController", "Error in UpdateActualClaim", ex);
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
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
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
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
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
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
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
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
                return StatusCode(500, new ServiceResponse
                {
                    isError = true,
                    Error = "Internal server error",
                    Message = "An error occurred while processing your request.",
                    Status = "Error",
                    Code = "500"
                });
            }
        }
    }
}