using DealerSetu.Repository.Common;
using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Services.IServices;
using DealerSetu_Services.Services;
using Microsoft.AspNetCore.Mvc;

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
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitDemoRequest");
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
                return BadRequest(ex.Message);
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
                //Utility.ExcepLog(ex);
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

                // Create a DemoReqModel instance to receive the blob URLs
                var demoReqModel = new DemoReqModel
                {
                    DemoRequestId = request.RequestId,
                    Model = request.Model, // Set appropriate value if available
                    DealerNo = empNo,
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
                var result = await _demoService.AddDemoActualClaimService(demoReqModel);
                return Ok(result);
            }
            catch (Exception ex)
            {
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

                // Create a DemoReqModel instance to receive the blob URLs
                var demoReqModel = new DemoReqModel
                {
                    DemoRequestId = request.RequestId,
                    //Model = request.Model, // Set appropriate value if available
                    DealerNo = empNo,
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
                var result = await _demoService.AddDemoActualClaimService(demoReqModel);
                return Ok(result);
            }
            catch (Exception ex)
            {
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
                //Utility.ExcepLog(ex);
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