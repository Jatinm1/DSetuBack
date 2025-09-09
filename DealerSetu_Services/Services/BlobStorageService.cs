using Azure.Storage.Blobs;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ImageMagick;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    /// <summary>
    /// Azure Blob Storage service for file operations using Magick.NET.
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
        /// Uploads a file to blob storage asynchronously after stripping EXIF/metadata.
        /// </summary>
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
                fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                _logger.LogInformation("Starting file upload for {FileName} (Original: {OriginalFileName})",
                    fileName, file.FileName);

                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(fileName);

                using (var inputStream = file.OpenReadStream())
                using (var image = new MagickImage(inputStream))
                {
                    // Strip ALL metadata (EXIF, ICC, IPTC, XMP, etc.)
                    image.Strip();

                    // Ensure good quality if saving as JPEG
                    if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        image.Quality = 90;
                    }

                    using (var outputStream = new MemoryStream())
                    {
                        // Save in the same format as uploaded file
                        var format = GetMagickFormat(file.FileName);
                        image.Write(outputStream, format);

                        outputStream.Position = 0;
                        await blobClient.UploadAsync(outputStream, overwrite: true);
                    }
                }

                _logger.LogInformation("Successfully uploaded file {FileName}", fileName);

                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} to blob storage", fileName ?? file.FileName);
                throw new InvalidOperationException($"Error uploading file to blob storage: {ex.Message}", ex);
            }
        }

        private MagickFormat GetMagickFormat(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".png" => MagickFormat.Png,
                ".webp" => MagickFormat.WebP,
                ".jpg" => MagickFormat.Jpeg,
                ".jpeg" => MagickFormat.Jpeg,
                _ => MagickFormat.Jpeg // fallback
            };
        }

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

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file URL for {FileName}", fileName);
                throw new InvalidOperationException($"Error getting file URL: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string blobUrl)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                throw new ArgumentException("Blob URL cannot be null or empty", nameof(blobUrl));

            string blobName = null;

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blob {BlobName} from storage", blobName ?? blobUrl);
                throw new InvalidOperationException($"Error deleting file from blob storage: {ex.Message}", ex);
            }
        }

        public async Task<bool> BlobExistsAsync(string blobUrl)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                throw new ArgumentException("Blob URL cannot be null or empty", nameof(blobUrl));

            string blobName = null;

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if blob {BlobName} exists", blobName ?? blobUrl);
                throw new InvalidOperationException($"Error checking if blob exists: {ex.Message}", ex);
            }
        }
    }
}
