using DealerSetu_Data;
using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dealersetu_repositories.irepositories
{
    public interface IPolicyRepository
    {
        dynamic GetPolicyListRepo();
        public string SendFilesToServerRepo(PolicyUploadModel model, int RAId);

    }
}


//*************************************ADD THESE ABOVE FOR UPLOADING FILE*************************************

//dynamic GetWhiteListingRepo();



