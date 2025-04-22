using DealerSetu_Services.IServices;
using System.Security.Cryptography;

namespace DealerSetu_Services.Services
{
    public class AesEncryptionService : IEncryptionService
    {
        public (byte[] EncryptedData, string Key, string IV) EncryptData(byte[] data)
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

            return (
                encryptedData,
                Convert.ToBase64String(aes.Key),
                Convert.ToBase64String(aes.IV)
            );
        }

        public byte[] DecryptData(byte[] encryptedData, string key, string iv)
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(key);
            aes.IV = Convert.FromBase64String(iv);

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }
    }
}
