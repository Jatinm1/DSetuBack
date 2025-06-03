using dealersetu_repositories.irepositories;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.Text.Json;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;

namespace dealersetu_services.services
{
    /// <summary>
    /// Service responsible for policy management operations including file uploads and policy retrieval
    /// </summary>
    public class PolicyService : IPolicyService
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IConfiguration _configuration;

        // Constants for file validation
        private const long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024; // 10 MB
        private const string VALID_EXTENSIONS = ".jpg|.jpeg|.png|.gif|.pdf|.tif|.tiff|.docx|.csv|.xlsx";
        private const int RANDOM_SUFFIX_RANGE = 1000000;

        /// <summary>
        /// Initializes a new instance of the PolicyService
        /// </summary>
        /// <param name="policyRepository">Repository for policy data operations</param>
        /// <param name="configuration">Application configuration settings</param>
        /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
        public PolicyService(IPolicyRepository policyRepository, IConfiguration configuration)
        {
            _policyRepository = policyRepository ?? throw new ArgumentNullException(nameof(policyRepository));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Retrieves the list of policies from the repository
        /// </summary>
        /// <returns>ServiceResponse containing the policy list or error information</returns>
        public ServiceResponse GetPolicyListService()
        {
            var response = new ServiceResponse();

            try
            {
                var result = _policyRepository.GetPolicyListRepo();
                if (result == null)
                {
                    response.Status = "Failure";
                    response.Code = "500";
                    response.Error = "Repository returned null result";
                    response.isError = true;
                    return response;
                }

                response.Status = result.Status;
                response.Code = result.Code;
                response.Message = result.Message;
                response.result = result.Result;

                // Deserialize JSON string to PolicyModel list if applicable
                if (result.Result is string jsonString && !string.IsNullOrWhiteSpace(jsonString))
                {
                    try
                    {
                        var policies = JsonSerializer.Deserialize<List<PolicyModel>>(jsonString, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        response.result = policies;
                    }
                    catch (JsonException ex)
                    {
                        response.isError = true;
                        response.Status = "Failure";
                        response.Code = "500";
                        response.Error = $"Failed to deserialize policy data: {ex.Message}";
                        response.result = null;
                    }
                }
            }
            catch (Exception ex)
            {
                response.isError = true;
                response.Status = "Failure";
                response.Code = "500";
                response.Error = $"Unexpected error occurred: {ex.Message}";
                response.result = null;
            }

            return response;
        }

        /// <summary>
        /// Uploads a policy recommendation file to Azure Blob Storage and saves metadata to database
        /// </summary>
        /// <param name="request">Policy upload request containing file and metadata</param>
        /// <param name="RAId">Related Agreement ID</param>
        /// <returns>ServiceResponse indicating success or failure of the upload operation</returns>
        public async Task<ServiceResponse> SendFiletoServerService(PolicyUploadModel request, int RAId)
        {
            var response = new ServiceResponse();

            // Validate input parameters
            if (request?.RecomendationFileName == null)
            {
                response.Status = "Failure";
                response.Code = "400";
                response.Error = "File is required";
                response.isError = true;
                return response;
            }

            if (RAId <= 0)
            {
                response.Status = "Failure";
                response.Code = "400";
                response.Error = "Invalid RA ID";
                response.isError = true;
                return response;
            }

            try
            {
                // Validate file size
                if (request.RecomendationFileName.Length > MAX_FILE_SIZE_BYTES)
                {
                    response.Status = "Failure";
                    response.Code = "400";
                    response.Error = $"File size exceeds the maximum allowed size of {MAX_FILE_SIZE_BYTES / (1024 * 1024)} MB";
                    response.isError = true;
                    return response;
                }

                // Validate file extension and type
                var fileExtension = Path.GetExtension(request.RecomendationFileName.FileName);
                if (string.IsNullOrWhiteSpace(fileExtension))
                {
                    response.Status = "Failure";
                    response.Code = "400";
                    response.Error = "File must have a valid extension";
                    response.isError = true;
                    return response;
                }

                var magicNumberType = MagicNumberClass.MagicNumber(request.RecomendationFileName);

                if (string.IsNullOrEmpty(magicNumberType) || !VALID_EXTENSIONS.Contains(fileExtension.ToLowerInvariant()))
                {
                    response.Status = "Failure";
                    response.Code = "400";
                    response.Error = "Invalid file type. Allowed types: " + VALID_EXTENSIONS.Replace("|", ", ");
                    response.isError = true;
                    return response;
                }

                // Generate unique filename
                var updatedFileName = GenerateUniqueFileName(request.RecomendationFileName.FileName);

                // Upload to Azure Blob Storage
                var blobUrl = await AddDocs(request.RecomendationFileName, updatedFileName);
                if (string.IsNullOrWhiteSpace(blobUrl))
                {
                    response.Status = "Failure";
                    response.Code = "500";
                    response.Error = "Failed to upload file to storage";
                    response.isError = true;
                    return response;
                }

                request.UpdatedName = updatedFileName;

                // Save to database
                var result = _policyRepository.SendFilesToServerRepo(request, RAId);

                response.Status = "Success";
                response.Code = "200";
                response.Message = "File uploaded successfully";
                response.result = new { BlobUrl = blobUrl, FileName = updatedFileName };
            }
            catch (ArgumentException ex)
            {
                response.Status = "Failure";
                response.Code = "400";
                response.Error = $"Invalid argument: {ex.Message}";
                response.isError = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                response.Status = "Failure";
                response.Code = "401";
                response.Error = "Unauthorized access to storage";
                response.isError = true;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Code = "500";
                response.Error = $"Upload failed: {ex.Message}";
                response.isError = true;
            }

            return response;
        }

        /// <summary>
        /// Uploads a file to Azure Blob Storage
        /// </summary>
        /// <param name="files">File to upload</param>
        /// <param name="fileName">Unique filename for the blob</param>
        /// <returns>The full URL of the uploaded blob</returns>
        /// <exception cref="ArgumentNullException">Thrown when file or filename is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when Azure configuration is invalid</exception>
        public async Task<string> AddDocs(IFormFile files, string fileName)
        {
            if (files == null)
                throw new ArgumentNullException(nameof(files));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));

            try
            {
                var storageConnectionString = _configuration.GetValue<string>("AzureBlobStorage:StorageAccount");
                var containerName = _configuration.GetValue<string>("AzureBlobStorage:ContainerName");

                if (string.IsNullOrWhiteSpace(storageConnectionString))
                    throw new InvalidOperationException("Azure Blob Storage connection string is not configured");

                if (string.IsNullOrWhiteSpace(containerName))
                    throw new InvalidOperationException("Azure Blob Storage container name is not configured");

                var cloudStorageAccount = CloudStorageAccount.Parse(storageConnectionString);
                var blobClient = cloudStorageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(containerName);

                // Ensure container exists
                await container.CreateIfNotExistsAsync();

                var blockBlob = container.GetBlockBlobReference(fileName);

                // Set content type based on file extension
                var contentType = GetContentType(Path.GetExtension(fileName));
                if (!string.IsNullOrWhiteSpace(contentType))
                {
                    blockBlob.Properties.ContentType = contentType;
                }

                // Upload file with retry mechanism
                using var fileStream = files.OpenReadStream();
                await blockBlob.UploadFromStreamAsync(fileStream);

                return blockBlob.Uri.ToString();
            }
            catch (StorageException ex)
            {
                throw new InvalidOperationException($"Azure Storage operation failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"File upload failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generates a unique filename by appending a random suffix
        /// </summary>
        /// <param name="originalFileName">Original filename</param>
        /// <returns>Unique filename with random suffix</returns>
        private static string GenerateUniqueFileName(string originalFileName)
        {
            if (string.IsNullOrWhiteSpace(originalFileName))
                throw new ArgumentException("Original filename cannot be null or empty", nameof(originalFileName));

            var random = new Random();
            var randomSuffix = random.Next(0, RANDOM_SUFFIX_RANGE).ToString("D6");

            var fileNameParts = originalFileName.Split('.');
            if (fileNameParts.Length < 2)
                throw new ArgumentException("Filename must have an extension", nameof(originalFileName));

            var nameWithoutExtension = string.Join(".", fileNameParts.Take(fileNameParts.Length - 1));
            var extension = fileNameParts.Last();

            return $"{nameWithoutExtension}_{randomSuffix}.{extension}";
        }

        /// <summary>
        /// Gets the MIME content type based on file extension
        /// </summary>
        /// <param name="fileExtension">File extension including the dot</param>
        /// <returns>MIME content type or null if unknown</returns>
        private static string GetContentType(string fileExtension)
        {
            return fileExtension?.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".tif" or ".tiff" => "image/tiff",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".csv" => "text/csv",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}





//*************************************METHODS TO BE USED IN FUTURE*************************************

//WHITELISTING
//public ServiceResponse GetWhiteListingService()
//{
//    ServiceResponse response = new ServiceResponse();
//    try
//    {
//        var result = _policyRepository.GetWhiteListingRepo();
//        response.Status = result.Status;
//        response.Code = result.Code;
//        response.Message = result.Message;
//        response.result = result.Result;

//        if (result.Result is string jsonString)
//        {
//            try
//            {
//                var partDetails = JsonSerializer.Deserialize<List<WhiteVillageModel>>(jsonString);
//                response.result = partDetails;


//            }
//            catch (JsonException ex)
//            {
//                response.isError = true;
//                response.Error = $"Error processing JSON result: {ex.Message}";
//                response.result = null;
//            }
//        }
//    }
//    catch (Exception ex)
//    {
//        throw;
//    }
//    return response;
//}


//*************************************IMPORTANT METHODS FOR FILE UPLOADING*************************************


