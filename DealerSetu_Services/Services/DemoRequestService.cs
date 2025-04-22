using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    public class DemoRequestService : IDemoRequestService
    {
        private readonly IDemoRequestRepository _demoRequestRepository;
        private readonly IFileValidationService _fileValidationService;
        private readonly ILogger<DemoRequestService> _logger;

        public DemoRequestService(IDemoRequestRepository demoRequestRepository, ILogger<DemoRequestService> logger, IFileValidationService fileValidationService)
        {
            _demoRequestRepository = demoRequestRepository ?? throw new ArgumentNullException(nameof(demoRequestRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileValidationService = fileValidationService;
        }

        public async Task<ServiceResponse> DemoTractorApprovedService(FilterModel filter, int pageIndex, int pageSize)
        {
            _logger.LogInformation("Getting approved demo tractor list with filters");

            try
            {
                ValidateFilterParams(filter, pageIndex, pageSize);

                var (demoTractorList, totalCount) = await _demoRequestRepository.DemoTractorApprovedRepo(filter, pageIndex, pageSize);

                _logger.LogInformation("Retrieved {Count} approved demo tractors out of {Total}",
                    demoTractorList.Count, totalCount);

                return CreateSuccessResponse(
                    demoTractorList,
                    totalCount,
                    "Demo tractor list retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters in DemoTractorApprovedService");
                return CreateErrorResponse(ex, "Invalid parameters for retrieving demo tractor list", "400");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoTractorApprovedService");
                return CreateErrorResponse(ex, "Error while retrieving demo tractor list");
            }
        }

        public async Task<ServiceResponse> DemoTractorPendingService(FilterModel filter, int pageIndex, int pageSize)
        {
            _logger.LogInformation("Getting pending demo tractor list with filters");

            try
            {
                ValidateFilterParams(filter, pageIndex, pageSize);

                var (pendingDemoTractorList, totalCount) = await _demoRequestRepository.DemoTractorPendingRepo(filter, pageIndex, pageSize);

                _logger.LogInformation("Retrieved {Count} pending demo tractors out of {Total}",
                    pendingDemoTractorList.Count, totalCount);

                return CreateSuccessResponse(
                    pendingDemoTractorList,
                    totalCount,
                    "Pending demo tractor list retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters in DemoTractorPendingService");
                return CreateErrorResponse(ex, "Invalid parameters for retrieving pending demo tractor list", "400");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoTractorPendingService");
                return CreateErrorResponse(ex, "Error while retrieving pending demo tractor list");
            }
        }

        public async Task<ServiceResponse> FiscalYearsService()
        {
            _logger.LogInformation("Getting fiscal years");

            try
            {
                var fiscalYears = await _demoRequestRepository.FiscalYearsRepo();

                _logger.LogInformation("Retrieved {Count} fiscal years", fiscalYears.Count);

                return CreateSuccessResponse(
                    fiscalYears,
                    fiscalYears.Count,
                    "Fiscal years retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FiscalYearsService");
                return CreateErrorResponse(ex, "Error retrieving fiscal years");
            }
        }

        public async Task<ServiceResponse> SubmitDemoReqService(DemoReqSubmissionModel request, string empNo)
        {
            _logger.LogInformation("Submitting demo request for employee {EmpNo}", empNo);

            try
            {
                ValidateSubmissionRequest(request, empNo);

                var demoReqId = await _demoRequestRepository.SubmitDemoReqRepo(request, empNo);

                _logger.LogInformation("Demo request submitted successfully with ID {DemoReqId}", demoReqId);

                return CreateSuccessResponse(
                    demoReqId,
                    1,
                    "Demo request submitted successfully");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid submission request parameters");
                return CreateErrorResponse(ex, "Invalid parameters for demo request submission", "400");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitDemoReqService");
                return CreateErrorResponse(ex, "Error submitting demo request");
            }
        }

        public async Task<ServiceResponse> DemoReqDataService(int reqId)
        {
            _logger.LogInformation("Getting demo request data for ID {ReqId}", reqId);

            try
            {
                if (reqId <= 0)
                {
                    throw new ArgumentException("Request ID must be greater than zero", nameof(reqId));
                }

                var demoRequestData = await _demoRequestRepository.DemoReqDataRepo(reqId);

                if (demoRequestData == null)
                {
                    _logger.LogWarning("No demo request data found for ID {ReqId}", reqId);
                    return CreateErrorResponse(null, "Demo request not found", "404");
                }

                _logger.LogInformation("Retrieved demo request data for ID {ReqId}", reqId);

                return CreateSuccessResponse(
                    demoRequestData,
                    1,
                    "Demo request data retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request ID parameter");
                return CreateErrorResponse(ex, "Invalid request ID", "400");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoReqDataService for request ID {ReqId}", reqId);
                return CreateErrorResponse(ex, "Error retrieving demo request data");
            }
        }

        public async Task<ServiceResponse> DemoTractorApproveRejectService(FilterModel filter)
        {
            _logger.LogInformation("Processing {Action} for request ID {ReqId}",
                (bool)filter.IsApproved ? "approval" : "rejection", filter.ReqId);

            try
            {
                ValidateApprovalRequest(filter);

                var result = await _demoRequestRepository.DemoTractorApproveRejectRepo(filter);

                var actionType = (bool)filter.IsApproved ? "approved" : "rejected";
                _logger.LogInformation("Demo tractor request {Action} successfully with result {Result}",
                    actionType, result);

                return CreateSuccessResponse(
                    result,
                    1,
                    $"Demo tractor request {actionType} successfully");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid approval/rejection parameters");
                return CreateErrorResponse(ex, "Invalid parameters for demo tractor approval/rejection", "400");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoTractorApproveRejectService for request ID {ReqId}", filter.ReqId);
                return CreateErrorResponse(ex, "Error approving/rejecting demo tractor request");
            }
        }

        public async Task<ServiceResponse> DemoActualClaimListService(FilterModel filter)
        {
            try
            {
                var demoActualClaimList= await _demoRequestRepository.DemoActualClaimListRepo(filter);
                return new ServiceResponse
                {
                    isError = false,
                    result = demoActualClaimList,
                    Message = "Demo Actual Claim List retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error retrieving Demo Actual Claim List",
                    Status = "Error",
                    Code = "500"
                };
            }
        }


        public async Task<ServiceResponse> AddDemoActualClaimService(DemoReqModel request)
        {
            try
            {
                // Define fields to validate
                var fieldsToValidate = new Dictionary<string, string>
                    {
                        { nameof(request.Model), request.Model },
                        { nameof(request.ChassisNo), request.ChassisNo },
                        { nameof(request.EngineNo), request.EngineNo }    
                    };
                foreach (var field in fieldsToValidate)
                {
                    string fieldName = field.Key;
                    string fieldValue = field.Value;

                    // Skip validation if field is null or empty
                    if (string.IsNullOrWhiteSpace(fieldValue))
                        continue;

                    // Format field name for display in error message (add spaces before capital letters)
                    string displayName = string.Concat(fieldName.Select(c => char.IsUpper(c) ? " " + c : c.ToString())).Trim();

                    // Check for malicious patterns
                    if (_fileValidationService.ContainsMaliciousPatterns(fieldValue))
                    {
                        return new ServiceResponse
                        {
                            isError = true,
                            result = null,
                            Status = "Error",
                            Message = $"{displayName} contains potentially malicious content",
                            Code = "400"
                        };
                    }
                }

                if (request.FileSale == null)
                {
                    var claimId = await _demoRequestRepository.AddBasicDemoActualClaimRepo(request);

                    return new ServiceResponse
                    {
                        isError = false,
                        result = claimId,
                        Message = "Claim Submitted successfully",
                        Status = "Success",
                        Code = "200"
                    };
                }
                else
                {
                    var claimId = await _demoRequestRepository.AddAllDemoActualClaimRepo(request);

                    return new ServiceResponse
                    {
                        isError = false,
                        result = claimId,
                        Message = "Claim Submitted successfully",
                        Status = "Success",
                        Code = "200"
                    };

                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error submitting Claim",
                    Status = "Error",
                    Code = "500"
                };
            }
        }

        public async Task<ServiceResponse> GetDemoTractorDocService(FilterModel filter)
        {
            try
            {
                var DemoTractorDoc = await _demoRequestRepository.GetDemoTractorDoc(filter);
                return new ServiceResponse
                {
                    isError = false,
                    result = DemoTractorDoc,
                    Message = "DemoTractorDoc data retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error retrieving DemoTractorDoc data",
                    Status = "Error",
                    Code = "500"
                };
            }
        }

        #region Helper Methods

        private void ValidateFilterParams(FilterModel filter, int pageIndex, int pageSize)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }
            if (filter.Export == false)
            {
                if (pageIndex < 0)
                {
                    throw new ArgumentException("Page index must be non-negative", nameof(pageIndex));
                }

                if (pageSize <= 0)
                {
                    throw new ArgumentException("Page size must be positive", nameof(pageSize));
                }
            }
        }

        private void ValidateSubmissionRequest(DemoReqSubmissionModel request, string empNo)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(empNo))
            {
                throw new ArgumentException("Employee number is required", nameof(empNo));
            }

            if (string.IsNullOrWhiteSpace(request.DealerNo))
            {
                throw new ArgumentException("Dealer number is required", nameof(request.DealerNo));
            }

            if (string.IsNullOrWhiteSpace(request.ModelRequested))
            {
                throw new ArgumentException("Model requested is required", nameof(request.ModelRequested));
            }
        }

        private void ValidateApprovalRequest(FilterModel filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (filter.ReqId <= 0)
            {
                throw new ArgumentException("Request ID must be positive", nameof(filter.ReqId));
            }

            if (string.IsNullOrWhiteSpace(filter.EmpNo))
            {
                throw new ArgumentException("Employee number is required", nameof(filter.EmpNo));
            }

            if ((bool)!filter.IsApproved && string.IsNullOrWhiteSpace(filter.RejectRemarks))
            {
                throw new ArgumentException("Rejection remarks are required when rejecting a request",
                    nameof(filter.RejectRemarks));
            }
        }

        private ServiceResponse CreateSuccessResponse(object result, int totalCount, string message)
        {
            return new ServiceResponse
            {
                isError = false,
                result = result,
                totalCount = totalCount,
                Message = message,
                Code = "200",
                Status = "Success"
            };
        }

        private ServiceResponse CreateErrorResponse(Exception ex, string message, string code = "500")
        {
            return new ServiceResponse
            {
                isError = true,
                Error = ex?.Message,
                Message = message,
                Code = code,
                Status = "Error"
            };
        }

        #endregion
    }
}