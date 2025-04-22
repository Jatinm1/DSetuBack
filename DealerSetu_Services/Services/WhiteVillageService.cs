using DealerSetu_Services.IServices;
using Microsoft.Extensions.Logging;
using DealerSetu_Repositories.IRepositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using DealerSetu.Repository.Common;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Services.Services
{
    public class WhiteVillageService : IWhiteVillageService
    {
        private readonly IWhiteVillageRepository _whiteVillageRepository;
        private readonly IFileValidationService _fileValidationService;
        private readonly ILogger<WhiteVillageService> _logger;
        private readonly IConfiguration _configuration;
        private readonly Utility _utility;

        public WhiteVillageService(
            IWhiteVillageRepository whiteVillageRepository,
            ILogger<WhiteVillageService> logger,
            IConfiguration configuration,
            IFileValidationService fileValidationService,
            Utility utility)
        {
            _whiteVillageRepository = whiteVillageRepository;
            _logger = logger;
            _configuration = configuration;
            _fileValidationService = fileValidationService;
            _utility = utility;
        }

        public async Task<IEnumerable<WhiteVillageModel>> GetWhiteListingService()
        {
            try
            {
                return await _whiteVillageRepository.GetWhiteListingRepo();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving white listing data");
                throw;
            }
        }

        public async Task<ServiceResponse> GetStateListService()
        {
            try
            {
                var states = await _whiteVillageRepository.GetStateListRepo();
                return new ServiceResponse
                {
                    isError = false,
                    result = states,
                    Message = "States retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error retrieving states",
                    Status = "Error",
                    Code = "500"
                };
            }
        }

        public async Task<ServiceResponse> UploadWhiteVillageFileService(IFormFile whiteVillageFile, string stateId, string empNo)
        {
            var response = new ServiceResponse();
            if (whiteVillageFile == null) return response;

            try
            {
                long maxFileSizeInBytes = 10 * 1024 * 1024; // 10MB limit
                if (whiteVillageFile.Length > maxFileSizeInBytes)
                {
                    return new ServiceResponse
                    {
                        Status = "Failure",
                        Code = "400",
                        Error = "File size exceeds the maximum allowed size."
                    };
                }

                string validExtensions = ".xlsx|.xls|.csv";
                string fileExtension = Path.GetExtension(whiteVillageFile.FileName).ToLower();
                var type = MagicNumberClass.MagicNumber(whiteVillageFile);
                var validationResult = await _fileValidationService.ValidateFile(whiteVillageFile);

                if ((bool)validationResult.isError)
                {
                    return new ServiceResponse
                    {
                        isError = true,
                        Status = "Failure",
                        Message = validationResult.Message
                    };
                }

                if (!string.IsNullOrEmpty(type) && validExtensions.Contains(fileExtension))
                {
                    var fileName = whiteVillageFile.FileName;

                    if (fileName.Count(c => c == '.') > 1)
                    {
                        return new ServiceResponse
                        {
                            isError = true,
                            Status = "Failure",
                            Code = "400",
                            Message = "File name can't contain multiple periods (.) except for the extension."
                        };
                    }

                    var blobUrl = await AddDocs(whiteVillageFile, fileName);
                    string createdBy = empNo;
                    string fiscalYear = DetermineFiscalYear();
                    var result = await _whiteVillageRepository.SaveWhiteVillageFileMetadata(fileName, stateId, createdBy, fiscalYear);

                    return new ServiceResponse
                    {
                        isError = result != "200",
                        Status = result == "200" ? "Success" : "Failure",
                        Code = result == "200" ? "200" : "500",
                        Message = result == "200" ? "File uploaded successfully" : "Failed to save file metadata."
                    };
                }
                else
                {
                    return new ServiceResponse
                    {
                        isError = true,
                        Status = "Failure",
                        Code = "400",
                        Message = "Invalid file type. Only Excel files (.xlsx, .xls, .csv) are allowed."
                    };
                }
            }
            catch (Exception ex)
            {
                response.isError = true;
                response.Error = ex.InnerException?.ToString() ?? ex.Message;
            }
            return response;
        }

        private string DetermineFiscalYear()
        {
            DateTime now = DateTime.Now;
            int year = now.Year;
            return now.Month >= 4 ? $"FY{(year + 1) % 100}" : $"FY{year % 100}";
        }

        public async Task<string> AddDocs(IFormFile file, string fileName)
        {
            var storageAccount = CloudStorageAccount.Parse(_configuration["AzureBlobStorage:StorageAccount"]);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(_configuration["AzureBlobStorage:ContainerName"]);
            var blockBlob = container.GetBlockBlobReference(fileName);

            await using (var data = file.OpenReadStream())
            {
                await blockBlob.UploadFromStreamAsync(data);
            }
            return blockBlob.Uri.ToString();
        }

        public async Task<string> WhiteVillageDownloadService(string fileName)
        {
            var blobUrlFromRepo = await _whiteVillageRepository.WhiteVillageDownloadRepo(fileName);
            var connectionString = _configuration["AzureBlobStorage:StorageAccount"];

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(_configuration["AzureBlobStorage:ContainerName"]);
            var blobUrl = $"{container.StorageUri.PrimaryUri}/{blobUrlFromRepo}";

            if (string.IsNullOrEmpty(blobUrl))
                throw new FileNotFoundException("File not found in the storage.");

            var blob = new CloudBlockBlob(new Uri(blobUrl), blobClient.Credentials);
            if (!await blob.ExistsAsync())
                throw new FileNotFoundException("The blob does not exist in the storage.");

            var sasToken = _utility.GenerateSasToken(blob);
            return $"{blob.Uri}?{sasToken}";
        }

        public async Task<List<string>> ListAllBlobsInContainer()
        {
            var connectionString = _configuration["AzureBlobStorage:StorageAccount"];
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(_configuration["AzureBlobStorage:ContainerName"]);

            // Check if container exists
            if (!await container.ExistsAsync())
                throw new InvalidOperationException($"Container '{_configuration["AzureBlobStorage:ContainerName"]}' does not exist.");

            var blobList = new List<string>();
            BlobContinuationToken continuationToken = null;

            do
            {
                var results = await container.ListBlobsSegmentedAsync(prefix: "", useFlatBlobListing: true,
                    blobListingDetails: BlobListingDetails.None, maxResults: 100,
                    currentToken: continuationToken, options: null, operationContext: null);

                continuationToken = results.ContinuationToken;

                foreach (var blob in results.Results)
                {
                    // Extract the blob name from the URI
                    var blobName = blob.Uri.ToString().Substring(container.Uri.ToString().Length + 1);
                    blobList.Add(blobName);
                }
            }
            while (continuationToken != null);

            return blobList;
        }


        public async Task<bool> DeleteBlobFile(string fileName)
        {
            try
            {
                var connectionString = _configuration["AzureBlobStorage:StorageAccount"];
                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");

                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(_configuration["AzureBlobStorage:ContainerName"]);

                // Check if container exists
                if (!await container.ExistsAsync())
                    throw new InvalidOperationException($"Container '{_configuration["AzureBlobStorage:ContainerName"]}' does not exist.");

                // Get blob reference
                var blob = container.GetBlockBlobReference(fileName);

                // Check if blob exists
                if (!await blob.ExistsAsync())
                    return false; // File doesn't exist

                // Delete the blob
                await blob.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log exception
                throw new Exception($"Failed to delete file: {ex.Message}", ex);
            }
        }


    }
}
