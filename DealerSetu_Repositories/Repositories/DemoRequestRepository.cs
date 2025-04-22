using Dapper;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DealerSetu_Repositories.Repositories
{
    public class DemoRequestRepository : IDemoRequestRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<DemoRequestRepository> _logger;

        public DemoRequestRepository(IConfiguration configuration, ILogger<DemoRequestRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("dbDealerSetuEntities")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'dbDealerSetuEntities' not found");
            _logger = logger;
        }

        public async Task<(List<DemoTractorResponseModel> DemoTractorList, int TotalCount)> DemoTractorApprovedRepo(
            FilterModel filter, int pageIndex, int pageSize)
        {
            _logger.LogInformation("Fetching approved demo tractor list with filter parameters");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@EmpNo", filter.EmpNo);
                parameters.Add("@RoleId", filter.RoleId);
                parameters.Add("@RequestNo", filter.RequestNo);
                parameters.Add("@From", filter.From);
                parameters.Add("@To", filter.To);
                parameters.Add("@State", filter.State);
                parameters.Add("@Status", filter.Status);
                parameters.Add("@Fyear", filter.Fyear);
                parameters.Add("@Export", filter.Export);
                parameters.Add("@PageIndex", pageIndex);
                parameters.Add("@PageSize", pageSize);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_DEMOTRAC_ApprovedList",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var demoTractorList = (await multi.ReadAsync<DemoTractorResponseModel>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                return (demoTractorList, totalCount);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error in DemoTractorApprovedRepo: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoTractorApprovedRepo: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<(List<DemoTractorResponseModel> PendingDemoTractorList, int TotalCount)> DemoTractorPendingRepo(
            FilterModel filter, int pageIndex, int pageSize)
        {
            _logger.LogInformation("Fetching pending demo tractor list with filter parameters");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@EmpNo", filter.EmpNo);
                parameters.Add("@RoleId", filter.RoleId);
                parameters.Add("@RequestNo", filter.RequestNo);
                parameters.Add("@From", filter.From);
                parameters.Add("@To", filter.To);
                parameters.Add("@PageIndex", pageIndex);
                parameters.Add("@PageSize", pageSize);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_DEMOTRAC_PendingList",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var pendingDemoTractorList = (await multi.ReadAsync<DemoTractorResponseModel>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                return (pendingDemoTractorList, totalCount);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error in DemoTractorPendingRepo: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoTractorPendingRepo: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<FYearModel>> FiscalYearsRepo()
        {
            _logger.LogInformation("Fetching fiscal years");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var result = await connection.QueryAsync<FYearModel>(
                    "sp_DEMOTRAC_GetFiscalYears",
                    commandType: CommandType.StoredProcedure);

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FiscalYearsRepo: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<int> SubmitDemoReqRepo(DemoReqSubmissionModel request, string empNo)
        {
            _logger.LogInformation("Submitting demo request for employee {EmpNo}", empNo);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@RequestNo", request.RequestNo);
                parameters.Add("@DealerNo", request.DealerNo);
                parameters.Add("@ModelRequested", request.ModelRequested);
                parameters.Add("@Reason", request.Reason);
                parameters.Add("@SchemeType", request.SchemeType);
                parameters.Add("@SpecialVariant", request.SpecialVariant);
                parameters.Add("@ImplementRequired", request.ImplementRequired);
                parameters.Add("@ImplementId", request.ImplementId);
                parameters.Add("@Message", request.Message);
                parameters.Add("@HpCategory", request.HpCategoryId);
                parameters.Add("@EmpNo", empNo);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "sp_DEMOTRAC_SubmitDemoRequest",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var result = parameters.Get<int>("@Result");
                _logger.LogInformation("Demo request submission result: {Result}", result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitDemoReqRepo: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<DemoReqModel> DemoReqDataRepo(int reqId)
        {
            _logger.LogInformation("Fetching demo request data for ID {ReqId}", reqId);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@reqId", reqId);

                var result = await connection.QueryFirstOrDefaultAsync<DemoReqModel>(
                    "sp_DEMOTRAC_DemoReqData",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoReqDataRepo: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<int> DemoTractorApproveRejectRepo(FilterModel filter)
        {
            _logger.LogInformation("Processing approval/rejection for request ID {ReqId}", filter.ReqId);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@ReqId", filter.ReqId);
                parameters.Add("@IsApproved", filter.IsApproved);
                parameters.Add("@EmpNo", filter.EmpNo);
                parameters.Add("@RoleId", filter.RoleId);
                parameters.Add("@RejectRemarks", filter.RejectRemarks ?? string.Empty);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "sp_DEMOTRAC_ApproveRejectReq",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                _logger.LogInformation("Approval/rejection process result: {Result}", result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoTractorApproveRejectRepo: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<DemoReqModel>> DemoActualClaimListRepo(FilterModel filter)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ReqId", filter.ReqId, DbType.Int32);
                parameters.Add("@EmpNo", filter.EmpNo, DbType.String);
                parameters.Add("@RoleId", filter.RoleId, DbType.Int32);

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var result = await connection.QueryAsync<DemoReqModel>(
                        "sp_DEMOTRAC_DemoActualClaimList",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                // Log exception
                throw;
            }
        }

        public async Task<int> AddBasicDemoActualClaimRepo(DemoReqModel docModel)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RequestId", docModel.DemoRequestId);
                parameters.Add("@Model", docModel.Model);
                parameters.Add("@ChassisNo", docModel.ChassisNo);
                parameters.Add("@EngineNo", docModel.EngineNo);
                parameters.Add("@DateOfBilling", DateTime.ParseExact(docModel.DateOfBilling, "dd/MM/yyyy", CultureInfo.InvariantCulture));
                parameters.Add("@InvoiceFile", docModel.InvoiceFile);
                parameters.Add("@RCFile", docModel.RCFile);
                parameters.Add("@InsuranceFile", docModel.InsuranceFile);
                parameters.Add("@EmpNo", docModel.DealerNo);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    await connection.ExecuteAsync("sp_DEMOTRAC_UploadDemoTractorDocs", parameters, commandType: CommandType.StoredProcedure);
                }

                return parameters.Get<int>("@Result");
            }
            catch (Exception ex)
            {
                //Utility.ExcepLog(ex);
                throw;
            }
        }

        public async Task<int> AddAllDemoActualClaimRepo(DemoReqModel docModel)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RequestId", docModel.DemoRequestId);               
                parameters.Add("@FileSale", docModel.FileSale); //Sale Document of Tractor
                parameters.Add("@FileTractor", docModel.FileTractor); //Format for Claiming
                parameters.Add("@FilePicture", docModel.FilePicture); //Picture of Hour Reading
                parameters.Add("@FilePicTractor", docModel.FilePicTractor); //Picture of Tractor
                parameters.Add("@LogDemonsFile", docModel.LogDemons);
                parameters.Add("@AffidavitFile", docModel.Affidavit);
                parameters.Add("@SaleDeedFile", docModel.SaleDeed);
                parameters.Add("@EmpNo", docModel.DealerNo);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    await connection.ExecuteAsync("sp_DEMOTRAC_UploadDemoTractorDocs", parameters, commandType: CommandType.StoredProcedure);
                }

                return parameters.Get<int>("@Result");
            }
            catch (Exception ex)
            {
                //Utility.ExcepLog(ex);
                throw;
            }
        }

        public async Task<List<DemoReqModel>> GetDemoTractorDoc(FilterModel filter)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@reqId", filter.ReqId);

                    var result = await connection.QueryAsync<DemoReqModel>(
                        "sp_DEMOTRAC_GetDemoTractorDoc",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                //Utility.ExcepLog(ex);
                throw;
            }
        }


    }
}