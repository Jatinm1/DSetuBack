using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class LoginModel
    {
        public string EmpNo { get; set; }
        public string Password { get; set; }
    }

    public class UserModel
    {
        public int UserId { get; set; }
        public string EmpNo { get; set; }
        public string Token { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string State { get; set; }
        public int RoleId { get; set; }
        public string Role { get; set; }
        public string DealerLocation { get; set; }
        public int Status { get; set; }
        public string Message { get; set; }
        public int IsArohan { get; set; } = 0;
        public int IsAbhar { get; set; } = 0;
        public int HPCategory { get; set; }

    }
}
