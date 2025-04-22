using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.IServices
{
    public interface IBlobStorageService
    {
        Task<string> UploadFileAsync(IFormFile file);
        Task<bool> DeleteFileAsync(string blobUrl);
        Task<bool> BlobExistsAsync(string blobUrl);
        string GetImageURL(string fileName);
    }
}
