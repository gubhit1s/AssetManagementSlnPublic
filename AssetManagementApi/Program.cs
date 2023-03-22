using AssetManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Models.AdUserGeneration;
using AssetManagementApi.Logics.EmailHandler;
using AssetManagementApi.Logics.TokenValidation;
using AssetManagementApi.Logics.TransferHandler.Assignation;
using AssetManagementApi.Logics.TransferHandler.Withdrawal;
using AssetManagementApi.Logics.UserOrderHandler;
using AssetManagementApi.Logics.TransferHandler.Reassignation;
using AssetManagementApi.Logics.TransferHandler.Decommission;
using AssetManagementApi.Logics.TransferHandler.Repair;
using AssetManagementApi.Logics.TransferHandler.Unidentified;
using AssetManagementApi.Logics.TransferHandler.Bulk;
using AssetManagementApi.Helpers;
using AssetManagementApi.Models.Authentication;
using AssetManagementApi.Logics.JwtHandler;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using Hangfire;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,
        PrepareSchemaIfNecessary = true,
    })
    .UseFilter<AutomaticRetryAttribute>(new AutomaticRetryAttribute { Attempts = 1 }) //Set max retry to 1
);

builder.Services.AddHangfireServer();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<AssetContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opt => {
    opt.User.RequireUniqueEmail = true;
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    opt.Lockout.MaxFailedAccessAttempts = 10;
    opt.Lockout.AllowedForNewUsers = true;

}).AddEntityFrameworkStores<AssetContext>();

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        RequireExpirationTime = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecurityKey"]!))
    };

});

//Inject dependencies.
builder.Services.AddScoped<IAdUser, AdUser>();
builder.Services.AddScoped<IEmailHandler, EmailHandler>();
builder.Services.AddScoped<AssetManagementApi.Logics.TokenValidation.TokenHandler>();
builder.Services.AddScoped<IAssignationHandler, AssignationHandler>();
builder.Services.AddScoped<IUserOrderHandler, UserOrderHandler>();
builder.Services.AddScoped<IWithdrawalHandler, WithdrawalHandler>();
builder.Services.AddScoped<IReassignationHandler, ReassignationHandler>();
builder.Services.AddScoped<IDecommissionHandler, DecommissionHandler>();
builder.Services.AddScoped<IDecommissionPrepHandler, DecommissionPrepHandler>();
builder.Services.AddScoped<TokenHandlerUd>();
builder.Services.AddScoped<UnidentifiedTransferHandler>();
builder.Services.AddScoped<IBulkTokenHandler, BulkTokenHandler>();
builder.Services.AddScoped<RepairHandler>();
builder.Services.AddScoped<JwtHandler>();
builder.Services.AddScoped<IBulkHandler, BulkHandler>();
builder.Services.AddScoped<BulkTransferHelper>();

builder.Services.AddLogging(conf =>
{
    conf.AddSimpleConsole(c =>
    {
        c.TimestampFormat = "[yyyy/MM/dd HH:mm:ss] ";
    });
});

builder.Services.AddCors(options => options.AddPolicy(name: "AngularPolicy", cfg =>
{
    cfg.AllowAnyHeader();
    cfg.AllowAnyMethod();
    cfg.WithOrigins(builder.Configuration["AllowedCORS"]!);
}));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard();
}

app.UseHttpsRedirection();

app.UseHangfireDashboard();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.UseCors("AngularPolicy");

app.MapControllers();

app.Run();
