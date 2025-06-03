using Dapper;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;

namespace DealerSetu_Repositories.Repositories
{
    /// <summary>
    /// Repository for managing new dealer activities including claims and approvals
    /// </summary>
    public class NewDealerActivityRepository : INewDealerActivityRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the NewDealerActivityRepository
        /// </summary>
        /// <param name="configuration">Configuration containing connection string</param>
        public NewDealerActivityRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("dbDealerSetuEntities")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string not found");
        }

        #region New Dealer Claim Methods

        /// <summary>
        /// Retrieves paginated list of dealer activities based on filter criteria
        /// </summary>
        /// <param name="filter">Filter criteria for dealer activities</param>
        /// <param name="pageIndex">Page index for pagination</param>
        /// <param name="pageSize">Page size for pagination</param>
        /// <returns>Tuple containing list of claims and total count</returns>
        public async Task<(List<ClaimModel> NewDealerActivityList, int TotalCount)> NewDealerActivityRepo(
            FilterModel filter, int pageIndex, int pageSize)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@EmpNo", filter.EmpNo);
            parameters.Add("@RoleId", filter.RoleId);
            parameters.Add("@From", filter.From);
            parameters.Add("@To", filter.To);
            parameters.Add("@ClaimNo", filter.ClaimNo);
            parameters.Add("@State", filter.State);
            parameters.Add("@Status", filter.Status);
            parameters.Add("@Export", filter.Export);
            parameters.Add("@PageIndex", pageIndex);
            parameters.Add("@PageSize", pageSize);

            using var multi = await connection.QueryMultipleAsync(
                "sp_NEWDEALER_GetClaimList", parameters, commandType: CommandType.StoredProcedure);

            var claimList = (await multi.ReadAsync<ClaimModel>()).ToList();
            var totalCount = await multi.ReadSingleOrDefaultAsync<int>();

            return (claimList, totalCount);
        }

        /// <summary>
        /// Retrieves list of dealer states
        /// </summary>
        /// <returns>List of dealer states</returns>
        public async Task<List<DealerStateModel>> DealerStatesRepo()
        {
            using var connection = new SqlConnection(_connectionString);
            var states = await connection.QueryAsync<DealerStateModel>(
                "sp_MASTER_GetDealerStates", commandType: CommandType.StoredProcedure);
            return states.ToList();
        }

        /// <summary>
        /// Retrieves paginated list of pending dealer claims
        /// </summary>
        /// <param name="filter">Filter criteria for pending claims</param>
        /// <param name="pageIndex">Page index for pagination</param>
        /// <param name="pageSize">Page size for pagination</param>
        /// <returns>Tuple containing list of pending claims and total count</returns>
        public async Task<(List<ClaimModel> NewDealerPendingList, int TotalCount)> NewDealerPendingListRepo(
            FilterModel filter, int pageIndex, int pageSize)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@EmpNo", filter.EmpNo);
            parameters.Add("@RoleId", filter.RoleId);
            parameters.Add("@ClaimNo", filter.ClaimNo);
            parameters.Add("@FromDate", filter.From);
            parameters.Add("@ToDate", filter.To);
            parameters.Add("@PageIndex", pageIndex);
            parameters.Add("@PageSize", pageSize);
            Console.WriteLine("Hello"+filter.From);
            using var multi = await connection.QueryMultipleAsync(
                "sp_NEWDEALER_GetPendingClaimList", parameters, commandType: CommandType.StoredProcedure);

            var pendingList = (await multi.ReadAsync<ClaimModel>()).ToList();
            var totalCount = await multi.ReadSingleOrDefaultAsync<int>();

            return (pendingList, totalCount);
        }

        /// <summary>
        /// Retrieves dealer data by request number
        /// </summary>
        /// <param name="requestNo">Request number to search for</param>
        /// <returns>Dealer user model</returns>
        public async Task<UserModel> DealerDataRepo(string requestNo)
        {
            ValidateStringParameter(requestNo, nameof(requestNo));

            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@RequestNo", requestNo);

            return await connection.QuerySingleOrDefaultAsync<UserModel>(
                "sp_NEWDEALER_GetDealerData", parameters, commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Submits a new claim with associated activities
        /// </summary>
        /// <param name="requestNo">Request number</param>
        /// <param name="dealerNo">Dealer number</param>
        /// <param name="activityData">List of activities for the claim</param>
        /// <param name="empNo">Employee number submitting the claim</param>
        /// <returns>Created claim ID</returns>
        public async Task<int> SubmitClaimRepo(string requestNo, string dealerNo,
            List<ActivityModel> activityData, string empNo)
        {
            ValidateStringParameter(requestNo, nameof(requestNo));
            ValidateStringParameter(dealerNo, nameof(dealerNo));
            ValidateStringParameter(empNo, nameof(empNo));
            ValidateListParameter(activityData, nameof(activityData));

            using var connection = new SqlConnection(_connectionString);
            var activitiesJson = JsonConvert.SerializeObject(activityData);

            var parameters = new DynamicParameters();
            parameters.Add("@RequestNo", requestNo);
            parameters.Add("@DealerNo", dealerNo);
            parameters.Add("@EmpNo", empNo);
            parameters.Add("@CreatedDate", DateTime.Now);
            parameters.Add("@ClaimActivities", activitiesJson);

            return await connection.QuerySingleAsync<int>(
                "sp_NEWDEALER_SubmitClaim", parameters, commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Updates an existing claim with new activity data
        /// </summary>
        /// <param name="claimId">Claim ID to update</param>
        /// <param name="activityData">Updated activity data</param>
        /// <param name="empNo">Employee number performing the update</param>
        /// <returns>Updated claim ID</returns>
        public async Task<int> UpdateClaimRepo(int claimId, List<ActivityModel> activityData, string empNo)
        {
            ValidateIntParameter(claimId, nameof(claimId));
            ValidateStringParameter(empNo, nameof(empNo));
            ValidateListParameter(activityData, nameof(activityData));

            using var connection = new SqlConnection(_connectionString);
            var activitiesJson = JsonConvert.SerializeObject(activityData);

            var parameters = new DynamicParameters();
            parameters.Add("@ClaimId", claimId);
            parameters.Add("@EmpNo", empNo);
            parameters.Add("@ActivityData", activitiesJson);

            return await connection.QuerySingleAsync<int>(
                "sp_NEWDEALER_UpdateClaim", parameters, commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Approves or rejects a claim
        /// </summary>
        /// <param name="filter">Filter containing approval/rejection details</param>
        /// <returns>Result of the approval/rejection operation</returns>
        public async Task<int> ApproveRejectClaimRepo(FilterModel filter)
        {
            ValidateObjectParameter(filter, nameof(filter));

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@ClaimId", filter.ClaimId);
            parameters.Add("@IsApproved", filter.IsApproved);
            parameters.Add("@EmpNo", filter.EmpNo);
            parameters.Add("@RoleId", filter.RoleId);
            parameters.Add("@RejectRemarks", filter.RejectRemarks ?? string.Empty);
            parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "sp_NEWDEALER_ApproveRejectClaim", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Result");
        }

        /// <summary>
        /// Retrieves detailed information about a specific claim including activities
        /// </summary>
        /// <param name="claimId">Claim ID to retrieve details for</param>
        /// <returns>Claim model with activity details</returns>
        public async Task<ClaimModel> ClaimDetailsRepo(int claimId)
        {
            ValidateIntParameter(claimId, nameof(claimId));

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var multi = await connection.QueryMultipleAsync(
                "sp_NEWDEALER_GetClaimDetails", new { ClaimId = claimId }, commandType: CommandType.StoredProcedure);

            var claimDetails = await multi.ReadFirstOrDefaultAsync<ClaimModel>();
            if (claimDetails != null)
            {
                claimDetails.ActivityDetails = (await multi.ReadAsync<ActivityModel>()).ToList();
            }

            return claimDetails;
        }

        #endregion

        #region Actual Claim Methods

        /// <summary>
        /// Retrieves details of an actual claim by activity ID
        /// </summary>
        /// <param name="activityId">Activity ID to retrieve actual claim details for</param>
        /// <returns>Actual claim model</returns>
        public async Task<ActualClaimModel> ActualClaimDetailsRepo(int activityId)
        {
            ValidateIntParameter(activityId, nameof(activityId));

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var multi = await connection.QueryMultipleAsync(
                "sp_NEWDEALER_GetActualClaimDetails", new { ActivityId = activityId }, commandType: CommandType.StoredProcedure);

            return await multi.ReadFirstOrDefaultAsync<ActualClaimModel>();
        }

        /// <summary>
        /// Adds or updates an actual claim
        /// </summary>
        /// <param name="actualClaim">Actual claim data to add/update</param>
        /// <returns>Result of the add/update operation</returns>
        public async Task<int> AddActualClaimRepo(ActualClaimModel actualClaim)
        {
            ValidateObjectParameter(actualClaim, nameof(actualClaim));

            using var connection = new SqlConnection(_connectionString);
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
                "sp_NEWDEALER_AddUpdateActualClaim", parameters, commandType: CommandType.StoredProcedure);
        }


        public async Task<int> UpdateActualClaimRepo(ActualClaimUpdateModel actualClaim)
        {
            ValidateObjectParameter(actualClaim, nameof(actualClaim));

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@ClaimId", actualClaim.ClaimId);
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
                "sp_NEWDEALER_UpdateActualClaim", parameters, commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Retrieves list of actual claims based on filter criteria
        /// </summary>
        /// <param name="filter">Filter criteria for actual claims</param>
        /// <returns>List of actual claim models</returns>
        public async Task<List<ActualClaimModel>> ActualClaimListRepo(FilterModel filter)
        {
            ValidateObjectParameter(filter, nameof(filter));

            var parameters = new DynamicParameters();
            parameters.Add("@ClaimId", filter.ClaimId, DbType.Int32);
            parameters.Add("@EmpNo", filter.EmpNo, DbType.String);
            parameters.Add("@RoleId", filter.RoleId, DbType.Int32);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var result = await connection.QueryAsync<ActualClaimModel>(
                "sp_NEWDEALER_GetActualClaimList", parameters, commandType: CommandType.StoredProcedure);

            return result.ToList();
        }

        /// <summary>
        /// Adds remarks to an actual claim
        /// </summary>
        /// <param name="claimId">Claim ID to add remarks to</param>
        /// <param name="remarks">Remarks to add</param>
        /// <returns>Activity ID</returns>
        public async Task<int> AddActualRemarkRepo(int claimId, string remarks)
        {
            ValidateIntParameter(claimId, nameof(claimId));
            ValidateStringParameter(remarks, nameof(remarks));

            var parameters = new DynamicParameters();
            parameters.Add("@ClaimId", claimId);
            parameters.Add("@Remarks", remarks);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            return await connection.QuerySingleAsync<int>(
                "sp_NEWDEALER_AddActualRemarks", parameters, commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Approves or rejects an actual claim
        /// </summary>
        /// <param name="filter">Filter containing approval/rejection details</param>
        /// <returns>Result of the approval/rejection operation</returns>
        public async Task<int> ApproveRejectActualClaimRepo(FilterModel filter)
        {
            ValidateObjectParameter(filter, nameof(filter));

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@ActivityId", filter.ActivityId);
            parameters.Add("@IsApproved", filter.IsApproved);
            parameters.Add("@EmpNo", filter.EmpNo);
            parameters.Add("@RoleId", filter.RoleId);
            parameters.Add("@RejectRemarks", filter.RejectRemarks ?? string.Empty);
            parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "sp_NEWDEALER_ApproveRejectActualClaim", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Result");
        }

        #endregion

        #region Private Validation Methods

        private static void ValidateStringParameter(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null or empty");
        }

        private static void ValidateIntParameter(int value, string parameterName)
        {
            if (value <= 0)
                throw new ArgumentException($"{parameterName} must be greater than zero", parameterName);
        }

        private static void ValidateObjectParameter(object value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null");
        }

        private static void ValidateListParameter<T>(List<T> value, string parameterName)
        {
            if (value == null || !value.Any())
                throw new ArgumentException($"{parameterName} cannot be null or empty", parameterName);
        }

        #endregion
    }
}