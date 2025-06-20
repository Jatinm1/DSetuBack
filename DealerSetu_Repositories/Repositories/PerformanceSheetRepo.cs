using Dapper;
using DealerSetu_Data.Models.DTOs;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;


namespace DealerSetu_Repositories.Repositories
{
    public class PerformanceSheetRepo : IPerformanceSheetRepo
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PerformanceSheetRepo> _logger;
        private readonly string _connectionString;

        public PerformanceSheetRepo(IConfiguration configuration, ILogger<PerformanceSheetRepo> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("dbDealerSetuEntities")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<(List<DealerModel>, int TotalCount)> GetTrackingDealersRepoAsync(DealersRequestModel request, string empNo)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@Keyword", request.Keyword, DbType.String);
                parameters.Add("@Month", request.Month, DbType.Int32); 
                parameters.Add("@FYear", request.FYear, DbType.String);
                parameters.Add("@EmpNo", empNo, DbType.String);
                parameters.Add("@PageIndex", request.PageIndex);
                parameters.Add("@PageSize", request.PageSize);


                using var multi = await connection.QueryMultipleAsync(
                    "sp_PERF_GetTrackingDealers",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );

                var trackingDealers = (await multi.ReadAsync<DealerModel>()).ToList();
                var totalCount = await multi.ReadSingleOrDefaultAsync<int>();
                return (trackingDealers, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tracking dealers for empNo: {EmpNo}, month: {Month}, year: {FYear}",
                    empNo, request.Month, request.FYear);
                throw;
            }
        }

        public async Task<(List<DealerModel>, int TotalCount)> GetPendingDealersRepoAsync(DealersRequestModel request, string empNo)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@Keyword", request.Keyword, DbType.String);
                parameters.Add("@Month", request.Month, DbType.Int32);
                parameters.Add("@FYear", request.FYear, DbType.String);
                parameters.Add("@EmpNo", empNo, DbType.String);
                parameters.Add("@PageIndex", request.PageIndex);
                parameters.Add("@PageSize", request.PageSize);


                using var multi = await connection.QueryMultipleAsync(
                    "sp_PERF_GetPendingDealers",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );

                var trackingDealers = (await multi.ReadAsync<DealerModel>()).ToList();
                var totalCount = await multi.ReadSingleOrDefaultAsync<int>();
                return (trackingDealers, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting pending dealers for empNo: {EmpNo}, month: {Month}, year: {FYear}",
                    empNo, request.Month, request.FYear);
                throw;
            }
        }



            public async Task<(List<DealerListModel>, int TotalCount)> GetDealerListRepoAsync(string empNo)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@EmpNo", empNo, DbType.String);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_PERF_GetDealerList",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );

                var trackingDealers = (await multi.ReadAsync<DealerListModel>()).ToList();
                var totalCount = await multi.ReadSingleOrDefaultAsync<int>();
                return (trackingDealers, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting Dealer List for empNo: {EmpNo}",empNo);
                throw;
            }
        }

        public async Task<PerformanceSheetModel> GetPerformanceSheetRepoAsync(PerformanceSheetReqModel request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@DealerEmpId", request.DealerEmpId, DbType.Int32);
                parameters.Add("@Month", request.Month, DbType.Int32);
                parameters.Add("@FYear", request.FYear, DbType.String, size: 10);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_PERF_GetPerformanceSheetData",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );

                // Read performance sheet main data
                var performanceSheet = await multi.ReadFirstOrDefaultAsync<PerformanceSheetDto>();
                if (performanceSheet == null)
                    return null;

                // Read coverage data
                var coverageData = await multi.ReadFirstOrDefaultAsync<PerformanceSheetCoverageDto>();

                // Read field activities
                var fieldActivities = (await multi.ReadAsync<FieldActivity>()).ToList();

                // Map to result model
                var result = MapToPerformanceSheetModel(performanceSheet, coverageData, fieldActivities);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in GetPerformanceSheetAsync for DealerEmpId: {dealerEmpId}", request.DealerEmpId);
                throw;
            }
        }

        public async Task<BusinessPerformancePlan> GetDealerBusinessPlanRepoAsync(PerformanceSheetReqModel request)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@DealerEmpId", request.DealerEmpId, DbType.Int32);
                    parameters.Add("@Month", request.Month, DbType.Int32);
                    parameters.Add("@FYear", request.FYear, DbType.String);

                    using (var multi = await connection.QueryMultipleAsync(
                        "sp_PERF_GetBusinessPlan",
                        parameters,
                        commandType: CommandType.StoredProcedure))
                    {
                        var businessPerformanceSheets = (await multi.ReadAsync<BusinessPerformanceSheet>()).ToList();
                        var createdBy = await multi.ReadFirstOrDefaultAsync<string>();

                        var businessPlan = new BusinessPerformancePlan
                        {
                            DealerEmpId = request.DealerEmpId,
                            FYear = request.FYear,
                            CreatedBy = createdBy,
                            businessPerformanceSheets = businessPerformanceSheets
                        };
                        return businessPlan;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task<BusinessPlanResult> SubmitDealerBusinessPlanRepoAsync(BusinessPerformancePlan planModel)
        {
            try
            {
                // Convert business performance sheets to JSON with proper formatting
                var businessPlanJson = JsonConvert.SerializeObject(planModel.businessPerformanceSheets, Formatting.None);

                // Debug: Log the JSON to see what's being sent
                Console.WriteLine($"JSON being sent: {businessPlanJson}");

                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@DealerEmpId", planModel.DealerEmpId, DbType.Int32);
                parameters.Add("@FYear", planModel.FYear, DbType.String);
                parameters.Add("@CreatedBy", planModel.CreatedBy, DbType.String);
                parameters.Add("@BusinessPlanData", businessPlanJson, DbType.String);

                var result = await connection.QuerySingleAsync<BusinessPlanResult>(
                    "sp_PERF_AddBusinesPlan",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );
                return result;
            }
            catch (SqlException sqlEx)
            {
                // Log SQL specific errors
                throw new ApplicationException($"Database error occurred: {sqlEx.Message}", sqlEx);
            }
            catch (JsonException jsonEx)
            {
                // Log JSON serialization errors
                throw new ApplicationException($"Data serialization error: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                // Log general errors
                throw;
            }
        }

        private PerformanceSheetModel MapToPerformanceSheetModel(
            PerformanceSheetDto sheet,
            PerformanceSheetCoverageDto coverage,
            List<FieldActivity> fieldActivities)
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

                // Field activities
                FieldActivities = fieldActivities ?? new List<FieldActivity>()
            };
        }

        public async Task<DealerModel> GetDealerDetailsRepoAsync(PerformanceSheetReqModel request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@DealerEmpId", request.DealerEmpId, DbType.Int32);
                parameters.Add("@Month", request.Month, DbType.Int32);
                parameters.Add("@FYear",request.FYear, DbType.String, size: 10);

                // Single result set with all data including aggregated business performance
                var dealerData = await connection.QueryFirstOrDefaultAsync<DealerModel>(
                    "sp_PERF_GetDealerDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);               

                return dealerData;
            }
            catch (SqlException ex)
            {
                throw new DataException("Database error occurred while retrieving dealer details.", ex);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<ActionPlanModel> GetActionPlanDetailsRepoAsync(ActionPlanDetailReqModel request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
            parameters.Add("@ActionPlanId", request.ActionPlanId);
            parameters.Add("@DealerEmpId", request.DealerEmpId);
            parameters.Add("@Month", request.Month);
            parameters.Add("@FYear", request.FYear);

            
                using var multi = await connection.QueryMultipleAsync(
                    "sp_PERF_ActionPlanDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                // Read first result set - Basic Details
                var basicResult = await multi.ReadSingleOrDefaultAsync<ActionPlanBasicResult>();

                // Read second result set - Action Plan Lists
                var actionPlanLists = (await multi.ReadAsync<ActionPlanList>()).ToList();

                // Create ActionPlanModel
                var actionPlanModel = new ActionPlanModel
                {
                    Id = basicResult?.Id ?? 0,
                    DealerEmpId = basicResult?.DealerEmpId ?? request.DealerEmpId,
                    MonthId = basicResult?.MonthId ?? request.Month,
                    FYear = basicResult?.FYear ?? request.FYear,
                    PerformanceSheetId = 0, // Not available in SP, set default or get from elsewhere
                    CreatedBy = "", // Not available in SP, set default or get from elsewhere
                    CreatedDate = "", // Not available in SP, set default or get from elsewhere
                    ActionPlanLists = actionPlanLists ?? new List<ActionPlanList>()
                };

                return actionPlanModel;
            }
            catch (Exception ex)
            {
                // Log exception here
                throw new Exception($"Error executing GetActionPlanDetails: {ex.Message}", ex);
            }
        }

        public async Task<ActionPlanResult> SubmitActionPlanRepoAsync(ActionPlanModel actionModel)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Convert ActionPlanLists to JSON
                    var actionPlanListJson = JsonConvert.SerializeObject(actionModel.ActionPlanLists);

                    var parameters = new DynamicParameters();
                    parameters.Add("@Id", actionModel.Id, DbType.Int32);
                    parameters.Add("@DealerEmpId", actionModel.DealerEmpId, DbType.Int32);
                    parameters.Add("@MonthId", actionModel.MonthId, DbType.Int32);
                    parameters.Add("@FYear", actionModel.FYear, DbType.String);
                    parameters.Add("@CreatedBy", actionModel.CreatedBy, DbType.String);
                    parameters.Add("@ActionPlanListJson", actionPlanListJson, DbType.String);

                    var result = await connection.QueryFirstOrDefaultAsync<ActionPlanResult>(
                        "sp_PERF_SubmitActionPlan",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 120
                    );

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> SubmitPerformanceSheetRepoAsync(PerformanceSheetModel model)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
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

                await connection.ExecuteAsync(
                    "sp_PERF_UpdatePerformanceSheet",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return parameters.Get<int>("@ResultId");
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
