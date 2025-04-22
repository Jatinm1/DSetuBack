using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
namespace DealerSetu_Services.Services
{
    public class RSAEncryptionService
    {
        private readonly RSA _rsa;
        private string _privateKey;
        private string _publicKey;
        private readonly IConfiguration _configuration;
        public RSAEncryptionService(IConfiguration configuration)
        {
            _configuration = configuration;
            _privateKey = _configuration["RSAKeys:PrivateKey"];
            _publicKey = _configuration["RSAKeys:PublicKey"];
            _rsa = RSA.Create();
            if (!string.IsNullOrEmpty(_privateKey))
            {
                _rsa.ImportRSAPrivateKey(Convert.FromBase64String(_privateKey), out _);
            }
        }
        public string GetPublicKey() => _publicKey;
        public string DecryptRSA(string encryptedData)
        {
            if (string.IsNullOrEmpty(_privateKey))
                throw new InvalidOperationException("Private key is not available");
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
                byte[] decryptedBytes = _rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Decryption failed. Please ensure the data was encrypted with the corresponding public key.", ex);
            }
        }
    }
    public static class AESDecryption
    {
        public static string DecryptAES(byte[] encryptedData, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var decryptor = aes.CreateDecryptor())
                using (var msDecrypt = new MemoryStream(encryptedData))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
    }
}