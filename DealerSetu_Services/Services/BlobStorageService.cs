using Azure.Storage.Blobs;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public BlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureBlobStorage:StorageAccount"];
            _containerName = configuration["AzureBlobStorage:ContainerName"];
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            try
            {
                // Generate a unique file name
                string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                // Create blob client and container client
                BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

                // Create the container if it doesn't already exist
                await containerClient.CreateIfNotExistsAsync();

                // Get a reference to the blob
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                // Upload the file
                using (Stream stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, true);
                }

                // Return the blob URL 
                return fileName;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading file to blob storage: {ex.Message}", ex);
            }
        }

        public string GetImageURL(string fileName)
        {
            try
            {

                BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                BlobClient blobClient = containerClient.GetBlobClient(fileName);
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error Geting file URL: {ex.Message}", ex);
            }

        }



        public async Task<bool> DeleteFileAsync(string blobUrl)
        {
            try
            {
                // Extract the blob name from the URL
                string blobName = Path.GetFileName(new Uri(blobUrl).LocalPath);

                BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                return await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting file from blob storage: {ex.Message}", ex);
            }
        }

        public async Task<bool> BlobExistsAsync(string blobUrl)
        {
            try
            {
                // Extract the blob name from the URL
                string blobName = Path.GetFileName(new Uri(blobUrl).LocalPath);

                BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                return await blobClient.ExistsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking if blob exists: {ex.Message}", ex);
            }
        }
    }
}
