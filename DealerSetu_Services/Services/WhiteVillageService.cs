using DealerSetu_Services.IServices;
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
    /// <summary>
    /// Service responsible for managing white village operations including file uploads, downloads, and state management.
    /// Handles Azure Blob Storage operations and integrates with repository layer for data persistence.
    /// </summary>
    public class WhiteVillageService : IWhiteVillageService
    {
        #region Private Fields

        private readonly IWhiteVillageRepository _whiteVillageRepository;
        private readonly IFileValidationService _fileValidationService;
        private readonly IConfiguration _configuration;
        private readonly Utility _utility;

        // Constants for validation
        private const long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024; // 10MB
        private const string VALID_EXTENSIONS = ".xlsx|.xls|.csv";
        private const int MAX_BLOB_RESULTS = 100;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the WhiteVillageService with required dependencies.
        /// </summary>
        /// <param name="whiteVillageRepository">Repository for white village data operations</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="fileValidationService">Service for file validation operations</param>
        /// <param name="utility">Utility service for common operations</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
        public WhiteVillageService(
            IWhiteVillageRepository whiteVillageRepository,
            IConfiguration configuration,
            IFileValidationService fileValidationService,
            Utility utility)
        {
            _whiteVillageRepository = whiteVillageRepository ?? throw new ArgumentNullException(nameof(whiteVillageRepository));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _fileValidationService = fileValidationService ?? throw new ArgumentNullException(nameof(fileValidationService));
            _utility = utility ?? throw new ArgumentNullException(nameof(utility));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves all white listing data from the repository.
        /// </summary>
        /// <returns>Collection of white village models</returns>
        /// <exception cref="InvalidOperationException">Thrown when repository operation fails</exception>
        public async Task<IEnumerable<WhiteVillageModel>> GetWhiteListingService()
        {
            try
            {
                var result = await _whiteVillageRepository.GetWhiteListingRepo();
                return result ?? Enumerable.Empty<WhiteVillageModel>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve white listing data", ex);
            }
        }

        /// <summary>
        /// Retrieves the list of available states.
        /// </summary>
        /// <returns>Service response containing state list or error information</returns>
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

        /// <summary>
        /// Uploads a white village file to Azure Blob Storage and saves metadata to the database.
        /// Validates file size, extension, and content before processing.
        /// </summary>
        /// <param name="whiteVillageFile">The file to upload</param>
        /// <param name="stateId">Associated state identifier</param>
        /// <param name="empNo">Employee number for audit trail</param>
        /// <returns>Service response indicating success or failure with details</returns>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public async Task<ServiceResponse> UploadWhiteVillageFileService(IFormFile whiteVillageFile, string stateId, string empNo)
        {
            if (whiteVillageFile == null)
            {
                return CreateErrorResponse("400", "File is required");
            }

            if (string.IsNullOrWhiteSpace(stateId))
            {
                return CreateErrorResponse("400", "State ID is required");
            }

            if (string.IsNullOrWhiteSpace(empNo))
            {
                return CreateErrorResponse("400", "Employee number is required");
            }

            try
            {
                // Validate file size
                var fileSizeValidation = ValidateFileSize(whiteVillageFile);
                if ((bool)fileSizeValidation.isError)
                {
                    return fileSizeValidation;
                }

                // Validate file extension and magic number
                var fileTypeValidation = await ValidateFileType(whiteVillageFile);
                if ((bool)fileTypeValidation.isError)
                {
                    return fileTypeValidation;
                }

                // Validate file content
                var contentValidation = await ValidateFileContent(whiteVillageFile);
                if ((bool)contentValidation.isError)
                {
                    return contentValidation;
                }

                // Validate filename format
                var filenameValidation = ValidateFileName(whiteVillageFile.FileName);
                if ((bool)filenameValidation.isError)
                {
                    return filenameValidation;
                }

                // Upload to blob storage
                await UploadFileToBlob(whiteVillageFile, whiteVillageFile.FileName);

                // Save metadata
                var fiscalYear = DetermineFiscalYear();
                var saveResult = await _whiteVillageRepository.SaveWhiteVillageFileMetadata(
                    whiteVillageFile.FileName, stateId, empNo, fiscalYear);

                return new ServiceResponse
                {
                    isError = saveResult != "200",
                    Status = saveResult == "200" ? "Success" : "Failure",
                    Code = saveResult == "200" ? "200" : "500",
                    Message = saveResult == "200" ? "File uploaded successfully" : "Failed to save file metadata."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Status = "Error",
                    Code = "500",
                    Error = ex.Message,
                    Message = "An unexpected error occurred during file upload"
                };
            }
        }

        /// <summary>
        /// Generates a download URL with SAS token for a specified file.
        /// </summary>
        /// <param name="fileName">Name of the file to download</param>
        /// <returns>Secure download URL with SAS token</returns>
        /// <exception cref="ArgumentException">Thrown when filename is null or empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when file doesn't exist</exception>
        /// <exception cref="InvalidOperationException">Thrown when storage configuration is invalid</exception>
        public async Task<string> WhiteVillageDownloadService(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Filename cannot be null or empty", nameof(fileName));
            }

            try
            {
                var blobUrlFromRepo = await _whiteVillageRepository.WhiteVillageDownloadRepo(fileName);

                if (string.IsNullOrWhiteSpace(blobUrlFromRepo))
                {
                    throw new FileNotFoundException($"File '{fileName}' not found in database");
                }

                var connectionString = GetConnectionString();
                var (container, blobClient) = CreateBlobClientAndContainer(connectionString);

                var blobUrl = $"{container.StorageUri.PrimaryUri}/{blobUrlFromRepo}";
                var blob = new CloudBlockBlob(new Uri(blobUrl), blobClient.Credentials);

                if (!await blob.ExistsAsync())
                {
                    throw new FileNotFoundException($"File '{fileName}' does not exist in storage");
                }

                var sasToken = _utility.GenerateSasToken(blob);
                return $"{blob.Uri}?{sasToken}";
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate download URL for file '{fileName}'", ex);
            }
        }

        /// <summary>
        /// Lists all blobs in the configured Azure Storage container.
        /// </summary>
        /// <returns>List of blob names in the container</returns>
        /// <exception cref="InvalidOperationException">Thrown when storage configuration is invalid or container doesn't exist</exception>
        public async Task<List<string>> ListAllBlobsInContainer()
        {
            try
            {
                var connectionString = GetConnectionString();
                var (container, _) = CreateBlobClientAndContainer(connectionString);

                if (!await container.ExistsAsync())
                {
                    throw new InvalidOperationException($"Container '{GetContainerName()}' does not exist");
                }

                var blobList = new List<(string Name, DateTimeOffset LastModified)>();
                BlobContinuationToken continuationToken = null;

                do
                {
                    var results = await container.ListBlobsSegmentedAsync(
                        prefix: "",
                        useFlatBlobListing: true,
                        blobListingDetails: BlobListingDetails.Metadata, // Need this to get last modified date
                        maxResults: MAX_BLOB_RESULTS,
                        currentToken: continuationToken,
                        options: null,
                        operationContext: null);

                    continuationToken = results.ContinuationToken;

                    foreach (var blob in results.Results.OfType<CloudBlockBlob>())
                    {
                        var blobName = ExtractBlobName(blob.Uri, container.Uri);
                        blobList.Add((blobName, blob.Properties.LastModified.GetValueOrDefault()));
                    }
                }
                while (continuationToken != null);

                // Sort by LastModified in descending order and return just names
                return blobList.OrderByDescending(x => x.LastModified)
                              .Select(x => x.Name)
                              .ToList();
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException("Failed to list blobs in container", ex);
            }
        }

        /// <summary>
        /// Deletes a specified blob file from Azure Storage.
        /// </summary>
        /// <param name="fileName">Name of the file to delete</param>
        /// <returns>True if file was deleted successfully, false if file doesn't exist</returns>
        /// <exception cref="ArgumentException">Thrown when filename is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when storage configuration is invalid or deletion fails</exception>
        public async Task<bool> DeleteBlobFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Filename cannot be null or empty", nameof(fileName));
            }

            try
            {
                var connectionString = GetConnectionString();
                var (container, _) = CreateBlobClientAndContainer(connectionString);

                if (!await container.ExistsAsync())
                {
                    throw new InvalidOperationException($"Container '{GetContainerName()}' does not exist");
                }

                var blob = container.GetBlockBlobReference(fileName);

                if (!await blob.ExistsAsync())
                {
                    return false;
                }

                await blob.DeleteAsync();
                return true;
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Failed to delete file '{fileName}'", ex);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines the current fiscal year based on the current date.
        /// Fiscal year starts from April.
        /// </summary>
        /// <returns>Fiscal year in format "FYxx"</returns>
        private static string DetermineFiscalYear()
        {
            var now = DateTime.Now;
            var year = now.Year;
            return now.Month >= 4 ? $"FY{(year + 1) % 100:00}" : $"FY{year % 100:00}";
        }

        /// <summary>
        /// Uploads a file to Azure Blob Storage.
        /// </summary>
        /// <param name="file">File to upload</param>
        /// <param name="fileName">Name for the blob</param>
        /// <returns>URL of the uploaded blob</returns>
        private async Task<string> UploadFileToBlob(IFormFile file, string fileName)
        {
            var connectionString = GetConnectionString();
            var (container, _) = CreateBlobClientAndContainer(connectionString);
            var blockBlob = container.GetBlockBlobReference(fileName);

            using var data = file.OpenReadStream();
            await blockBlob.UploadFromStreamAsync(data);

            return blockBlob.Uri.ToString();
        }

        /// <summary>
        /// Validates file size against maximum allowed size.
        /// </summary>
        private static ServiceResponse ValidateFileSize(IFormFile file)
        {
            if (file.Length > MAX_FILE_SIZE_BYTES)
            {
                return CreateErrorResponse("400", "File size exceeds the maximum allowed size of 10MB");
            }
            return CreateSuccessResponse();
        }

        /// <summary>
        /// Validates file type using extension and magic number validation.
        /// </summary>
        private async Task<ServiceResponse> ValidateFileType(IFormFile file)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var magicNumberType = MagicNumberClass.MagicNumber(file);

            if (string.IsNullOrEmpty(magicNumberType) || !VALID_EXTENSIONS.Contains(fileExtension))
            {
                return CreateErrorResponse("400", "Invalid file type. Only Excel files (.xlsx, .xls, .csv) are allowed");
            }

            return CreateSuccessResponse();
        }

        /// <summary>
        /// Validates file content using the file validation service.
        /// </summary>
        private async Task<ServiceResponse> ValidateFileContent(IFormFile file)
        {
            var validationResult = await _fileValidationService.ValidateFile(file);

            if (validationResult.isError == true)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Status = "Failure",
                    Code = "400",
                    Message = validationResult.Message ?? "File content validation failed"
                };
            }

            return CreateSuccessResponse();
        }

        /// <summary>
        /// Validates filename format to ensure it doesn't contain multiple periods.
        /// </summary>
        private static ServiceResponse ValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return CreateErrorResponse("400", "Filename cannot be empty");
            }

            if (fileName.Count(c => c == '.') > 1)
            {
                return CreateErrorResponse("400", "File name cannot contain multiple periods (.) except for the extension");
            }

            return CreateSuccessResponse();
        }

        /// <summary>
        /// Gets the Azure Storage connection string from configuration.
        /// </summary>
        private string GetConnectionString()
        {
            var connectionString = _configuration["AzureBlobStorage:StorageAccount"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Azure Blob Storage connection string is not configured");
            }
            return connectionString;
        }

        /// <summary>
        /// Gets the container name from configuration.
        /// </summary>
        private string GetContainerName()
        {
            var containerName = _configuration["AzureBlobStorage:ContainerName"];
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new InvalidOperationException("Azure Blob Storage container name is not configured");
            }
            return containerName;
        }

        /// <summary>
        /// Creates blob client and container reference.
        /// </summary>
        private (CloudBlobContainer container, CloudBlobClient blobClient) CreateBlobClientAndContainer(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(GetContainerName());
            return (container, blobClient);
        }

        /// <summary>
        /// Extracts blob name from URI relative to container URI.
        /// </summary>
        private static string ExtractBlobName(Uri blobUri, Uri containerUri)
        {
            return blobUri.ToString().Substring(containerUri.ToString().Length + 1);
        }

        /// <summary>
        /// Creates a success service response.
        /// </summary>
        private static ServiceResponse CreateSuccessResponse()
        {
            return new ServiceResponse
            {
                isError = false,
                Status = "Success",
                Code = "200"
            };
        }

        /// <summary>
        /// Creates an error service response with specified code and message.
        /// </summary>
        private static ServiceResponse CreateErrorResponse(string code, string message)
        {
            return new ServiceResponse
            {
                isError = true,
                Status = "Failure",
                Code = code,
                Message = message
            };
        }

        #endregion
    }
}