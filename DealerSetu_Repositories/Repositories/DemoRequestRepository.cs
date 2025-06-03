using Dapper;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DealerSetu_Repositories.Repositories
{
    /// <summary>
    /// Repository for handling demo tractor request operations including CRUD operations,
    /// approval workflows, and document management
    /// </summary>
    public class DemoRequestRepository : IDemoRequestRepository
    {
        private readonly string _connectionString;

        public DemoRequestRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("dbDealerSetuEntities")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'dbDealerSetuEntities' not found");
        }

        /// <summary>
        /// Retrieves approved demo tractor requests with pagination and filtering
        /// </summary>
        public async Task<(List<DemoTractorResponseModel> DemoTractorList, int TotalCount)> DemoTractorApprovedRepo(
            FilterModel filter, int pageIndex, int pageSize)
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

        /// <summary>
        /// Retrieves pending demo tractor requests with pagination and filtering
        /// </summary>
        public async Task<(List<DemoTractorResponseModel> PendingDemoTractorList, int TotalCount)> DemoTractorPendingRepo(
            FilterModel filter, int pageIndex, int pageSize)
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

        /// <summary>
        /// Retrieves demo tractor requests with pending claim uploads
        /// </summary>
        public async Task<(List<DemoTractorResponseModel> PendingClaimUploadList, int TotalCount)> DemoTractorPendingClaimRepo(
            FilterModel filter, int pageIndex, int pageSize)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@EmpNo", filter.EmpNo);
            parameters.Add("@RoleId", filter.RoleId);
            parameters.Add("@RequestNo", filter.RequestNo);
            parameters.Add("@FromDate", filter.From);
            parameters.Add("@ToDate", filter.To);
            parameters.Add("@PageIndex", pageIndex);
            parameters.Add("@PageSize", pageSize);

            using var multi = await connection.QueryMultipleAsync(
                "sp_DEMOTRAC_PendingClaimList",
                parameters,
                commandType: CommandType.StoredProcedure);

            var pendingClaimUploadList = (await multi.ReadAsync<DemoTractorResponseModel>()).ToList();
            var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

            return (pendingClaimUploadList, totalCount);
        }

        /// <summary>
        /// Retrieves all available fiscal years
        /// </summary>
        public async Task<List<FYearModel>> FiscalYearsRepo()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var result = await connection.QueryAsync<FYearModel>(
                "sp_DEMOTRAC_GetFiscalYears",
                commandType: CommandType.StoredProcedure);

            return result.ToList();
        }

        /// <summary>
        /// Submits a new demo request
        /// </summary>
        public async Task<int> SubmitDemoReqRepo(DemoReqSubmissionModel request, string empNo)
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

            return parameters.Get<int>("@Result");
        }

        /// <summary>
        /// Retrieves demo request data by request ID
        /// </summary>
        public async Task<DemoReqModel> DemoReqDataRepo(int reqId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@reqId", reqId);

            return await connection.QueryFirstOrDefaultAsync<DemoReqModel>(
                "sp_DEMOTRAC_DemoReqData",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Approves or rejects a demo tractor request
        /// </summary>
        public async Task<int> DemoTractorApproveRejectRepo(FilterModel filter)
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
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Result");
        }

        /// <summary>
        /// Updates an existing demo request
        /// </summary>
        public async Task<string> UpdateDemoReqRepo(DemoReqUpdateModel request, string empNo)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@RequestNo", request.RequestNo);
            parameters.Add("@EmpNo", empNo);
            parameters.Add("@ModelRequested", request.ModelRequested);
            parameters.Add("@Reason", request.Reason);
            parameters.Add("@SchemeType", request.SchemeType);
            parameters.Add("@SpecialVariant", request.SpecialVariant);
            parameters.Add("@ImplementRequired", request.ImplementRequired);
            parameters.Add("@ImplementId", request.ImplementId);
            parameters.Add("@Message", request.Message);
            parameters.Add("@HpCategoryId", request.HpCategoryId);

            return await connection.QuerySingleAsync<string>(
                "sp_DEMOTRAC_UpdateReq",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Retrieves actual claim list for demo requests
        /// </summary>
        public async Task<List<DemoReqModel>> DemoActualClaimListRepo(FilterModel filter)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@ReqId", filter.ReqId, DbType.Int32);
            parameters.Add("@EmpNo", filter.EmpNo, DbType.String);
            parameters.Add("@RoleId", filter.RoleId, DbType.Int32);

            var result = await connection.QueryAsync<DemoReqModel>(
                "sp_DEMOTRAC_DemoActualClaimList",
                parameters,
                commandType: CommandType.StoredProcedure);

            return result.ToList();
        }

        /// <summary>
        /// Adds basic demo actual claim with required documents
        /// </summary>
        public async Task<int> AddBasicDemoActualClaimRepo(DemoReqModel docModel)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@RequestId", docModel.DemoRequestId);
            parameters.Add("@Model", docModel.Model);
            parameters.Add("@ChassisNo", docModel.ChassisNo);
            parameters.Add("@EngineNo", docModel.EngineNo);
            parameters.Add("@DateOfBilling", DateTime.ParseExact(docModel.DateOfBilling, "dd/MM/yyyy", CultureInfo.InvariantCulture));
            parameters.Add("@InvoiceFile", docModel.InvoiceFile);
            parameters.Add("@RCFile", docModel.RCFile);
            parameters.Add("@InsuranceFile", docModel.InsuranceFile);
            parameters.Add("@EmpNo", docModel.EmpNo);
            parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("sp_DEMOTRAC_UploadDemoTractorDocs", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Result");
        }

        /// <summary>
        /// Adds all demo actual claim documents
        /// </summary>
        public async Task<int> AddAllDemoActualClaimRepo(DemoReqModel docModel)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@RequestId", docModel.DemoRequestId);
            parameters.Add("@FileSale", docModel.FileSale);
            parameters.Add("@FileTractor", docModel.FileTractor);
            parameters.Add("@FilePicture", docModel.FilePicture);
            parameters.Add("@FilePicTractor", docModel.FilePicTractor);
            parameters.Add("@LogDemonsFile", docModel.LogDemons);
            parameters.Add("@AffidavitFile", docModel.Affidavit);
            parameters.Add("@SaleDeedFile", docModel.SaleDeed);
            parameters.Add("@EmpNo", docModel.EmpNo);
            parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("sp_DEMOTRAC_UploadDemoTractorDocs", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Result");
        }

        /// <summary>
        /// Retrieves demo tractor documents by request ID
        /// </summary>
        public async Task<List<DemoReqModel>> GetDemoTractorDoc(FilterModel filter)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@reqId", filter.ReqId);

            var result = await connection.QueryAsync<DemoReqModel>(
                "sp_DEMOTRAC_GetDemoTractorDoc",
                parameters,
                commandType: CommandType.StoredProcedure);

            return result.ToList();
        }

        /// <summary>
        /// Approves or rejects a demo tractor claim
        /// </summary>
        public async Task<int> DemoTractorApproveRejectClaimRepo(FilterModel filter)
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
                "sp_DEMOTRAC_ApproveRejectClaim",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Result");
        }

        /// <summary>
        /// Adds remarks to a demo tractor request
        /// </summary>
        public async Task<int> AddActualDemoRemarkRepo(AddDemoTracRemarksModel request)
        {
            if (!DateTime.TryParseExact(request.RemarksDate, "MM/dd/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                throw new ArgumentException("Invalid date format. Expected MM/dd/yyyy format.");
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Id", request.RequestId);
            parameters.Add("@Remarks", request.Remarks);
            parameters.Add("@RemarksDate", parsedDate);

            return await connection.QuerySingleAsync<int>(
                "sp_DEMOTRAC_AddRemarks",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}