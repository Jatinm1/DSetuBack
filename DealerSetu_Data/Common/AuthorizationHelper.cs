using Microsoft.AspNetCore.Authorization;

public class RequestSectionAuthorizeAttribute : AuthorizeAttribute
{
    public RequestSectionAuthorizeAttribute()
    {
        Policy = "RequestSectionAccess";
    }
}

public class HPCategoryAuthorizeAttribute : AuthorizeAttribute
{
    public HPCategoryAuthorizeAttribute()
    {
        Policy = "HPCategoryAccess";
    }
}

public class DownloadFilesAuthorizeAttribute : AuthorizeAttribute
{
    public DownloadFilesAuthorizeAttribute()
    {
        Policy = "DownloadAccess";
    }
}