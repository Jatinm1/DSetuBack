using Dapper;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DealerSetu_Repositories.Repositories
{
    public class NewDealerActivityRepository : INewDealerActivityRepository
    {
        private readonly string _connectionString;

        public NewDealerActivityRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("dbDealerSetuEntities");
        }

        //****************************************NEW DEALER CLAIM API Repository Methods**************************************

        public async Task<(List<ClaimModel> NewDealerActivityList, int TotalCount)> NewDealerActivityRepo(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(_connectionString))
                {
                    await sqlConnection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@EmpNo", filter.EmpNo);
                    parameters.Add("@RoleId", filter.RoleId);
                    parameters.Add("@From", filter.From);
                    parameters.Add("@To", filter.To);
                    parameters.Add("@@ClaimNo", filter.ClaimNo);
                    parameters.Add("@State", filter.State);
                    parameters.Add("@Status", filter.Status);
                    parameters.Add("@Export", filter.Export);
                    parameters.Add("@PageIndex", pageIndex);
                    parameters.Add("@PageSize", pageSize);

                    using (var multi = sqlConnection.QueryMultiple("sp_NEWDEALER_GetClaimList", parameters, commandType: CommandType.StoredProcedure))
                    {
                        // First result set: demo requests data
                        var NewDealerActivityList = multi.Read<ClaimModel>().ToList();

                        // Second result set: total count
                        var totalCount = multi.Read<int>().FirstOrDefault();

                        return (NewDealerActivityList, totalCount);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<DealerStateModel>> DealerStatesRepo()
        {
            using var connection = new SqlConnection(_connectionString);
            try
            {
                return connection.Query<DealerStateModel>("sp_MASTER_GetDealerStates", commandType: CommandType.StoredProcedure).ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<(List<ClaimModel> NewDealerPendingList, int TotalCount)> NewDealerPendingListRepo(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(_connectionString))
                {
                    await sqlConnection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@EmpNo", filter.EmpNo);
                    parameters.Add("@RoleId", filter.RoleId);
                    parameters.Add("@@ClaimNo", filter.ClaimNo);
                    parameters.Add("@FromDate", filter.From);
                    parameters.Add("@ToDate", filter.To);
                    parameters.Add("@PageIndex", pageIndex);
                    parameters.Add("@PageSize", pageSize);

                    using (var multi = sqlConnection.QueryMultiple("sp_NEWDEALER_GetPendingClaimList", parameters, commandType: CommandType.StoredProcedure))
                    {
                        // First result set: demo requests data


                        var NewDealerPendingList = multi.Read<ClaimModel>().ToList();

                        // Second result set: total count
                        var totalCount = multi.Read<int>().FirstOrDefault();

                        return (NewDealerPendingList, totalCount);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<UserModel> DealerDataRepo(string requestNo)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@RequestNo", requestNo);

                    var DealerData = await connection.QuerySingleAsync<UserModel>(
                        "sp_NEWDEALER_GetDealerData",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return DealerData;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> SubmitClaimRepo(string requestNo, string dealerNo, List<ActivityModel> activityData, string empNo)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {

                    // Convert activities to JSON for passing to stored procedure
                    var activitiesJson = JsonConvert.SerializeObject(activityData);

                    var parameters = new DynamicParameters();
                    parameters.Add("@RequestNo", requestNo);
                    parameters.Add("@DealerNo", dealerNo);
                    parameters.Add("@EmpNo", empNo);
                    parameters.Add("@CreatedDate", DateTime.Now);
                    parameters.Add("@ClaimActivities", activitiesJson);

                    int claimId = await connection.QuerySingleAsync<int>(
                        "sp_NEWDEALER_SubmitClaim",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return claimId;
                }
            }
            catch (Exception ex)
            {
                // Log exception
                throw;
            }
        }

        public async Task<int> UpdateClaimRepo(int claimId, List<ActivityModel> activityData, string empNo)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {

                    // Convert activities to JSON for passing to stored procedure
                    var activitiesJson = JsonConvert.SerializeObject(activityData);

                    var parameters = new DynamicParameters();
                    parameters.Add("@ClaimId", claimId);
                    parameters.Add("@EmpNo", empNo);
                    parameters.Add("@ActivityData", activitiesJson);

                    int claimIdS = await connection.QuerySingleAsync<int>(
                        "sp_NEWDEALER_UpdateClaim",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return claimIdS;
                }
            }
            catch (Exception ex)
            {
                // Log exception
                throw;
            }
        }       

        public async Task<int> ApproveRejectClaimRepo(FilterModel filter)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@ClaimId", filter.ClaimId);
                    parameters.Add("@IsApproved", filter.IsApproved);
                    parameters.Add("@EmpNo", filter.EmpNo);
                    parameters.Add("@RoleId", filter.RoleId);
                    parameters.Add("@RejectRemarks", filter.RejectRemarks ?? string.Empty);
                    parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await connection.ExecuteAsync(
                        "sp_NEWDEALER_ApproveRejectClaim",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return parameters.Get<int>("@Result");
                }
            }
            catch (SqlException ex)
            {                
                throw; // Rethrow to allow calling method to handle
            }
            catch (Exception ex)
            {                
                throw; // Rethrow to allow calling method to handle
            }
        }
        
        public async Task<ClaimModel> ClaimDetailsRepo(int claimId)
        {
            try
            {

                using (var connection = new SqlConnection(_connectionString))
                {
                   await connection.OpenAsync();

                    // Use multi-mapping to handle multiple result sets
                    using (var multi = await connection.QueryMultipleAsync("sp_NEWDEALER_GetClaimDetails",
                        new { ClaimId = claimId },
                        commandType: CommandType.StoredProcedure))
                    {
                        // First result set is the claim details
                        var claimDetails = await multi.ReadFirstOrDefaultAsync<ClaimModel>();

                        if (claimDetails != null)
                        {
                            // Second result set is the activities
                            claimDetails.ActivityDetails = (await multi.ReadAsync<ActivityModel>()).ToList();
                        }

                        return claimDetails;
                    }
                }
            }
            catch (Exception ex)
            {
                // Use a proper logging mechanism
                //Utility.ExcepLog(ex);
                throw;
            }
        }

        //***********************************NEW DEALER ACTUAL CLAIM API Repository Methods*************************************

        public async Task<ActualClaimModel> ActualClaimDetailsRepo(int activityId)
        {
            try
            {

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Use multi-mapping to handle multiple result sets
                    using (var multi = await connection.QueryMultipleAsync("sp_NEWDEALER_GetActualClaimDetails",
                        new { ActivityId = activityId },
                        commandType: CommandType.StoredProcedure))
                    {
                        // First result set is the claim details
                        var actualclaimDetails = await multi.ReadFirstOrDefaultAsync<ActualClaimModel>();


                        return actualclaimDetails;
                    }
                }
            }
            catch (Exception ex)
            {
                // Use a proper logging mechanism
                //Utility.ExcepLog(ex);
                throw;
            }
        }

        public async Task<int> AddActualClaimRepo(ActualClaimModel actualClaim)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@ActivityId", actualClaim.ActivityId);
                parameters.Add("@EmpNo", actualClaim.EmpNo);
                parameters.Add("@ActualExpenses", actualClaim.ActualExpenses);
                parameters.Add("@DateOfActivity", actualClaim.DateOfActivity);
                parameters.Add("@CustomerContacted", actualClaim.CustomerContacted);
                parameters.Add("@Enquiry", actualClaim.Enquiry);
                parameters.Add("@Delivery", actualClaim.Delivery);
                parameters.Add("@Image1", actualClaim.Image1);
                parameters.Add("@Image2", actualClaim.Image2);
                parameters.Add("@Image3", actualClaim.Image3);
                parameters.Add("@ActualClaimOn", actualClaim.ActualClaimOn);

                return await connection.ExecuteScalarAsync<int>(
                    "sp_NEWDEALER_AddUpdateActualClaim",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
        }        

        public async Task<List<ActualClaimModel>> ActualClaimListRepo(FilterModel filter)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ClaimId", filter.ClaimId, DbType.Int32);
                parameters.Add("@EmpNo", filter.EmpNo, DbType.String);
                parameters.Add("@RoleId", filter.RoleId, DbType.Int32);

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var result = await connection.QueryAsync<ActualClaimModel>(
                        "sp_NEWDEALER_GetActualClaimList",
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

        public async Task<int> AddActualRemarkRepo(int claimId, string remarks)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ClaimId", claimId);
                parameters.Add("@Remarks", remarks);

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    int activityId = await connection.QuerySingleAsync<int>(
                        "sp_NEWDEALER_AddActualRemarks",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return activityId;
                }
            }
            catch (Exception ex)
            {
                // Log exception
                throw;
            }
        }

        public async Task<int> ApproveRejectActualClaimRepo(FilterModel filter)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@ActivityId", filter.ActivityId);
                    parameters.Add("@IsApproved", filter.IsApproved);
                    parameters.Add("@EmpNo", filter.EmpNo);
                    parameters.Add("@RoleId", filter.RoleId);
                    parameters.Add("@RejectRemarks", filter.RejectRemarks ?? string.Empty);
                    parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await connection.ExecuteAsync(
                        "sp_NEWDEALER_ApproveRejectActualClaim",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return parameters.Get<int>("@Result");
                }
            }
            catch (SqlException ex)
            {
                throw; // Rethrow to allow calling method to handle
            }
            catch (Exception ex)
            {
                throw; // Rethrow to allow calling method to handle
            }
        }

    }
}
