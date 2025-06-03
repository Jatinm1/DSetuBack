using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DealerSetu_Data.Models.ViewModels;
using dealersetu_repositories.irepositories;
using DealerSetu_Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DealerSetu_Repositories.Repositories
{
    /// <summary>
    /// Repository for managing white village data operations and file handling
    /// </summary>
    public class WhiteVillageRepository : IWhiteVillageRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the WhiteVillageRepository
        /// </summary>
        /// <param name="configuration">Configuration containing connection string</param>
        public WhiteVillageRepository(IConfiguration configuration)
        {
            _connectionString = configuration?.GetConnectionString("dbDealerSetuEntities")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'dbDealerSetuEntities' not found");
        }

        /// <summary>
        /// Retrieves the white listing data from the database
        /// </summary>
        /// <returns>Collection of white village models</returns>
        public async Task<IEnumerable<WhiteVillageModel>> GetWhiteListingRepo()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var result = await connection.QueryAsync<WhiteVillageModel>(
                    "sp_WHITEVILLAGE_WhiteVillageList",
                    commandType: CommandType.StoredProcedure);

                return result ?? Enumerable.Empty<WhiteVillageModel>();
            }
            catch (SqlException)
            {
                throw new InvalidOperationException("Database connection failed while retrieving white village list");
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Failed to retrieve white village list");
            }
        }

        /// <summary>
        /// Retrieves the list of states from the database
        /// </summary>
        /// <returns>List of state models</returns>
        public async Task<List<StateModel>> GetStateListRepo()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var result = await connection.QueryAsync<StateModel>(
                    "sp_WHITEVILLAGE_GetStateList",
                    commandType: CommandType.StoredProcedure);

                return result?.ToList() ?? new List<StateModel>();
            }
            catch (SqlException)
            {
                throw new InvalidOperationException("Database connection failed while retrieving state list");
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Failed to retrieve state list");
            }
        }

        /// <summary>
        /// Saves metadata for the uploaded White Village file in the database
        /// </summary>
        /// <param name="filename">Name of the uploaded file</param>
        /// <param name="stateId">State identifier</param>
        /// <param name="createdBy">User who created the record</param>
        /// <param name="fiscalYear">Fiscal year for the data</param>
        /// <returns>Result code from the stored procedure</returns>
        public async Task<string> SaveWhiteVillageFileMetadata(string filename, string stateId, string createdBy, string fiscalYear)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException("Filename cannot be null or empty", nameof(filename));

            if (string.IsNullOrWhiteSpace(stateId))
                throw new ArgumentException("State ID cannot be null or empty", nameof(stateId));

            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("Created by cannot be null or empty", nameof(createdBy));

            if (string.IsNullOrWhiteSpace(fiscalYear))
                throw new ArgumentException("Fiscal year cannot be null or empty", nameof(fiscalYear));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@StateId", stateId);
                parameters.Add("@Filename", filename);
                parameters.Add("@CreatedBy", createdBy);
                parameters.Add("@CreatedDate", DateTime.Now);
                parameters.Add("@FYear", fiscalYear);
                parameters.Add("@Result", dbType: DbType.String, direction: ParameterDirection.Output, size: 10);

                await connection.ExecuteAsync(
                    "sp_WHITEVILLAGE_SaveExcelFileMetadata",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return parameters.Get<string>("@Result") ?? "Failed";
            }
            catch (SqlException)
            {
                throw new InvalidOperationException("Database error occurred while saving file metadata");
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Failed to save file metadata");
            }
        }

        /// <summary>
        /// Retrieves the Blob URL for downloading a White Village file
        /// </summary>
        /// <param name="fileName">Name of the file to download</param>
        /// <returns>Blob URL for file download or null if not found</returns>
        public async Task<string> WhiteVillageDownloadRepo(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Filename cannot be null or empty", nameof(fileName));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var result = await connection.QueryFirstOrDefaultAsync<string>(
                    "sp_WHITEVILLAGE_FileDownload",
                    new { FileName = fileName },
                    commandType: CommandType.StoredProcedure);

                return result;
            }
            catch (SqlException)
            {
                throw new InvalidOperationException("Database connection failed while retrieving download URL");
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Failed to retrieve file download URL");
            }
        }
    }
}