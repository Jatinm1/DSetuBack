using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.HelperModels
{
    public class UserDetails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        public string EmpNoOrDealerNo { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int? RoleId { get; set; }
        public bool? IsDelete { get; set; }
        public string Password { get; set; }
        public string DealerLocation { get; set; }
        public string DealerDistrict { get; set; }
        public string DealerZone { get; set; }
        public string DealerState { get; set; }
        public int? FailedAttempt { get; set; } = 0;
        public DateTime? LoginTime { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool? LockoutEnabled { get; set; } = false;
        public byte? LoggedIn { get; set; } = 0;
        public string? Token { get; set; }
        public bool? Validate { get; set; } = false;
        public string? IpAddress { get; set; }
        public string? BrowserName { get; set; }
        public string? BrowserVersion { get; set; }
        public DateTime? HeartBeat { get; set; }

        // public int RoleId { get; set; }
    }
}
