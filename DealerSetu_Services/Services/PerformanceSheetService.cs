using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    public class PerformanceSheetService : IPerformanceSheetService
    {
        private readonly IPerformanceSheetRepo _perfSheetRepository;
        private readonly IFileValidationService _fileValidationService;

        #region VALIDATION FLAGS - Enable/Disable validations by commenting/uncommenting

        // Data Input Validations
        private const bool ENABLE_DEALER_EMP_ID_VALIDATION = true;
        private const bool ENABLE_MONTH_VALIDATION = true;
        private const bool ENABLE_FYEAR_VALIDATION = true;
        private const bool ENABLE_EMP_NO_VALIDATION = true;
        private const bool ENABLE_MALICIOUS_CONTENT_CHECK = true;
        private const bool ENABLE_ALPHANUMERIC_VALIDATION = true;
        private const bool ENABLE_NUMERIC_VALIDATION = true;

        // Business Logic Validations
        private const bool ENABLE_RANGE_VALIDATION = true;
        private const bool ENABLE_REQUIRED_FIELD_VALIDATION = true;

        #endregion

        public PerformanceSheetService(
            IPerformanceSheetRepo perfRepository,
            IFileValidationService fileValidationService)
        {
            _perfSheetRepository = perfRepository ?? throw new ArgumentNullException(nameof(perfRepository));
            _fileValidationService = fileValidationService ?? throw new ArgumentNullException(nameof(fileValidationService));
        }

        public async Task<ServiceResponse> GetTrackingDealersServiceAsync(DealersRequestModel request, string empNo)
        {
            try
            {
                // VALIDATION BLOCK 1: Basic Request Validation
                var requestValidation = ValidateDealersRequest(request, empNo);
                if (requestValidation != null)
                    return requestValidation;

                var (dealers, count) = await _perfSheetRepository.GetTrackingDealersRepoAsync(request, empNo);
                return CreateSuccessResponse(dealers, count, "Tracking Dealers fetched Successfully");
            }
            catch (ArgumentNullException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error in fetching Tracking Dealers", "500", ex.Message);
            }
        }

        public async Task<ServiceResponse> GetPendingDealersServiceAsync(DealersRequestModel request, string empNo)
        {
            try
            {
                // VALIDATION BLOCK 1: Basic Request Validation
                var requestValidation = ValidateDealersRequest(request, empNo);
                if (requestValidation != null)
                    return requestValidation;

                var (dealers, count) = await _perfSheetRepository.GetPendingDealersRepoAsync(request, empNo);
                return CreateSuccessResponse(dealers, count, "Pending Dealers fetched Successfully");
            }
            catch (ArgumentNullException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error in fetching Pending Dealers", "500", ex.Message);
            }
        }

        public async Task<ServiceResponse> GetDealerListServiceAsync(string empNo)
        {
            try
            {
                // VALIDATION BLOCK 2: Employee Number Validation
                var empNoValidation = ValidateEmployeeNumber(empNo);
                if (empNoValidation != null)
                    return empNoValidation;

                var (dealers, count) = await _perfSheetRepository.GetDealerListRepoAsync(empNo);
                return CreateSuccessResponse(dealers, count, "Dealer List fetched Successfully");
            }
            catch (ArgumentNullException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error in fetching Dealer List", "500", ex.Message);
            }
        }

        public async Task<ServiceResponse> GetPerformanceSheetServiceAsync(PerformanceSheetReqModel request)
        {
            try
            {
                // VALIDATION BLOCK 3: Performance Sheet Request Validation
                var requestValidation = ValidatePerformanceSheetRequest(request);
                if (requestValidation != null)
                    return requestValidation;

                var sheet = await _perfSheetRepository.GetPerformanceSheetRepoAsync(request);
                return CreateSuccessResponse(sheet, "Performance Sheet fetched Successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error in fetching Performance Sheet", "500", ex.Message);
            }
        }

        public async Task<ServiceResponse> GetDealerBusinessPlanServiceAsync(BusinessPlanReqModel request)
        {
            try
            {
                // VALIDATION BLOCK 4: Business Plan Request Validation
                var requestValidation = ValidateBusinessPlanRequest(request);
                if (requestValidation != null)
                    return requestValidation;

                var plan = await _perfSheetRepository.GetDealerBusinessPlanRepoAsync(request);
                return CreateSuccessResponse(plan, "Business Plan fetched Successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error in fetching Business Plan", "500", ex.Message);
            }
        }

        public async Task<ServiceResponse> SubmitDealerBusinessPlanServiceAsync(BusinessPerformancePlan request)
        {
            try
            {
                // VALIDATION BLOCK 5: Business Performance Plan Validation
                var requestValidation = ValidateBusinessPerformancePlan(request);
                if (requestValidation != null)
                    return requestValidation;

                var result = await _perfSheetRepository.SubmitDealerBusinessPlanRepoAsync(request);

                return result.BusinessPlanAdded
                    ? CreateSuccessResponse(result, "Business Plan submitted Successfully")
                    : CreateErrorResponse("Error in submitting Business Plan", "500", "Business Plan not submitted");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error in submitting Business Plan", "500", ex.Message);
            }
        }

        public async Task<ServiceResponse> GetDealerDetailsServiceAsync(PerformanceSheetReqModel request)
        {
            try
            {
                // VALIDATION BLOCK 3: Performance Sheet Request Validation
                var requestValidation = ValidatePerformanceSheetRequest(request);
                if (requestValidation != null)
                    return requestValidation;

                var details = await _perfSheetRepository.GetDealerDetailsRepoAsync(request);
                return CreateSuccessResponse(details, "Dealer details fetched Successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error in fetching Dealer details", "500", ex.Message);
            }
        }

        public async Task<ServiceResponse> GetActionPlanDetailServiceAsync(ActionPlanDetailReqModel request)
        {
            try
            {
                // VALIDATION BLOCK 6: Action Plan Request Validation
                var requestValidation = ValidateActionPlanRequest(request);
                if (requestValidation != null)
                    return requestValidation;

                var details = await _perfSheetRepository.GetActionPlanDetailsRepoAsync(request);
                return CreateSuccessResponse(details, "Action Plan details fetched Successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error in fetching Action Plan details", "500", ex.Message);
            }
        }

        public async Task<ServiceResponse> SubmitActionPlanServiceAsync(ActionPlanModel request)
        {
            try
            {
                // VALIDATION BLOCK 7: Action Plan Model Validation
                var requestValidation = ValidateActionPlanModel(request);
                if (requestValidation != null)
                    return requestValidation;

                var result = await _perfSheetRepository.SubmitActionPlanRepoAsync(request);
                return CreateSuccessResponse(result, "Action Plan submitted Successfully");
            }
            catch (ArgumentNullException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error in submitting Action Plan", "500", ex.Message);
            }
        }

        public async Task<ServiceResponse> SubmitPerformanceSheetServiceAsync(PerformanceSheetUpdateModel request, string empNo)
        {
            try
            {
                // VALIDATION BLOCK 8: Performance Sheet Update Model Validation
                var requestValidation = ValidatePerformanceSheetUpdateModel(request);
                if (requestValidation != null)
                    return requestValidation;

                // VALIDATION BLOCK 2: Employee Number Validation
                var empNoValidation = ValidateEmployeeNumber(empNo);
                if (empNoValidation != null)
                    return empNoValidation;

                // Map update model to domain model
                var model = MapToPerformanceSheetModel(request, empNo);
                var result = await _perfSheetRepository.SubmitPerformanceSheetRepoAsync(model);
                return CreateSuccessResponse(result, "Performance Sheet Updated Successfully");
            }
            catch (ArgumentNullException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error in updating Performance Sheet", "500", ex.Message);
            }
        }

        #region VALIDATION METHODS - Modular and Easy to Enable/Disable

        /// <summary>
        /// VALIDATION BLOCK 1: Dealers Request Validation
        /// </summary>
        private ServiceResponse ValidateDealersRequest(DealersRequestModel request, string empNo)
        {
            if (ENABLE_REQUIRED_FIELD_VALIDATION)
            {
                if (request == null)
                    return CreateErrorResponse("Request cannot be null", "400");
            }

            // Employee Number Validation
            var empNoValidation = ValidateEmployeeNumber(empNo);
            if (empNoValidation != null)
                return empNoValidation;

            // Validate pagination parameters if they exist
            if (request != null)
            {
                if (ENABLE_RANGE_VALIDATION)
                {
                    // Assuming common pagination properties exist
                    var pageIndexProperty = request.GetType().GetProperty("PageIndex");
                    var pageSizeProperty = request.GetType().GetProperty("PageSize");

                    if (pageIndexProperty != null)
                    {
                        var pageIndex = (int?)pageIndexProperty.GetValue(request);
                        if (pageIndex.HasValue && pageIndex < 0)
                            return CreateErrorResponse("Page index cannot be negative", "400");
                    }

                    if (pageSizeProperty != null)
                    {
                        var pageSize = (int?)pageSizeProperty.GetValue(request);
                        if (pageSize.HasValue && (pageSize <= 0 || pageSize > 100))
                            return CreateErrorResponse("Page size must be between 1 and 100", "400");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 2: Employee Number Validation
        /// </summary>
        private ServiceResponse ValidateEmployeeNumber(string empNo)
        {
            if (ENABLE_EMP_NO_VALIDATION)
            {
                if (string.IsNullOrWhiteSpace(empNo))
                    return CreateErrorResponse("Employee number cannot be null or empty", "400");

                // Malicious Content Check
                if (ENABLE_MALICIOUS_CONTENT_CHECK)
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(empNo))
                        return CreateErrorResponse("Employee number contains potentially malicious content", "400");
                }

                // Alphanumeric Validation
                if (ENABLE_ALPHANUMERIC_VALIDATION)
                {
                    if (!_fileValidationService.IsAlphanumericWithSpace(empNo))
                        return CreateErrorResponse("Employee number must contain only letters, numbers and spaces", "400");
                }
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 3: Performance Sheet Request Validation
        /// </summary>
        private ServiceResponse ValidatePerformanceSheetRequest(PerformanceSheetReqModel request)
        {
            if (ENABLE_REQUIRED_FIELD_VALIDATION)
            {
                if (request == null)
                    return CreateErrorResponse("Request cannot be null", "400");
            }

            if (ENABLE_DEALER_EMP_ID_VALIDATION)
            {
                if (request.DealerEmpId <= 0)
                    return CreateErrorResponse("DealerEmpId must be greater than 0", "400");
            }

            if (ENABLE_MONTH_VALIDATION)
            {
                if (request.Month < 1 || request.Month > 12)
                    return CreateErrorResponse("Month must be between 1 and 12", "400");
            }

            if (ENABLE_FYEAR_VALIDATION)
            {
                if (string.IsNullOrWhiteSpace(request.FYear))
                    return CreateErrorResponse("FYear cannot be null or empty", "400");

                // Malicious Content Check
                if (ENABLE_MALICIOUS_CONTENT_CHECK)
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(request.FYear))
                        return CreateErrorResponse("FYear contains potentially malicious content", "400");
                }
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 4: Business Plan Request Validation
        /// </summary>
        private ServiceResponse ValidateBusinessPlanRequest(BusinessPlanReqModel request)
        {
            if (ENABLE_REQUIRED_FIELD_VALIDATION)
            {
                if (request == null)
                    return CreateErrorResponse("Request cannot be null", "400");
            }

            if (ENABLE_DEALER_EMP_ID_VALIDATION)
            {
                if (request.DealerEmpId <= 0)
                    return CreateErrorResponse("DealerEmpId must be greater than 0", "400");
            }

            if (ENABLE_FYEAR_VALIDATION)
            {
                if (string.IsNullOrWhiteSpace(request.FYear))
                    return CreateErrorResponse("FYear cannot be null or empty", "400");

                // Malicious Content Check
                if (ENABLE_MALICIOUS_CONTENT_CHECK)
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(request.FYear))
                        return CreateErrorResponse("FYear contains potentially malicious content", "400");
                }
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 5: Business Performance Plan Validation
        /// </summary>
        private ServiceResponse ValidateBusinessPerformancePlan(BusinessPerformancePlan request)
        {
            if (ENABLE_REQUIRED_FIELD_VALIDATION)
            {
                if (request == null)
                    return CreateErrorResponse("Request cannot be null", "400");
            }

            if (ENABLE_DEALER_EMP_ID_VALIDATION)
            {
                if (request.DealerEmpId <= 0)
                    return CreateErrorResponse("DealerEmpId must be greater than 0", "400");
            }

            if (ENABLE_FYEAR_VALIDATION)
            {
                if (string.IsNullOrWhiteSpace(request.FYear))
                    return CreateErrorResponse("FYear cannot be null or empty", "400");

                // Malicious Content Check
                if (ENABLE_MALICIOUS_CONTENT_CHECK)
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(request.FYear))
                        return CreateErrorResponse("FYear contains potentially malicious content", "400");
                }
            }

            // Validate numeric fields if they exist
            if (ENABLE_NUMERIC_VALIDATION)
            {
                var numericValidation = ValidateNumericFields(request);
                if (numericValidation != null)
                    return numericValidation;
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 6: Action Plan Request Validation
        /// </summary>
        private ServiceResponse ValidateActionPlanRequest(ActionPlanDetailReqModel request)
        {
            if (ENABLE_REQUIRED_FIELD_VALIDATION)
            {
                if (request == null)
                    return CreateErrorResponse("Request cannot be null", "400");
            }

            if (ENABLE_DEALER_EMP_ID_VALIDATION)
            {
                if (request.DealerEmpId <= 0)
                    return CreateErrorResponse("Invalid dealer employee ID", "400");
            }

            if (ENABLE_MONTH_VALIDATION)
            {
                if (request.Month < 1 || request.Month > 12)
                    return CreateErrorResponse("Invalid month. Month should be between 1 and 12", "400");
            }

            if (ENABLE_FYEAR_VALIDATION)
            {
                if (string.IsNullOrWhiteSpace(request.FYear))
                    return CreateErrorResponse("Financial year is required", "400");

                // Malicious Content Check
                if (ENABLE_MALICIOUS_CONTENT_CHECK)
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(request.FYear))
                        return CreateErrorResponse("Financial year contains potentially malicious content", "400");
                }
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 7: Action Plan Model Validation
        /// </summary>
        private ServiceResponse ValidateActionPlanModel(ActionPlanModel request)
        {
            if (ENABLE_REQUIRED_FIELD_VALIDATION)
            {
                if (request == null)
                    return CreateErrorResponse("Action plan model cannot be null", "400");
            }

            // Validate text fields in the action plan
            var textFields = new Dictionary<string, string>();

            // Use reflection to get all string properties
            var properties = request.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(request) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        textFields.Add(property.Name, value);
                    }
                }
            }

            // Validate each text field
            foreach (var field in textFields)
            {
                if (ENABLE_MALICIOUS_CONTENT_CHECK)
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(field.Value))
                        return CreateErrorResponse($"{field.Key} contains potentially malicious content", "400");
                }
            }

            return null;
        }

        /// <summary>
        /// VALIDATION BLOCK 8: Performance Sheet Update Model Validation
        /// </summary>
        private ServiceResponse ValidatePerformanceSheetUpdateModel(PerformanceSheetUpdateModel request)
        {
            if (ENABLE_REQUIRED_FIELD_VALIDATION)
            {
                if (request == null)
                    return CreateErrorResponse("Performance sheet update model cannot be null", "400");
            }

            if (ENABLE_DEALER_EMP_ID_VALIDATION)
            {
                if (request.DealerEmpId <= 0)
                    return CreateErrorResponse("DealerEmpId must be greater than 0", "400");
            }

            if (ENABLE_MONTH_VALIDATION)
            {
                if (request.Month < 1 || request.Month > 12)
                    return CreateErrorResponse("Month must be between 1 and 12", "400");
            }

            if (ENABLE_FYEAR_VALIDATION)
            {
                if (string.IsNullOrWhiteSpace(request.FYear))
                    return CreateErrorResponse("FYear cannot be null or empty", "400");

                // Malicious Content Check
                if (ENABLE_MALICIOUS_CONTENT_CHECK)
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(request.FYear))
                        return CreateErrorResponse("FYear contains potentially malicious content", "400");
                }
            }

            // Validate all string fields for malicious content
            if (ENABLE_MALICIOUS_CONTENT_CHECK)
            {
                var stringValidation = ValidateStringFieldsForMaliciousContent(request);
                if (stringValidation != null)
                    return stringValidation;
            }

            // Validate numeric fields
            if (ENABLE_NUMERIC_VALIDATION)
            {
                var numericValidation = ValidateNumericFields(request);
                if (numericValidation != null)
                    return numericValidation;
            }

            return null;
        }

        /// <summary>
        /// Helper method to validate numeric fields using reflection
        /// </summary>
        private ServiceResponse ValidateNumericFields(object request)
        {
            if (request == null) return null;

            var properties = request.GetType().GetProperties();
            foreach (var property in properties)
            {
                // Check for numeric types
                if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?) ||
                    property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?) ||
                    property.PropertyType == typeof(double) || property.PropertyType == typeof(double?) ||
                    property.PropertyType == typeof(float) || property.PropertyType == typeof(float?))
                {
                    var value = property.GetValue(request);
                    if (value != null)
                    {
                        // Convert to decimal for validation
                        if (decimal.TryParse(value.ToString(), out decimal numericValue))
                        {
                            if (numericValue < 0)
                                return CreateErrorResponse($"{property.Name} cannot be negative", "400");
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Helper method to validate string fields for malicious content
        /// </summary>
        private ServiceResponse ValidateStringFieldsForMaliciousContent(object request)
        {
            if (request == null) return null;

            var properties = request.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(request) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (_fileValidationService.ContainsMaliciousPatterns(value))
                            return CreateErrorResponse($"{property.Name} contains potentially malicious content", "400");
                    }
                }
            }

            return null;
        }

        #endregion

        // Optimized mapping method
        private static PerformanceSheetModel MapToPerformanceSheetModel(PerformanceSheetUpdateModel request, string empNo)
        {
            return new PerformanceSheetModel
            {
                DealerEmpId = request.DealerEmpId,
                Month = request.Month,
                FYear = request.FYear,
                TractorVol = request.TractorVol,
                TractorVolAdherence = request.TractorAdherence,
                BusinessPerformanceRemarks = request.BusinessRemarks,
                SpareParts = request.SpareParts,
                SparePartsAdherence = request.SparPartsAdherence,
                XMOil = request.XmOil,
                XMOilAdherence = request.XmOilAdherence,
                TACFLPlan = request.TaCFLPlan,
                OwnFundPlan = request.OwnFundPlan,
                BGPlan = request.BgPlan,
                OwnFund = request.OwnFund,
                BG = request.Bg,
                TACFLActual = request.TaCFLActual,
                FundRemarks = request.FundRemarks,
                SalesManpower = request.SalesManpower,
                SalesBranch = request.SalesBranch,
                SalesInfra = request.SalesInfra,
                SalesCIP = request.SalesCIP,
                ServiceManpower = request.ServiceManpower,
                ServiceBranch = request.ServiceBranch,
                ServiceInfra = request.ServiceInfra,
                ServiceCIP = request.ServiceCIP,
                AdminManpower = request.AdminManpower,
                AdminBranch = request.AdminBranch,
                AdminInfra = request.AdminInfra,
                AdminCIP = request.AdminCIP,
                SalesManpowerAct = request.SalesManpowerAct,
                SalesBranchAct = request.SalesBranchAct,
                SalesInfraAct = request.SalesInfraAct,
                SalesCIPAct = request.SalesCIPAct,
                ServiceManpowerAct = request.ServiceManpowerAct,
                ServiceBranchAct = request.ServiceBranchAct,
                ServiceInfraAct = request.ServiceInfraAct,
                ServiceCIPAct = request.ServiceCIPAct,
                AdminManpowerAct = request.AdminManpowerAct,
                AdminBranchAct = request.AdminBranchAct,
                AdminInfraAct = request.AdminInfraAct,
                AdminCIPAct = request.AdminCIPAct,
                CoverageRemarks = request.CoverageRemarks,
                OwnRentedPlan = request.OwnRentedPlan,
                CIPStatusPlan = request.CipStatusPlan,
                CoverageRemarksPlan = request.CoverageRemarksPlan,
                ShowroomSize = request.ShowroomSize,
                WorkshopSize = request.WorkshopSize,
                OwnRentedActual = request.OwnRentedActual,
                CIPStatusActual = request.CipStatusActual,
                CoverageRemarksActual = request.CoverageRemarksActual,
                FinalRemarks = request.FinalRemarks,
                IsActionPlanReq = request.IsActionPlanReq,
                ActionRequired = request.ActionRequired,
                FieldActivities = request.FieldActivities,
                CreatedBy = empNo
            };
        }

        // Consolidated response creation methods
        private static ServiceResponse CreateSuccessResponse(object result, string message) =>
            new ServiceResponse
            {
                isError = false,
                result = result,
                Message = message,
                Code = "200",
                Status = "Success"
            };

        private static ServiceResponse CreateSuccessResponse(object result, int totalCount, string message) =>
            new ServiceResponse
            {
                isError = false,
                result = result,
                totalCount = totalCount,
                Message = message,
                Code = "200",
                Status = "Success"
            };

        private static ServiceResponse CreateErrorResponse(string message, string code, string error = null) =>
            new ServiceResponse
            {
                isError = true,
                Error = error,
                Message = message,
                Status = "Error",
                Code = code
            };
    }
}