using DealerSetu.Repository.Common;
using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Services.IServices;
using DealerSetu_Services.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace DealerSetu.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DemoRequestController : ControllerBase
    {
        private readonly IDemoRequestService _demoService;
        private readonly IFileValidationService _fileValidationService;
        IBlobStorageService _blobStorageService;
        private readonly JwtHelper _jwtHelper;
        private readonly ValidationHelper _validationHelper;
        private readonly ILogger<DemoRequestController> _logger;
        private readonly FileLoggerService _fileLogger;
        private const long MaxFileSize = 20 * 1024 * 1024; // 20 MB


        public DemoRequestController(
            IDemoRequestService demoService,
            JwtHelper jwtHelper,
            ValidationHelper validationHelper,
            ILogger<DemoRequestController> logger,
            IFileValidationService fileValidationService,
            IBlobStorageService blobStorageService)
        {
            _demoService = demoService ?? throw new ArgumentNullException(nameof(demoService));
            _jwtHelper = jwtHelper ?? throw new ArgumentNullException(nameof(jwtHelper));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileValidationService = fileValidationService;
            _blobStorageService = blobStorageService;
            _fileLogger = new FileLoggerService();
        }

        [HttpPost("GetDemoTractorList")]
        public async Task<IActionResult> DemoTractorApproved([FromBody] DemoTractorRequestModel request)
        {
            try
            {
                var validationResult = _validationHelper.ValidateDemoTractorRequest(request);
                if (validationResult != null)
                {
                    return BadRequest(validationResult);
                }

                var filter = CreateFilterModel(request);
                var result = await _demoService.DemoTractorApprovedService(filter, (int)request.PageIndex, (int)request.PageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoTractorApproved");
                _fileLogger.LogError("DemoRequestController", "Error in DemoTractorApproved", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpPost("DemoTractorPendingList")]
        public async Task<IActionResult> DemoTractorPending([FromBody] DemoTractorRequestModel request)
        {
            try
            {
                var validationResult = _validationHelper.ValidateDemoTractorRequest(request);
                if (validationResult != null)
                {
                    return BadRequest(validationResult);
                }

                var filter = CreateFilterModel(request);
                var result = await _demoService.DemoTractorPendingService(filter, (int)request.PageIndex, (int)request.PageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoTractorPending");
                _fileLogger.LogError("DemoRequestController", "Error in DemoTractorPending", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpPost("DemoTractorPendingClaimList")]
        public async Task<IActionResult> DemoTractorPendingClaim([FromBody] DemoTractorRequestModel request)
        {
            try
            {
                var validationResult = _validationHelper.ValidateDemoTractorRequest(request);
                if (validationResult != null)
                {
                    return BadRequest(validationResult);
                }

                var filter = CreateFilterModel(request);
                var result = await _demoService.DemoTractorPendingClaimService(filter, (int)request.PageIndex, (int)request.PageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoTractorPendingClaim");
                _fileLogger.LogError("DemoRequestController", "Error in DemoTractorPendingClaim", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetFiscalYears")]
        public async Task<IActionResult> GetFiscalYears()
        {
            try
            {
                var response = await _demoService.FiscalYearsService();

                if ((bool)response.isError)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFiscalYears");
                _fileLogger.LogError("DemoRequestController", "Error in GetFiscalYears", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "An unexpected error occurred",
                    Status = "Error",
                    Code = "500"
                });
            }
        }

        [HttpPost("SubmitDemoRequest")]
        public async Task<IActionResult> SubmitDemoRequest([FromBody] DemoReqSubmissionModel request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");

                if (string.IsNullOrEmpty(empNo))
                {
                    return Unauthorized("User not authenticated or missing employee number");
                }

                var result = await _demoService.SubmitDemoReqService(request, empNo);

                if (result.isError == true)
                {
                    // Return 400 if it's a known error (e.g., validation), else 500
                    if (result.Code == "400")
                        return BadRequest(result);
                    else
                        return StatusCode(StatusCodes.Status500InternalServerError, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitDemoRequest");
                _fileLogger.LogError("DemoRequestController", "Error in SubmitDemoRequest", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }

        }

        [HttpPost("GetDemoReqData")]
        public async Task<IActionResult> DemoReqData([FromBody] DemoReqDetailReqModel request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");

                if (request == null)
                {
                    return BadRequest("Request ID is required");
                }

                var result = await _demoService.DemoReqDataService(request.reqId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoReqData for request ID: {ReqId}", request?.reqId);
                _fileLogger.LogError("DemoRequestController", $"Error in DemoReqData for request ID: {request?.reqId}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching claim details.");
            }
        }

        [HttpPost("ApproveRejectDemoRequest")]
        public async Task<IActionResult> ApproveRejectDemoRequest([FromBody] DemoTractorApproveRejectRequest request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                if (string.IsNullOrEmpty(empNo) || string.IsNullOrEmpty(roleId))
                {
                    return Unauthorized("User not authenticated or missing required claims");
                }

                var filter = new FilterModel
                {
                    EmpNo = empNo,
                    RoleId = roleId,
                    ReqId = request.ReqId,
                    IsApproved = request.IsApproved,
                    RejectRemarks = request.RejectRemarks
                };

                var result = await _demoService.DemoTractorApproveRejectService(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApproveRejectDemoRequest for request ID: {ReqId}", request?.ReqId);
                _fileLogger.LogError("DemoRequestController", $"Error in ApproveRejectDemoRequest for request ID: {request?.ReqId}", ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("UpdateDemoRequest")]
        public async Task<IActionResult> UpdateDemoReq([FromBody] DemoReqUpdateModel request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                var result = await _demoService.UpdateDemoReqService(
                    request,
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

        //*******************************************DEMO ACTUAL CLAIM APIS*****************************************

        [HttpPost("GetDemoActualClaimList")]

        public async Task<IActionResult> GetDemoActualClaimList([FromBody] DemoActualClaimListReq request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                var filter = new FilterModel
                {
                    EmpNo = empNo,
                    RoleId = roleId,
                    ReqId = request.DemoReqId
                };


                var result = await _demoService.DemoActualClaimListService(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("DemoRequestController", "Error in GetDemoActualClaimList", ex);
                return StatusCode(500, "An error occurred while fetching claim details.");
            }
        }


        [HttpPost("AddBasicDemoClaimDocs")]
        public async Task<IActionResult> AddUpdateBasicClaim([FromForm] DemoBasicDocUploadModel request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");

                // Define all possible image properties to process
                var imagesToProcess = new Dictionary<string, IFormFile>
        {
            { nameof(DemoBasicDocUploadModel.InvoiceFile), request.InvoiceFile },
            { nameof(DemoBasicDocUploadModel.RCFile), request.RCFile },
            { nameof(DemoBasicDocUploadModel.InsuranceFile), request.InsuranceFile },
            //{ nameof(DemoDocUploadModel.FileSale), request.FileSale }, //Sale Document of Tractor
            //{ nameof(DemoDocUploadModel.FileTractor), request.FileTractor }, //Format for Claiming
            //{ nameof(DemoDocUploadModel.FilePicture), request.FilePicture }, //Picture of Hour Reading
            //{ nameof(DemoDocUploadModel.FilePicTractor), request.FilePicTractor }, //Picture of Tractor
            //{ nameof(DemoDocUploadModel.LogDemonsFile), request.LogDemonsFile },
            //{ nameof(DemoDocUploadModel.Affidavitfile), request.Affidavitfile },
            //{ nameof(DemoDocUploadModel.SaleDeedfile), request.SaleDeedfile }
        };
                //-------------------------------------------------------------------------------------------------
                // Check if all images are provided since they're all mandatory
                foreach (var imageEntry in imagesToProcess)
                {
                    var propertyName = imageEntry.Key;
                    var image = imageEntry.Value;
                    if (image == null)
                    {
                        return StatusCode(400, $"{propertyName} is required");
                    }
                }
                //-------------------------------------------------------------------------------------------------
                #region Validation For Image Files
                // Validate each image
                foreach (var imageEntry in imagesToProcess)
                {
                    var image = imageEntry.Value;
                    var validationResult = await _fileValidationService.ValidateImageAsync(image, MaxFileSize);
                    if ((bool)validationResult.isError)
                    {
                        return StatusCode(int.Parse(validationResult.Code), new { Message = validationResult.Message });
                    }
                }
                #endregion
                //-------------------------------------------------------------------------------------------------

                // Create a DemoReqModel instance to receive the blob URLs
                var demoReqModel = new DemoReqModel
                {
                    DemoRequestId = request.RequestId,
                    Model = request.Model, // Set appropriate value if available
                    EmpNo = empNo,
                    ChassisNo = request.ChassisNo,
                    EngineNo = request.EngineNo,
                    DateOfBilling = request.DateOfBilling,
                    CreatedDate = DateTime.Now,
                    CreatedBy = empNo,
                };

                // Upload valid images to blob storage and map to DemoReqModel fields
                foreach (var imageEntry in imagesToProcess)
                {
                    var propertyName = imageEntry.Key;
                    var image = imageEntry.Value;

                    // Upload to blob storage and get URL string
                    string blobUrl = await _blobStorageService.UploadFileAsync(image);

                    // Map to appropriate property in DemoReqModel based on the property name
                    switch (propertyName)
                    {
                        case nameof(DemoBasicDocUploadModel.InvoiceFile):
                            demoReqModel.InvoiceFile = blobUrl;
                            break;
                        case nameof(DemoBasicDocUploadModel.RCFile):
                            demoReqModel.RCFile = blobUrl;
                            break;
                        case nameof(DemoBasicDocUploadModel.InsuranceFile):
                            demoReqModel.InsuranceFile = blobUrl;
                            break;

                    }
                }

                // Pass the DemoReqModel instead of DemoDocUploadModel to your service
                var result = await _demoService.AddDemoActualClaimService(demoReqModel,request.BasicFlag);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("DemoRequestController", "Error in AddUpdateBasicClaim", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("AddAllDemoClaimDocs")]
        public async Task<IActionResult> AddUpdateAllClaim([FromForm] DemoDocUploadModel request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");

                // Define all possible image properties to process
                var imagesToProcess = new Dictionary<string, IFormFile>
        {
            //{ nameof(DemoDocUploadModel.InvoiceFile), request.InvoiceFile },
            //{ nameof(DemoDocUploadModel.RCFile), request.RCFile },
            { nameof(DemoDocUploadModel.FileSale), request.FileSale }, //Sale Document of Tractor
            { nameof(DemoDocUploadModel.FileTractor), request.FileTractor }, //Format for Claiming
            { nameof(DemoDocUploadModel.FilePicture), request.FilePicture }, //Picture of Hour Reading
            { nameof(DemoDocUploadModel.FilePicTractor), request.FilePicTractor }, //Picture of Tractor
            //{ nameof(DemoDocUploadModel.InsuranceFile), request.InsuranceFile },
            { nameof(DemoDocUploadModel.LogDemonsFile), request.LogDemonsFile },
            { nameof(DemoDocUploadModel.Affidavitfile), request.Affidavitfile },
            { nameof(DemoDocUploadModel.SaleDeedfile), request.SaleDeedfile }
        };
                //-------------------------------------------------------------------------------------------------
                // Check if all images are provided since they're all mandatory
                foreach (var imageEntry in imagesToProcess)
                {
                    var propertyName = imageEntry.Key;
                    var image = imageEntry.Value;
                    if (image == null)
                    {
                        return StatusCode(400, $"{propertyName} is required");
                    }
                }

                #region Validation For Image Files
                // Validate each image
                foreach (var imageEntry in imagesToProcess)
                {
                    var image = imageEntry.Value;
                    var validationResult = await _fileValidationService.ValidateImageAsync(image, MaxFileSize);
                    if ((bool)validationResult.isError)
                    {
                        return StatusCode(int.Parse(validationResult.Code), new { Message = validationResult.Message });
                    }
                }
                #endregion
                //-------------------------------------------------------------------------------------------------
                // Create a DemoReqModel instance to receive the blob URLs
                var demoReqModel = new DemoReqModel
                {
                    DemoRequestId = request.RequestId,
                    //Model = request.Model, // Set appropriate value if available
                    EmpNo = empNo,
                    //ChassisNo = request.ChassisNo,
                    //EngineNo = request.EngineNo,
                    //DateOfBilling = request.DateOfBilling,
                    CreatedDate = DateTime.Now,
                    CreatedBy = empNo,
                };

                // Upload valid images to blob storage and map to DemoReqModel fields
                foreach (var imageEntry in imagesToProcess)
                {
                    var propertyName = imageEntry.Key;
                    var image = imageEntry.Value;

                    // Upload to blob storage and get URL string
                    string blobUrl = await _blobStorageService.UploadFileAsync(image);

                    // Map to appropriate property in DemoReqModel based on the property name
                    switch (propertyName)
                    {
                        //case nameof(DemoDocUploadModel.InvoiceFile):
                        //    demoReqModel.InvoiceFile = blobUrl;
                        //    break;
                        //case nameof(DemoDocUploadModel.RCFile):
                        //    demoReqModel.RCFile = blobUrl;
                        //    break;
                        case nameof(DemoDocUploadModel.FileSale): //Sale Document of Tractor
                            demoReqModel.FileSale = blobUrl;
                            break;
                        case nameof(DemoDocUploadModel.FileTractor): //Format for Claiming
                            demoReqModel.FileTractor = blobUrl;
                            break;
                        case nameof(DemoDocUploadModel.FilePicture): //Picture of Hour Reading
                            demoReqModel.FilePicture = blobUrl;
                            break;
                        case nameof(DemoDocUploadModel.FilePicTractor): //Picture of Tractor
                            demoReqModel.FilePicTractor = blobUrl;
                            break;
                        case nameof(DemoDocUploadModel.LogDemonsFile):
                            demoReqModel.LogDemons = blobUrl;
                            break;
                        case nameof(DemoDocUploadModel.Affidavitfile):
                            demoReqModel.Affidavit = blobUrl;
                            break;
                        case nameof(DemoDocUploadModel.SaleDeedfile):
                            demoReqModel.SaleDeed = blobUrl;
                            break;
                    }
                }

                // Pass the DemoReqModel instead of DemoDocUploadModel to your service
                var result = await _demoService.AddDemoActualClaimService(demoReqModel,request.BasicFlag);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("DemoRequestController", "Error in AddUpdateAllClaim", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("UpdateDemoActualClaim")]
        public async Task<IActionResult> UpdateDemoActualClaim([FromForm] DemoUpdateModel request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                // Validate that RequestId is provided as it's required for updates


                if (request.RequestId == null || request.RequestId == 0)
                {
                    return StatusCode(400, "RequestId is required for update operations");

                }

                // Define all possible image properties to process (all optional for updates)
                var imagesToProcess = new Dictionary<string, IFormFile>
        {
            // Basic documents
            { nameof(DemoUpdateModel.InvoiceFile), request.InvoiceFile },
            { nameof(DemoUpdateModel.RCFile), request.RCFile },
            { nameof(DemoUpdateModel.InsuranceFile), request.InsuranceFile },
            
            // Additional documents
            { nameof(DemoUpdateModel.FileSale), request.FileSale }, //Sale Document of Tractor
            { nameof(DemoUpdateModel.FileTractor), request.FileTractor }, //Format for Claiming
            { nameof(DemoUpdateModel.FilePicture), request.FilePicture }, //Picture of Hour Reading
            { nameof(DemoUpdateModel.FilePicTractor), request.FilePicTractor }, //Picture of Tractor
            { nameof(DemoUpdateModel.LogDemonsFile), request.LogDemonsFile },
            { nameof(DemoUpdateModel.Affidavitfile), request.Affidavitfile },
            { nameof(DemoUpdateModel.SaleDeedfile), request.SaleDeedfile }
        };

                #region Validation For Image Files (Only validate files that are provided)
                // Validate each image that is provided (skip null files since they're optional in updates)
                foreach (var imageEntry in imagesToProcess.Where(x => x.Value != null))
                {
                    var image = imageEntry.Value;
                    var validationResult = await _fileValidationService.ValidateImageAsync(image, MaxFileSize);
                    if ((bool)validationResult.isError)
                    {
                        return StatusCode(int.Parse(validationResult.Code), new { Message = validationResult.Message });
                    }
                }
                #endregion

                // Create a DemoReqModel instance for update
                var demoReqModel = new DemoReqModel
                {
                    DemoRequestId = request.RequestId,
                    EmpNo = empNo,                    
                };

                // Only set non-file properties if they are provided
                if (!string.IsNullOrEmpty(request.Model))
                    demoReqModel.Model = request.Model;

                if (!string.IsNullOrEmpty(request.ChassisNo))
                    demoReqModel.ChassisNo = request.ChassisNo;

                if (!string.IsNullOrEmpty(request.EngineNo))
                    demoReqModel.EngineNo = request.EngineNo;

                if (!string.IsNullOrEmpty(request.DateOfBilling))
                    demoReqModel.DateOfBilling = request.DateOfBilling;

                // Upload provided images to blob storage and map to DemoReqModel fields
                foreach (var imageEntry in imagesToProcess.Where(x => x.Value != null))
                {
                    var propertyName = imageEntry.Key;
                    var image = imageEntry.Value;

                    // Upload to blob storage and get URL string
                    string blobUrl = await _blobStorageService.UploadFileAsync(image);

                    // Map to appropriate property in DemoReqModel based on the property name
                    switch (propertyName)
                    {
                        case nameof(DemoUpdateModel.InvoiceFile):
                            demoReqModel.InvoiceFile = blobUrl;
                            break;
                        case nameof(DemoUpdateModel.RCFile):
                            demoReqModel.RCFile = blobUrl;
                            break;
                        case nameof(DemoUpdateModel.InsuranceFile):
                            demoReqModel.InsuranceFile = blobUrl;
                            break;
                        case nameof(DemoUpdateModel.FileSale): //Sale Document of Tractor
                            demoReqModel.FileSale = blobUrl;
                            break;
                        case nameof(DemoUpdateModel.FileTractor): //Format for Claiming
                            demoReqModel.FileTractor = blobUrl;
                            break;
                        case nameof(DemoUpdateModel.FilePicture): //Picture of Hour Reading
                            demoReqModel.FilePicture = blobUrl;
                            break;
                        case nameof(DemoUpdateModel.FilePicTractor): //Picture of Tractor
                            demoReqModel.FilePicTractor = blobUrl;
                            break;
                        case nameof(DemoUpdateModel.LogDemonsFile):
                            demoReqModel.LogDemons = blobUrl;
                            break;
                        case nameof(DemoUpdateModel.Affidavitfile):
                            demoReqModel.Affidavit = blobUrl;
                            break;
                        case nameof(DemoUpdateModel.SaleDeedfile):
                            demoReqModel.SaleDeed = blobUrl;
                            break;
                    }
                }

                // Call update service method instead of add
                var result = await _demoService.UpdateDemoActualClaimService(demoReqModel,request.BasicFlag);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("DemoRequestController", "Error in UpdateDemoActualClaim", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }        

        [HttpPost("GetDemoTractorDoc")]
        public async Task<IActionResult> DemoTractorDoc([FromBody] DemoTractorDocReq request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                var filter = new FilterModel
                {
                    EmpNo = empNo,
                    RoleId = roleId,
                    ReqId = request.RequestId
                };


                var result = await _demoService.GetDemoTractorDocService(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("DemoRequestController", "Error in DemoTractorDoc", ex);
                return StatusCode(500, "An error occurred while fetching claim details.");
            }
        }


        [HttpPost("ApproveRejectDemoClaim")]
        public async Task<IActionResult> ApproveRejectDemoClaim ([FromBody] DemoTractorApproveRejectRequest request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");

                if (string.IsNullOrEmpty(empNo) || string.IsNullOrEmpty(roleId))
                {
                    return Unauthorized("User not authenticated or missing required claims");
                }

                var filter = new FilterModel
                {
                    EmpNo = empNo,
                    RoleId = roleId,
                    ReqId = request.ReqId,
                    IsApproved = request.IsApproved,
                    RejectRemarks = request.RejectRemarks
                };

                var result = await _demoService.DemoTractorApproveRejectClaimService(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApproveRejectDemoRequest for request ID: {ReqId}", request?.ReqId);
                _fileLogger.LogError("DemoRequestController", $"Error in ApproveRejectDemoClaim for request ID: {request?.ReqId}", ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("AddDemoRemarks")]
        public async Task<IActionResult> AddDemoRemarks([FromBody] AddDemoTracRemarksModel request)
        {
            try
            {
                var empNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo");
                var roleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId");
                var result = await _demoService.AddDemoRemarksService(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("DemoTractorReqController", "Error in AddDemoRemarks", ex);
                return StatusCode(500, "An error occurred while fetching claim details.");
            }
        }

        private FilterModel CreateFilterModel(DemoTractorRequestModel request)
        {
            return new FilterModel
            {
                EmpNo = _jwtHelper.GetClaimValue(HttpContext, "EmpNo"),
                RoleId = _jwtHelper.GetClaimValue(HttpContext, "RoleId"),
                State = request.State,
                Status = request.Status,
                RequestNo = request.RequestNo,
                Fyear = request.Fyear,
                From = request.From,
                To = request.To,
                Export = request.Export,
            };
        }
    }
}