using Azure.Storage.Blobs;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    /// <summary>
    /// Azure Blob Storage service for file operations.
    /// </summary>
    public class BlobStorageService : IBlobStorageService
    {
        private readonly string _connectionString;
        private readonly string _containerName;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _connectionString = configuration["AzureBlobStorage:StorageAccount"];
            _containerName = configuration["AzureBlobStorage:ContainerName"];

            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("Azure Blob Storage connection string is not configured");

            if (string.IsNullOrWhiteSpace(_containerName))
                throw new InvalidOperationException("Azure Blob Storage container name is not configured");
        }

        /// <summary>
        /// Uploads a file to blob storage asynchronously.
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <returns>The generated file name</returns>
        /// <exception cref="ArgumentNullException">Thrown when file is null</exception>
        /// <exception cref="ArgumentException">Thrown when file is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when upload fails</exception>
        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file), "File cannot be null");

            if (file.Length == 0)
                throw new ArgumentException("File cannot be empty", nameof(file));

            if (string.IsNullOrWhiteSpace(file.FileName))
                throw new ArgumentException("File name cannot be empty", nameof(file));

            string fileName = null;

            try
            {
                // Generate a unique file name
                fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                _logger.LogInformation("Starting file upload for {FileName} (Original: {OriginalFileName})",
                    fileName, file.FileName);

                // Create blob client and container client
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

                // Create the container if it doesn't already exist
                await containerClient.CreateIfNotExistsAsync();

                // Get a reference to the blob
                var blobClient = containerClient.GetBlobClient(fileName);

                // Upload the file
                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                _logger.LogInformation("Successfully uploaded file {FileName}", fileName);

                // Return the blob file name
                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} to blob storage", fileName ?? file.FileName);
                throw new InvalidOperationException($"Error uploading file to blob storage: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the URL for a blob file.
        /// </summary>
        /// <param name="fileName">The name of the blob file</param>
        /// <returns>The blob URL</returns>
        /// <exception cref="ArgumentException">Thrown when fileName is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when URL retrieval fails</exception>
        public string GetImageURL(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            try
            {
                _logger.LogInformation("Getting URL for blob {FileName}", fileName);

                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                var url = blobClient.Uri.ToString();

                _logger.LogInformation("Successfully retrieved URL for blob {FileName}", fileName);

                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file URL for {FileName}", fileName);
                throw new InvalidOperationException($"Error getting file URL: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a file from blob storage asynchronously.
        /// </summary>
        /// <param name="blobUrl">The URL of the blob to delete</param>
        /// <returns>True if the blob was deleted, false if it didn't exist</returns>
        /// <exception cref="ArgumentException">Thrown when blobUrl is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when deletion fails</exception>
        public async Task<bool> DeleteFileAsync(string blobUrl)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                throw new ArgumentException("Blob URL cannot be null or empty", nameof(blobUrl));

            string blobName = null;

            try
            {
                // Extract the blob name from the URL
                if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
                    throw new ArgumentException("Invalid blob URL format", nameof(blobUrl));

                blobName = Path.GetFileName(uri.LocalPath);

                if (string.IsNullOrWhiteSpace(blobName))
                    throw new ArgumentException("Cannot extract blob name from URL", nameof(blobUrl));

                _logger.LogInformation("Deleting blob {BlobName} from URL {BlobUrl}", blobName, blobUrl);

                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var result = await blobClient.DeleteIfExistsAsync();

                _logger.LogInformation("Blob deletion result for {BlobName}: {Result}", blobName, result.Value);

                return result.Value;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blob {BlobName} from storage", blobName ?? blobUrl);
                throw new InvalidOperationException($"Error deleting file from blob storage: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if a blob exists in storage asynchronously.
        /// </summary>
        /// <param name="blobUrl">The URL of the blob to check</param>
        /// <returns>True if the blob exists, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when blobUrl is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when existence check fails</exception>
        public async Task<bool> BlobExistsAsync(string blobUrl)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                throw new ArgumentException("Blob URL cannot be null or empty", nameof(blobUrl));

            string blobName = null;

            try
            {
                // Extract the blob name from the URL
                if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
                    throw new ArgumentException("Invalid blob URL format", nameof(blobUrl));

                blobName = Path.GetFileName(uri.LocalPath);

                if (string.IsNullOrWhiteSpace(blobName))
                    throw new ArgumentException("Cannot extract blob name from URL", nameof(blobUrl));

                _logger.LogInformation("Checking existence of blob {BlobName}", blobName);

                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var response = await blobClient.ExistsAsync();

                _logger.LogInformation("Blob existence check for {BlobName}: {Exists}", blobName, response.Value);

                return response.Value;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if blob {BlobName} exists", blobName ?? blobUrl);
                throw new InvalidOperationException($"Error checking if blob exists: {ex.Message}", ex);
            }
        }
    }
}