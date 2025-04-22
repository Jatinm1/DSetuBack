using Dapper;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Repositories.Repositories
{
    public class HomeDashboardRepo : IHomeDashboardRepo
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public HomeDashboardRepo(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("dbDealerSetuEntities");
        }

        public async Task<HomeDashboard> GetUserDashboardDataAsync(string userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var user = await connection.QueryFirstOrDefaultAsync<HomeDashboard>(
                    "sp_HOME_GetUserDashboardData",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure
                );

                return user;
            }
        }

        public async Task<List<PendingCountModel>> PendingCountRepo(string empNo, string roleId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@EmpNo", empNo, DbType.String);
                    parameters.Add("@RoleId", roleId, DbType.String);

                    // Use QueryAsync to get all rows
                    var results = await connection.QueryAsync<PendingCountModel>(
                        "sp_HOME_GetAllPendingCounts",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                // Consider logging the exception here
                return new List<PendingCountModel>();
            }
        }


        public bool IsUserInAbharDealer(string userId)
        {
            var query = "SELECT COUNT(1) FROM tblAbharDealer WHERE UserId = @UserId";
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.ExecuteScalar<int>(query, new { UserId = userId }) > 0;
            }
        }

        public bool IsUserInArohanDealer(string userId)
        {
            var query = "SELECT COUNT(1) FROM tblArohanDealer WHERE UserId = @UserId";
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.ExecuteScalar<int>(query, new { UserId = userId }) > 0;
            }
        }


        //public async Task<DashboardCounts> GetDashboardCountsAsync(string userId)
        //{
        //    using (var connection = new SqlConnection(_connectionString))
        //    {
        //        await connection.OpenAsync();

        //        // Replace with your actual SQL queries to fetch counts
        //        var counts = new DashboardCounts
        //        {
        //            RequestCount = await connection.ExecuteScalarAsync<int>(
        //                "SELECT COUNT(*) FROM Requests WHERE Status = 'Pending'"),
        //            NewDealerCount = await connection.ExecuteScalarAsync<int>(
        //                "SELECT COUNT(*) FROM NewDealerActivities WHERE Status = 'Pending'"),
        //            DemoCount = await connection.ExecuteScalarAsync<int>(
        //                "SELECT COUNT(*) FROM DemoRequests WHERE Status = 'Pending'"),
        //            PerformanceCount = await connection.ExecuteScalarAsync<int>(
        //                "SELECT COUNT(*) FROM PerformanceSheets WHERE Status = 'Pending'")
        //        };

        //        return counts;
        //    }
        //}
    }
}
