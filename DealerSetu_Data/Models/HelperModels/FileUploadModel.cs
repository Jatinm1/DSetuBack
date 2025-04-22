using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.HelperModels
{
    public class FileUploadModel
    {
        public int RAId { get; set; }
        public IFormFile File { get; set; }
        public string BlobUrl { get; set; }
        public string ContentType { get; set; }
    }
}
