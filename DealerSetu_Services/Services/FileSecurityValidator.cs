using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    public class FileSecurityValidator : IFileSecurityService
    {
        private static readonly string[] SuspiciousPatterns = {
        // Script injection patterns
        "javascript:", "vbscript:", "jscript:", "data:", "blob:",
        "<script", "<?php", "<%", "<asp", "<meta",
        
        // Command execution patterns
        "cmd.exe", "powershell", "bash", "sh -c",
        "=cmd|", "=pow|", "|iex", "iwr ", "wget ",
        ":/bin/", ":/tmp/", ":/var/", 
        
        // XML attack patterns
        "<!entity", "<!doctype", "<!element", "<!attlist",
        "xmlns:", "xsl:", "xslt:", 
        
        // Formula injection for spreadsheets
        "=dm", "=cmd", "=shell", "@sum",
        "=http", "=ftp", "=file:", 
        
        // Common malware patterns
        ".exe", ".dll", ".bat", ".ps1", ".vbs",
        ".hta", ".wsf", ".wsh", ".scr",
        
        // Shell code and hex patterns
        "0x", "\\x", "\\u", "\\00",
        "kali", "metasploit", "msfvenom",
        
        // Remote inclusion
        "http://", "https://", "ftp://", "file://",
        "\\\\", "gopher://", "dict://",
        
        // Encoding/obfuscation indicators
        "base64", "eval(", "fromcharcode",
        "substring(", "concat(", "chr(",
        
        // OS command injection
        ";", "&&", "||", "|", "`",
        "$(`", "$()", "@()", 
        
        // Buffer overflow attempts
        new string('A', 256),
        new string('\\', 256),
        new string('%', 256)
    };

        // Additional checks for Excel files
        private static readonly string[] ExcelMaliciousPatterns = {
        // DDE attack patterns
        "=dde", "=system", "=exec",
        
        // Macro indicators
        "auto_open", "auto_close", "workbook_open",
        "document_open", "auto_exec", 
        
        // Remote template injection
        "=link", "=dynamic", "=remote",
        
        // Formula injection
        "=cmd|", "=pow|", "=mshta|",
        "=certification", "=register"
    };
        private readonly long _maxFileSizeBytes;
        private readonly long _minFileSizeBytes;
        private readonly Dictionary<string, List<byte[]>> _fileSignatureMap;
        private readonly HashSet<string> _allowedExtensions;
        private readonly HashSet<string> _allowedMimeTypes;

        public FileSecurityValidator(long maxFileSizeBytes = 20 * 1024 * 1024, long minFileSizeBytes = 1 * 1024)
        {
            _maxFileSizeBytes = maxFileSizeBytes;
            _minFileSizeBytes = minFileSizeBytes;

            _fileSignatureMap = new Dictionary<string, List<byte[]>>
        {
            { "application/pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
            { "image/jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }, new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 } } },
            { "image/png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
            { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { "application/vnd.openxmlformats-officedocument.presentationml.presentation", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { "application/vnd.ms-excel", new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } },
            { "application/zip", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { "application/x-zip-compressed", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }
        };

            _allowedExtensions = new HashSet<string> {
            ".xlsx", ".xls"
        };

            _allowedMimeTypes = new HashSet<string>(_fileSignatureMap.Keys);
        }

        public async Task<ServiceResponse> ValidateContent(string content)
        {
            var response = new ServiceResponse
            {
                isError = false,
                Status = "Processing",
                Code = "VALIDATION_CHECK"
            };

            try
            {
                if (string.IsNullOrEmpty(content))
                {
                    return new ServiceResponse
                    {
                        isError = true,
                        Error = "Content is empty or null",
                        Status = "Failed",
                        Code = "VALIDATION_EMPTY",
                        Message = "The provided content cannot be empty or null"
                    };
                }

                // Normalize input for consistent checking
                var normalizedContent = NormalizeContent(content);

                // Check for suspicious patterns
                foreach (var pattern in SuspiciousPatterns)
                {
                    if (normalizedContent.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        return new ServiceResponse
                        {
                            isError = true,
                            Error = $"Suspicious content detected: {pattern}",
                            Status = "Failed",
                            Code = "VALIDATION_SUSPICIOUS_CONTENT",
                            Message = "The file contains potentially malicious content"
                        };
                    }
                }

                // Check for potential SQL injection
                if (ContainsSqlInjection(normalizedContent))
                {
                    return new ServiceResponse
                    {
                        isError = true,
                        Error = "SQL injection attempt detected",
                        Status = "Failed",
                        Code = "VALIDATION_SQL_INJECTION",
                        Message = "The content contains potential SQL injection patterns"
                    };
                }

                // Check for potential XSS
                if (ContainsXss(normalizedContent))
                {
                    return new ServiceResponse
                    {
                        isError = true,
                        Error = "XSS attempt detected",
                        Status = "Failed",
                        Code = "VALIDATION_XSS",
                        Message = "The content contains potential cross-site scripting patterns"
                    };
                }

                // If all validations pass
                return new ServiceResponse
                {
                    isError = false,
                    Status = "Success",
                    Code = "VALIDATION_SUCCESS",
                    Message = "Content validation completed successfully",
                    result = new { isValid = true }
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Status = "Error",
                    Code = "VALIDATION_EXCEPTION",
                    Message = "An unexpected error occurred during validation",
                    result = new { stackTrace = ex.StackTrace }
                };
            }
        }

        public async Task<ServiceResponse> ValidateFile(IFormFile file)
        {
            try
            {
                // Basic validation
                if (file == null || file.Length == 0)
                {
                    return new ServiceResponse
                    {
                        isError = true,
                        Error = "No file was uploaded",
                        Status = "Failed",
                        Code = "FILE_EMPTY",
                        Message = "Please provide a valid file"
                    };
                }

                // Size validation
                if (file.Length > _maxFileSizeBytes)
                {
                    return new ServiceResponse
                    {
                        isError = true,
                        Error = $"File size exceeds maximum limit of {_maxFileSizeBytes / 1024 / 1024}MB",
                        Status = "Failed",
                        Code = "FILE_SIZE_EXCEEDED",
                        Message = "The uploaded file is too large"
                    };
                }

                if (file.Length < _minFileSizeBytes)
                {
                    return new ServiceResponse
                    {
                        isError = true,
                        Error = "File size is suspiciously small",
                        Status = "Failed",
                        Code = "FILE_SIZE_TOO_SMALL",
                        Message = "The uploaded file is too small to be valid"
                    };
                }

                // Extension validation
                string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                {
                    return new ServiceResponse
                    {
                        isError = true,
                        Error = $"Invalid file type. Allowed extensions are: {string.Join(", ", _allowedExtensions)}",
                        Status = "Failed",
                        Code = "INVALID_FILE_TYPE",
                        Message = "The file type is not supported"
                    };
                }

                // Content validation
                using (var stream = file.OpenReadStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        string content = await reader.ReadToEndAsync();
                        var contentValidation = await ValidateContent(content);
                        if (contentValidation.isError == true)
                        {
                            return contentValidation;
                        }
                    }
                }

                // If all validations pass
                return new ServiceResponse
                {
                    isError = false,
                    Status = "Success",
                    Code = "FILE_VALIDATION_SUCCESS",
                    Message = "File validation completed successfully",
                    result = new
                    {
                        fileName = file.FileName,
                        fileSize = file.Length,
                        contentType = file.ContentType
                    }
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Status = "Error",
                    Code = "FILE_VALIDATION_EXCEPTION",
                    Message = "An unexpected error occurred during file validation",
                    result = new { stackTrace = ex.StackTrace }
                };
            }
        }

        // Helper methods remain the same...
        public string NormalizeContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;

            // Convert to lowercase
            content = content.ToLowerInvariant();

            // Remove white space
            content = Regex.Replace(content, @"\s+", "");

            // Decode common HTML entities
            content = WebUtility.HtmlDecode(content);

            // Decode URL encoding
            content = Uri.UnescapeDataString(content);

            // Remove comment markers
            content = Regex.Replace(content, @"(/\*.*?\*/|//[^\n]*)", "", RegexOptions.Singleline);

            return content;
        }

        public bool ContainsSqlInjection(string content)
        {
            var sqlPatterns = new[]
            {
            "union select", "union all", "union distinct",
            "order by", "group by", "having",
            "--", ";--", ";", "/*", "*/", "@@",
            "char(", "convert(", "cast(",
            "declare", "exec(", "execute(",
            "sp_", "xp_", "msdb.."
        };

            return sqlPatterns.Any(pattern =>
                content.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        public bool ContainsXss(string content)
        {
            var xssPatterns = new[]
            {
            "onload=", "onerror=", "onmouseover=",
            "onfocus=", "onblur=", "onkeyup=",
            "onkeydown=", "onkeypress=", "onmouseout=",
            "onmousedown=", "onmousemove=", "onsubmit=",
            "onunload=", "onchange=", "ondblclick=",
            "alert(", "confirm(", "prompt(",
            "eval(", "function(", "return(",
            "settimeout(", "setinterval(", "new function(",
            "<img", "<iframe", "<object",
            "<embed", "<video", "<audio",
            "<svg", "<math", "<form"
        };

            return xssPatterns.Any(pattern =>
                content.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
    }
}
