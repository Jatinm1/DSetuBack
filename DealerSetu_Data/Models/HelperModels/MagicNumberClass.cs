using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.HelperModels
{
    public static class MagicNumberClass
    {
        public static string MagicNumber(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    var fileBytes = ms.ToArray();

                    // Read first 8 bytes max for more reliable signature checking
                    string hexHeader = BitConverter.ToString(fileBytes.Take(8).ToArray());

                    string output = hexHeader switch
                    {
                        string s when s.StartsWith("38-42-50-53") => "psd",
                        string s when s.StartsWith("25-50-44-46") => "pdf",
                        string s when s.StartsWith("49-49-2A-00") => "tif",
                        string s when s.StartsWith("4D-4D-00-2A") => "tiff",
                        string s when s.StartsWith("FF-D8-FF-E0") => "jpg",
                        string s when s.StartsWith("89-50-4E-47") => "png",
                        string s when s.StartsWith("50-4B-03-04") => DetermineOpenXmlType(fileBytes), // Could be DOCX/XLSX/PPTX
                        string s when s.StartsWith("D0-CF-11-E0") => "xls", // OLE compound file
                        string s when s.StartsWith("EF-BB-BF-4F") => "csv",
                        string s when s.StartsWith("53-65-72-69") => "csv",
                        _ => string.Empty
                    };

                    return output;
                }
            }
            return "";
        }

        // Optional helper for detecting DOCX/XLSX/PPTX
        private static string DetermineOpenXmlType(byte[] fileBytes)
        {
            using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(fileBytes), System.IO.Compression.ZipArchiveMode.Read);
            if (zip.Entries.Any(e => e.FullName.StartsWith("word/")))
                return "docx";
            if (zip.Entries.Any(e => e.FullName.StartsWith("xl/")))
                return "xlsx";
            if (zip.Entries.Any(e => e.FullName.StartsWith("ppt/")))
                return "pptx";
            return "zip"; // generic fallback
        }



    }
}
