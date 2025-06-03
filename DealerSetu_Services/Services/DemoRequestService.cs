using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    public class DemoRequestService : IDemoRequestService
    {
        private static readonly Dictionary<string, string> FieldDisplayNames = new Dictionary<string, string>
        {
            { "Model", "Model" },
            { "ChassisNo", "Chassis No" },
            { "EngineNo", "Engine No" }
        };

        private readonly IDemoRequestRepository _demoRequestRepository;
        private readonly IFileValidationService _fileValidationService;

        public DemoRequestService(
            IDemoRequestRepository demoRequestRepository, 
            ILogger<DemoRequestService> logger, 
            IFileValidationService fileValidationService)
        {
            _demoRequestRepository = demoRequestRepository ?? throw new ArgumentNullException(nameof(demoRequestRepository));
            _fileValidationService = fileValidationService ?? throw new ArgumentNullException(nameof(fileValidationService));
        }

        public async Task<ServiceResponse> DemoTractorApprovedService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                ValidateFilterParams(filter, pageIndex, pageSize);

                var (demoTractorList, totalCount) = await _demoRequestRepository.DemoTractorApprovedRepo(filter, pageIndex, pageSize);

                return CreateSuccessResponse(
                    demoTractorList,
                    totalCount,
                    "Demo tractor list retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex, "Invalid parameters for retrieving demo tractor list", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error while retrieving demo tractor list");
            }
        }

        public async Task<ServiceResponse> DemoTractorPendingService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                ValidateFilterParams(filter, pageIndex, pageSize);

                var (pendingDemoTractorList, totalCount) = await _demoRequestRepository.DemoTractorPendingRepo(filter, pageIndex, pageSize);

                return CreateSuccessResponse(
                    pendingDemoTractorList,
                    totalCount,
                    "Pending demo tractor list retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex, "Invalid parameters for retrieving pending demo tractor list", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error while retrieving pending demo tractor list");
            }
        }

        public async Task<ServiceResponse> DemoTractorPendingClaimService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                ValidateFilterParams(filter, pageIndex, pageSize);

                var (pendingDemoTractorList, totalCount) = await _demoRequestRepository.DemoTractorPendingClaimRepo(filter, pageIndex, pageSize);

                return CreateSuccessResponse(
                    pendingDemoTractorList,
                    totalCount,
                    "Pending demo tractor claim list retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex, "Invalid parameters for retrieving pending demo tractor claim list", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error while retrieving pending demo tractor claim list");
            }
        }

        public async Task<ServiceResponse> FiscalYearsService()
        {
            try
            {
                var fiscalYears = await _demoRequestRepository.FiscalYearsRepo();

                return CreateSuccessResponse(
                    fiscalYears,
                    fiscalYears?.Count ?? 0,
                    "Fiscal years retrieved successfully");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error retrieving fiscal years");
            }
        }

        public async Task<ServiceResponse> SubmitDemoReqService(DemoReqSubmissionModel request, string empNo)
        {
            try
            {
                ValidateSubmissionRequest(request, empNo);

                var demoReqId = await _demoRequestRepository.SubmitDemoReqRepo(request, empNo);

                return CreateSuccessResponse(
                    demoReqId,
                    1,
                    "Demo request submitted successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex, "Invalid parameters for demo request submission", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error submitting demo request");
            }
        }

        public async Task<ServiceResponse> DemoReqDataService(int reqId)
        {
            try
            {
                if (reqId <= 0)
                {
                    throw new ArgumentException("Request ID must be greater than zero", nameof(reqId));
                }

                var demoRequestData = await _demoRequestRepository.DemoReqDataRepo(reqId);

                if (demoRequestData == null)
                {
                    return CreateErrorResponse(null, "Demo request not found", "404");
                }

                return CreateSuccessResponse(
                    demoRequestData,
                    1,
                    "Demo request data retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex, "Invalid request ID", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error retrieving demo request data");
            }
        }

        public async Task<ServiceResponse> DemoTractorApproveRejectService(FilterModel filter)
        {
            var action = (bool)filter.IsApproved ? "approval" : "rejection";

            try
            {
                ValidateApprovalRequest(filter);

                var result = await _demoRequestRepository.DemoTractorApproveRejectRepo(filter);

                var actionType = (bool)filter.IsApproved ? "approved" : "rejected";

                return CreateSuccessResponse(
                    result,
                    1,
                    $"Demo tractor request {actionType} successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex, "Invalid parameters for demo tractor approval/rejection", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error approving/rejecting demo tractor request");
            }
        }

        public async Task<ServiceResponse> UpdateDemoReqService(DemoReqUpdateModel request, string empNo)
        {

            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                if (string.IsNullOrWhiteSpace(empNo))
                {
                    throw new ArgumentException("Employee number is required", nameof(empNo));
                }

                var reqNo = await _demoRequestRepository.UpdateDemoReqRepo(request, empNo);


                return CreateSuccessResponse(
                    reqNo,
                    1,
                    "Demo request updated successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex, "Invalid parameters for updating demo request", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error updating demo request");
            }
        }

        public async Task<ServiceResponse> DemoActualClaimListService(FilterModel filter)
        {

            try
            {
                if (filter == null)
                {
                    throw new ArgumentNullException(nameof(filter));
                }

                var demoActualClaimList = await _demoRequestRepository.DemoActualClaimListRepo(filter);


                return CreateSuccessResponse(
                    demoActualClaimList,
                    demoActualClaimList?.Count ?? 0,
                    "Demo actual claim list retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex, "Invalid parameters for retrieving demo actual claim list", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error retrieving demo actual claim list");
            }
        }

        public async Task<ServiceResponse> AddDemoActualClaimService(DemoReqModel request)
        {

            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                var validationResult = ValidateClaimFields(request);
                if (validationResult != null)
                {
                    return validationResult;
                }

                int claimId;
                if (!string.IsNullOrWhiteSpace(request.ChassisNo))
                {
                    claimId = await _demoRequestRepository.AddBasicDemoActualClaimRepo(request);
                }
                else
                {
                    claimId = await _demoRequestRepository.AddAllDemoActualClaimRepo(request);
                }

                return CreateSuccessResponse(
                    claimId,
                    1,
                    "Claim submitted successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex, "Invalid parameters for submitting claim", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error submitting claim");
            }
        }

        public async Task<ServiceResponse> GetDemoTractorDocService(FilterModel filter)
        {

            try
            {
                if (filter == null)
                {
                    throw new ArgumentNullException(nameof(filter));
                }

                var demoTractorDoc = await _demoRequestRepository.GetDemoTractorDoc(filter);


                return CreateSuccessResponse(
                    demoTractorDoc,
                    1,
                    "Demo tractor document retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex, "Invalid parameters for retrieving demo tractor document", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error retrieving demo tractor document");
            }
        }

        public async Task<ServiceResponse> DemoTractorApproveRejectClaimService(FilterModel filter)
        {
            var action = (bool)filter.IsApproved ? "approval" : "rejection";

            try
            {
                ValidateApprovalRequest(filter);

                var result = await _demoRequestRepository.DemoTractorApproveRejectClaimRepo(filter);

                var actionType = (bool)filter.IsApproved ? "approved" : "rejected";

                return CreateSuccessResponse(
                    result,
                    1,
                    $"Demo tractor claim {actionType} successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex, "Invalid parameters for demo tractor claim approval/rejection", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error approving/rejecting demo tractor claim");
            }
        }

        public async Task<ServiceResponse> AddDemoRemarksService(AddDemoTracRemarksModel request)
        {

            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                var validationResult = ValidateRemarksFields(request);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var demoReqId = await _demoRequestRepository.AddActualDemoRemarkRepo(request);


                return CreateSuccessResponse(
                    demoReqId,
                    1,
                    "Remarks added successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex, "Invalid parameters for adding remarks", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex, "Error adding remarks");
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

            if (!(bool)filter.IsApproved && string.IsNullOrWhiteSpace(filter.RejectRemarks))
            {
                throw new ArgumentException("Rejection remarks are required when rejecting a request", nameof(filter.RejectRemarks));
            }
        }

        private ServiceResponse ValidateClaimFields(DemoReqModel request)
        {
            var fieldsToValidate = new Dictionary<string, string>
            {
                { nameof(request.Model), request.Model },
                { nameof(request.ChassisNo), request.ChassisNo },
                { nameof(request.EngineNo), request.EngineNo }
            };

            foreach (var field in fieldsToValidate)
            {
                if (string.IsNullOrWhiteSpace(field.Value))
                    continue;

                var displayName = FieldDisplayNames.ContainsKey(field.Key) 
                    ? FieldDisplayNames[field.Key] 
                    : FormatFieldName(field.Key);

                if (_fileValidationService.ContainsMaliciousPatterns(field.Value))
                {
                    return CreateErrorResponse(null, $"{displayName} contains potentially malicious content", "400");
                }
            }

            return null;
        }

        private ServiceResponse ValidateRemarksFields(AddDemoTracRemarksModel request)
        {
            if (!string.IsNullOrWhiteSpace(request.Remarks) && 
                _fileValidationService.ContainsMaliciousPatterns(request.Remarks))
            {
                return CreateErrorResponse(null, "Remarks contains potentially malicious content", "400");
            }

            if (!string.IsNullOrWhiteSpace(request.RemarksDate) && 
                _fileValidationService.ContainsMaliciousPatterns(request.RemarksDate))
            {
                return CreateErrorResponse(null, "Remarks date contains potentially malicious content", "400");
            }

            return null;
        }

        private static string FormatFieldName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return fieldName;

            return string.Concat(fieldName.Select(c => char.IsUpper(c) ? " " + c : c.ToString())).Trim();
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