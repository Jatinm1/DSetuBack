using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DealerSetu_Data.Models.DTOs;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

        public async Task<IEnumerable<DealerModel>> GetTrackingDealersAsync(string? keyword, int month, string fYear, string empNo)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@Keyword", keyword, DbType.String);
                parameters.Add("@Month", month, DbType.Int32); // Fixed here
                parameters.Add("@FYear", fYear, DbType.String);
                parameters.Add("@EmpNo", empNo, DbType.String);


                var dealers = await connection.QueryAsync<DealerModel>(
                    "sp_PERF_GetTrackingDealers",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );

                return dealers ?? Enumerable.Empty<DealerModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tracking dealers for empNo: {EmpNo}, month: {Month}, year: {FYear}",
                    empNo, month, fYear);
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
                fieldActivities = fieldActivities ?? new List<FieldActivity>()
            };
        }
    }
}
