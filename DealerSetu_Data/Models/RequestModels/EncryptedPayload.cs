using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class EncryptedPayload
    {
        public string EncryptedData { get; set; }
        public string EncryptedKey { get; set; }
        public string EncryptedIV { get; set; }
        //public string reCaptcha { get; set; }

    }
}