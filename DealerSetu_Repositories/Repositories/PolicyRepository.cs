using System;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using dealersetu_repositories.irepositories;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Repositories.Repositories
{
    /// <summary>
    /// Repository for managing policy-related operations and file uploads
    /// </summary>
    public class PolicyRepository : IPolicyRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the PolicyRepository
        /// </summary>
        /// <param name="configuration">Configuration containing connection string</param>
        public PolicyRepository(IConfiguration configuration)
        {
            _connectionString = configuration?.GetConnectionString("dbDealerSetuEntities")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'dbDealerSetuEntities' not found");
        }

        /// <summary>
        /// Retrieves the list of policies from the database
        /// </summary>
        /// <returns>Dynamic object containing policy data or null if no data found</returns>
        public dynamic GetPolicyListRepo()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var result = connection.Query<dynamic>(
                    "sp_POLICY_List",
                    commandType: CommandType.StoredProcedure)
                    .FirstOrDefault();

                return result;
            }
            catch (SqlException)
            {
                throw new InvalidOperationException("Database connection failed while retrieving policy list");
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Failed to retrieve policy list");
            }
        }

        /// <summary>
        /// Uploads policy files to server and updates database with file information
        /// </summary>
        /// <param name="model">Policy upload model containing file details</param>
        /// <param name="raId">Recommendation Action ID</param>
        /// <returns>Success code "200" or error message</returns>
        public string SendFilesToServerRepo(PolicyUploadModel model, int raId)
        {
            if (model == null)
                return "Invalid upload model";

            if (raId <= 0)
                return "Invalid RA ID";

            if (string.IsNullOrWhiteSpace(model.UpdatedName))
                return "File name is required";

            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@recomendationFileName", model.UpdatedName);
                parameters.Add("@RAId", raId);
                parameters.Add("@ContentType", model.RecomendationFileName?.ContentType ?? "application/octet-stream");

                var result = connection.Execute(
                    "sp_UPLOAD_NewFileToBLOB",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return result > 0 ? "200" : "File upload failed";
            }
            catch (SqlException)
            {
                return "Database error during file upload";
            }
            catch (Exception)
            {
                return "File upload operation failed";
            }
        }
    }
}



//*************************************Methods for FUTURE USE*************************************

//public dynamic GetWhiteListingRepo()
//{
//    try
//    {
//        using (var connection = new SqlConnection(_connectionString))
//        {
//            return connection.Query<dynamic>("sp_GetWhiteListing", commandType: CommandType.StoredProcedure).FirstOrDefault();

//        }
//    }
//    catch (Exception ex)
//    {
//        //Utility.ExcepLog(ex); // Log exception
//        //throw; // Propagate exception

//        return new ServiceResponse
//        {
//            Message = "An error occurred",
//            Status = "Failure",
//            Code = "500",
//            isError = true,
//        };
//    }
//}


//*************************************Add this Method above to Upload New Polices*************************************

