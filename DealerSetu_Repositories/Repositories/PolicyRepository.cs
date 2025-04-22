using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using dealersetu_repositories.irepositories;
using Microsoft.EntityFrameworkCore;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Repositories.Repositories
{
    public class PolicyRepository : IPolicyRepository
    {
        private IConfiguration _configuration;
        private readonly string _connectionString;

        public PolicyRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("dbDealerSetuEntities");

        }
        public dynamic GetPolicyListRepo()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    return connection.Query<dynamic>("sp_POLICY_List", commandType: CommandType.StoredProcedure).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public string SendFilesToServerRepo(PolicyUploadModel model, int RAId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@recomendationFileName", model.UpdatedName); // Now this is the Blob URL
                parameters.Add("@RAId", RAId);
                parameters.Add("@ContentType", model.RecomendationFileName.ContentType);

                var result = connection.Execute(
                    "sp_UPLOAD_NewFileToBLOB",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result > 0 ? "200" : "File upload failed";
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

