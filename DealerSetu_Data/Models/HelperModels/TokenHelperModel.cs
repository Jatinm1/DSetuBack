using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.HelperModels
{
    public class TokenHelperModel
    {
        public string EmpNo { get; set; }
        public string UserName { get; set; }

        public int UserId { get; set; }
        //public string Password { get; set; }
        //public byte LoggedIn { get; set; } = 0;
        public string Token { get; set; }

        //public int UserId { get; set; }

        //public string Name { get; set; }
        //public string Email { get; set; }
        //public string State { get; set; }
        public int RoleId { get; set; }
        public string Role { get; set; }
        //public string DealerLocation { get; set; }

        //public int IsArohan { get; set; } = 0;
        //public int IsAbhar { get; set; } = 0;
        //public int HPCategory { get; set; }

    }

    public class UserViewModel
    {
        public string EmpNo { get; set; }
        public string UserName { get; set; }

        public string Role { get; set; }
        public string Token { get; set; }
        public int RoleId { get; set; }



    }
}
