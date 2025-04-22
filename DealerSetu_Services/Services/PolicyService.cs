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
    public class PolicyService : IPolicyService
    {
        private IPolicyRepository _policyRepository;
        private readonly IConfiguration _configuration;

        public PolicyService(IPolicyRepository policyRepository, IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _policyRepository = policyRepository;
        }       

        public ServiceResponse GetPolicyListService()
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var result = _policyRepository.GetPolicyListRepo();

                response.Status = result.Status;
                response.Code = result.Code;
                response.Message = result.Message;
                response.result = result.Result;

                if (result.Result is string jsonString)
                {
                    try
                    {
                        var policies = JsonSerializer.Deserialize<List<PolicyModel>>(jsonString);
                        response.result = policies;
                    }
                    catch (JsonException ex)
                    {
                        response.isError = true;
                        response.Error = $"Error processing JSON result: {ex.Message}";
                        response.result = null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return response;
        }

        public async Task<ServiceResponse> SendFiletoServerService(PolicyUploadModel request, int RAId)
        {
            var response = new ServiceResponse();

            if (request.RecomendationFileName != null)
            {
                try
                {
                    // File size validation (max 10 MB)
                    long maxFileSizeInBytes = 10 * 1024 * 1024;
                    if (request.RecomendationFileName.Length > maxFileSizeInBytes)
                    {
                        response.Status = "Failure";
                        response.Code = "400";
                        response.Error = "File size exceeds the maximum allowed size.";
                        return response;
                    }

                    // Allowed file extensions
                    string validExtensions = ".jpg|.jpeg|.png|.gif|.pdf|.tif|.tiff|.docx|.pdf|.csv||.xlsx";

                    string fileExtension = Path.GetExtension(request.RecomendationFileName.FileName);
                    var type = MagicNumberClass.MagicNumber(request.RecomendationFileName);

                    // Validate file type
                    if (!string.IsNullOrEmpty(type) && validExtensions.Contains(fileExtension.ToLower()))
                    {
                        // Generate unique filename
                        Random generator = new Random();
                        String randomstring = generator.Next(0, 1000000).ToString("D6");
                        var fileName = request.RecomendationFileName.FileName;
                        var fileNames = fileName.Split('.');
                        var updatedFileName = fileNames[0] + "_" + randomstring + "." + fileNames[1];

                        // Upload to Azure Blob Storage and get the Blob URL
                        var blobUrl = await AddDocs(request.RecomendationFileName, updatedFileName);
                        request.UpdatedName = blobUrl; // Store the Blob URL instead of filename
                    }
                    else
                    {
                        response.Status = "Failure";
                        response.Code = "200";
                        response.Error = "Invalid File type";
                        return response;
                    }

                    // Save the Blob URL to the database
                    var result = _policyRepository.SendFilesToServerRepo(request, RAId);
                    response.Status = "Success";
                    response.Code = "200";
                }
                catch (Exception ex)
                {
                    response.isError = true;
                    response.Error = ex.InnerException?.ToString() ?? ex.Message;
                }
            }
            return response;
        }

        public async Task<string> AddDocs(IFormFile files, string fileName)
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(
                _configuration.GetValue<string>("AzureBlobStorage:StorageAccount")
            );

            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(
                _configuration.GetValue<string>("AzureBlobStorage:ContainerName")
            );

            // Get the blob reference
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

            // Upload the file
            await using (var data = files.OpenReadStream())
            {
                await blockBlob.UploadFromStreamAsync(data);
            }

            // Return the full Blob URL
            return blockBlob.Uri.ToString();
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


