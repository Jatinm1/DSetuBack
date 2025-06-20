using Dapper;
using DealerSetu_Data.Models.DTOs;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;

namespace DealerSetu_Repositories.Repositories
{
    public class PerformanceSheetRepo : IPerformanceSheetRepo
    {
        private readonly string _connectionString;

        public PerformanceSheetRepo(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("dbDealerSetuEntities")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        // Generic method for dealer queries with pagination
        private async Task<(List<T>, int)> GetDealersAsync<T>(string storedProcedure, DealersRequestModel request, string empNo)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Keyword", request.Keyword, DbType.String);
            parameters.Add("@Month", request.Month, DbType.Int32);
            parameters.Add("@FYear", request.FYear, DbType.String);
            parameters.Add("@EmpNo", empNo, DbType.String);
            parameters.Add("@PageIndex", request.PageIndex);
            parameters.Add("@PageSize", request.PageSize);

            using var multi = await connection.QueryMultipleAsync(storedProcedure, parameters,
                commandType: CommandType.StoredProcedure, commandTimeout: 30);

            var dealers = (await multi.ReadAsync<T>()).ToList();
            var totalCount = await multi.ReadSingleOrDefaultAsync<int>();
            return (dealers, totalCount);
        }

        public async Task<(List<DealerModel>, int TotalCount)> GetTrackingDealersRepoAsync(DealersRequestModel request, string empNo)
        {
            return await GetDealersAsync<DealerModel>("sp_PERF_GetTrackingDealers", request, empNo);
        }

        public async Task<(List<DealerModel>, int TotalCount)> GetPendingDealersRepoAsync(DealersRequestModel request, string empNo)
        {
            return await GetDealersAsync<DealerModel>("sp_PERF_GetPendingDealers", request, empNo);
        }

        public async Task<(List<DealerListModel>, int TotalCount)> GetDealerListRepoAsync(string empNo)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@EmpNo", empNo, DbType.String);

            using var multi = await connection.QueryMultipleAsync("sp_PERF_GetDealerList", parameters,
                commandType: CommandType.StoredProcedure, commandTimeout: 30);

            var dealers = (await multi.ReadAsync<DealerListModel>()).ToList();
            var totalCount = await multi.ReadSingleOrDefaultAsync<int>();
            return (dealers, totalCount);
        }

        public async Task<PerformanceSheetModel> GetPerformanceSheetRepoAsync(PerformanceSheetReqModel request)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@DealerEmpId", request.DealerEmpId, DbType.Int32);
            parameters.Add("@Month", request.Month, DbType.Int32);
            parameters.Add("@FYear", request.FYear, DbType.String, size: 10);

            using var multi = await connection.QueryMultipleAsync("sp_PERF_GetPerformanceSheetData", parameters,
                commandType: CommandType.StoredProcedure, commandTimeout: 30);

            var performanceSheet = await multi.ReadFirstOrDefaultAsync<PerformanceSheetDto>();
            if (performanceSheet == null) return null;

            var coverageData = await multi.ReadFirstOrDefaultAsync<PerformanceSheetCoverageDto>();
            var fieldActivities = (await multi.ReadAsync<FieldActivity>()).ToList();

            return MapToPerformanceSheetModel(performanceSheet, coverageData, fieldActivities);
        }

        public async Task<BusinessPerformancePlan> GetDealerBusinessPlanRepoAsync(BusinessPlanReqModel request)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@DealerEmpId", request.DealerEmpId, DbType.Int32);
            parameters.Add("@FYear", request.FYear, DbType.String);

            using var multi = await connection.QueryMultipleAsync("sp_PERF_GetBusinessPlan", parameters,
                commandType: CommandType.StoredProcedure);

            var businessPerformanceSheets = (await multi.ReadAsync<BusinessPerformanceSheet>()).ToList();
            var createdBy = await multi.ReadFirstOrDefaultAsync<string>();

            return new BusinessPerformancePlan
            {
                DealerEmpId = request.DealerEmpId,
                FYear = request.FYear,
                CreatedBy = createdBy,
                businessPerformanceSheets = businessPerformanceSheets
            };
        }

        public async Task<BusinessPlanResult> SubmitDealerBusinessPlanRepoAsync(BusinessPerformancePlan planModel)
        {
            var businessPlanJson = JsonConvert.SerializeObject(planModel.businessPerformanceSheets);

            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@DealerEmpId", planModel.DealerEmpId, DbType.Int32);
            parameters.Add("@FYear", planModel.FYear, DbType.String);
            parameters.Add("@CreatedBy", planModel.CreatedBy, DbType.String);
            parameters.Add("@BusinessPlanData", businessPlanJson, DbType.String);

            return await connection.QuerySingleAsync<BusinessPlanResult>("sp_PERF_AddBusinesPlan", parameters,
                commandType: CommandType.StoredProcedure, commandTimeout: 30);
        }

        public async Task<DealerModel> GetDealerDetailsRepoAsync(PerformanceSheetReqModel request)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@DealerEmpId", request.DealerEmpId, DbType.Int32);
            parameters.Add("@Month", request.Month, DbType.Int32);
            parameters.Add("@FYear", request.FYear, DbType.String, size: 10);

            return await connection.QueryFirstOrDefaultAsync<DealerModel>("sp_PERF_GetDealerDetails", parameters,
                commandType: CommandType.StoredProcedure, commandTimeout: 30);
        }

        public async Task<ActionPlanModel> GetActionPlanDetailsRepoAsync(ActionPlanDetailReqModel request)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@ActionPlanId", request.ActionPlanId);
            parameters.Add("@DealerEmpId", request.DealerEmpId);
            parameters.Add("@Month", request.Month);
            parameters.Add("@FYear", request.FYear);

            using var multi = await connection.QueryMultipleAsync("sp_PERF_ActionPlanDetails", parameters,
                commandType: CommandType.StoredProcedure);

            var basicResult = await multi.ReadSingleOrDefaultAsync<ActionPlanBasicResult>();
            var actionPlanLists = (await multi.ReadAsync<ActionPlanList>()).ToList();

            return new ActionPlanModel
            {
                Id = basicResult?.Id ?? 0,
                DealerEmpId = basicResult?.DealerEmpId ?? request.DealerEmpId,
                MonthId = basicResult?.MonthId ?? request.Month,
                FYear = basicResult?.FYear ?? request.FYear,
                PerformanceSheetId = 0,
                CreatedBy = "",
                CreatedDate = "",
                ActionPlanLists = actionPlanLists ?? new List<ActionPlanList>()
            };
        }

        public async Task<ActionPlanResult> SubmitActionPlanRepoAsync(ActionPlanModel actionModel)
        {
            using var connection = new SqlConnection(_connectionString);
            var actionPlanListJson = JsonConvert.SerializeObject(actionModel.ActionPlanLists);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", actionModel.Id, DbType.Int32);
            parameters.Add("@DealerEmpId", actionModel.DealerEmpId, DbType.Int32);
            parameters.Add("@MonthId", actionModel.MonthId, DbType.Int32);
            parameters.Add("@FYear", actionModel.FYear, DbType.String);
            parameters.Add("@CreatedBy", actionModel.CreatedBy, DbType.String);
            parameters.Add("@ActionPlanListJson", actionPlanListJson, DbType.String);

            return await connection.QueryFirstOrDefaultAsync<ActionPlanResult>("sp_PERF_SubmitActionPlan", parameters,
                commandType: CommandType.StoredProcedure, commandTimeout: 120);
        }

        public async Task<int> SubmitPerformanceSheetRepoAsync(PerformanceSheetModel model)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = CreatePerformanceSheetParameters(model);

            await connection.ExecuteAsync("sp_PERF_UpdatePerformanceSheet", parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@ResultId");
        }

        // Helper method to map performance sheet data
        private PerformanceSheetModel MapToPerformanceSheetModel(PerformanceSheetDto sheet,
            PerformanceSheetCoverageDto coverage, List<FieldActivity> fieldActivities)
        {
            return new PerformanceSheetModel
            {
                DealerEmpId = sheet.DealerEmpId,
                TractorVol = sheet.TractorVol,
                SpareParts = sheet.SpareParts,
                XMOil = sheet.XMOil,
                TractorVolAdherence = sheet.TractorVolAdherence,
                SparePartsAdherence = sheet.SparePartsAdherence,
                XMOilAdherence = sheet.XMOilAdherence,
                BusinessPerformanceRemarks = sheet.BusinessPerformanceRemarks,
                OwnFundPlan = sheet.OwnFundPlan,
                BGPlan = sheet.BGPlan,
                OwnFund = sheet.OwnFund,
                BG = sheet.BG,
                TACFLPlan = sheet.TACFLPlan,
                TACFLActual = sheet.TACFLActual,
                FundRemarks = sheet.FundRemarks,
                ShowroomSize = sheet.ShowroomSize,
                WorkshopSize = sheet.WorkshopSize,
                OwnRentedPlan = sheet.OwnRentedPlan,
                OwnRentedActual = sheet.OwnRentedActual,
                CIPStatusPlan = sheet.CIPStatusPlan,
                CIPStatusActual = sheet.CIPStatusActual,
                CoverageRemarksPlan = sheet.CoverageRemarksPlan,
                CoverageRemarksActual = sheet.CoverageRemarksActual,
                FieldActvities = sheet.FieldActvities,
                FinalRemarks = sheet.FinalRemarks,
                IsActionPlanReq = sheet.IsActionPlanReq ? 1 : 0,
                CreatedBy = sheet.CreatedBy,
                CreatedDate = sheet.CreatedDate.ToString("dd MMM yyyy"),
                Month = sheet.Month,
                FYear = sheet.FYear,
                ActionRequired = sheet.ActionRequired,

                // Coverage data
                SalesManpower = coverage?.SalesManpower,
                SalesBranch = coverage?.SalesBranch,
                SalesInfra = coverage?.SalesInfra,
                SalesCIP = coverage?.SalesCIP,
                ServiceManpower = coverage?.ServiceManpower,
                ServiceBranch = coverage?.ServiceBranch,
                ServiceInfra = coverage?.ServiceInfra,
                ServiceCIP = coverage?.ServiceCIP,
                AdminManpower = coverage?.AdminManpower,
                AdminBranch = coverage?.AdminBranch,
                AdminInfra = coverage?.AdminInfra,
                AdminCIP = coverage?.AdminCIP,
                SalesManpowerAct = coverage?.SalesManpowerAct,
                SalesBranchAct = coverage?.SalesBranchAct,
                SalesInfraAct = coverage?.SalesInfraAct,
                SalesCIPAct = coverage?.SalesCIPAct,
                ServiceManpowerAct = coverage?.ServiceManpowerAct,
                ServiceBranchAct = coverage?.ServiceBranchAct,
                ServiceInfraAct = coverage?.ServiceInfraAct,
                ServiceCIPAct = coverage?.ServiceCIPAct,
                AdminManpowerAct = coverage?.AdminManpowerAct,
                AdminBranchAct = coverage?.AdminBranchAct,
                AdminInfraAct = coverage?.AdminInfraAct,
                AdminCIPAct = coverage?.AdminCIPAct,
                CoverageRemarks = coverage?.Remarks,

                FieldActivities = fieldActivities ?? new List<FieldActivity>()
            };
        }

        // Helper method to create performance sheet parameters
        private DynamicParameters CreatePerformanceSheetParameters(PerformanceSheetModel model)
        {
            var parameters = new DynamicParameters();

            // Basic parameters
            parameters.Add("@DealerEmpId", model.DealerEmpId);
            parameters.Add("@Month", model.Month);
            parameters.Add("@FYear", model.FYear);
            parameters.Add("@TractorVol", model.TractorVol);
            parameters.Add("@TractorVolAdherence", model.TractorVolAdherence);
            parameters.Add("@BusinessPerformanceRemarks", model.BusinessPerformanceRemarks);
            parameters.Add("@SpareParts", model.SpareParts);
            parameters.Add("@SparePartsAdherence", model.SparePartsAdherence);
            parameters.Add("@XMOil", model.XMOil);
            parameters.Add("@XMOilAdherence", model.XMOilAdherence);
            parameters.Add("@TACFLPlan", model.TACFLPlan);
            parameters.Add("@OwnFundPlan", model.OwnFundPlan);
            parameters.Add("@BGPlan", model.BGPlan);
            parameters.Add("@OwnFund", model.OwnFund);
            parameters.Add("@BG", model.BG);
            parameters.Add("@TACFLActual", model.TACFLActual);
            parameters.Add("@FundRemarks", model.FundRemarks);
            parameters.Add("@ShowroomSize", model.ShowroomSize);
            parameters.Add("@WorkshopSize", model.WorkshopSize);
            parameters.Add("@OwnRentedPlan", model.OwnRentedPlan);
            parameters.Add("@OwnRentedActual", model.OwnRentedActual);
            parameters.Add("@CIPStatusPlan", model.CIPStatusPlan);
            parameters.Add("@CIPStatusActual", model.CIPStatusActual);
            parameters.Add("@CoverageRemarksPlan", model.CoverageRemarksPlan);
            parameters.Add("@CoverageRemarksActual", model.CoverageRemarksActual);
            parameters.Add("@FinalRemarks", model.FinalRemarks);
            parameters.Add("@IsActionPlanReq", model.IsActionPlanReq);
            parameters.Add("@ActionRequired", model.ActionRequired);
            parameters.Add("@CreatedBy", model.CreatedBy);

            // Coverage parameters
            parameters.Add("@SalesManpower", model.SalesManpower);
            parameters.Add("@ServiceManpower", model.ServiceManpower);
            parameters.Add("@AdminManpower", model.AdminManpower);
            parameters.Add("@SalesBranch", model.SalesBranch);
            parameters.Add("@ServiceBranch", model.ServiceBranch);
            parameters.Add("@AdminBranch", model.AdminBranch);
            parameters.Add("@SalesInfra", model.SalesInfra);
            parameters.Add("@ServiceInfra", model.ServiceInfra);
            parameters.Add("@AdminInfra", model.AdminInfra);
            parameters.Add("@SalesCIP", model.SalesCIP);
            parameters.Add("@ServiceCIP", model.ServiceCIP);
            parameters.Add("@AdminCIP", model.AdminCIP);
            parameters.Add("@SalesManpowerAct", model.SalesManpowerAct);
            parameters.Add("@ServiceManpowerAct", model.ServiceManpowerAct);
            parameters.Add("@AdminManpowerAct", model.AdminManpowerAct);
            parameters.Add("@SalesBranchAct", model.SalesBranchAct);
            parameters.Add("@ServiceBranchAct", model.ServiceBranchAct);
            parameters.Add("@AdminBranchAct", model.AdminBranchAct);
            parameters.Add("@SalesInfraAct", model.SalesInfraAct);
            parameters.Add("@ServiceInfraAct", model.ServiceInfraAct);
            parameters.Add("@AdminInfraAct", model.AdminInfraAct);
            parameters.Add("@SalesCIPAct", model.SalesCIPAct);
            parameters.Add("@ServiceCIPAct", model.ServiceCIPAct);
            parameters.Add("@AdminCIPAct", model.AdminCIPAct);
            parameters.Add("@CoverageRemarks", model.CoverageRemarks);

            // Field Activities as JSON
            var fieldActivitiesJson = JsonConvert.SerializeObject(model.FieldActivities);
            parameters.Add("@FieldActivitiesJson", fieldActivitiesJson);

            // Output parameter
            parameters.Add("@ResultId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            return parameters;
        }
    }
}