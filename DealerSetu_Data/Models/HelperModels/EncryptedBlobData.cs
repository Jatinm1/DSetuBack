using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.HelperModels
{
    // Create a model for encrypted blob data
    public class EncryptedBlobData
    {
        public string BlobUrl { get; set; }
        public string EncryptionKey { get; set; }
        public string IV { get; set; }
    }
}
