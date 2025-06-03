using DealerSetu_Services.IServices;
using System;
using System.Security.Cryptography;

namespace DealerSetu_Services.Services
{
    /// <summary>
    /// AES encryption service implementing IEncryptionService interface.
    /// Provides secure AES encryption and decryption with proper error handling and resource management.
    /// </summary>
    public class AesEncryptionService : IEncryptionService
    {
        /// <summary>
        /// Encrypts the provided data using AES encryption with a randomly generated key and IV.
        /// </summary>
        /// <param name="data">The data to encrypt</param>
        /// <returns>A tuple containing the encrypted data, base64-encoded key, and base64-encoded IV</returns>
        /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
        /// <exception cref="ArgumentException">Thrown when data is empty</exception>
        /// <exception cref="CryptographicException">Thrown when encryption fails</exception>
        public (byte[] EncryptedData, string Key, string IV) EncryptData(byte[] data)
        {
            // Input validation
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Data cannot be null");

            if (data.Length == 0)
                throw new ArgumentException("Data cannot be empty", nameof(data));

            try
            {
                using var aes = Aes.Create();

                if (aes == null)
                    throw new CryptographicException("Failed to create AES instance");

                // Generate secure random key and IV
                aes.GenerateKey();
                aes.GenerateIV();

                // Validate key and IV generation
                if (aes.Key == null || aes.Key.Length == 0)
                    throw new CryptographicException("Failed to generate encryption key");

                if (aes.IV == null || aes.IV.Length == 0)
                    throw new CryptographicException("Failed to generate initialization vector");

                using var encryptor = aes.CreateEncryptor();

                if (encryptor == null)
                    throw new CryptographicException("Failed to create encryptor");

                var encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

                if (encryptedData == null || encryptedData.Length == 0)
                    throw new CryptographicException("Encryption resulted in empty data");

                return (
                    encryptedData,
                    Convert.ToBase64String(aes.Key),
                    Convert.ToBase64String(aes.IV)
                );
            }
            catch (CryptographicException)
            {
                // Re-throw cryptographic exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                // Wrap other exceptions in CryptographicException
                throw new CryptographicException("An error occurred during encryption", ex);
            }
        }

        /// <summary>
        /// Decrypts the provided encrypted data using AES decryption with the specified key and IV.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt</param>
        /// <param name="key">The base64-encoded encryption key</param>
        /// <param name="iv">The base64-encoded initialization vector</param>
        /// <returns>The decrypted data as a byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
        /// <exception cref="CryptographicException">Thrown when decryption fails</exception>
        /// <exception cref="FormatException">Thrown when key or IV are not valid base64 strings</exception>
        public byte[] DecryptData(byte[] encryptedData, string key, string iv)
        {
            // Input validation
            if (encryptedData == null)
                throw new ArgumentNullException(nameof(encryptedData), "Encrypted data cannot be null");

            if (encryptedData.Length == 0)
                throw new ArgumentException("Encrypted data cannot be empty", nameof(encryptedData));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty");

            if (string.IsNullOrWhiteSpace(iv))
                throw new ArgumentNullException(nameof(iv), "IV cannot be null or empty");

            byte[] keyBytes;
            byte[] ivBytes;

            try
            {
                // Validate and convert base64 strings
                keyBytes = Convert.FromBase64String(key);
                ivBytes = Convert.FromBase64String(iv);
            }
            catch (FormatException ex)
            {
                throw new FormatException("Invalid base64 format for key or IV", ex);
            }

            // Validate key and IV lengths
            if (keyBytes.Length == 0)
                throw new ArgumentException("Decoded key cannot be empty", nameof(key));

            if (ivBytes.Length == 0)
                throw new ArgumentException("Decoded IV cannot be empty", nameof(iv));

            try
            {
                using var aes = Aes.Create();

                if (aes == null)
                    throw new CryptographicException("Failed to create AES instance");

                // Set key and IV with validation
                try
                {
                    aes.Key = keyBytes;
                    aes.IV = ivBytes;
                }
                catch (CryptographicException ex)
                {
                    throw new CryptographicException("Invalid key or IV provided", ex);
                }

                using var decryptor = aes.CreateDecryptor();

                if (decryptor == null)
                    throw new CryptographicException("Failed to create decryptor");

                var decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

                if (decryptedData == null)
                    throw new CryptographicException("Decryption resulted in null data");

                return decryptedData;
            }
            catch (CryptographicException)
            {
                // Re-throw cryptographic exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                // Wrap other exceptions in CryptographicException
                throw new CryptographicException("An error occurred during decryption", ex);
            }
        }
    }
}