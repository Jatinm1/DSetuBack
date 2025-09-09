using DealerSetu_Services.IServices;
using DealerSetu_Services.Services;
using Microsoft.OpenApi.Models;
using DealerSetu_Data.Common;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Repositories.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using dealersetu_services.services;
using dealersetu_repositories.irepositories;
using DealerSetu_Data.Middleware;
using Azure.Storage.Blobs;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu.Repository.Common;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Load appsettings.json
var configuration = builder.Configuration;

// Configure database context
builder.Services.AddDbContext<ETSContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("dbDealerSetuEntities")));

// Register singleton services
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ILoginRepository, LoginRepository>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IHomeDashboardRepo, HomeDashboardRepo>();
builder.Services.AddScoped<IHomeDashboardService, HomeDashboardService>();
builder.Services.AddScoped<IMasterService, MasterService>();
builder.Services.AddScoped<IMasterRepository, MasterRepository>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IWhiteVillageService, WhiteVillageService>();
builder.Services.AddScoped<IWhiteVillageRepository, WhiteVillageRepository>();
builder.Services.AddScoped<IDemoRequestRepository, DemoRequestRepository>();
builder.Services.AddScoped<IDemoRequestService, DemoRequestService>();
builder.Services.AddScoped<INewDealerActivityRepository, NewDealerActivityRepository>();
builder.Services.AddScoped<INewDealerActivityService, NewDealerActivityService>();
builder.Services.AddScoped<ITempRepository, TempRepository>();
builder.Services.AddScoped<ITempService, TempService>();
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IPerformanceSheetRepo, PerformanceSheetRepo>();
builder.Services.AddScoped<IPerformanceSheetService, PerformanceSheetService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();
builder.Services.AddSingleton<IFileValidationService, FileValidationService>();
builder.Services.AddSingleton<FileLoggerService>();
builder.Services.AddSingleton<DealerSetu.Repository.Common.Utility>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<RSAEncryptionService>();
builder.Services.AddSingleton<RecaptchaService>();  // Register RecaptchaService Dependency
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddSingleton<ValidationHelper>();
//builder.Services.AddSingleton<IEmailService, EmailService>();
//// Configure email settings
//builder.Services.Configure<EmailSettings>(
//    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton(x => new BlobServiceClient(builder.Configuration.GetConnectionString("StorageAccount")));

#region JWT Token
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            // Important: Map JWT claims to .NET role claims
            RoleClaimType = "Role",
            NameClaimType = ClaimTypes.NameIdentifier
        };

        // Configure to read JWT token from HTTP-only cookie
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Try to get token from cookie first
                var token = context.Request.Cookies["jwt"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                // If not found in cookie, fall back to Authorization header
                else if (context.Request.Headers.ContainsKey("Authorization"))
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (authHeader.StartsWith("Bearer "))
                    {
                        context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // Debug: Log all claims to see what's available
                var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
                Console.WriteLine("Available claims:");
                claims?.ForEach(Console.WriteLine);

                // Ensure role claim is properly mapped
                var identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity != null)
                {
                    var roleClaim = identity.FindFirst("Role");
                    if (roleClaim != null && !identity.HasClaim(ClaimTypes.Role, roleClaim.Value))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                    }
                }

                return Task.CompletedTask;
            }
        };
    });
#endregion

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequestSectionAccess", policy =>
        policy.RequireRole("Dealer", "HO"));

    options.AddPolicy("NewDealerActivityAccess", policy =>
        policy.RequireRole("Dealer", "TM", "CCM", "CM", "SH", "HO", "AM"));

    options.AddPolicy("DemoTractorAccess", policy =>
        policy.RequireRole("Dealer", "TM", "CCM", "CM", "SH", "HO", "AM"));

    options.AddPolicy("MasterSectionAccess", policy =>
        policy.RequireRole("HO"));

    options.AddPolicy("PerformanceAccess", policy =>
        policy.RequireRole("Dealer", "TM", "CCM", "CM", "SH", "HO", "AM"));

    options.AddPolicy("WhiteVillageKhojAccess", policy =>
        policy.RequireRole("TM", "CCM", "CM", "SH", "HO", "AM"));

    options.AddPolicy("ReportSectionAccess", policy =>
        policy.RequireRole("HO"));

    options.AddPolicy("HPCategoryAccess", policy =>
    policy.RequireRole("Dealer", "TM", "CCM", "CM", "SH", "HO", "AM"));

    options.AddPolicy("DownloadAccess", policy =>
    policy.RequireRole("Dealer", "TM", "CCM", "CM", "SH", "HO", "AM"));
});

builder.Services.AddHttpContextAccessor();

// Add services to the container
builder.Services.AddSingleton<JwtTokenGenerator>();    // Register JWT Token Generator
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(configuration.GetSection("Session:IdleTimeoutInMinutes").Get<int>());
    options.Cookie.HttpOnly = configuration.GetSection("Session:CookieHttpOnly").Get<bool>();
    options.Cookie.IsEssential = configuration.GetSection("Session:CookieIsEssential").Get<bool>();
});

// Configure Swagger
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "DealerSetu", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

#region CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        // builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();  // local
        builder.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader().AllowCredentials();   // Staging
        builder.WithOrigins("http://dealersetu.stg103.netsmartz.us").AllowAnyMethod().AllowAnyHeader().AllowCredentials();   // Staging
        builder.WithOrigins("https://swdsetu.m-devsecops.com").AllowAnyMethod().AllowAnyHeader().AllowCredentials();   // Developement
    });
});
#endregion

var app = builder.Build();

// 1. Development vs Production Configuration
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Adds strict transport security
}

// 2. Middleware: UseSwagger for API documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyAPI v1");
    //c.SwaggerEndpoint("/delset-backend/swagger/v1/swagger.json", "DealerSetu v1");
});

// 3. HTTPS Redirection
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseMiddleware<SecurityMiddleware>();
//app.UseMiddleware<HeaderMiddleware>(); // Ensure headers are processed early

// 4. Static Files
app.UseStaticFiles();

// 5. Enable Routing
app.UseRouting();

// 6. Authentication and Authorization
app.UseAuthentication(); // Must come before UseAuthorization
app.UseMiddleware<JWTSecurityMiddleware>(); //HAVE TO ADD THIS AGAIN
app.UseMiddleware<ConnectionValidation>(); //HAVE TO ADD THIS AGAIN
app.UseAuthorization();

// 7. Session Middleware
app.UseSession();

// 8. Map Controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

// 9. Run the Application
app.Run();