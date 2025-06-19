using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    public class PerformanceSheetService : IPerformanceSheetService
    {
        private readonly IPerformanceSheetRepo _perfSheetRepository;
        private readonly ILogger<PerformanceSheetService> _logger;

        public PerformanceSheetService(IPerformanceSheetRepo perfRepository, ILogger<PerformanceSheetService> logger)
        {
            _perfSheetRepository = perfRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse> GetTrackingDealersServiceAsync(DealersRequestModel request, string empNo)
        {
            try
            {
                var (trackingDealers,totalCount) = await _perfSheetRepository.GetTrackingDealersRepoAsync(request, empNo); ;
                return CreateSuccessResponse(trackingDealers, totalCount, "Tracking Dealers fetched Successfully");
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
                var (trackingDealers, totalCount) = await _perfSheetRepository.GetPendingDealersRepoAsync(request, empNo); ;
                return CreateSuccessResponse(trackingDealers, totalCount, "Pending Dealers fetched Successfully");
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
                var (dealerList, totalCount) = await _perfSheetRepository.GetDealerListRepoAsync(empNo); ;
                return CreateSuccessResponse(dealerList, totalCount, "Dealer List fetched Successfully");
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
                if (request.DealerEmpId <= 0)
                    throw new ArgumentException("DealerEmpId must be greater than 0", nameof(request.DealerEmpId));

                if (request.Month < 1 || request.Month > 12)
                    throw new ArgumentException("Month must be between 1 and 12", nameof(request.Month));
                

                if (request.FYear == null || request.FYear == "")
                {
                    throw new ArgumentException("FYear cannot be null or empty", nameof(request.FYear));
                }

                var performanceSheet = await _perfSheetRepository.GetPerformanceSheetRepoAsync(request);

                return CreateSuccessResponse(performanceSheet, "Performance Sheet fetched Successfully");
            }
            catch (ArgumentNullException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error in fetching Performance Sheet", "500", ex.Message);
            }
        }


        public async Task<ServiceResponse> GetDealerBusinessPlanServiceAsync(PerformanceSheetReqModel request)
        {
            try
            {
                if (request.DealerEmpId <= 0)
                    throw new ArgumentException("DealerEmpId must be greater than 0", nameof(request.DealerEmpId));

                if (request.Month < 1 || request.Month > 12)
                    throw new ArgumentException("Month must be between 1 and 12", nameof(request.Month));


                if (request.FYear == null || request.FYear == "")
                {
                    throw new ArgumentException("FYear cannot be null or empty", nameof(request.FYear));
                }

                var businessPlan = await _perfSheetRepository.GetDealerBusinessPlanRepoAsync(request);

                return CreateSuccessResponse(businessPlan, "Business Plan fetched Successfully");
            }
            catch (ArgumentNullException ex)
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
                if (request.DealerEmpId <= 0)
                    throw new ArgumentException("DealerEmpId must be greater than 0", nameof(request.DealerEmpId));               


                if (request.FYear == null || request.FYear == "")
                {
                    throw new ArgumentException("FYear cannot be null or empty", nameof(request.FYear));
                }

                var businessPlan = await _perfSheetRepository.SubmitDealerBusinessPlanRepoAsync(request);
                if (businessPlan.BusinessPlanAdded == true)
                {
                    return CreateSuccessResponse(businessPlan, "Business Plan submitted Successfully");
                }
                else 
                {
                    return CreateErrorResponse("Error in submitting Business Plan", "500", "Business Plan not submitted");
                }
            }
            catch (ArgumentNullException ex)
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
                // Validate input parameters
                if (request.DealerEmpId<= 0)
                {
                    throw new ArgumentException("Invalid dealer employee ID.", nameof(request.DealerEmpId));
                }

                if ( request.Month < 1 || request.Month> 12)
                {
                    throw new ArgumentException("Invalid month. Month should be between 1 and 12.", nameof(request.Month));
                }

                if (string.IsNullOrWhiteSpace(request.FYear))
                {
                    throw new ArgumentException("Financial year is required.", nameof(request.FYear));
                }

                var dealerDetails = await _perfSheetRepository.GetDealerDetailsRepoAsync(request); ;

                return CreateSuccessResponse(dealerDetails, "Dealer details fetched Successfully");
            }
            catch (ArgumentNullException ex)
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
                // Validate input parameters
                if (request.DealerEmpId <= 0)
                {
                    throw new ArgumentException("Invalid dealer employee ID.", nameof(request.DealerEmpId));
                }

                if (request.Month < 1 || request.Month > 12)
                {
                    throw new ArgumentException("Invalid month. Month should be between 1 and 12.", nameof(request.Month));
                }

                if (string.IsNullOrWhiteSpace(request.FYear))
                {
                    throw new ArgumentException("Financial year is required.", nameof(request.FYear));
                }

                var dealerDetails = await _perfSheetRepository.GetActionPlanDetailsRepoAsync(request); ;

                return CreateSuccessResponse(dealerDetails, "Dealer details fetched Successfully");
            }
            catch (ArgumentNullException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error in fetching Dealer details", "500", ex.Message);
            }
        }

        public async Task<ServiceResponse> SubmitActionPlanServiceAsync(ActionPlanModel request)
        {
            try
            {            

                var actionPlanId = await _perfSheetRepository.SubmitActionPlanRepoAsync(request); ;
                return CreateSuccessResponse(actionPlanId, "Action Plan submitted Successfully");
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

        public async Task<ServiceResponse> SubmitPerformanceSheetServiceAsync(PerformanceSheetUpdateModel request,string empNo)
        {
            try
            {
                var performanceSheetModel = new PerformanceSheetModel
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

                var performancSheetId = await _perfSheetRepository.SubmitPerformanceSheetRepoAsync(performanceSheetModel);
                return CreateSuccessResponse(performancSheetId, "Performance Sheet Updated Successfully");
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

        private static ServiceResponse CreateSuccessResponse(object result, string message)
        {
            return new ServiceResponse
            {
                isError = false,
                result = result,
                Message = message,
                Code = "200",
                Status = "Success"
            };
        }

        /// <summary>
        /// Creates a successful service response with result and total count.
        /// </summary>
        /// <param name="result">The result data</param>
        /// <param name="totalCount">Total count for pagination</param>
        /// <param name="message">Success message</param>
        /// <returns>Service response indicating success</returns>
        private static ServiceResponse CreateSuccessResponse(object result, int totalCount, string message)
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

        /// <summary>
        /// Creates an error service response.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="code">Error code</param>
        /// <param name="error">Detailed error information</param>
        /// <returns>Service response indicating error</returns>
        private static ServiceResponse CreateErrorResponse(string message, string code, string error = null)
        {
            return new ServiceResponse
            {
                isError = true,
                Error = error,
                Message = message,
                Status = "Error",
                Code = code
            };
        }

    }
}
