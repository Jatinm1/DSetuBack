using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class DemoUpdateModel
    {
        public int RequestId { get; set; } // Required for updates
                                           // Optional basic properties
        public string? Model { get; set; }
        public string? ChassisNo { get; set; }
        public string? EngineNo { get; set; }
        public string? DateOfBilling { get; set; }

        // Optional file properties - Basic documents
        public IFormFile? InvoiceFile { get; set; }
        public IFormFile? RCFile { get; set; }
        public IFormFile? InsuranceFile { get; set; }

        // Optional file properties - Additional documents
        public IFormFile? FileSale { get; set; } //Sale Document of Tractor
        public IFormFile? FileTractor { get; set; } //Format for Claiming
        public IFormFile? FilePicture { get; set; } //Picture of Hour Reading
        public IFormFile? FilePicTractor { get; set; } //Picture of Tractor
        public IFormFile? LogDemonsFile { get; set; }
        public IFormFile? Affidavitfile { get; set; }
        public IFormFile? SaleDeedfile { get; set; }
        public bool BasicFlag { get; set; }

    }
}
