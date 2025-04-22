using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.IServices
{
    public interface IEncryptionService
    {
        (byte[] EncryptedData, string Key, string IV) EncryptData(byte[] data);
        byte[] DecryptData(byte[] encryptedData, string key, string iv);
    }
}
