using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DealerSetu_Services.Services
{
    /// <summary>
    /// Provides RSA encryption and decryption services using public/private key pairs.
    /// </summary>
    public class RSAEncryptionService : IDisposable
    {
        private readonly RSA _rsa;
        private readonly string _privateKey;
        private readonly string _publicKey;
        private readonly IConfiguration _configuration;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the RSAEncryptionService with configuration-based keys.
        /// </summary>
        /// <param name="configuration">Configuration containing RSA keys</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when RSA keys are missing or invalid</exception>
        public RSAEncryptionService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _privateKey = _configuration["RSAKeys:PrivateKey"];
            _publicKey = _configuration["RSAKeys:PublicKey"];

            if (string.IsNullOrWhiteSpace(_publicKey))
                throw new InvalidOperationException("RSA public key is not configured or is empty.");

            try
            {
                _rsa = RSA.Create();

                if (!string.IsNullOrWhiteSpace(_privateKey))
                {
                    _rsa.ImportRSAPrivateKey(Convert.FromBase64String(_privateKey), out _);
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is CryptographicException)
            {
                _rsa?.Dispose();
                throw new InvalidOperationException("Invalid RSA key format in configuration.", ex);
            }
        }

        /// <summary>
        /// Gets the public key for RSA encryption.
        /// </summary>
        /// <returns>Base64 encoded public key</returns>
        public string GetPublicKey()
        {
            ThrowIfDisposed();
            return _publicKey;
        }

        /// <summary>
        /// Decrypts RSA encrypted data using the private key.
        /// </summary>
        /// <param name="encryptedData">Base64 encoded encrypted data</param>
        /// <returns>Decrypted plain text</returns>
        /// <exception cref="ArgumentException">Thrown when encrypted data is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when private key is not available</exception>
        /// <exception cref="CryptographicException">Thrown when decryption fails</exception>
        public string DecryptRSA(string encryptedData)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(encryptedData))
                throw new ArgumentException("Encrypted data cannot be null or empty.", nameof(encryptedData));

            if (string.IsNullOrWhiteSpace(_privateKey))
                throw new InvalidOperationException("Private key is not available for decryption.");

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
                byte[] decryptedBytes = _rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid Base64 format for encrypted data.", nameof(encryptedData), ex);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("RSA decryption failed. Ensure the data was encrypted with the corresponding public key.", ex);
            }
        }

        /// <summary>
        /// Throws ObjectDisposedException if the service has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RSAEncryptionService));
        }

        /// <summary>
        /// Releases all resources used by the RSAEncryptionService.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources used by the RSAEncryptionService.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _rsa?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Provides AES decryption functionality using symmetric encryption.
    /// </summary>
    public static class AESDecryption
    {
        /// <summary>
        /// Decrypts AES encrypted data using the provided key and initialization vector.
        /// </summary>
        /// <param name="encryptedData">Encrypted data bytes</param>
        /// <param name="key">AES encryption key</param>
        /// <param name="iv">Initialization vector</param>
        /// <returns>Decrypted plain text</returns>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when parameters have invalid lengths</exception>
        /// <exception cref="CryptographicException">Thrown when decryption fails</exception>
        public static string DecryptAES(byte[] encryptedData, byte[] key, byte[] iv)
        {
            if (encryptedData == null)
                throw new ArgumentNullException(nameof(encryptedData));
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (iv == null)
                throw new ArgumentNullException(nameof(iv));

            if (encryptedData.Length == 0)
                throw new ArgumentException("Encrypted data cannot be empty.", nameof(encryptedData));
            if (key.Length == 0)
                throw new ArgumentException("Key cannot be empty.", nameof(key));
            if (iv.Length == 0)
                throw new ArgumentException("IV cannot be empty.", nameof(iv));

            try
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
                    using (var srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("AES decryption failed. Verify the key, IV, and encrypted data are correct.", ex);
            }
        }
    }
}